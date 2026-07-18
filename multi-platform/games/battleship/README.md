# Battleship

A browser-based chat-driven Battleship game for Streamer.bot. Viewers type coordinates (e.g. `B5`) in chat to collectively fire at a hidden fleet. Easy and normal mode fire at the most-voted coordinate; extreme mode uses the average of all submitted coordinates. Ships sink, mines explode, and the chat works together (or chaotically) to sink all 5 ships.

> **Architecture note:** All game logic lives in Streamer.bot C# actions. The browser page (`index.html` + `battleship.js`) is purely an animation renderer that listens for WebSocket events from Streamer.bot.

---

## Game Modes

| Mode     | Grid      | Round Timer | Targeting                           | Mines                     | Points per Hit | Mine Penalty | Flawless Bonus |
|----------|-----------|-------------|-------------------------------------|---------------------------|----------------|--------------|----------------|
| Easy     | 10×10     | 30s         | Most-voted coordinate               | 0                         | 100            | 0            | N/A            |
| Normal   | 10×10     | 30s         | Most-voted coordinate               | 0–25 (default 5)          | 250            | 0            | 1000           |
| Extreme  | 15×15     | 15s         | Average of all submitted coordinates | 1×–5× ship cells (def 2×) | 500            | 500          | 10000          |

### Ships
- Carrier (5 blocks), Battleship (4), Cruiser (3), Submarine (3), Destroyer (2)
- Placed horizontally or vertically, no diagonal, no overlap

### Mines
- Normal mode: 0–25 single-cell mines (default 5)
- Extreme mode: multiplier of total ship cells (1×–5×, default 2× = 34 mines); half are placed adjacent to ships
- Hitting a mine times out floor(half of round participants) players for the next round
- Mine penalties only deduct points in extreme mode

### Points
- Per round, per ship block hit: all players who submitted ≥1 coordinate that round receive points
- Extreme mine penalty: -500 per mine hit (configurable, default 0)
- Flawless victory bonus: if all ships sunk and no mines ever hit, players with ≥5 rounds of participation receive a bonus

---

## Streamer.bot and OBS Setup

### Enable the WebSocket server

- In Streamer.bot, open **Servers/Clients > WebSocket Server**.
- Set the following values exactly:
  - **IP Address:** `127.0.0.1`
  - **Port:** `8080`
  - **Endpoint:** `/`
  - **Auto Start:** enabled
  - **Authentication:** disabled
- Click **Start** so the status shows "Started".

### Create a Streamer.bot timer

Services → Timers, right-click and choose **Add**.

- Name: `battleship-game`
- Set to **DISABLED** to start
- Repeat: ON
- Interval: 30 seconds (the game adjusts this dynamically)

After saving, right-click the timer name → **Copy Timer ID**. Paste it in a text editor — you'll need it for the setup action.

### Add the Browser Source in OBS

- Create a new **Browser Source**
- Check **Local file** and browse to `index.html`
- Width: `1920`, Height: `1080`
- Recommended: enable **Shutdown source when not visible** and **Refresh browser when scene becomes active**

### Install the DLLs

[Read the instructions](../../../dlls-needed/README.md)

You'll need `iandouglas736.dll` which includes the `Log`, `Data`, `Chat`, `Groups`, `Points`, `Timers`, and `Media` helpers.

---

## Game Setup in Streamer.bot

### battleship-game-setup.cs

Create a new action called `battleship-game-setup`.

**Trigger:** A command `!game` (shared with other games like higher-lower). The action reads `rawInput` to determine the subcommand.

Example: `!game battleship normal` or `!game battleship extreme`

**Sub-action arguments** (Add → Core → Arguments → Set argument):

| Argument                | Example                              | Description                                      |
|-------------------------|--------------------------------------|--------------------------------------------------|
| `obsScene`              | `GameScene`                          | OBS scene containing the browser source          |
| `obsSource`             | `BattleshipBrowser`                  | OBS browser source name                          |
| `timerGuid`             | `1288da0a-...`                       | Timer ID copied from the battleship-game timer   |
| `joinTimer`             | `60`                                 | Seconds for the join countdown                   |
| `roundSeconds`          | `30`                                 | Seconds per round (extreme uses max(this/2, 15)) |
| `interRoundSeconds`     | `3`                                  | Minimum gap between rounds                       |
| `pointsName`            | `points`                             | Currency name for awarding/losing points         |
| `shipHitPointsEasy`     | `100`                                | Points per block hit, easy mode                  |
| `shipHitPointsNormal`   | `250`                                | Points per block hit, normal mode                |
| `shipHitPointsExtreme`  | `500`                                | Points per block hit, extreme mode               |
| `minePenalty`           | `0`                                  | Points lost per mine hit in extreme mode only    |
| `flawlessBonusNormal`   | `1000`                               | End-of-game flawless bonus, normal mode          |
| `flawlessBonusExtreme`  | `10000`                              | End-of-game flawless bonus, extreme mode         |
| `minRoundsForBonus`     | `5`                                  | Rounds a player must play to receive bonus       |
| `normalMines`           | `5`                                  | Mine count for normal mode (0–25)                |
| `extremeMultiplier`     | `2`                                  | Mine multiplier for extreme (1–5)                |
| `debugMines`            | `0`                                  | Set to 1 to reveal one mine coordinate in chat for testing |

Add a sub-action to **Execute C# Code** and paste the code from `battleship-game-setup.cs`.

### battleship-player-join.cs

Create an action called `battleship-player-join`.

**Trigger:** Command `!join`

Add a sub-action to **Execute C# Code** and paste `battleship-player-join.cs`.

> **Note:** The action is **disabled by default** and auto-enabled when a game starts, auto-disabled when the game ends. If you already have a `!join` command for the Higher or Lower game, both join actions can share the same `!join` command — each checks its own game's active state and returns early if its game isn't running.

### battleship-coord.cs

Create an action called `battleship-coord`.

**Trigger:** Two Streamer.bot commands (enable/disable as the game starts/stops):

| Command name            | Regex trigger                          | Grid size |
|-------------------------|----------------------------------------|-----------|
| `battleship-normal`     | `^\s*([a-jA-J])\s*(10|[1-9])\s*$`   | 10×10     |
| `battleship-extreme`    | `^\s*([a-oA-O])\s*(1[0-5]|[1-9])\s*$` | 15×15    |

Set both commands to **disabled** by default. The game setup action enables the appropriate one based on mode.

Restrict both commands to the `battleship-players` group only.

Add a sub-action to **Execute C# Code** and paste `battleship-coord.cs`. Click the "Find Refs" button before compiling.

### battleship-tick.cs

Create an action called `battleship-tick`.

**Trigger:** The `battleship-game` timer (Add → Core → Timed Actions → pick `battleship-game`).

Add a sub-action to **Execute C# Code** and paste `battleship-tick.cs`.

### battleship-bomber-complete.cs

Create an action called `battleship-bomber-complete`.

**Trigger:** Called by the HTML page via WebSocket when the bomber animation completes.

Add a sub-action to **Execute C# Code** and paste `battleship-bomber-complete.cs`. Click the "Find Refs" button before compiling.

### battleship-game-end.cs

Create an action called `battleship-game-end`.

**Trigger:** Command `!game battleship end`

Add a sub-action to **Execute C# Code** and paste `battleship-game-end.cs`. Click the "Find Refs" button before compiling.

### battleship-browser-loaded.cs

Create an action called `battleship-browser-loaded`.

**Trigger:** Called by the HTML page via WebSocket when the browser source loads.

Add a sub-action to **Execute C# Code** and paste `battleship-browser-loaded.cs`.

---

## How It Works

1. **Streamer starts the game:** `!game battleship normal` (or `easy` / `extreme`)
2. **Ships and mines are placed** randomly on the grid
3. **The board appears in OBS**, covered in fog
4. **Viewers `!join`** to enter the game
5. **Join window starts:** a centered `!join to play` overlay and countdown appear on the board
6. **Round begins:** a 30s (or 15s) countdown starts, and the target indicator appears
7. **Viewers type coordinates** (e.g. `B5`, `j10`) — no command prefix needed
8. **Easy/normal:** the most-voted coordinate is targeted; **extreme:** the live average is targeted
9. **Timer expires:** the final target is locked, announced in chat ("Firing on position C-9!")
10. **Bomber animation:** a 1960s-era bomber flies across the board, drops a bomb at the target
11. **Result:** white peg (miss), red peg (hit), or mine icon (mine hit)
12. **Ship sunk:** the ship shape is revealed with a red X
13. **Mine hit:** half the round's players are muted for the next round; point penalties only apply in extreme mode
14. **Next round starts** after the animation completes
15. **Game ends** when all ships are sunk (win), all mines are hit in extreme mode (lost), or the streamer ends it

---

## Audio Files

All audio files are in `assets/sounds/` and are pre-merged (airplane + SFX + voice baked in):

| File                       | When played                              | Airplane animation? |
|----------------------------|------------------------------------------|---------------------|
| `__game-start.mp3`         | Game starts                              | No                  |
| `__ship-hit.mp3`           | Ship block hit                           | Yes                 |
| `__ship-sunk.mp3`          | Ship fully sunk (after hit)              | (continues from hit) |
| `__miss.mp3`               | Water miss                               | Yes                 |
| `__mine-hit.mp3`           | Mine hit                                 | Yes                 |
| `__no-coords.mp3`          | No coordinates submitted in a round      | No                  |
| `__repeat-coordinates.mp3` | Firing on an already-shot cell           | No                  |
| `__game-win.mp3`           | All ships sunk (game won)                | No                  |
| `__game-lost.mp3`          | All mines hit (extreme) or streamer ended | No                 |

---

## Logging

All game actions log to `S:/logs/battleship-YYYYMMDD.txt` using `id736.Log.File()`. Check there for debugging information.

---

## Troubleshooting

- **Page loads blank:** expected. It waits for the `setup` event from Streamer.bot.
- **No connection to Streamer.bot:** confirm the WebSocket server is enabled and the game is loaded after Streamer.bot is running.
- **Audio does not play:** browsers block audio until user interaction. Click the Browser Source preview once, or load it inside OBS where the source is interactive.
- **Coordinates not registering:** ensure the player has `!join`ed and the correct coordinate command (`battleship-normal` or `battleship-extreme`) is enabled.
- **Check the logs:** `S:/logs/battleship-YYYYMMDD.txt` has detailed debugging info.
