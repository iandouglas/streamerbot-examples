# Connect Four

A chat-driven Connect Four game for Streamer.bot. Chat votes for which column to drop the red piece into, then the AI plays the yellow piece. Three difficulty levels scale the AI from random moves to a minimax search with an optional Ollama LLM opponent.

> **Architecture note:** All game logic lives in Streamer.bot C# sub-actions. The browser page (`index.html` + `styles.css` + `connectfour.js`) is purely a display renderer that listens for WebSocket events broadcast by the C# code.

## Game flow

1. Streamer runs `!game connectfour [easy|normal|extreme]` — game state is initialized, a 60-second join window opens.
2. Viewers type `!join` to participate.
3. When the join window closes, the **voting phase** starts (30 seconds). Viewers type a bare number (1-7, or 1-11 on extreme) to vote for that column.
4. If votes tie, a 15-second **tiebreak phase** runs only between the tied columns.
5. The chat's piece drops and win/draw is checked.
6. The **AI phase** runs: the `connectfour-ai-move` sub-action picks a column and drops the AI's piece.
7. Repeat until someone connects four or the board fills.
8. Points are awarded to all joined players (more for a chat win, fewer for a loss, a small amount for a draw).

## Difficulties

| Difficulty | Grid | AI behavior |
|---|---|---|
| `easy` | 7×6 | Random column (20% chance to block an immediate human win) |
| `normal` | 7×6 | Wins immediately, blocks 70% of immediate human wins, 1-step lookahead scoring |
| `extreme` | 11×6 | Ollama LLM if configured, otherwise minimax depth-4 with alpha-beta pruning and immediate win/block |

## Files

| File | Purpose |
|---|---|
| `connectfour-game-setup.cs` | Handles `!game connectfour [easy|normal|extreme]` and `!game connectfour end`. Initializes state and resets between games. |
| `connectfour-player-join.cs` | Handles `!join` during the join window. Adds viewers to the player group. |
| `connectfour-vote.cs` | Handles `!vote <column>` during voting and tiebreak phases. Only joined players can vote. |
| `connectfour-game-tick.cs` | Main game loop, fired by a 1-second timer. Drives phase transitions, resolves votes, checks win/draw, awards points, and resets state. |
| `connectfour-ai-move.cs` | AI opponent logic. Invoked by the tick via `CPH.RunAction("connectfour-ai-move")` during the AI phase. |
| `index.html` | Browser source layout. |
| `styles.css` | Styling for the board, pieces, status bar, timer, and winner overlay. |
| `connectfour.js` | WebSocket client + renderer. Listens for `setup`, `player-joined`, `phase`, `move`, `game-over`, and `game-end` events. |
| `assets/js/streamerbot-client.js` | Streamer.bot WebSocket client library (shared with other games). |

## Streamer.bot and OBS setup

### 1. Enable the WebSocket server

- In Streamer.bot, open **Servers/Clients > WebSocket Server**.
- Set:
  - **IP Address:** `127.0.0.1`
  - **Port:** `8080`
  - **Endpoint:** `/`
  - **Auto Start:** enabled
  - **Authentication:** disabled
- Click **Start**.

### 2. Create the game timer

- **Services → Timers → Add**
- Name: `connectfour-game`
- **Disabled** to start, **Repeat** ON, interval **1 second**.
- Save, then right-click the timer → **Copy Timer ID**. Paste it somewhere; you'll need the GUID for the setup sub-action arguments.

### 3. Add the OBS browser source

- Create a new **Browser Source** in OBS.
- Check **Local file** and browse to this folder's `index.html`.
- Width: `1920`, Height: `1080`.
- Recommended: enable **Shutdown source when not visible** and **Refresh browser when scene becomes active**.

### 4. Install the DLLs

[Read the instructions](../../../dlls-needed/README.md)

You'll likely need to restart Streamer.bot after installing the DLL.

## Streamer.bot sub-actions

### `connectfour-game-setup`

- **Action name:** `connectfour-game-setup`
- **Trigger:** Command `!game connectfour [easy|normal|extreme]` (and `!game connectfour end` to cancel).
- Required arguments:

| Argument | Description |
|---|---|
| `timerGuid` | Timer ID you copied above (GUID format). |
| `obsScene` | Name of the OBS scene containing the game's browser source. |
| `obsSource` | Name of the OBS browser source that loads `index.html`. |

- Optional arguments for points configuration (defaults shown):

| Argument | Default | Description |
|---|---|---|
| `pointsName` | `points` | Name of the points currency to award |
| `winPointsEasy` | `10` | Points per player if chat wins on easy |
| `winPointsNormal` | `25` | Points per player if chat wins on normal |
| `winPointsExtreme` | `50` | Points per player if chat wins on extreme |
| `lossPointsEasy` | `2` | Points per player if AI wins on easy |
| `lossPointsNormal` | `5` | Points per player if AI wins on normal |
| `lossPointsExtreme` | `10` | Points per player if AI wins on extreme |
| `drawPointsEasy` | `1` | Points per player on a draw (easy) |
| `drawPointsNormal` | `2` | Points per player on a draw (normal) |
| `drawPointsExtreme` | `5` | Points per player on a draw (extreme) |
| `ollamaUrl` | *(empty)* | Base URL of your Ollama server, e.g. `http://localhost:11434`. Required for LLM-driven extreme mode. |
| `ollamaModel` | `llama3` | Model name to pass to Ollama's `/api/generate`. Must be installed/pulled at the configured URL. |
| `joinSeconds` | `60` | How long the join window stays open. Chat reminders fire every 10 seconds. Minimum 10. |
| `voteCommandName` | `connectfour vote` | Name of the Streamer.bot command that triggers `connectfour-vote`. The command is enabled when a game starts and disabled when it ends. |

- Add **Execute C# Code** sub-action → paste `connectfour-game-setup.cs`.

When `difficulty=extreme` and `ollamaUrl` is set, setup performs a pre-flight check by hitting Ollama's `/api/tags` endpoint. If the server is unreachable or the requested model isn't installed, a warning is sent to chat and the game proceeds using the built-in minimax AI as a fallback.

The setup action shows the OBS browser source when a game starts. The tick hides it automatically after the game-over animation finishes (or immediately if the game is cancelled or nobody joins).

### `connectfour-player-join`

- **Action name:** `connectfour-player-join`
- **Trigger:** Command `!join`.
- Add **Execute C# Code** sub-action → paste `connectfour-player-join.cs`.

### `connectfour-vote`

- **Action name:** `connectfour-vote`
- **Command name:** `connectfour vote` (must match the `voteCommandName` arg on `connectfour-game-setup`)
- **Trigger:** A Streamer.bot command with regex pattern `^\d$` (or `^\d{1,2}$` for extreme mode's 11 columns) so viewers can type a bare number like `3` to vote for column 3.
- The command should be **disabled by default**; `connectfour-game-setup` enables it when a game starts and disables it when the game ends.
- Only viewers who joined the game (in the `connectfour_players` group) can vote. Invalid numbers and non-players are silently ignored.
- Add **Execute C# Code** sub-action → paste `connectfour-vote.cs`.

### `connectfour-game-tick`

- **Action name:** `connectfour-game-tick`
- **Trigger:** Timer `connectfour-game` (Add → Core → Timed Actions).
- Add **Execute C# Code** sub-action → paste `connectfour-game-tick.cs`.

### `connectfour-ai-move`

- **Action name:** `connectfour-ai-move`
- **Trigger:** none (called via `CPH.RunAction("connectfour-ai-move")` from the tick).
- Add **Execute C# Code** sub-action → paste `connectfour-ai-move.cs`.
- No additional arguments needed — `ollamaUrl` and `ollamaModel` are read from the global vars set by `connectfour-game-setup`. If the pre-flight check failed, the AI skips the Ollama call and uses the depth-4 minimax player.

## Optional: Ollama integration (extreme mode)

If you run [Ollama](https://ollama.ai) locally, set the `ollamaUrl` argument on the `connectfour-game-setup` action (e.g. `http://localhost:11434`) and `ollamaModel` to choose which model to use (e.g. `llama3`, `mistral`, or `qwen2.5`). The setup action performs a pre-flight check against `/api/tags` to verify the server is reachable and the model is installed. During the game, `connectfour-ai-move` POSTs to `/api/generate` with a board description and asks the LLM for a column number.

If the pre-flight check fails, or the Ollama call fails at runtime, extreme mode automatically falls back to the depth-4 minimax player.

## Points

Points are awarded at the end of each game to every joined player. Defaults:

| Result | easy | normal | extreme |
|---|---|---|---|
| Chat wins | 10 | 25 | 50 |
| Chat loses (AI wins) | 2 | 5 | 10 |
| Draw | 1 | 2 | 5 |

All values are configurable via sub-action arguments on `connectfour-game-setup` (see the setup table above). Use the `pointsName` argument to target a different points currency.

## Troubleshooting

- **Board loads blank:** expected. The display waits for the `setup` event from `connectfour-game-setup.cs`. Start a game with `!game connectfour`.
- **No connection to Streamer.bot:** confirm the WebSocket server is running and `connectfour.js` is loaded after `streamerbot-client.js`.
- **Timer never fires:** ensure the `connectfour-game` timer is created in Streamer.bot and the `timerGuid` argument on the setup action matches its Timer ID.
- **AI never moves:** confirm `connectfour-ai-move` exists as an action with the exact name. The tick calls it by name via `CPH.RunAction`.
- **Votes not counted:** only viewers who typed `!join` before the join window closed can vote.