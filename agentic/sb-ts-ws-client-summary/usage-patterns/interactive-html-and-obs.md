# Usage Patterns: Interactive HTML Pages, Games, and OBS

This file collects practical patterns for using `@streamerbot/client` in the
context the site is built for: browser overlays, interactive games, and
Streamer.bot / OBS integrations.

## 1. Browser overlay basics

Browser overlays are plain HTML/JS pages loaded as a Browser source in OBS or
as a dock in Streamer.bot. The CDN build is the fastest way to start:

```html
<script src="https://cdn.jsdelivr.net/npm/@streamerbot/client/dist/streamerbot-client.js"></script>
```

In the page:

```js
const client = new StreamerbotClient({
  host: '127.0.0.1',
  port: 8080,
  endpoint: '/',
  autoReconnect: true,
});
```

### Reliable overlay rules

- Always set `autoReconnect: true` so OBS or a reload doesn't lose the link.
- Keep DOM updates inside `requestAnimationFrame` when many events fire quickly.
- Use CSS classes and `transform` for animations; avoid layout thrashing.
- Destroy listeners on page hide to prevent memory leaks:
  `window.addEventListener('beforeunload', () => client.disconnect());`

## 2. Reacting to chat / redeems / subs

The `client.on()` helper auto-subscribes and gives typed event payloads:

```js
client.on('Twitch.ChatMessage', ({ event, data, timeStamp }) => {
  const { displayName, message, color } = data.message;
  addChatMessage(displayName, message, color);
});

client.on('Twitch.RewardRedemption', ({ data }) => {
  if (data.reward.title === 'Launch Firework') {
    launchFirework(data.user.displayName);
  }
});
```

For high-frequency events (cheers, mass subs) consider batching DOM updates:

```js
let queue = [];
client.on('Twitch.Cheer', ({ data }) => queue.push(data));
setInterval(() => {
  if (!queue.length) return;
  renderCheers(queue);
  queue = [];
}, 100);
```

## 3. Driving games from Streamer.bot

Two-way pattern:

1. **Streamer.bot -> game**: send a custom event from a C# action with
   `CPH.WebsocketBroadcastJson(json)`, then listen on the client:

   ```js
   client.on('General.Custom', ({ data }) => {
     if (data.event === 'spawnEnemy' && data.enemyType) {
       spawnEnemy(data.enemyType);
     }
   });
   ```

2. **Game -> Streamer.bot**: call `client.doAction(id, args)` from the game:

   ```js
   client.doAction('my-game-over-action', {
     score: player.score,
     winner: player.name,
   });
   ```

Use `customEventResponse: true` when the game needs data back from the action:

```js
const res = await client.doAction('get-high-scores', {}, { customEventResponse: true });
const scores = res.customEventResponseArgs?.scores ?? [];
```

On the Streamer.bot side, end the action with the **Custom Event Trigger**
sub-action and include the `%sbClientResponse%` argument in the payload.

## 4. OBS-aware clients

Subscribe to OBS events from the same WebSocket connection:

```js
client.on('Obs.SceneChanged', ({ data }) => {
  updateOverlayForScene(data.sceneName);
});

client.on('Obs.StreamingStarted', () => showLiveIndicator(true));
client.on('Obs.StreamingStopped', () => showLiveIndicator(false));
client.on('Obs.RecordingStarted', () => showRecIndicator(true));
client.on('Obs.RecordingStopped', () => showRecIndicator(false));
```

Use `Application.*` to refresh action lists when actions are edited:

```js
client.on('Application.*', async () => {
  const actions = await client.getActions();
  populateActionPicker(actions.actions);
});
```

## 5. Reading and writing shared state

Read global variables for overlays:

```js
const latest = await client.getGlobal('latestFollower');
console.log(latest.variable.value);
```

For write-back, use a Streamer.bot action or C# code triggered from the client:

```js
client.doAction('update-overlay-state', {
  scene: currentScene,
  round: currentRound,
});
```

## 6. Vue.js dashboards

The `@streamerbot/vue` package wraps the client in a reactive composable:

```vue
<script setup>
import { useStreamerbot } from '@streamerbot/vue';

const { client, status, data, error, connect, disconnect } = useStreamerbot({
  subscribe: '*',
});
</script>
```

`data` is a ref that updates on every incoming message; `status` is
`'OPEN' | 'CONNECTING' | 'CLOSED'`.

## 7. Security and connection tips

- The default WebSocket is unauthenticated. Enable a password in Streamer.bot
  `Servers/Clients > WebSocket Server` and pass it as `password`.
- For remote tunnels use `scheme: 'wss'`.
- `immediate: false` lets you create the client and call `connect()` later.
- The client auto-reconnects with exponential-ish backoff (1s per attempt up
  to 30s). Set `retries` to limit attempts.

## 8. Common pitfalls

- Subscriptions only take effect if the WebSocket server is enabled and
  running in Streamer.bot.
- `sendMessage` requires an **authenticated** WebSocket connection.
- Event names use the format `Source.Type` (e.g. `Twitch.ChatMessage`).
- `client.on()` accepts wildcards (`Twitch.*`, `*`), but the client still
  expands them into real subscriptions before sending to Streamer.bot.
- The `Raw` source emits internal `Action`, `SubAction`, and `ActionCompleted`
  events for introspection.
