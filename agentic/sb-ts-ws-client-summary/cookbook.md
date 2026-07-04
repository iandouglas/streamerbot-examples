# Streamer.bot Client Cookbook

Ready-to-adapt recipes for the most common overlay, game, and integration tasks.

## 1. Chat overlay with badges, color, and pronouns

```html
<!DOCTYPE html>
<html>
<head>
  <script src="https://cdn.jsdelivr.net/npm/@streamerbot/client/dist/streamerbot-client.js"></script>
  <style>
    body { font-family: sans-serif; color: white; background: transparent; }
    .msg { margin: 0.2em 0; }
    .name { font-weight: bold; margin-right: 0.4em; }
    .badge { height: 1em; vertical-align: middle; margin-right: 0.2em; }
  </style>
</head>
<body>
  <div id="chat"></div>
  <script>
    const client = new StreamerbotClient();
    const chat = document.getElementById('chat');

    client.on('Twitch.ChatMessage', ({ data }) => {
      const m = data.message;
      const badges = (m.badges || []).map(b =>
        `\u003cimg class="badge" src="${b.imageUrl}" alt="${b.name}"\u003e`
      ).join('');
      const el = document.createElement('div');
      el.className = 'msg';
      el.innerHTML = `${badges}<span class="name" style="color:${m.color}">${m.displayName}</span>${m.message}`;
      chat.appendChild(el);
      chat.scrollTop = chat.scrollHeight;
      if (chat.children.length > 50) chat.firstChild.remove();
    });
  </script>
</body>
</html>
```

## 2. Channel point redemption mini-game

```js
const client = new StreamerbotClient();

client.on('Twitch.RewardRedemption', ({ data }) => {
  const { user_name, reward, user_input } = data;
  switch (reward.title) {
    case 'Spawn Enemy':
      spawnEnemy(user_input || 'default');
      break;
    case 'Heal Player':
      healPlayer(user_name);
      break;
    case 'Start Boss Fight':
      startBossFight();
      break;
  }
});
```

## 3. Follow / sub / cheer alert queue

```js
const client = new StreamerbotClient();
const queue = [];
let playing = false;

function enqueueAlert(type, payload) {
  queue.push({ type, payload });
  if (!playing) playNext();
}

async function playNext() {
  if (!queue.length) { playing = false; return; }
  playing = true;
  const { type, payload } = queue.shift();
  await showAlert(type, payload);
  playNext();
}

function showAlert(type, payload) {
  return new Promise((resolve) => {
    const el = document.getElementById('alert');
    el.textContent = `${type}: ${payload.user_name || payload.displayName}`;
    el.classList.add('animate');
    setTimeout(() => {
      el.classList.remove('animate');
      resolve();
    }, 4000);
  });
}

client.on('Twitch.Follow', ({ data }) => enqueueAlert('Follow', data));
client.on('Twitch.Sub', ({ data }) => enqueueAlert('Sub', data));
client.on('Twitch.ReSub', ({ data }) => enqueueAlert('Resub', data));
client.on('Twitch.Cheer', ({ data }) => enqueueAlert('Cheer', data));
```

## 4. Execute a Streamer.bot action from the browser

```js
async function triggerOverlay(overlayName) {
  const actions = await client.getActions();
  const action = actions.actions.find(a => a.name === overlayName);
  if (!action) {
    console.warn('Action not found:', overlayName);
    return;
  }
  await client.doAction(action.id, {
    requestedBy: 'browser-overlay',
    scene: 'Gameplay',
  });
}
```

## 5. Two-way request/response with Custom Event Trigger

Client side:

```js
const res = await client.doAction('fetch-current-goal', {}, {
  customEventResponse: true,
});
console.log(res.customEventResponseArgs);
```

Streamer.bot side (C# Execute sub-action at the end of the action):

```cs
string json = $"{{\"event\": \"sbResponse\", \"goal\": \"%currentGoal%\", \"sbClientResponse\": \"%sbClientResponse%\"}}";
CPH.WebsocketBroadcastJson(json);
```

## 6. OBS scene-aware overlay

```js
client.on('Obs.SceneChanged', ({ data }) => {
  document.body.dataset.scene = data.sceneName;
});

client.on('Obs.StreamingStarted', () => document.body.classList.add('live'));
client.on('Obs.StreamingStopped', () => document.body.classList.remove('live'));
```

## 7. Send chat messages from the client

Requires the WebSocket server to have a password configured and the client to authenticate.

```js
const client = new StreamerbotClient({
  password: 'your-ws-password',
});

async function announceRound(round) {
  await client.sendMessage('twitch', `Round ${round} starting!`, {
    bot: false,
    internal: true,
  });
}
```

## 8. Read global variables into a Vue dashboard

```vue
<script setup>
import { useStreamerbot } from '@streamerbot/vue';
import { StreamerbotGlobalVariable } from '@streamerbot/vue/components';

const { status, data } = useStreamerbot({ subscribe: '*' });
</script>

<template>
  <div>
    <p>Status: {{ status }}</p>
    <p>Latest follow: <StreamerbotGlobalVariable name="latestFollower" /></p>
    <pre>{{ data }}</pre>
  </div>
</template>
```

## 9. Batch high-frequency events

```js
const pending = [];

client.on('Twitch.Cheer', ({ data }) => pending.push(data));
client.on('Twitch.Sub', ({ data }) => pending.push(data));

setInterval(() => {
  if (!pending.length) return;
  const batch = pending.splice(0);
  renderCelebrations(batch);
}, 250);
```

## 10. Reconnect guard and status indicator

```js
const statusEl = document.getElementById('status');

const client = new StreamerbotClient({
  onConnect: () => statusEl.textContent = 'Connected',
  onDisconnect: () => statusEl.textContent = 'Disconnected',
  onError: (err) => console.error('WS error', err),
  autoReconnect: true,
  retries: -1,
});

window.addEventListener('beforeunload', () => client.disconnect());
```
