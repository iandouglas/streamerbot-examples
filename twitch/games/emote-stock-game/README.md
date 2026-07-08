# Emote Stock Game — A Chat Game for Streamer.bot

A long-running chat game where viewers buy and sell imaginary "shares" of emotes. Every few minutes a timer fires, stock prices swing up or down based on each emote's volatility, and players try to buy low and sell high to earn stream points.

This version uses the shared helper DLL `iandouglas736.dll` and works across **Twitch, YouTube, and Kick**. Each platform has its own separate points and portfolio storage.

---

## Requirements

Install the shared helper DLL following the main [`README.md`](../../../README.md) or [`dlls-needed/README.md`](../../../dlls-needed/README.md).

---

## How the Game Works

1. A timer runs every **10 minutes** (configurable) and adjusts every stock price up or down by a random amount, up to that stock's `volatility`.
2. Viewers type commands like `!buy Kappa 5` or `!sell Kappa 5` to trade shares.
3. Players can type `!stocks` to see current prices and `!holdings` to see their own portfolio.
4. If a stock price drops below zero, it resets to its `startPrice`. If it spikes above `maxPrice`, the market "corrects" it back toward the middle.
5. The game runs continuously in the background; there is no start/end round.

---

## Files

| File | Purpose |
|---|---|
| `SetResetStockPrices.cs` | Creates the stock list on first run, then adjusts prices every timer tick. |
| `CheckStockPrices.cs` | Replies to `!stocks` with the current price of every emote stock. |
| `CheckHoldings.cs` | Replies to `!holdings` with the player's owned shares and remaining points. |
| `BuyStock.cs` | Handles `!buy <emote> <quantity>`. Deducts points and adds shares. |
| `SellStock.cs` | Handles `!sell <emote> <quantity>`. Sells shares and refunds points. |

---

## Setup

### 1. Create the timer

Go to **Services → Timers** and add a new timer:

| Setting | Value |
|---|---|
| **Name** | `EmoteStockPriceUpdate` (or any name) |
| **Interval** | `600` seconds (10 minutes) |
| **Enabled** | ✅ |
| **Repeat** | ✅ |

### 2. Create the `SetResetStockPrices` action

1. **Actions** tab → **+** → name it `SetResetStockPrices`.
2. **Trigger** → **Add → Core → Timed Actions** → select your timer.
3. Add `Core → C# → Execute C# Code` → paste `SetResetStockPrices.cs`.
4. Add a second sub-action: `Core → Actions → Run Action` → choose `CheckStockPrices`.
5. Save.

> **First run:** right-click the timer trigger and select **Test Trigger**. You should see a `!stocks`-style message appear in chat.

### 3. Create the `CheckStockPrices` action

1. **Actions** tab → **+** → name it `CheckStockPrices`.
2. **Trigger** → **Add → Command** → select the `!stocks` command (create it in the next step).
3. Add `Core → C# → Execute C# Code` → paste `CheckStockPrices.cs`.
4. Save.

### 4. Create the `!stocks` command

| Setting | Value |
|---|---|
| **Command** | `!stocks` |
| **Mode** | `Starts With` |
| **Permission** | `Everyone` |
| **Enabled** | ✅ |

Assign the `CheckStockPrices` action.

### 5. Create the `CheckHoldings` action

1. **Actions** tab → **+** → name it `CheckHoldings`.
2. **Trigger** → **Add → Command** → select the `!holdings` command.
3. Add `Core → C# → Execute C# Code` → paste `CheckHoldings.cs`.
4. Save.

### 6. Create the `!holdings` command

| Setting | Value |
|---|---|
| **Command** | `!holdings` |
| **Mode** | `Starts With` |
| **Permission** | `Everyone` |
| **Enabled** | ✅ |

Assign the `CheckHoldings` action.

### 7. Create the `!buy` command

| Setting | Value |
|---|---|
| **Command** | *(anything, ignored in Regex mode)* |
| **Mode** | `Regex` |
| **Regex** | `!buy([\s]*)([^ ]*)([\s]*)([\d]*)` |
| **Permission** | `Everyone` |
| **Enabled** | ✅ |

Assign the `BuyStock` action. The code reads `match[2]` as the emote and `match[4]` as the quantity.

### 8. Create the `BuyStock` action

1. **Actions** tab → **+** → name it `BuyStock`.
2. **Trigger** → select the `!buy` regex command.
3. Add `Core → C# → Execute C# Code` → paste `BuyStock.cs`.
4. Save.

### 9. Create the `!sell` command

| Setting | Value |
|---|---|
| **Command** | *(anything, ignored in Regex mode)* |
| **Mode** | `Regex` |
| **Regex** | `!sell([\s]*)([^ ]*)([\s]*)([\d]*)` |
| **Permission** | `Everyone` |
| **Enabled** | ✅ |

Assign the `SellStock` action.

### 10. Create the `SellStock` action

1. **Actions** tab → **+** → name it `SellStock`.
2. **Trigger** → select the `!sell` regex command.
3. Add `Core → C# → Execute C# Code` → paste `SellStock.cs`.
4. Save.

---

## Cross-platform notes

- Points and stock portfolios are stored per-platform. A viewer's Twitch points are separate from their YouTube or Kick points.
- The currency variable name is `points`. If you use a different name, edit the C# files or set up a global variable mapping.
- On Twitch, messages include emotes like `TwitchSings` and `TheIlluminati`. On YouTube/Kick, emotes are omitted.

---

## Customizing the stocks

Open `SetResetStockPrices.cs` and edit the `BuildDefaultStocks` method. Each stock looks like this:

```csharp
["MyEmote"] = new Dictionary<string, int> {
    ["volatility"] = 100,
    ["startPrice"] = 1000,
    ["currentPrice"] = 1000,
    ["maxPrice"] = 10000
},
```

| Field | Meaning |
|---|---|
| **volatility** | Maximum random price change per tick. |
| **startPrice** | Starting price and floor reset if price drops below 0. |
| **currentPrice** | Live price, updated every tick. |
| **maxPrice** | Soft ceiling; price corrects toward `(maxPrice + startPrice) / 2` if exceeded. |

After editing, recompile the action in Streamer.bot. If you already ran the timer once, delete the `EmoteStockGame_prices` global variable so the new defaults are recreated.

---

## Testing Checklist

- [ ] Test the timer trigger manually and confirm `!stocks` prices appear in chat.
- [ ] From a second account, type `!buy Kappa 1` and verify points are deducted.
- [ ] Type `!holdings` and confirm the share appears with its current value.
- [ ] Type `!sell Kappa 1` and verify points are refunded.
- [ ] Wait for the next timer tick (or retest the trigger) and confirm prices moved.
- [ ] Check that `!stocks` still works after the price update.

---

## Support

If you need help, join [my Discord community](https://736.fyi/discord) and I'll provide free support.
