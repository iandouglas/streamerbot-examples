let audioContext;

/**
 * Initialize the Web Audio API context.
 */
function initAudio() {
  try {
    if (!audioContext) {
      audioContext = new (window.AudioContext || window.webkitAudioContext)();
    }
    if (audioContext.state === 'suspended') {
      audioContext.resume();
    }
  } catch (e) {
    console.error('Web Audio API is not supported in this browser');
  }
}

/**
 * Play an audio file by path.
 * @param {string} path - Path to the audio file.
 */
function playAudioFile(path) {
  initAudio();
  const audio = new Audio(path);
  audio.play().catch((err) => {
    console.warn('Audio playback failed:', err);
  });
}

// Canvas and drawing context.
const canvas = document.getElementById('gameCanvas');
const ctx = canvas.getContext('2d');

// Constants.
const FUSE_DURATION = 800;
const TARGET_WIDTH = 500;
const TARGET_HEIGHT = 35;
const PROJECTILE_SIZE = 50;
const GRAVITY = 900; // pixels per second squared.
const QUEUE_DROP_DURATION = 600;

/**
 * Game display state. All logic is driven by C# via WebSocket events.
 * @typedef {Object} GameState
 * @property {boolean} initialized - Whether setup has been received.
 * @property {'left'|'right'} cannonSide - Cannon side for the round.
 * @property {number} cannonAngle - Cannon barrel angle in degrees.
 * @property {number} targetX - Target center X.
 * @property {number} targetY - Target center Y.
 * @property {number} wind - Signed wind value.
 * @property {Array<QueueEntry>} queue - Players waiting to fire.
 * @property {Array<Projectile>} projectiles - Active cannonballs.
 * @property {Array<LandedShot>} landedShots - Shots that have landed.
 * @property {boolean} paused - Whether the game is paused for celebration.
 * @property {number} pauseEnd - Timestamp when pause ends.
 * @property {string|null} scoreText - Score text to display.
 * @property {number} scoreX - X position for score text.
 * @property {number} scoreY - Y position for score text.
 */

/**
 * Queue entry descriptor.
 * @typedef {Object} QueueEntry
 * @property {string} name - Player name.
 * @property {string} platform - Platform id, e.g. 'twitch', 'youtube', 'kick', 'trovo'.
 * @property {number} angle - Launch angle in degrees.
 * @property {number} power - Launch power 1-100.
 * @property {string|null} profileImageUrl - Optional profile image URL.
 */

/**
 * Projectile descriptor.
 * @typedef {Object} Projectile
 * @property {string} name - Player name.
 * @property {string} platform - Platform id.
 * @property {string|null} profileImageUrl - Optional profile image URL.
 * @property {number} startX - Initial X.
 * @property {number} startY - Initial Y.
 * @property {number} velocityX - Horizontal velocity in px/s.
 * @property {number} velocityY - Initial vertical velocity in px/s.
 * @property {number} time - Elapsed flight time in seconds.
 * @property {number} rotation - Current rotation in radians.
 * @property {number} rotationSpeed - Rotation speed in radians/s.
 * @property {number} bounces - Number of wall bounces.
 */

/**
 * Landed shot descriptor.
 * @typedef {Object} LandedShot
 * @property {number} x - Landing X.
 * @property {number} y - Landing Y.
 * @property {string} name - Player name.
 * @property {string} platform - Platform id.
 * @property {string|null} profileImageUrl - Optional profile image URL.
 * @property {number} score - Score (1-100).
 */

/** @type {GameState} */
const gameState = {
  initialized: false,
  cannonSide: 'left',
  cannonAngle: 45,
  targetX: canvas.width / 2,
  targetY: canvas.height - TARGET_HEIGHT / 2,
  wind: 0,
  queue: [],
  projectiles: [],
  landedShots: [],
  paused: false,
  pauseEnd: 0,
  scoreText: null,
  scoreX: 0,
  scoreY: 0
};

/**
 * Clamp a number between min and max.
 * @param {number} value - Value to clamp.
 * @param {number} min - Minimum.
 * @param {number} max - Maximum.
 * @returns {number} Clamped value.
 */
function clamp(value, min, max) {
  return Math.max(min, Math.min(max, value));
}

/**
 * Get the platform icon or profile image for a player.
 * @param {QueueEntry|Projectile|LandedShot} entry - Player entry.
 * @returns {HTMLImageElement} Image element.
 */
function getPlayerImage(entry) {
  // Use profile image if provided and not obviously a default avatar.
  const profileUrl = entry.profileImageUrl;
  if (profileUrl && !profileUrl.includes('default') && !profileUrl.includes('default_avatar')) {
    return loadImage('profile-' + entry.name, profileUrl);
  }

  const platform = (entry.platform || 'twitch').toLowerCase();
  const path = `assets/images/emote-${platform}.png`;
  return loadImage('platform-' + platform, path);
}

// Image cache.
/** @type {Object<string, HTMLImageElement>} */
const imageCache = {};

/**
 * Load and cache an image.
 * @param {string} key - Cache key.
 * @param {string} src - Image source path/URL.
 * @returns {HTMLImageElement} Image element.
 */
function loadImage(key, src) {
  if (!imageCache[key]) {
    const img = new Image();
    img.src = src;
    imageCache[key] = img;
  }
  return imageCache[key];
}

/**
 * Draw the target rug.
 */
function drawTarget() {
  const x = gameState.targetX;
  const y = gameState.targetY;
  const rx = TARGET_WIDTH / 2;
  const ry = TARGET_HEIGHT / 2;

  const rings = [
    { color: '#ffffff', rx: 1.0, ry: 1.0 },
    { color: '#c41e3a', rx: 0.78, ry: 0.78 },
    { color: '#ffffff', rx: 0.56, ry: 0.56 },
    { color: '#c41e3a', rx: 0.34, ry: 0.34 },
    { color: '#ffd700', rx: 0.14, ry: 0.14 }
  ];

  for (const ring of rings) {
    ctx.fillStyle = ring.color;
    ctx.beginPath();
    ctx.ellipse(x, y, rx * ring.rx, ry * ring.ry, 0, 0, Math.PI * 2);
    ctx.fill();
  }
}

/**
 * Get the cannon base anchor position (bottom center of the base image).
 * @returns {{x: number, y: number}} Base position.
 */
function getCannonBasePos() {
  return {
    x: gameState.cannonSide === 'left' ? 90 : canvas.width - 90,
    y: canvas.height
  };
}

/**
 * Get the pivot point around which the barrel rotates.
 * The pivot is the beige pin in cannon-base.svg at local (90, 35).
 * @returns {{x: number, y: number}} Pivot position.
 */
function getCannonPivot() {
  const base = getCannonBasePos();
  return {
    x: base.x + (gameState.cannonSide === 'left' ? -60 : 60),
    y: base.y - 65
  };
}

/**
 * Draw the cannon base at the bottom of the screen.
 */
function drawCannonBase() {
  const base = getCannonBasePos();
  const img = loadImage('cannon-base', 'assets/images/cannon-base.svg');

  ctx.save();
  ctx.translate(base.x, base.y);
  ctx.drawImage(img, -img.width / 2, -img.height);
  ctx.restore();
}

/**
 * Draw the rotating cannon barrel around the beige pivot pin.
 */
function drawCannonBarrel() {
  const pivot = getCannonPivot();
  const img = loadImage('cannon-barrel', 'assets/images/cannon-barrel.svg');
  const angle = clamp(gameState.cannonAngle, 1, 90) * Math.PI / 180;

  ctx.save();
  ctx.translate(pivot.x, pivot.y);
  if (gameState.cannonSide === 'right') {
    ctx.scale(-1, 1);
    ctx.rotate(angle);
  } else {
    ctx.rotate(-angle);
  }
  // The barrel pivot in the SVG is at (15, 35).
  ctx.drawImage(img, -15, -35);
  ctx.restore();
}

/**
 * Draw a short windsock at the bottom of the screen next to the cannon base.
 * The pole is ~25 px to the right of the cannon base, ~50 px tall.
 * The sock only appears when wind is outside ±0.5, pointing in the wind direction.
 * Wind speed text sits to the right of the pole, 5 px above the bottom edge.
 */
function drawWindsock() {
  const base = getCannonBasePos();
  const poleX = base.x + (gameState.cannonSide === 'left' ? 100 : -100);
  const poleTopY = canvas.height - 60;
  const hasWind = Math.abs(gameState.wind) > 0.5;

  // Always draw the pole anchored at the bottom.
  const poleImg = loadImage('windsock-pole', 'assets/images/windsock-pole.svg');
  ctx.save();
  ctx.translate(poleX, poleTopY);
  ctx.drawImage(poleImg, 0, 0);
  ctx.restore();

  if (hasWind) {
    const windForce = clamp(Math.abs(gameState.wind) / 20, 0, 1);
    const scale = 0.6 + windForce * 0.4;
    const flip = gameState.wind < 0 ? -1 : 1;
    const sockImg = loadImage('windsock-sock', 'assets/images/windsock-sock.svg');

    ctx.save();
    ctx.translate(poleX + 5, poleTopY + 5);
    ctx.scale(flip * scale, scale);
    ctx.drawImage(sockImg, 0, 0);
    ctx.restore();
  }

  // Wind speed text to the right of the pole, near the bottom.
  const textX = poleX + 18;
  const textY = canvas.height - 8;
  drawOutlinedText(`${gameState.wind.toFixed(1)} mph`, textX, textY, 'bold 18px sans-serif');
}

/**
 * Draw the fuse spark before a shot.
 */
function drawFuse(progress) {
  const pivot = getCannonPivot();
  const angle = clamp(gameState.cannonAngle, 1, 90) * Math.PI / 180;
  const dir = gameState.cannonSide === 'left'
    ? { x: Math.cos(-angle), y: Math.sin(-angle) }
    : { x: -Math.cos(angle), y: Math.sin(angle) };

  // Barrel length from pivot to muzzle is roughly 145 px.
  const muzzleX = pivot.x + dir.x * 145;
  const muzzleY = pivot.y + dir.y * 145;

  ctx.fillStyle = '#ff9900';
  const radius = 4 + progress * 10;
  ctx.beginPath();
  ctx.arc(muzzleX, muzzleY, radius, 0, Math.PI * 2);
  ctx.fill();
}

/**
 * Draw the player queue above the cannon.
 */
function drawQueue() {
  const base = getCannonBasePos();
  const startY = canvas.height - 180;

  ctx.textAlign = 'center';
  for (let i = 0; i < gameState.queue.length; i++) {
    const entry = gameState.queue[i];
    const img = getPlayerImage(entry);
    const x = base.x;
    const y = startY - i * 60;

    ctx.save();
    ctx.translate(x, y);
    ctx.globalAlpha = 0.9;
    ctx.drawImage(img, -PROJECTILE_SIZE / 2, -PROJECTILE_SIZE / 2, PROJECTILE_SIZE, PROJECTILE_SIZE);
    ctx.globalAlpha = 1.0;
    ctx.restore();

    drawOutlinedText(entry.name, x, y + PROJECTILE_SIZE / 2 + 24, 'bold 20px sans-serif');
  }
}

/**
 * Draw text with white fill and black outline.
 * @param {string} text - Text.
 * @param {number} x - X.
 * @param {number} y - Y.
 * @param {string} font - Font string.
 */
function drawOutlinedText(text, x, y, font) {
  ctx.font = font;
  ctx.textAlign = 'center';
  ctx.lineWidth = 4;
  ctx.strokeStyle = '#000';
  ctx.strokeText(text, x, y);
  ctx.fillStyle = '#fff';
  ctx.fillText(text, x, y);
}

/**
 * Draw the player currently dropping into the cannon during the fuse.
 */
function drawFiringEntry() {
  if (!gameState.firingEntry || gameState.fuseProgress <= 0) return;

  const base = getCannonBasePos();
  const pivot = getCannonPivot();
  const startY = canvas.height - 180;
  const t = easeInQuad(gameState.fuseProgress);
  const x = base.x;
  const y = startY + (pivot.y - startY) * t;

  const img = getPlayerImage(gameState.firingEntry);
  ctx.save();
  ctx.translate(x, y);
  ctx.rotate(gameState.fuseProgress * Math.PI * 4);
  ctx.drawImage(img, -PROJECTILE_SIZE / 2, -PROJECTILE_SIZE / 2, PROJECTILE_SIZE, PROJECTILE_SIZE);
  ctx.restore();

  drawOutlinedText(gameState.firingEntry.name, x, y + PROJECTILE_SIZE / 2 + 24, 'bold 20px sans-serif');
}

/**
 * Ease-in quadratic for drop animation.
 * @param {number} t - 0 to 1.
 * @returns {number} Eased value.
 */
function easeInQuad(t) {
  return t * t;
}

/**
 * Draw active projectiles, handle wall bounce, and detect landings.
 */
function updateAndDrawProjectiles() {
  for (let i = gameState.projectiles.length - 1; i >= 0; i--) {
    const p = gameState.projectiles[i];
    const dt = 1 / 60;
    p.time += dt;

    let x = p.startX + p.velocityX * p.time;
    let y = p.startY + p.velocityY * p.time + 0.5 * GRAVITY * p.time * p.time;

    // Wall bounce once off the far wall.
    if (p.bounces < 1) {
      if (x < 0) {
        x = -x;
        p.velocityX = Math.abs(p.velocityX);
        p.startX = x;
        p.startY = y;
        p.time = 0;
        p.bounces += 1;
      } else if (x > canvas.width) {
        x = canvas.width - (x - canvas.width);
        p.velocityX = -Math.abs(p.velocityX);
        p.startX = x;
        p.startY = y;
        p.time = 0;
        p.bounces += 1;
      }
    }

    // Trail.
    const prevTime = p.time - dt;
    const prevX = p.startX + p.velocityX * prevTime;
    const prevY = p.startY + p.velocityY * prevTime + 0.5 * GRAVITY * prevTime * prevTime;
    ctx.strokeStyle = 'rgba(0, 0, 0, 0.3)';
    ctx.lineWidth = 4;
    ctx.beginPath();
    ctx.moveTo(prevX, prevY);
    ctx.lineTo(x, y);
    ctx.stroke();

    // Rotating icon while flying.
    p.rotation += p.rotationSpeed * dt;

    const img = getPlayerImage(p);
    ctx.save();
    ctx.translate(x, y);
    ctx.rotate(p.rotation);
    ctx.drawImage(img, -PROJECTILE_SIZE / 2, -PROJECTILE_SIZE / 2, PROJECTILE_SIZE, PROJECTILE_SIZE);
    ctx.restore();

    // Landing on the target/floor level.
    if (y >= gameState.targetY) {
      const score = calculateScore(x);

      /** @type {LandedShot} */
      const landed = {
        x,
        y: gameState.targetY,
        name: p.name,
        platform: p.platform,
        profileImageUrl: p.profileImageUrl,
        score
      };
      gameState.landedShots.push(landed);
      gameState.projectiles.splice(i, 1);

      if (score >= 0) {
        showScore(x, gameState.targetY, score);
        startPause(10000);
      } else {
        startPause(5000);
      }

      // Report shot result back to Streamer.bot (hit or miss).
      reportShotEnded(p.name, score, p.platform);
    } else if (x < 0 || x > canvas.width) {
      gameState.projectiles.splice(i, 1);
    }
  }
}

/**
 * Calculate score based on landing distance from target center.
 * @param {number} x - Landing X.
 * @returns {number} Score 1-100, or -1 if off target.
 */
function calculateScore(x) {
  const dx = Math.abs(x - gameState.targetX);
  const rx = TARGET_WIDTH / 2;
  if (dx > rx) return -1;
  const t = dx / rx;
  return Math.round(100 - t * 99);
}

/**
 * Start a pause after a shot lands.
 * @param {number} duration - Pause duration in milliseconds.
 */
function startPause(duration) {
  gameState.paused = true;
  gameState.pauseEnd = Date.now() + duration;
}

/**
 * End the current pause.
 */
function endPause() {
  gameState.paused = false;
}

/**
 * Show a score popup above the landing spot.
 * @param {number} x - X position.
 * @param {number} y - Y position.
 * @param {number} score - Score value.
 */
function showScore(x, y, score) {
  gameState.scoreText = `${score}`;
  gameState.scoreX = x;
  gameState.scoreY = y - PROJECTILE_SIZE / 2 - 20;
}

/**
 * Draw previously landed shots. Icons are right-side up.
 */
function drawLandedShots() {
  for (const shot of gameState.landedShots) {
    const img = getPlayerImage(shot);
    ctx.drawImage(img, shot.x - PROJECTILE_SIZE / 2, shot.y - PROJECTILE_SIZE / 2, PROJECTILE_SIZE, PROJECTILE_SIZE);

    drawOutlinedText(shot.name, shot.x, shot.y - PROJECTILE_SIZE / 2 - 12, 'bold 22px sans-serif');

    if (shot.score >= 0) {
      drawOutlinedText(`${shot.score} pts`, shot.x, shot.y - PROJECTILE_SIZE / 2 - 40, 'bold 18px sans-serif');
    }
  }
}

/**
 * Draw the score popup if active.
 */
function drawScorePopup() {
  if (!gameState.scoreText) return;
  drawOutlinedText(gameState.scoreText, gameState.scoreX, gameState.scoreY, 'bold 72px sans-serif');
}

/**
 * Report a shot result back to Streamer.bot.
 * @param {string} name - Player name.
 * @param {number} score - Score, or -1 for a miss.
 * @param {string} platform - Platform id.
 */
function reportShotEnded(name, score, platform) {
  if (!streamerbotClient) return;
  streamerbotClient.doAction('cannon-shot-ended', {
    userName: name,
    score,
    platform
  }).catch((err) => {
    console.warn('Failed to report shot ended:', err);
  });
}

/**
 * Normalize a player payload from C# into a QueueEntry.
 * @param {Object} p - Raw player payload.
 * @returns {QueueEntry} Normalized entry.
 */
function normalizePlayer(p) {
  return {
    name: p.name || 'Player',
    platform: (p.platform || 'twitch').toLowerCase(),
    angle: clamp(parseInt(p.angle, 10) || 45, 1, 90),
    power: clamp(parseInt(p.power, 10) || 50, 1, 100),
    profileImageUrl: p.profileImageUrl || null
  };
}

/**
 * Fire the first player in the queue.
 * Triggered by the 'fire' event from C#.
 * @param {QueueEntry} entry - Player to fire.
 */
function animateFire(entry) {
  playAudioFile(gameState.audioPaths.fuse || 'assets/sounds/fuse.mp3');
  gameState.fuseStartTime = Date.now();
  gameState.firingEntry = entry;

  const timer = setInterval(() => {
    const elapsed = Date.now() - gameState.fuseStartTime;
    gameState.fuseProgress = elapsed / FUSE_DURATION;

    if (elapsed >= FUSE_DURATION) {
      clearInterval(timer);
      gameState.fuseProgress = 0;
      gameState.firingEntry = null;
      launchProjectile(entry);
    }
  }, 16);
}

/**
 * Launch a projectile.
 * @param {QueueEntry} entry - Player entry.
 */
function launchProjectile(entry) {
  playAudioFile(gameState.audioPaths.fire || 'assets/sounds/cannon-fire.mp3');

  const angleRad = entry.angle * Math.PI / 180;
  const windBoost = gameState.wind * 5;
  const powerPx = entry.power * 11;
  const velocityX = (powerPx + windBoost) * Math.cos(angleRad);
  const velocityY = -powerPx * Math.sin(angleRad);

  const pivot = getCannonPivot();
  const startX = pivot.x + Math.cos(angleRad) * 145 * (gameState.cannonSide === 'left' ? 1 : -1);
  const startY = pivot.y - Math.sin(angleRad) * 145;

  /** @type {Projectile} */
  const projectile = {
    name: entry.name,
    platform: entry.platform,
    profileImageUrl: entry.profileImageUrl,
    startX,
    startY,
    velocityX,
    velocityY,
    time: 0,
    rotation: 0,
    rotationSpeed: (Math.random() * 4 + 2) * (Math.random() < 0.5 ? -1 : 1),
    bounces: 0
  };

  gameState.projectiles.push(projectile);
}

/**
 * Streamer.bot WebSocket client.
 * @type {any|null}
 */
let streamerbotClient = null;

/**
 * Audio file paths provided by C#.
 * @type {{fuse: string, fire: string, impact: string}}
 */
gameState.audioPaths = { fuse: '', fire: '', impact: '' };

/**
 * Append a message to the on-page debug overlay.
 * Only important events are shown so the overlay doesn't scroll too fast.
 * @param {string} message - Message to display.
 */
function debugOverlay(message) {
  const el = document.getElementById('debugOverlay');
  if (!el) return;

  // Suppress high-frequency wind updates; they still appear in the browser console.
  if (typeof message === 'string' && message.includes('Received event: wind')) {
    return;
  }

  const line = `[${new Date().toLocaleTimeString()}] ${message}`;
  el.textContent = `${line}\n${el.textContent}`;
  // Keep only the most recent 40 lines.
  const lines = el.textContent.split('\n').slice(0, 40);
  el.textContent = lines.join('\n');
}

/**
 * Read connection settings from URL query parameters.
 * @returns {{host: string, port: number, password: string|undefined}}
 */
function getConnectionSettings() {
  const params = new URLSearchParams(window.location.search);
  return {
    host: params.get('host') || '127.0.0.1',
    port: parseInt(params.get('port') || '8080', 10),
    password: params.get('password') || undefined
  };
}

/**
 * Connect to Streamer.bot and listen for game events.
 */
function connectStreamerbot() {
  if (typeof StreamerbotClient === 'undefined') {
    const msg = '[cannon] StreamerbotClient not available.';
    console.warn(msg);
    debugOverlay(msg);
    return;
  }

  const settings = getConnectionSettings();
  const url = `ws://${settings.host}:${settings.port}/`;
  const connectingMsg = `[cannon] Connecting to ${url}`;
  console.log(connectingMsg);
  debugOverlay(connectingMsg);

  const clientOptions = {
    host: settings.host,
    port: settings.port,
    endpoint: '/',
    autoReconnect: true,
    onConnect: () => {
      const msg = '[cannon] WebSocket connected.';
      console.log(msg);
      debugOverlay(msg);
    },
    onDisconnect: () => {
      const msg = '[cannon] WebSocket disconnected.';
      console.warn(msg);
      debugOverlay(msg);
    },
    onError: (err) => {
      const msg = `[cannon] WebSocket error: ${err?.message || err}`;
      console.error(msg, err);
      debugOverlay(msg);
    },
    onData: (raw) => {
      const source = raw?.event?.source || '?';
      const type = raw?.event?.type || '?';
      const msg = `[cannon] Raw data: ${source}.${type}`;
      console.log(msg, raw);
      debugOverlay(msg);
    }
  };

  if (settings.password) {
    clientOptions.password = settings.password;
  }

  streamerbotClient = new StreamerbotClient(clientOptions);

  streamerbotClient.on('General.Custom', (payload) => {
    // Streamer.bot wraps the broadcast in an envelope; our actual data is in payload.data.
    const data = payload && typeof payload === 'object' && 'data' in payload ? payload.data : payload;
    const eventName = typeof data?.event === 'string' ? data.event : 'unknown';
    const msg = `[cannon] Received event: ${eventName}`;
    console.log(msg, payload, data);
    debugOverlay(msg);
    handleEvent(data);
  });
}

/**
 * Normalize a player payload from C# into a QueueEntry.
 * @param {Object} p - Raw player payload.
 * @returns {QueueEntry} Normalized entry.
 */
function normalizePlayer(p) {
  return {
    name: p.name || 'Player',
    platform: (p.platform || 'twitch').toLowerCase(),
    angle: clamp(parseInt(p.angle, 10) || 45, 1, 90),
    power: clamp(parseInt(p.power, 10) || 50, 1, 100),
    profileImageUrl: p.profileImageUrl || null
  };
}

/**
 * Handle a C# game event.
 * @param {Object} data - Event payload.
 */
function handleEvent(data) {
  if (!data || !data.event) return;

  switch (data.event) {
    case 'setup':
      gameState.cannonSide = data.cannonSide === 'right' ? 'right' : 'left';
      gameState.cannonAngle = clamp(data.cannonAngle || 45, 1, 90);
      gameState.targetX = clamp(data.targetX || canvas.width / 2, TARGET_WIDTH / 2, canvas.width - TARGET_WIDTH / 2);
      gameState.targetY = data.targetY || canvas.height - TARGET_HEIGHT / 2;
      gameState.wind = clamp(data.wind || 0, -20, 20);
      if (data.audio) {
        gameState.audioPaths = {
          fuse: data.audio.fuse || '',
          fire: data.audio.fire || '',
          impact: data.audio.impact || ''
        };
      }
      gameState.initialized = true;
      break;

    case 'queue':
      if (Array.isArray(data.players)) {
        gameState.queue = data.players.map(p => normalizePlayer(p));
      }
      break;

    case 'wind':
      gameState.wind = clamp(data.wind || 0, -20, 20);
      break;

    case 'fire':
      if (!gameState.paused && gameState.projectiles.length === 0 && data.player) {
        const player = normalizePlayer(data.player);
        animateFire(player);
      }
      break;

    case 'impactSound':
      playAudioFile(gameState.audioPaths.impact || 'assets/sounds/land-clang.mp3');
      break;

    case 'clearLanded':
      gameState.landedShots = [];
      gameState.scoreText = null;
      break;

    default:
      break;
  }
}

/**
 * Main animation loop.
 */
function gameLoop() {
  // Handle pause expiration.
  if (gameState.paused && Date.now() >= gameState.pauseEnd) {
    endPause();
    gameState.scoreText = null;
  }

  // Clear the canvas.
  ctx.clearRect(0, 0, canvas.width, canvas.height);

  drawTarget();
  drawCannonBase();
  drawCannonBarrel();

  if (gameState.fuseProgress > 0 && gameState.fuseProgress < 1) {
    drawFuse(gameState.fuseProgress);
  }

  drawQueue();
  drawFiringEntry();

  if (!gameState.paused) {
    updateAndDrawProjectiles();
  }

  drawLandedShots();
  drawScorePopup();
  drawWindsock();

  requestAnimationFrame(gameLoop);
}

// Start the game loop once the DOM is ready.
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', () => {
    connectStreamerbot();
    gameLoop();
  });
} else {
  connectStreamerbot();
  gameLoop();
}
