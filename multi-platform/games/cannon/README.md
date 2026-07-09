# Cannon Game

A browser-based chat-driven cannon game for Streamer.bot. Players type `!fire <angle> <power>` in chat, the cannon fuses, fires their profile/platform icon across the screen, and scores 0–100 points based on how close they land to the target.

> **Architecture note:** All game logic lives in Streamer.bot C# actions. The browser page (`index.html` + `cannon.js`) is purely an animation renderer that listens for WebSocket events from Streamer.bot.


## Streamer.bot and OBS Setup

### Enable the WebSocket server

- In Streamer.bot, open **Servers/Clients > WebSocket Server**.
- Enable the server and note the port (default `8080`).
- The game connects to `ws://127.0.0.1:8080/` automatically.

### Create a Streamer.bot timer

Services -> Timers, right-click somewhere and choose 'Add'

The name of the timer must be `cannon-game`.

Set this to be DISABLED to start, set Repeat to ON, and set the interval to 2 seconds. The game will set this to varying amounts of time as the game plays, and the game will also enable/disable the timer itself so it doesn't fill up your log with information.


### Add the Browser Source in OBS

- Create a new **Browser Source**.
- Check **Local file** and browse to `index.html`.
- Width: `1920`, Height: `1080`.
- Recommended: enable **Shutdown source when not visible** and **Refresh browser when scene becomes active**.

The page loads blank until Streamer.bot sends a `setup` event. This lets you show/hide the game by toggling source visibility without stale game state.


### 3. Install the DLLs

[Read the instructions](../../../dlls-needed/README.md)

You'll likely need to restart Streamer.bot after installing the DLL.

---




## Game Setup in Streamer.bot:


### cannon-game-setup.cs

Create a new action called 'cannon-game-setup'

The Trigger for this action should either be BLANK or SET to when your stream goes live:

#### When you go live

Add a Trigger for your platform(s) like "Add -> Twitch -> Channel -> Stream Online"

#### Leave it Blank

Leave the Trigger area blank if you already have another Action defined in Streamer.bot for when you go live. In THAT Action, set an additional sub-action to "Add -> Core -> Actions -> Run Action" and choose this new "cannon-game-setup" action.

#### Setting the cannon-game-setup sub-actions:

Add the following arguments (Add -> Core -> Arguments -> Set argument)

- "obsScene", set this to the name of the OBS scene where your game's "source" will be used
- "obsSource", set this to the name of the Browser source you set up to load the HTML file for the game
- "timerName", set this to "cannon-game"

Add a subaction to 'Execute C# Code' and paste in the code from `cannon-game-setup.cs`



### cannon-player-fires.cs

Set up a new Action called "cannon-player-fires"

Its Trigger will be a new command you make called `!fire`

Add a subaction to 'Execute C# Code' and add the contents of `cannon-player-fires.cs`, click Compile, then click Ok if it compiles successfully.

#### How it works

Your viewer will enter a command like `!fire 20 40` to aim the cannon at 20 degrees and fire at 40% power. This will add them to the game queue where their name will be stacked above the cannon until it's ready to fire.

If they are the first player to fire in a while, everything will be hidden so they won't know where they're shooting so the game will become visible and they'll likely miss that first shot. You can specify a timer on the `!fire` command if you want to pace how far apart each player can attempt to fire the cannon.



### cannon-game-ticks.cs

Create a new Action called "cannon-game-ticks".

Its Trigger will be the "cannon-game" timer you created earlier. "Add -> Core -> Timed Actions" and pick the "cannon-game" timer.

Add three subactions for arguments to point to the audio files for lighting the fuse and firing the cannon:

Add -> Core -> Arguments -> Set argument

| Argument | Example | Description |
|---|---|---|
| `audioFuse` | `assets/sounds/fuse.mp3` | Path to fuse sound |
| `audioFire` | `assets/sounds/cannon-fire.mp3` | Path to cannon fire sound |
| `audioImpact` | `assets/sounds/land-thud.mp3` | Path to impact sound |

Finally, create a fourth subaction to 'Execute C# Code' and paste the code from the `cannon-game-tick.cs` file.

#### How it works

This will ensure the game is visible, and sends data to the HTML page to place the cannon and target, determine an initial sind speed, and set the audio filenames, etc.

It will update the wind every 7 seconds and send any waiting queue of players to the HTML page.

When a player takes a shot with the `!fire` command, this sends it to the game to render.


### cannon-shot-ended.cs

Create a new Action called "cannon-shot-ended".

The HTML page's websocket connection to Streamer.bot will call this when a player lands and scores points so it can be announced in chat.

Create a subaction to 'Execute C# Code' and paste the code from the `cannon-game-tick.cs` file.

#### How it works:

When a player lands on the target, the HTML page calls this action and sends their username, score and platform to the C# code, and will assign that player that number of points.

Then it resets the "cannon firing" state so the next queued player gets their turn.



## Troubleshooting

- **Page loads blank**: expected. It waits for the `setup` event from `cannon-game-tick.cs`.

- **No connection to Streamer.bot**: confirm the WebSocket server is enabled and the game is loaded after Streamer.bot is running.

- **Audio does not play**: browsers block audio until user interaction. Click the Browser Source preview once, or load it inside OBS where the source is interactive.

- **Icons not showing**: ensure `assets/images/emote-twitch.png`, `emote-youtube.png`, `emote-kick.png`, and `emote-trovo.png` exist.
