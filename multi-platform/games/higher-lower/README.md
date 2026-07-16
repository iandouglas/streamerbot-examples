# Higher or Lower — A Multi-Platform Chat Game for Streamer.bot

(documentation was AI-generated based on my plan.md notes, and C# code was started by AI but hand-edited and optimized by myself)

## What is "Higher-Lower"

A chat-powered guessing game where viewers `!join` and then work together to guess a secret number. In each round, their guesses are averaged, and the chat collectively tries to hit the target number. If they get it right within the round limit, everyone who participated wins a share of the prize pool.

The game supports **Twitch, YouTube, and Kick** chat simultaneously. Players are tracked per platform account by user ID, so `ian` on Twitch and `ian` on YouTube are treated as separate players. Points are awarded in the correct platform's user variables.

---

## Game Modes

The game now supports three difficulty modes, chosen at launch time:

| Mode | Command | Rounds | Range | Prize Pool | Exact Bonus | Range Narrowing |
|------|---------|--------|-------|------------|-------------|-----------------|
| **Easy** | `!game hl easy` | 10 (configurable) | 1-100 (configurable) | 1,000 | 100 | Yes — range shrinks after each hint |
| **Normal** | `!game hl normal` | 10 (configurable) | 1-100 (configurable) | 10,000 | 1,000 | No |
| **Extreme** | `!game hl extreme` | Scaled by player count (max 15) | Scaled by player count | 100,000 | 10,000 | No |

If you leave the mode blank (`!game hl` or `!game higher-lower`), it defaults to **normal**.

### Easy Mode

In easy mode, the visible range narrows after each hint. If the average guess was 75 and the answer is lower, the next round shows `1-74`. If the next average is 26 and the answer is higher, the range becomes `27-74`. Guesses outside the narrowed range are rejected in chat with a message to try again, and they do not count toward participation.

### Normal Mode

Normal mode always shows the full `1-TopNumber` range. No narrowing. Fixed rounds and fixed range per the configuration globals.

### Extreme Mode

Extreme mode scales both the number of rounds and the number range based on how many players join:

- Rounds: `min(15, 8 + ceil(players / 2))`
- Range: `max(2048, players^2 * 256)`

So 5 players get 10 rounds and a range of 1-6400, while 8 players get 12 rounds and 1-16384. The full `1-TopNumber` range is always shown (no narrowing). All point values are rounded to integers.

### Prize Pool Decay

All modes use **linear decay** so that the prize pool reaches exactly 5% of its starting value by the final round. The per-round decay is calculated automatically as:

```
decayStep = (startingPool - finalPool) / (rounds - 1)
```

where `finalPool = startingPool * 0.05`. This means the pool never drops below 5% of its original value, so players who exhaust all rounds still earn something for trying.

### Exact Bonus Rule

The exact-number bonus is only awarded to players who guess the correct number **before the final round**. If someone guesses the exact number in the final round, their guess still counts toward the round average, but they do not receive the bonus. This prevents the late-game scenario where everyone converges on the answer and all collect the bonus.

---

## How the Game Works (The Short Version)

1. A mod or the streamer types `!game hl <mode>` (e.g. `!game hl easy`, `!game hl normal`, `!game hl extreme`)
2. Chat has **60 seconds** to `!join` the game
3. Everyone who joined then gets **30 seconds per round** to type a number in chat
4. After each round, all guesses are averaged — if the average matches the secret number, each player wins a share of the prize pool based on how many rounds they participated in (if you played all rounds, you get 100%; if you missed a round, your share shrinks)
5. If someone guesses the exact number before the final round, they get an extra bonus
6. Per-round settings like guess timer, guess mode ("first guess only" or "last guess wins"), and inter-round delay are all configurable
7. If nobody guesses it within the chosen number of rounds, the game ends with no winners
8. At the end, a top-10 tally is printed showing each player's awarded points in descending order, followed by a total summary

---

## What You Need to Build

You'll create these things inside Streamer.bot:

### Actions (4 total)

| Action Name | What It Does |
|-------------|--------------|
| `HL_GameSetup` | Prepares all game settings — run this once when you go live |
| `HL_GameEngine` | Runs the entire game from start to finish (OBS is handled directly inside this action — no separate OBS actions needed) |
| `HL_PlayerJoin` | Handles people typing `!join` |
| `HL_GuessNumber` | Watches for number guesses during a round |
| `HL_AwardPoints` | Gives points to winners (called automatically) |

### Commands (3 total)

| Command | Who Can Use It |
|---------|----------------|
| `!game` | **Moderators and Streamer only** |
| `!join` | Anyone |
| *number guess* (e.g. typing `42`) | Anyone who joined the game |

---

## Step-by-Step Setup

### Step 1: Create the Command `!game`

1. In Streamer.bot, go to the **Commands** tab
2. Click the **+** button to add a new command
3. Set it up like this:

| Setting | Value |
|---------|-------|
| **Command** | `!game` |
| **Mode** | Starts With |
| **Group** | Click `...` → Add New → name it `higher-lower` |
| **Permission** | `Moderators` + `Broadcaster` (click both) |
| **Enabled** | ✅ Checked |
| **Platforms** | ✅ Check any platforms you want the command to work on (Twitch, YouTube, Kick) |

4. Click OK to save

### Step 2: Create the Setup Action `HL_GameSetup`

This action stores your preferred game settings. No C# code needed — just sub-actions with descriptions. You'll run it once when your stream starts (manually clicking "Run" in Streamer.bot, or you can trigger it from a Stream Start event).

1. **Actions** tab → **+** → name it `HL_GameSetup`
2. **Trigger** — add a 'custom' trigger you can run manually. Right click in the triggers area, go to "Add", then "Custom", and then "Custom", leave the fields blank and click the "Ok" button.
3. For each setting below, add a **Set Global Variable** sub-action (`Core → Variables → Set Global Variable`). Set the **Name** and **Value** from the table. In the **Comment** field, type the description so you know what it does later. Each of these will be "temporary", not persisted values.

#### General Settings

| Sub-action | Name | Value | Comment |
|------------|------|-------|---------|
| 1 | `hl_currency_name` | `points` | What to call player points |
| 2 | `hl_join_timer` | `60` | Seconds viewers have to type !join |
| 3 | `hl_guess_timer` | `30` | Seconds per round to submit a guess |
| 4 | `hl_guess_mode` | `first` | "first" = only first guess counts, "last" = most recent guess wins |
| 5 | `hl_round_delay_ms` | `5000` | Milliseconds between rounds (default 5000 = 5 seconds) |
| 6 | `hl_use_obs` | `True` | Set to False to disable OBS notifications entirely |
| 7 | `hl_obs_scene` | *(your scene)* | OBS scene name containing your text source |
| 8 | `hl_obs_source` | *(your source)* | OBS text source name to show game messages |

#### Easy Mode Settings

| Sub-action | Name | Value | Comment |
|------------|------|-------|---------|
| 9 | `hl_easy_rounds` | `10` | Number of rounds in easy mode |
| 10 | `hl_easy_range_top` | `100` | Max number to guess in easy mode |
| 11 | `hl_easy_starting_pool` | `1000` | Prize pool for easy mode |
| 12 | `hl_easy_exact_bonus` | `100` | Exact-number bonus for easy mode |

#### Normal Mode Settings

| Sub-action | Name | Value | Comment |
|------------|------|-------|---------|
| 13 | `hl_normal_rounds` | `10` | Number of rounds in normal mode |
| 14 | `hl_normal_range_top` | `100` | Max number to guess in normal mode |
| 15 | `hl_normal_starting_pool` | `10000` | Prize pool for normal mode |
| 16 | `hl_normal_exact_bonus` | `1000` | Exact-number bonus for normal mode |

#### Extreme Mode Settings

| Sub-action | Name | Value | Comment |
|------------|------|-------|---------|
| 17 | `hl_extreme_starting_pool` | `100000` | Prize pool for extreme mode |
| 18 | `hl_extreme_exact_bonus` | `10000` | Exact-number bonus for extreme mode |

> **Note:** Extreme mode rounds and range are calculated automatically from the player count. You only configure the pool and bonus.

4. To activate your settings, right-click on the 'Custom' trigger and pick 'Test Trigger'. You can change any value and re-run it anytime. You can also trigger this by starting your stream if you add a platform-specific **Stream Online** trigger.

> **What do these settings do?**
> - **hl_currency_name** — what your points are called (`points`, `gems`, `coins`, etc.) -- use the same name here as the [points system](../../points-system)
> - **hl_round_delay_ms** — milliseconds between rounds. Increase this (e.g. `8000` or `10000`) if players are typing numbers too quickly after seeing the hint and accidentally guessing during the delay
> - **hl_guess_mode** — `first` (only the first number you type counts) or `last` (your latest number replaces any previous guess until the guess timer runs out)
> - **hl_use_obs** — set to `True` to show game notifications on stream via OBS, `False` to skip OBS entirely
> - **hl_obs_scene** — the name of your OBS scene that contains the notification text source
> - **hl_obs_source** — the name of your OBS text source (GDI+ or FreeType2) that will display the game messages
> - Mode-specific settings (`hl_easy_*`, `hl_normal_*`, `hl_extreme_*`) control the rounds, range, pool, and bonus for each difficulty mode

> **OBS tip:** If `hl_use_obs` is `False`, or if either `hl_obs_scene` or `hl_obs_source` are left blank, the game will skip all OBS calls and just run in chat.
>
> **OBS cleanup:** When the game ends, `HL_GameEngine` hides the configured OBS source before clearing any game variables, so your source should disappear from stream automatically.

### Step 3: Create the Action `HL_GameEngine`

1. Go to the **Actions** tab
2. Click the **+** button to add a new action
3. Name it `HL_GameEngine`
4. In the **Trigger** dropdown on the right, choose `Command` and select your `!game` command
5. Click the **+** under Sub-Actions and choose `Core → C# → Execute C# Code`
6. A code window will open — open the file `HL_GameEngine.cs` from this folder, copy ALL the text, and paste it in, replacing the placeholder code
7. Click **Compile** — if you see "Compiled successfully", you're good
8. Click "Save and Compile"

### Step 4: Create the Command `!join`

1. Go back to **Commands**, click **+**
2. Set it up:

| Setting | Value |
|---------|-------|
| **Command** | `!join` |
| **Mode** | Starts With |
| **Group** | Select your existing `higher-lower` group |
| **Permission** | `Everyone` |
| **Enabled** | ✅ Checked |
| **Platforms** | ✅ Check any platforms you want to allow joins from |

> **Note:** The `HL_PlayerJoin` action is **disabled by default** and auto-enabled when a game starts, auto-disabled when the join period ends. This prevents `!join` from doing anything when no Higher or Lower game is running. If you also play Battleship, both join actions can share the same `!join` command.

### Step 5: Create the Action `HL_PlayerJoin`

1. **Actions** tab → **+** → name it `HL_PlayerJoin`
2. **Trigger** → choose `Command` → select your `!join` command
3. **Sub-Actions** → **+** → `Core → C# → Execute C# Code`
4. Paste the contents of `HL_PlayerJoin.cs` into the code window
5. Click **Compile**, then OK

### Step 6: Create the Number-Guess Command (a bit different)

This one catches any message that is just a number — like `7`, `42`, `100` etc.

1. **Commands** tab → **+** → set it up:

| Setting | Value |
|---------|-------|
| **Command** | `higher-lower number guess` |
| **Mode** | `Regex` |
| **Regex Pattern** | `^(\d+)$` |
| **Group** | Select your `higher-lower` group |
| **Permission** | `Everyone` |
| **Enabled** | ✅ Checked |
| **Platforms** | ✅ Check any platforms you want to allow guesses from |

> **Important:** The command name **must** be exactly `higher-lower number guess`. The game engine looks up this command by name and automatically enables it when a game starts and disables it when the game ends. This prevents the regex from consuming number messages when no game is running. If you name it something else, the enable/disable won't work and you'll need to manage it manually.

> **What's that regex thing?** It's just Streamer.bot's way of saying "trigger this command when someone types a message that is only numbers, nothing else." Users will just type `42` in chat and it'll work.

### Step 7: Create the Action `HL_GuessNumber`

1. **Actions** tab → **+** → name it `HL_GuessNumber`
2. **Trigger** → choose `Command` → select the Regex command you just made
3. **Sub-Actions** → **+** → `Core → C# → Execute C# Code`
4. Paste the contents of `HL_GuessNumber.cs`
5. **Compile** → OK

### Step 8: Create the Action `HL_AwardPoints`

1. **Actions** tab → **+** → name it `HL_AwardPoints`
2. **Trigger** — leave this one with **no trigger** (it gets called automatically by the game engine)
3. **Sub-Actions** → **+** → `Core → C# → Execute C# Code`
4. Paste the contents of `HL_AwardPoints.cs`
5. **Compile** → OK

> `HL_AwardPoints` awards points on the correct platform for the user. The game engine passes `hlAwardUser`, `hlAwardPoints`, and `hlAwardPlatform`.

### Step 9: Set Up the OBS Text Source (optional, skip if you don't use OBS)

No separate OBS actions are needed — the game engine controls OBS directly. You just need one text source in OBS.

1. In **OBS Studio**, create a new **Text (GDI+)** or **Text (FreeType 2)** source in whatever scene you want
2. Give it a name like `HL_Notification` (or whatever you put in `HL_OBSSource`)
3. Style it however you want — font, size, color, position, background
4. Put some placeholder text like "Game starting soon..." so you can see it works
5. Make sure OBS is connected in Streamer.bot (`Servers/Clients → OBS` — click Connect)

That's it. The game engine will:
- Set the text dynamically — e.g. `Round 2/10 — Guess 27-74! Prize: 950`, `Higher or Lower! Type !join to enter! 30s remaining!`, `CORRECT! The number was 42!`
- Show the source during join, countdown, guess, and result phases
- Hide the source when nothing is happening

> **No OBS?** Set `hl_use_obs` to `False` and leave `hl_obs_scene` / `hl_obs_source` blank. The game runs entirely in chat with no OBS calls.

---

## How the Whole Thing Flows

When the streamer types `!game hl normal`:

```
┌─ Before streaming: run HL_GameSetup to load your settings
│
├─ HL_GameEngine kicks off when streamer types `!game hl <mode>`
│
├─ 1. Creates a group called "higher-lower-group" to track players from all connected platforms
├─ 2. Sets OBS text to "Type !join!" and shows the source (if OBS is configured)
├─ 3. Announces join window (duration from hl_join_timer)
│     └─ Every 10 seconds: announces remaining time + player count
│
├─ 4. After join window closes, calculates mode config (extreme scales by player count)
├─ 5. Computes linear decay so prize pool reaches 5% of starting value by the final round
├─ 6. Picks a random number between 1 and the mode's range top
├─ 7. Sets OBS text to "Game on! Starting in 15s..." and shows it; waits 15s then hides
│
├─ 8. For each round (1 through mode rounds):
│     ├─ In easy mode: shows the narrowed range (e.g. "27-74")
│     ├─ Sets OBS text to "Round X — Guess min-max! Prize: Y" and shows the source
│     ├─ Announces: "Round X — type a number! hl_guess_timer seconds!"
│     ├─ Waits hl_guess_timer seconds
│     │     └─ HL_GuessNumber catches numbers
│     │           └─ In easy mode: rejects out-of-range guesses with a chat message
│     │           └─ In easy mode: does not count rejected guesses toward participation
│     ├─ Hides the OBS source
│     ├─ Averages all the guesses
│     ├─ If average = target number:
│     │     ├─ Each player earns prize_pool × (rounds_they_played / total_rounds)
│     │     └─ Game over!
│     └─ If not:
│           ├─ Reduces prize pool by decay_step (linear, floor at 5% of starting pool)
│           ├─ In easy mode: narrows the range (e.g. avg 75, target lower → new range 1-74)
│           ├─ Tells chat "guess higher" or "guess lower" (with new range in easy mode)
│           └─ Waits hl_round_delay_ms before next round
│
└─ 9. Game ends
      ├─ Awards participation points to all players who guessed in at least one round
      ├─ Awards exact bonus to anyone who guessed the exact number before the final round
      ├─ Prints top-10 tally: "Points awarded: UserA 1050 pts, UserB 950 pts, ..."
      │    └─ Splits into multiple messages if a line exceeds 400 bytes
      ├─ Prints total: "In total, we gave out X points to everyone who played"
      ├─ Prints exact bonus recap if anyone earned the bonus
      └─ Cleans up all game state
```

---

## What the Game Stores (For the Curious)

The game tracks everything in variables inside Streamer.bot. You don't need to touch these, but it's helpful to know they exist:

| Variable Name | What It Holds |
|--------------|---------------|
| `hl_game_active` | Whether a game is currently running |
| `hl_mode` | Current game mode: `easy`, `normal`, or `extreme` |
| `hl_phase` | What's happening right now: `join`, `guess`, `judging`, `idle` |
| `hl_target_number` | The secret number |
| `hl_winnable_points` | Current prize pool (decays linearly, floor at 5% of starting pool) |
| `hl_round` | Which round number we're on |
| `hl_players` | List of canonical player keys (`platform:userId`) |
| `hl_player_names` | Map of `platform:userId` → display name for chat messages |
| `hl_exact_guessers` | List of player keys who guessed the exact number before the final round |
| `hl_participation` | How many rounds each player participated in (keyed by `platform:userId`) |
| `hl_current_min` | Current low bound for easy-mode range narrowing |
| `hl_current_max` | Current high bound for easy-mode range narrowing |
| `hl_decay_step` | Linear per-round decay amount |
| `hl_final_pool` | Floor value for the prize pool (5% of starting) |
| `hl_mode_rounds` | Number of rounds for this game |
| `hl_mode_range_top` | Number range top for this game |
| `hl_mode_starting_pool` | Starting prize pool for this game |
| `hl_mode_exact_bonus` | Exact-number bonus for this game |
| `hl_mode_round_delay_ms` | Delay between rounds in milliseconds |
| `hl_mode_narrow_range` | Whether easy-mode range narrowing is active |
| `hl_currency_name` | Your chosen points variable name |
| `hl_join_timer` | Join phase duration in seconds |
| `hl_guess_timer` | Guess phase duration in seconds |
| `hl_guess_mode` | `first` or `last` — how multiple guesses in one round are handled |
| `hl_use_obs` | Whether OBS notifications are enabled |
| `hl_obs_scene` | OBS scene name for the notification source |
| `hl_obs_source` | OBS text source name for dynamic messages |

---

## Testing It Out

Before going live, run through this checklist:

- [ ] Type `!game hl normal` from a modded account or the broadcaster
- [ ] From a different account, type `!join` — you should get a confirmation message
- [ ] Wait for the join timer to finish, then when it says "type a number", type `50`
- [ ] Check that the game announces the average and a hint ("guess higher" / "guess lower") after each round
- [ ] After the game ends, verify the top-10 tally and total summary appear in chat
- [ ] Verify points were awarded (check in Streamer.bot under the appropriate platform's user variables)
- [ ] Try `!game hl easy` and confirm the range narrows after each hint
- [ ] Try `!game hl easy` and type a number outside the narrowed range — confirm it's rejected
- [ ] Try `!game hl extreme` with several players and confirm the range and rounds scale up

### Common Issues

| Problem | Likely Fix |
|---------|------------|
| Nothing happens when typing `!game` | Check the command's Permission is set to allow your account |
| `!join` says "no game running" | The join window may have expired — start a new game |
| Typing a number does nothing | Make sure the Regex command is set up with `^(\d+)$` exactly |
| Easy mode doesn't reject out-of-range guesses | Make sure `hl_mode_narrow_range` is being set (it's automatic in easy mode) |
| Guess mode `last` isn't working | Make sure `hl_guess_mode` is set to `last` (lowercase) |
| OBS notification doesn't show or update | Check that `hl_use_obs` is `True`, `hl_obs_scene` and `hl_obs_source` match exactly (case-sensitive), and OBS is connected |
| The game doesn't end | Type `!game hl normal` to start a fresh game and reset things |
| Players not showing in participation | Players are now tracked by `platform:userId`, not username. Make sure `userId` is available in the command arguments |

---

## Future Ideas

- **Guess timer decay** — Reduce the guess timer each round (e.g. start at 60s, decay by 5s/round, with a configurable floor). Not yet implemented but considered for a future update.
- **Mode-specific guess timers** — Allow easy/normal/extreme to have different guess timer durations.