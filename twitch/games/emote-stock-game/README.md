# Twitch Emote Stock Game — A Chat Game for Streamer.bot

This is a long-running chat game you can leave running while your stream is live. Viewers buy and sell imaginary "shares" of your channel emotes (or global Twitch emotes). Every few minutes a timer fires, stock prices swing up or down based on each emote's volatility, and players try to buy low and sell high to earn your stream's points/currency.

---

## How the Game Works (The Short Version)

1. A timer runs every **10 minutes** (configurable) and adjusts every stock price up or down by a random amount, up to that stock's `volatility`.
2. Viewers type commands like `!buy Kappa 5` or `!sell Kappa 5` to trade shares.
3. Players can type `!stocks` to see current prices and `!holdings` to see their own portfolio.
4. If a stock price drops below zero, it resets to its `startPrice`. If it spikes above `maxPrice`, the market "corrects" it back toward the middle.
5. The game runs continuously in the background; there is no start/end round.

---

## What You Need to Build

You'll create these inside Streamer.bot:

### Actions (5 total)

| Action Name | What It Does |
|-------------|--------------|
| `SetResetStockPrices` | Triggered by a repeating timer. Creates the stock list on first run, then adjusts prices every tick afterward. |
| `CheckStockPrices` | Replies to `!stocks` with the current price of every emote stock. |
| `CheckHoldings` | Replies to `!holdings` with the player's owned shares and remaining points. |
| `BuyStock` | Triggered by the `!buy` regex command. Deducts points and adds shares. |
| `SellStock` | Triggered by the `!sell` regex command. Sells shares and refunds points. |

### Commands (4 total)

| Command | Who Can Use It |
|---------|----------------|
| `!stocks` | Anyone |
| `!holdings` | Anyone |
| `!buy <emote> <quantity>` | Anyone |
| `!sell <emote> <quantity>` | Anyone |

### Timers (1 total)

| Timer | Purpose |
|-------|---------|
| `EmoteStockPriceUpdate` (or any name you choose) | Fires every 10 minutes to update stock prices and print them to chat. |

---

## Step-by-Step Setup

### General C# Sub-Action Pattern

For every `.cs` file listed below, the process is the same:

1. Go to the **Actions** tab and click **+**.
2. Give the action the exact name shown (the names are not magic, but these instructions assume you use them).
3. Add the correct trigger(s) as described in each step.
4. Click **+** under Sub-Actions → `Core → C# → Execute C# Code`.
5. Copy **all** the text from the matching `.cs` file and paste it into the code window, replacing the placeholder code.
6. Click **Compile**. If you see **"Compiled successfully"**, click **Save and Compile**.

---

### Step 1: Create the Timer That Drives the Market

1. In Streamer.bot, go to **Services → Timers** (left-side menu).
2. Click **+** to add a new timer.
3. Configure it like this:

| Setting | Value |
|---------|-------|
| **Name** | `EmoteStockPriceUpdate` (or any name you like) |
| **Interval** | `600` seconds (10 minutes) |
| **Enabled** | ✅ Checked |
| **Repeat** | ✅ Checked |

4. Click **OK**.

---

### Step 2: Create the Action `SetResetStockPrices`

This is the heart of the market engine. It initializes prices the very first time it runs, then randomly moves prices on every future timer tick.

1. **Actions** tab → **+** → name it `SetResetStockPrices`.
2. **Trigger** → right-click → **Add → Core → Timed Actions** → select your timer from the dropdown.
3. Add a sub-action: `Core → C# → Execute C# Code` → paste `SetResetStockPrices.cs`.
4. Add a **second** sub-action: `Core → Actions → Run Action` → choose `CheckStockPrices` (you will create that action in Step 3). This prints the new prices to chat after every market update.
5. Save the action.

> **First run:** Right-click the timer trigger and select **Test Trigger**. You should see a `!stocks`-style message appear in chat with all the default emote prices.

> **Subsequent ticks:** Every 10 minutes the timer will fire, prices will move, and the updated price list will be posted to chat automatically.

---

### Step 3: Create the Action `CheckStockPrices`

1. **Actions** tab → **+** → name it `CheckStockPrices`.
2. **Trigger** → right-click → **Add → Command** → select the `!stocks` command (create it in Step 4 if you haven't yet, or come back and set this after Step 4).
3. Add a sub-action: `Core → C# → Execute C# Code` → paste `CheckStockPrices.cs`.
4. Save.

---

### Step 4: Create the Command `!stocks`

1. Go to the **Commands** tab → **+**.
2. Configure it:

| Setting | Value |
|---------|-------|
| **Command** | `!stocks` |
| **Mode** | `Starts With` |
| **Group** | *(optional)* create a group called `emote-stock-game` |
| **Permission** | `Everyone` |
| **Enabled** | ✅ Checked |

3. In the action area, assign the `CheckStockPrices` action.
4. Click **OK**.

---

### Step 5: Create the Action `CheckHoldings`

1. **Actions** tab → **+** → name it `CheckHoldings`.
2. **Trigger** → right-click → **Add → Command** → select the `!holdings` command (create it in Step 6, or come back).
3. Add a sub-action: `Core → C# → Execute C# Code` → paste `CheckHoldings.cs`.
4. Save.

---

### Step 6: Create the Command `!holdings`

1. **Commands** tab → **+**.
2. Configure it:

| Setting | Value |
|---------|-------|
| **Command** | `!holdings` |
| **Mode** | `Starts With` |
| **Group** | `emote-stock-game` *(if you created one)* |
| **Permission** | `Everyone` |
| **Enabled** | ✅ Checked |

3. Assign the `CheckHoldings` action.
4. Click **OK**.

---

### Step 7: Create the `!buy` Regex Command

This command catches messages like `!buy Kappa 5`, `!buy  Kappa   5`, etc.

1. **Commands** tab → **+**.
2. Configure it:

| Setting | Value |
|---------|-------|
| **Command** | *(anything, ignored in Regex mode)* |
| **Mode** | `Regex` |
| **Regex Pattern** | `!buy([\s]*)([^ ]*)([\s]*)([\d]*)` |
| **Group** | `emote-stock-game` *(if you created one)* |
| **Permission** | `Everyone` |
| **Enabled** | ✅ Checked |

3. Assign the `BuyStock` action.
4. Click **OK**.

> **Regex group mapping:** The `BuyStock.cs` code reads `match[2]` as the emote name and `match[4]` as the quantity.

---

### Step 8: Create the Action `BuyStock`

1. **Actions** tab → **+** → name it `BuyStock`.
2. **Trigger** → right-click → **Add → Command** → select the `!buy` regex command from Step 7.
3. Add a sub-action: `Core → C# → Execute C# Code` → paste `BuyStock.cs`.
4. Save.

---

### Step 9: Create the `!sell` Regex Command

Same pattern as `!buy`, but with `!sell` at the start.

1. **Commands** tab → **+**.
2. Configure it:

| Setting | Value |
|---------|-------|
| **Command** | *(anything, ignored in Regex mode)* |
| **Mode** | `Regex` |
| **Regex Pattern** | `!sell([\s]*)([^ ]*)([\s]*)([\d]*)` |
| **Group** | `emote-stock-game` *(if you created one)* |
| **Permission** | `Everyone` |
| **Enabled** | ✅ Checked |

3. Assign the `SellStock` action.
4. Click **OK**.

---

### Step 10: Create the Action `SellStock`

1. **Actions** tab → **+** → name it `SellStock`.
2. **Trigger** → right-click → **Add → Command** → select the `!sell` regex command from Step 9.
3. Add a sub-action: `Core → C# → Execute C# Code` → paste `SellStock.cs`.
4. Save.

---

## How the Whole Thing Flows

```
Streamer starts the stream
    │
    ▼
Timer fires every 10 minutes
    │
    ▼
SetResetStockPrices runs
    ├── first run: creates EmoteStockGame_prices global var
    └── later runs: coin flip + volatility adjusts each stock price
    │
    ▼
Run Action → CheckStockPrices posts updated prices to chat
    │
    ▼
Viewers type:
    !stocks      → see all current prices
    !holdings    → see their shares + remaining points
    !buy Kappa 5 → buy 5 shares of Kappa if they have enough points
    !sell Kappa 5 → sell 5 shares of Kappa back for points
```

---

## What the Game Stores

| Variable Name | Where | What It Holds |
|--------------|-------|---------------|
| `EmoteStockGame_prices` | Global (persisted) | Dictionary of every stock and its `volatility`, `startPrice`, `currentPrice`, `maxPrice`. |
| `points` | Per Twitch user (persisted) | How many points/currency the viewer has to spend. |
| `stocks` | Per Twitch user (persisted) | Dictionary of `emoteName → quantity` for each viewer. |

> **Note:** This game expects your stream's currency to be stored in a Twitch user variable called `points`. If your channel uses a different name, you will need to edit the C# files (or add a global variable mapping).

---

## Changing the Emotes

By default, the game uses 3 global Twitch emotes (`DoritosChip`, `Kappa`, `OhMyDog`) plus 7 channel emotes from the author's channel. You should replace the channel emotes with your own.

Inside `SetResetStockPrices.cs`, each stock is defined like this:

```csharp
["iandouDeadpoolHeart"] = new Dictionary<string, int>{
    ["volatility"] = 50,
    ["startPrice"] = 1000,
    ["currentPrice"] = 1000,
    ["maxPrice"] = 10000
},
```

| Field | Meaning |
|-------|---------|
| **Key** (`["..."]`) | The typed name of the emote. This is what players type after `!buy`/`!sell`. |
| **volatility** | The maximum random price change (up or down) each timer tick. |
| **startPrice** | The price the first time the stock is created. Also used as a floor reset if the price drops below 0. |
| **currentPrice** | The live price. This value is overwritten every timer tick. |
| **maxPrice** | Soft ceiling. If volatility pushes the price above this, it corrects back to `(maxPrice + startPrice) / 2`. |

To customize:

1. Open `SetResetStockPrices.cs`.
2. Replace the strings inside the square brackets with your own emote names, e.g. `["MyCoolEmote"]`.
3. Tweak `volatility`, `startPrice`, and `maxPrice` to match how risky/stable you want each stock to be.
4. Recompile the action in Streamer.bot.
5. **Important:** If you already ran the timer once, the `EmoteStockGame_prices` global variable already exists. Either delete that global variable in Streamer.bot (`Variables → Global`) and re-test the timer, or edit the existing values in the variable inspector.

---

## A Note About Followers and Subscribers

If a viewer is **not a follower** of your channel, Twitch will not render your channel emotes in their chat input. They can still type `!buy YourEmote 10` and the game will process it correctly — they will own the shares and can sell them later — but the emote text will appear as plain text in chat instead of as the actual emote image.

Subscriber-only emotes work the same way: non-subscribers can still buy/sell them, but the emote won't render visually for them.

A future roadmap item is to restrict buying subscriber-only emotes to subscribers at the appropriate tier level.

---

## Testing It Out

Before going live, run through this checklist:

- [ ] Test the timer trigger manually and confirm `!stocks` prices appear in chat.
- [ ] From a second account, type `!buy Kappa 1` and verify points are deducted.
- [ ] Type `!holdings` and confirm the share appears with its current value.
- [ ] Type `!sell Kappa 1` and verify points are refunded.
- [ ] Wait for the next timer tick (or retest the trigger) and confirm prices moved.
- [ ] Check that `!stocks` still works after the price update.

---

## Common Issues

| Problem | Likely Fix |
|---------|------------|
| `!stocks` does nothing | Make sure the `CheckStockPrices` action is assigned to the `!stocks` command and compiled successfully. |
| `!buy` or `!sell` does nothing | Make sure the command is in **Regex** mode with the exact pattern shown above, and that `BuyStock`/`SellStock` are assigned to the right commands. |
| "Sorry I don't have a matching stock to buy" | You typed an emote that isn't in the `SetResetStockPrices.cs` list. Add it there and reinitialize the global var. |
| Player has points but `!buy` says they don't | The game reads from a Twitch user variable named `points`. If your currency uses a different variable name, the C# code needs to be updated. |
| Timer fires but no prices are shown | Confirm `SetResetStockPrices` has a second sub-action `Run Action → CheckStockPrices`. |
| Prices don't seem to change between ticks | The `EmoteStockGame_prices` global variable may have been edited manually or cached. Delete it and re-test the timer trigger. |
| The game worked once but stopped after editing emotes | Old emote data is persisted in the global variable. Delete `EmoteStockGame_prices` so the new defaults are recreated. |

---

## Future Ideas

- Restrict subscriber-only emotes to actual subscribers.
- Restrict channel emotes to followers.
- Add a "market crash" or "boom" event that doubles volatility for one tick.
- Add a leaderboard for richest players.
- Let viewers gift shares to each other.
- Add per-stock dividends paid to current shareholders each tick.

---

## Quick Reference: All Files

| File | Action Name | Trigger |
|------|-------------|---------|
| `SetResetStockPrices.cs` | `SetResetStockPrices` | Repeating timer |
| `CheckStockPrices.cs` | `CheckStockPrices` | `!stocks` command |
| `CheckHoldings.cs` | `CheckHoldings` | `!holdings` command |
| `BuyStock.cs` | `BuyStock` | `!buy` regex command |
| `SellStock.cs` | `SellStock` | `!sell` regex command |
