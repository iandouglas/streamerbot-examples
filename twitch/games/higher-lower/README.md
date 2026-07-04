# Higher or Lower — A Chat Game for Streamer.bot

(documentation was AI-generated based on my plan.md notes, and C# code was started by AI but hand-edited and optimized by myself)

## What is "Higher-Lower"

A chat-powered guessing game where viewers `!join` and then work together to guess a secret number between 1 and 100. In each round, their guesses are averaged, and the chat collectively tries to hit the target number. If they get it right within 10 rounds, everyone who participated wins a share of the prize pool.

---

## How the Game Works (The Short Version)

1. A mod or the streamer types `!game higher-lower 10000` (the number at the end is the starting prize pool in points)
2. Chat has **60 seconds** to `!join` the game
3. Everyone who joined then gets **30 seconds per round** to type a number in chat
4. After each round, all guesses are averaged — if the average matches the secret number, each player wins a share of the prize pool based on how many rounds they participated in (if you played all rounds, you get 100%; if you missed a round, your share shrinks)
5. If someone guesses the exact number during a round, they get an extra bonus (default 1000 points)
6. Per-round settings like guess timer, guess mode ("first guess only" or "last guess wins"), and prize decay per wrong round are all configurable
6. If nobody guesses it within the chosen number of rounds, the game ends with no winners

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

4. Click OK to save

### Step 2: Create the Setup Action `HL_GameSetup`

This action stores your preferred game settings. No C# code needed — just sub-actions with descriptions. You'll run it once when your stream starts (manually clicking "Run" in Streamer.bot, or you can trigger it from a Stream Start event).

1. **Actions** tab → **+** → name it `HL_GameSetup`
2. **Trigger** — add a 'custom' trigger you can run manually. Right click in the triggers area, go to "Add", then "Custom", and then "Custom", leave the fields blank and click the "Ok" button.
3. For each setting below, add a **Set Global Variable** sub-action (`Core → Variables → Set Global Variable`). Set the **Name** and **Value** from the table. In the **Comment** field, type the description so you know what it does later. Each of these will be "temporary", not persisted values.

| Sub-action | Name | Value | Comment (what to type) |
|------------|------|-------|------------------------|
| 1 | `hl_currency_name` | `points` | What to call player points (e.g. "points", "gems", "coins") |
| 2 | `hl_game_rounds` | `10` | How many rounds before the game ends |
| 3 | `hl_default_points` | `10000` | Default prize pool when no number is given in !game |
| 4 | `hl_range_top` | `100` | Max number to guess (1 to this value) |
| 5 | `hl_point_decay` | `100` | Points lost from the prize pool each wrong round |
| 6 | `hl_exact_bonus` | `1000` | Bonus points for guessing the exact number |
| 7 | `hl_join_timer` | `60` | Seconds viewers have to type !join |
| 8 | `hl_guess_timer` | `30` | Seconds per round to submit a guess |
| 9 | `hl_guess_mode` | `first` | "first" = only first guess counts, "last" = most recent guess wins |
| 10 | `hl_use_obs` | `True` | Set to False to disable OBS notifications entirely |
| 11 | `hl_obs_scene` | *(your scene)* | OBS scene name containing your text source |
| 12 | `hl_obs_source` | *(your source)* | OBS text source name to show game messages |

4. To activate your settings, right-click on the 'Custom' trigger and pick 'Test Trigger'. You can change any value and re-run it anytime. You can also trigger this by starting your stream if you do "Add -> Twitch -> Channel -> Stream Online" so these will be loaded and ready as soon as your stream goes live.

> **What do these settings do?**
> - **hl_currency_name** — what your points are called (`points`, `gems`, `coins`, etc.) — this is the Twitch user variable name
> - **hl_default_points** — the prize pool when someone types `!game higher-lower` without specifying a number of points, or you can override this by running `!game higher-lower 100000` if you want to give away a much larger pool of points
> - **hl_point_decay** — how many points the prize pool loses each wrong round, set this to be a little less than the winnable points divided by the number of rounds. So if you're playing for 1000 points and have 10 rounds, you may want to set the decay to 95 points so every player still wins some amount of points at the end.
> - **hl_game_rounds** — how many rounds before the game ends if no one guesses correctly
> - **hl_range_top** — the secret number will be between 1 and this value (e.g. `50` = 1–50)
> - **hl_exact_bonus** — bonus points awarded to any player at the end of the game if they guessed the exact number at any point in the game
> - **hl_join_timer** — how many seconds viewers have to `!join` the game
> - **hl_guess_timer** — how many seconds per round to submit a guess
> - **hl_guess_mode** — `first` (only the first number you type counts) or `last` (your latest number replaces any previous guess until the guess timer runs out)
> - **hl_use_obs** — set to `True` to show game notifications on stream via OBS, `False` to skip OBS entirely
> - **hl_obs_scene** — the name of your OBS scene that contains the notification text source
> - **hl_obs_source** — the name of your OBS text source (GDI+ or FreeType2) that will display the game messages

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
| **Command** | (what you put here doesn't matter since we're using Regex mode) |
| **Mode** | `Regex` |
| **Regex Pattern** | `^(\d+)$` |
| **Group** | Select your `higher-lower` group |
| **Permission** | `Everyone` |
| **Enabled** | ✅ Checked |

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

### Step 9: Set Up the OBS Text Source (optional, skip if you don't use OBS)

No separate OBS actions are needed — the game engine controls OBS directly. You just need one text source in OBS.

1. In **OBS Studio**, create a new **Text (GDI+)** or **Text (FreeType 2)** source in whatever scene you want
2. Give it a name like `HL_Notification` (or whatever you put in `HL_OBSSource`)
3. Style it however you want — font, size, color, position, background
4. Put some placeholder text like "Game starting soon..." so you can see it works
5. Make sure OBS is connected in Streamer.bot (`Servers/Clients → OBS` — click Connect)

That's it. The game engine will:
- Set the text dynamically — e.g. `Round 2/10 — Guess 1-100! Prize: 9800`, `Higher or Lower! Type !join to enter! 30s remaining!`, `CORRECT! The number was 42! +10000 pts to players!`
- Show the source during join, countdown, guess, and result phases
- Hide the source when nothing is happening

> **No OBS?** Set `HL_UseOBSNotifications` to `False` and leave `HL_OBSScene` / `HL_OBSSource` blank. The game runs entirely in chat with no OBS calls.

---

## How the Whole Thing Flows

When the streamer types `!game higher-lower 10000`:

```
┌─ Before streaming: run HL_GameSetup to load your settings
│
├─ HL_GameEngine kicks off when streamer types `!game higher-lower`
│
├─ 1. Creates a group called "higher-lower-group" to track players
├─ 2. Sets OBS text to "Type !join!" and shows the source (if OBS is configured)
├─ 3. Announces join window (duration from HL_JoinTimer setting)
│     └─ Every 10 seconds: announces remaining time + player count
│
├─ 4. Hides the OBS source
├─ 5. Sets OBS text to "Game on! Starting in 15s..." and shows it; waits 15s then hides
├─ 6. Picks a random number between 1 and HL_RangeTop
│
├─ 7. For each round (1 through HL_GameRounds):
│     ├─ Sets OBS text to "Round X — Prize: Y — Guess!" and shows the source
│     ├─ Announces: "Round X — type a number! HL_GuessTimer seconds!"
│     ├─ Waits HL_GuessTimer seconds
│     │     └─ HL_GuessNumber catches numbers (HL_GuessMode: first or last)
│     ├─ Hides the OBS source
│     ├─ Averages all the guesses
│     ├─ If average = target number:
│     │     ├─ Each player earns prize_pool × (rounds_they_played / total_rounds)
│     │     │   (100% if you played every round, less if you missed any)
│     │     └─ Game over!
│     └─ If not:
│           ├─ Reduces prize pool by HL_PointDecay points
│           ├─ Tells chat "guess higher" or "guess lower"
│           └─ Moves to next round
│
└─ 8. Game ends — awards HL_ExactBonus to anyone who guessed the exact number
     └─ Shows result on OBS for 8 seconds, then hides and cleans up
```

---

## What the Game Stores (For the Curious)

The game tracks everything in variables inside Streamer.bot. You don't need to touch these, but it's helpful to know they exist:

| Variable Name | What It Holds |
|--------------|---------------|
| `hl_game_active` | Whether a game is currently running |
| `hl_phase` | What's happening right now: `join`, `guess`, or something else |
| `hl_target_number` | The secret number (1–HL_RangeTop) |
| `hl_starting_points` | The prize pool value set when the game started |
| `hl_winnable_points` | Current prize pool (starts from `!game` argument, reduced by HL_PointDecay each wrong round) |
| `hl_round` | Which round number we're on |
| `hl_exact_guessers` | List of people who guessed the exact number |
| `hl_participation` | How many rounds each player participated in |
| `*currencyName*` *(per user)* | Each player's total points — stored in Twitch user variables using whatever name you set in HL_CurrencyName (default `points`) |
| `hl_currency_name` | Your chosen points variable name (set by HL_GameSetup) |
| `hl_game_rounds` | Number of rounds per game |
| `hl_default_points` | Default starting prize pool |
| `hl_range_top` | Upper bound of the secret number range |
| `hl_point_decay` | Points deducted from prize pool per wrong round |
| `hl_exact_bonus` | Bonus for guessing the exact number |
| `hl_join_timer` | Join phase duration in seconds |
| `hl_guess_timer` | Guess phase duration in seconds |
| `hl_guess_mode` | `first` or `last` — how multiple guesses in one round are handled |
| `hl_use_obs` | Whether OBS notifications are enabled |
| `hl_obs_scene` | OBS scene name for the notification source |
| `hl_obs_source` | OBS text source name for dynamic messages |

---

## Testing It Out

Before going live, run through this checklist:

- [ ] Type `!game higher-lower 5000` from a modded account or the broadcaster
- [ ] From a different account, type `!join` — you should get a confirmation message
- [ ] Wait for the join timer to finish, then when it says "type a number", type `50`
- [ ] Check that the game announces the average and a hint ("guess higher" / "guess lower") after each round
- [ ] After the game ends, verify points were awarded (you can check in Streamer.bot under Twitch → User Variables)

### Common Issues

| Problem | Likely Fix |
|---------|------------|
| Nothing happens when typing `!game` | Check the command's Permission is set to allow your account |
| `!join` says "no game running" | The join window may have expired — start a new game |
| Typing a number does nothing | Make sure the Regex command is set up with `^(\d+)$` exactly |
| Numbers above HL_RangeTop get through | The HL_GuessNumber code rejects guesses > HL_RangeTop, but the Regex command will still match them — that's normal, the C# code handles the validation |
| Guess mode `last` isn't working | Make sure you ran `HL_GameSetup` after changing the setting, and that `HL_GuessMode` is set to `last` (lowercase) |
| OBS notification doesn't show or update | Check that `HL_UseOBSNotifications` is `True`, `HL_OBSScene` and `HL_OBSSource` match exactly (case-sensitive), and OBS is connected in Streamer.bot (`Servers/Clients → OBS`) |
| The game doesn't end | Type `!game higher-lower 100` to start a fresh game and reset things |

---

## Future Ideas

- **Guess timer decay** — Reduce the guess timer each round (e.g. start at 60s, decay by 5s/round, with a configurable floor). Not yet implemented but considered for a future update.
