# OBS Integration Guide

The `@streamerbot/client` WebSocket connection can receive OBS events and send
commands back through Streamer.bot. This guide covers how to build
OBS-aware browser overlays and remote controls.

## How OBS events reach the client

Streamer.bot has a built-in OBS integration. When OBS is connected inside
Streamer.bot, OBS events are forwarded on the WebSocket server under the `Obs`
event source.

```js
client.on('Obs.SceneChanged', ({ data }) => {
  console.log('New scene:', data.sceneName);
});
```

## OBS event reference

Source: `Obs`

| Event | Typical use |
| --- | --- |
| `Obs.Connected` | OBS just connected to Streamer.bot. |
| `Obs.Disconnected` | OBS disconnected. |
| `Obs.SceneChanged` | Change overlay layout for the new scene. |
| `Obs.StreamingStarted` | Show "LIVE" indicator, start timers. |
| `Obs.StreamingStopped` | Hide "LIVE" indicator, show run stats. |
| `Obs.RecordingStarted` | Show "REC" indicator. |
| `Obs.RecordingStopped` | Hide "REC" indicator. |
| `Obs.Event` | Raw OBS WebSocket event forwarded from Streamer.bot. |

The data payloads vary by event; `SceneChanged` includes `sceneName`, while
streaming/recording events are usually simple signals.

## Scene-aware overlay pattern

```js
const client = new StreamerbotClient();

client.on('Obs.SceneChanged', ({ data }) => {
  document.body.dataset.scene = data.sceneName;
});

client.on('Obs.StreamingStarted', () => {
  document.body.classList.add('is-live');
});

client.on('Obs.StreamingStopped', () => {
  document.body.classList.remove('is-live');
});
```

```css
/* Show/hide elements per scene using CSS */
body[data-scene="Starting Soon"] .game-ui { display: none; }
body[data-scene="Gameplay"] .game-ui { display: block; }
body.is-live .live-badge { opacity: 1; }
```

## Triggering OBS actions from the client

The client does not talk to OBS directly. Instead, trigger a Streamer.bot
action that contains OBS sub-actions:

```js
// Browser overlay button
async function switchScene(sceneName) {
  await client.doAction('obs-switch-scene', { sceneName });
}

async function toggleSource(sourceName) {
  await client.doAction('obs-toggle-source', { sourceName });
}
```

Inside Streamer.bot create actions named `obs-switch-scene` and
`obs-toggle-source` that use the **OBS** sub-actions (Set Scene, Set Source
Visibility, etc.) and read the passed arguments as variables.

## Combining OBS with Twitch events

```js
client.on('Twitch.Raid', ({ data }) => {
  // Auto-switch to a "Raid" scene for 10 seconds
  client.doAction('obs-switch-scene', { sceneName: 'Raid' });
  setTimeout(() => {
    client.doAction('obs-switch-scene', { sceneName: 'Gameplay' });
  }, 10000);
});
```

## Synchronizing action lists when the user edits Streamer.bot

```js
client.on('Application.*', async () => {
  const actions = await client.getActions();
  populateActionButtons(actions.actions);
});
```

## OBS Browser source tips

- Set the Browser source size to match the overlay resolution.
- Use `body { background: transparent; }` or `rgba(0,0,0,0)`.
- Add `?v=1` to the local file URL during development to bypass caching.
- For file:// overlays, CORS does not apply, but you still need the WebSocket
  server reachable from the same machine.

## Controlling OBS from a remote client

If the client runs on a different machine than Streamer.bot:

1. In Streamer.bot, bind the WebSocket Server to `0.0.0.0` or the LAN IP.
2. Use the LAN IP in the client options.
3. For internet access, tunnel with `wss` (Tailscale, ngrok, etc.) and set
   `scheme: 'wss'` and `password`.

OBS itself remains controlled by Streamer.bot; the remote client only needs the
WebSocket connection.
