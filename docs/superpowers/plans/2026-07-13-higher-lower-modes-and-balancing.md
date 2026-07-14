# Higher-Lower Modes and Balancing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `easy`, `normal`, and `extreme` modes to Higher or Lower, fix participation tracking, and tighten end-of-game reporting so players always see accurate payout summaries.

**Architecture:** Keep Streamer.bot C# as the source of truth for identity, rounds, range, decay, and payouts. Use the browser only for OBS text and any optional visual feedback, and store all round state in globals keyed by a canonical per-user ID so participation cannot drift from bonus tracking.

**Tech Stack:** Streamer.bot inline C#, Streamer.bot globals, chat replies, OBS text source updates, `Platform` enums, JSON-backed globals, `System.Text.Encoding` for message splitting.

---

### Task 1: Canonicalize Player Identity and Round State

**Files:**
- Modify: `multi-platform/games/higher-lower/HL_PlayerJoin.cs`
- Modify: `multi-platform/games/higher-lower/HL_GuessNumber.cs`
- Modify: `multi-platform/games/higher-lower/HL_GameEngine.cs:1-525`

- [ ] **Step 1: Rewrite the shared key shape to use `platform:userId` instead of `platform:userName`**

```cs
private string MakeUserKey(string platform, string userId)
{
    return $"{platform.ToLowerInvariant()}:{userId}";
}

private string MakeUserKey(Platform platform, string userId)
{
    return $"{platform.ToString().ToLowerInvariant()}:{userId}";
}

private string GetUserDisplayName(string userKey)
{
    var playerNames = CPH.GetGlobalVar<Dictionary<string, string>>("hl_player_names", true) ?? new Dictionary<string, string>();
    return playerNames.TryGetValue(userKey, out string name) && !string.IsNullOrWhiteSpace(name)
        ? name
        : userKey;
}
```

- [ ] **Step 2: Store display names separately from participation keys**

```cs
var playerNames = CPH.GetGlobalVar<Dictionary<string, string>>("hl_player_names", true) ?? new Dictionary<string, string>();
playerNames[userKey] = userName;
CPH.SetGlobalVar("hl_player_names", playerNames, true);
```

- [ ] **Step 3: Update `HL_PlayerJoin.cs` to store canonical keys and names together**

```cs
if (!CPH.TryGetArg("userId", out string userId) || string.IsNullOrWhiteSpace(userId))
    return false;

string userKey = $"{platform}:{userId}";
var players = CPH.GetGlobalVar<List<string>>("hl_players", true) ?? new List<string>();
if (!players.Contains(userKey))
{
    players.Add(userKey);
    CPH.SetGlobalVar("hl_players", players, true);
}
```

- [ ] **Step 4: Update `HL_GuessNumber.cs` so round guessers and exact guessers use the same canonical key**

```cs
if (!CPH.TryGetArg("userId", out string userId) || string.IsNullOrWhiteSpace(userId))
    return false;

string userKey = MakeUserKey(platform, userId);
```

- [ ] **Step 5: Run a manual two-user trace in Streamer.bot**
  - Start one game.
  - Have two different accounts `!join`.
  - Have one account guess an exact number.
  - Confirm `hl_players`, `hl_round_1_guessers`, `hl_participation`, and `hl_exact_guessers` all contain the same canonical key format.
  - Expected: a user who guessed exactly must also appear in participation state if they guessed during any round.

### Task 2: Add Mode Parsing and Mode Configuration

**Files:**
- Modify: `multi-platform/games/higher-lower/HL_GameEngine.cs:67-240`

- [ ] **Step 1: Parse `!game hl <mode>` and default blank mode to `normal`**

```cs
string mode = parts.Length > 1 ? parts[1].Trim().ToLowerInvariant() : "normal";
if (mode != "easy" && mode != "normal" && mode != "extreme")
    mode = "normal";
```

- [ ] **Step 2: Centralize mode defaults in one helper**

```cs
private void LoadModeConfig(string mode, int playerCount, out int rounds, out int rangeTop, out int startingPool, out int exactBonus, out int roundDelayMs, out bool narrowRange)
{
    roundDelayMs = CPH.GetGlobalVar<int>("hl_round_delay_ms", false);
    if (roundDelayMs < 1000) roundDelayMs = 5000;

    switch (mode)
    {
        case "easy":
            rounds = CPH.GetGlobalVar<int>("hl_easy_rounds", false);
            if (rounds < 1) rounds = 10;
            rangeTop = CPH.GetGlobalVar<int>("hl_easy_range_top", false);
            if (rangeTop < 10) rangeTop = 100;
            startingPool = CPH.GetGlobalVar<int>("hl_easy_starting_pool", false);
            if (startingPool < 100) startingPool = 1000;
            exactBonus = CPH.GetGlobalVar<int>("hl_easy_exact_bonus", false);
            if (exactBonus < 10) exactBonus = 100;
            narrowRange = true;
            break;
        case "extreme":
            rounds = Math.Min(15, 8 + ((playerCount + 1) / 2));
            rangeTop = Math.Max(2048, playerCount * playerCount * 256);
            startingPool = 100000;
            exactBonus = 10000;
            narrowRange = false;
            break;
        default:
            rounds = CPH.GetGlobalVar<int>("hl_normal_rounds", false);
            if (rounds < 1) rounds = 10;
            rangeTop = CPH.GetGlobalVar<int>("hl_normal_range_top", false);
            if (rangeTop < 10) rangeTop = 100;
            startingPool = CPH.GetGlobalVar<int>("hl_normal_starting_pool", false);
            if (startingPool < 100) startingPool = 10000;
            exactBonus = CPH.GetGlobalVar<int>("hl_normal_exact_bonus", false);
            if (exactBonus < 10) exactBonus = 1000;
            narrowRange = false;
            break;
    }
}
```

- [ ] **Step 3: Persist the chosen mode and mode-specific values for the rest of the run**

```cs
CPH.SetGlobalVar("hl_mode", mode, false);
CPH.SetGlobalVar("hl_mode_rounds", rounds, false);
CPH.SetGlobalVar("hl_mode_range_top", rangeTop, false);
CPH.SetGlobalVar("hl_mode_starting_pool", startingPool, false);
CPH.SetGlobalVar("hl_mode_exact_bonus", exactBonus, false);
CPH.SetGlobalVar("hl_mode_round_delay_ms", roundDelayMs, false);
CPH.SetGlobalVar("hl_mode_narrow_range", narrowRange, false);
```

- [ ] **Step 4: Derive the linear decay so the final round lands at 5% of the starting pool**

```cs
int finalPool = (int)Math.Round(startingPool * 0.05m);
int decayStep = rounds > 1
    ? (int)Math.Round((startingPool - finalPool) / (decimal)(rounds - 1))
    : 0;
CPH.SetGlobalVar("hl_decay_step", decayStep, false);
CPH.SetGlobalVar("hl_final_pool", finalPool, false);
```

- [ ] **Step 5: Manual check**
  - Run `!game hl` with no mode and confirm it behaves as `normal`.
  - Run `!game hl easy`, `!game hl normal`, and `!game hl extreme`.
  - Confirm each mode sets the expected pool, bonus, range, and round count globals.

### Task 3: Add Easy-Mode Range Narrowing and Out-of-Range Rejection

**Files:**
- Modify: `multi-platform/games/higher-lower/HL_GameEngine.cs:180-390`
- Modify: `multi-platform/games/higher-lower/HL_GuessNumber.cs:46-120`

- [ ] **Step 1: Track the active min/max range for easy mode**

```cs
CPH.SetGlobalVar("hl_current_min", 1, false);
CPH.SetGlobalVar("hl_current_max", rangeTop, false);
```

- [ ] **Step 2: Reject out-of-range guesses in easy mode without counting them**

```cs
int currentMin = CPH.GetGlobalVar<int>("hl_current_min", false);
int currentMax = CPH.GetGlobalVar<int>("hl_current_max", false);
bool narrowRange = CPH.GetGlobalVar<bool>("hl_mode_narrow_range", false);

if (narrowRange && (guess < currentMin || guess > currentMax))
{
    SendChatMessage($"Sorry {userName}, that number is outside the current range {currentMin}-{currentMax}. Try again.");
    return false;
}
```

- [ ] **Step 3: Update the visible range after each hint in easy mode**

```cs
if (avg < target)
    currentMin = Math.Max(currentMin, avg + 1);
else
    currentMax = Math.Min(currentMax, avg - 1);

CPH.SetGlobalVar("hl_current_min", currentMin, false);
CPH.SetGlobalVar("hl_current_max", currentMax, false);
```

- [ ] **Step 4: Show the narrowed range in chat and OBS**

```cs
if (mode == "easy")
    id736.Chat.SendMessage($"Average guess was {avg}. Guess HIGHER! New range: {currentMin}-{currentMax}.");
else
    id736.Chat.SendMessage($"Average guess was {avg}. Guess HIGHER!");
```

- [ ] **Step 5: Verify the narrowing rules manually**
  - Start an easy game.
  - Enter a guess outside the current range.
  - Confirm the guess is rejected and not counted.
  - Confirm `75` then `1-74`, then `26` then `27-74` works as expected.

### Task 4: Make Round Delay Configurable and Tighten Exact-Bonus Eligibility

**Files:**
- Modify: `multi-platform/games/higher-lower/HL_GameEngine.cs:180-390`
- Modify: `multi-platform/games/higher-lower/HL_GuessNumber.cs:109-118`

- [ ] **Step 1: Replace the hard-coded 5-second inter-round wait with `hl_round_delay_ms`**

```cs
int roundDelayMs = CPH.GetGlobalVar<int>("hl_mode_round_delay_ms", false);
if (roundDelayMs < 1000) roundDelayMs = 5000;
CPH.Wait(roundDelayMs);
```

- [ ] **Step 2: Only record exact guesses before the final round**

```cs
int round = CPH.GetGlobalVar<int>("hl_round", false);
int maxRounds = CPH.GetGlobalVar<int>("hl_mode_rounds", false);
bool allowExactBonus = round < maxRounds;

if (allowExactBonus && guess == target)
{
    var exactGuessers = CPH.GetGlobalVar<List<string>>("hl_exact_guessers", true) ?? new List<string>();
    if (!exactGuessers.Contains(userKey))
    {
        exactGuessers.Add(userKey);
        CPH.SetGlobalVar("hl_exact_guessers", exactGuessers, true);
    }
}
```

- [ ] **Step 3: Keep the bonus award itself unchanged, but skip late-game registrations**
  - The exact bonus still pays out at the end.
  - Only the registration gate changes so a final-round exact guess does not count for the bonus pool.

- [ ] **Step 4: Verify the final-round rule manually**
  - Force a game to the final round.
  - Guess the exact number.
  - Confirm the guess is counted for the round but not added to `hl_exact_guessers`.

### Task 5: Final Tally Output and Top-10 Summary

**Files:**
- Modify: `multi-platform/games/higher-lower/HL_GameEngine.cs:240-360`

- [ ] **Step 1: Sort awards descending by points and keep only the top 10 for the recap**

```cs
var topAwards = awards
    .OrderByDescending(kvp => kvp.Value)
    .Take(10)
    .ToList();
```

- [ ] **Step 2: Build compact summary lines that stay under 400 bytes**

```cs
var line = new StringBuilder("Points awarded: ");
foreach (var award in topAwards)
{
    string entry = $"{GetUserDisplayName(award.Key)} {award.Value} pts";
    string candidate = line.Length == 16 ? entry : ", " + entry;
    if (Encoding.UTF8.GetByteCount(line.ToString() + candidate) > 400)
    {
        id736.Chat.SendMessage(line.ToString());
        line = new StringBuilder("Points awarded: ");
    }
    if (line.Length > 16)
        line.Append(", ");
    line.Append(entry);
}
```

- [ ] **Step 3: Emit the final total line after the recap**

```cs
id736.Chat.SendMessage($"In total, we gave out {grandTotal} points to everyone who played");
```

- [ ] **Step 4: Verify the summary format with a wide player list**
  - Use more than 10 players.
  - Confirm only the top 10 show up in the recap.
  - Confirm multiple messages are sent if a line would exceed 400 bytes.

### Task 6: Update the Higher-Lower README for the New Modes

**Files:**
- Modify: `multi-platform/games/higher-lower/README.md`

- [ ] **Step 1: Document the new `!game hl <mode>` launch flow**
- [ ] **Step 2: Document the easy/normal/extreme behavior and their default pools/bonuses**
- [ ] **Step 3: Document the easy-mode rejected-guess behavior and the final-bonus cutoff**
- [ ] **Step 4: Update the example flow diagram so it matches the new mode selection and recap behavior**

- [ ] **Step 5: Read the README once after editing to check for contradictions with the code**

---

### Self-Check

1. Every round-state write should use the same canonical player key.
2. Easy mode must reject out-of-range guesses without incrementing participation.
3. Exact bonuses must stop registering on the final round.
4. `normal` must remain fixed-round, fixed-range, and default when mode is blank.
5. `extreme` must cap at 15 rounds and derive both rounds and range from player count.
6. Final recap messages must be sorted, capped at 10 players, and split by byte length.
