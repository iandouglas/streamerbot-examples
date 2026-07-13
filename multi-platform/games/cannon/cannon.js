let audioContext;
let audioUnlocked = false;

/**
 * Initialize the Web Audio API context.
 */
function initAudio() {
  try {
    if (!audioContext) {
      audioContext = new (window.AudioContext || window.webkitAudioContext)();
    }
    if (audioContext.state === 'suspended') {
      audioContext.resume().catch(() => {});
    }
  } catch (e) {
    // Web Audio API not supported; fallback to HTMLAudioElement.
  }
}

/**
 * Unlock audio playback on the first user interaction.
 * Browsers block autoplay until the page has been clicked or touched.
 */
function unlockAudio() {
  if (audioUnlocked) return;
  audioUnlocked = true;
  initAudio();
  // Play a silent buffer to fully unlock the audio element.
  const silent = new Audio('data:audio/wav;base64,UklGRigAAABXQVZFZm10IBAAAAABAAEAQB8AAEAfAAABAAgAZGF0YQQAAAAAAA==');
  silent.play().catch(() => {});
}

/**
 * Play an audio file by path.
 * @param {string} path - Path to the audio file.
 */
function playAudioFile(path) {
  initAudio();
  const audio = new Audio(path);
  audio.play().catch(() => {});
}

// Canvas and drawing context.
const canvas = document.getElementById('gameCanvas');
const ctx = canvas.getContext('2d');

// Constants.
const FUSE_DURATION = 1500;
const TARGET_WIDTH = 500;
const TARGET_HEIGHT = 35;
const PROJECTILE_SIZE = 50;
const GRAVITY = 600; // pixels per second squared.
const MAX_LAUNCH_SPEED = 850; // px/s; prevents high-power shots from leaving the screen.
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
  displayQueue: [],
  loadingEntry: null,
  currentShotId: 0,
  projectiles: [],
  landedShots: [],
  paused: false,
  pauseEnd: 0,
  scoreText: null,
  scoreX: 0,
  scoreY: 0,
  connectedAt: 0,
  setupReceived: false
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
 * Fixed dimensions for SVG game assets.
 */
const ASSET_DIMS = {
  cannonBase: { width: 180, height: 110 },
  cannonBarrel: { width: 170, height: 70 }
};

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
 * The base SVG is 180x110 with the pivot pin at local (30, 45).
 * @returns {{x: number, y: number}} Pivot position.
 */
function getCannonPivot() {
  const base = getCannonBasePos();
  // On the left, pivot is 60 px left of center (x=30 in SVG).
  // On the right, flip the SVG so the pivot stays at the rear (60 px right of center).
  return {
    x: base.x + (gameState.cannonSide === 'left' ? -60 : 60),
    y: base.y - 65
  };
}

/**
 * Draw a thin gold stroke around the actual barrel shape.
 * The SVG path is M 15 18 L 155 20 L 155 50 L 15 52 Z.
 */
function drawBarrelOutline() {
  ctx.save();
  ctx.strokeStyle = '#ffd700';
  ctx.lineWidth = 2;
  ctx.beginPath();
  ctx.moveTo(0, -17);
  ctx.lineTo(140, -15);
  ctx.lineTo(140, 15);
  ctx.lineTo(0, 17);
  ctx.closePath();
  ctx.stroke();
  ctx.restore();
}

/**
 * Draw the cannon base at the bottom of the screen.
 */
function drawCannonBase() {
  const base = getCannonBasePos();
  const img = loadImage('cannon-base', 'assets/images/cannon-base.svg');
  const dims = ASSET_DIMS.cannonBase;

  ctx.save();
  ctx.translate(base.x, base.y);
  if (gameState.cannonSide === 'right') {
    ctx.scale(-1, 1);
  }
  // Anchor the SVG at its bottom center (90, 110).
  ctx.drawImage(img, -dims.width / 2, -dims.height, dims.width, dims.height);
  ctx.restore();
}

/**
 * Draw the rotating cannon barrel around the beige pivot pin.
 */
function drawCannonBarrel() {
  const pivot = getCannonPivot();
  const img = loadImage('cannon-barrel', 'assets/images/cannon-barrel.svg');
  const dims = ASSET_DIMS.cannonBarrel;
  const angle = clamp(gameState.cannonAngle, 1, 90) * Math.PI / 180;

  ctx.save();
  ctx.translate(pivot.x, pivot.y);
  if (gameState.cannonSide === 'right') {
    ctx.scale(-1, 1);
  }
  // Rotate the barrel up and away from the ground. The right-side scale mirrors
  // the drawing, so the same rotation direction keeps the barrel pointing outward.
  ctx.rotate(-angle);
  // The barrel pivot in the SVG is at (15, 35).
  ctx.drawImage(img, -15, -35, dims.width, dims.height);
  drawBarrelOutline();
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
  // Keep the pole well clear of the cannon base.
  const poleX = base.x + (gameState.cannonSide === 'left' ? 140 : -140);
  const poleTopY = canvas.height - 60;
  const hasWind = Math.abs(gameState.wind) > 0.5;

  // Always draw the pole anchored at the bottom.
  const poleImg = loadImage('windsock-pole', 'assets/images/windsock-pole.svg');
  const POLE_WIDTH = 10;
  const POLE_HEIGHT = 60;

  ctx.save();
  ctx.translate(poleX, poleTopY);
  ctx.drawImage(poleImg, 0, 0, POLE_WIDTH, POLE_HEIGHT);
  ctx.restore();

  if (hasWind) {
    const windForce = clamp(Math.abs(gameState.wind) / 20, 0, 1);
    // Sock is about half the original size and grows slightly with stronger wind.
    const scale = 0.35 + windForce * 0.15;
    // Point the sock with the wind. Positive wind blows right; negative blows left.
    const flip = gameState.wind < 0 ? -1 : 1;
    const sockImg = loadImage('windsock-sock', 'assets/images/windsock-sock.svg');
    const sockWidth = 130 * scale;

    ctx.save();
    // Attach the sock to the top of the pole. When flipped, translate so the attachment
    // point (left edge of the sock in its local space) stays at the pole top.
    const attachX = flip === 1 ? poleX + POLE_WIDTH - 2 : poleX - POLE_WIDTH + 2;
    ctx.translate(attachX, poleTopY + 5);
    ctx.scale(flip * scale, scale);
    ctx.drawImage(sockImg, 0, 0, 130, 60);
    ctx.restore();
  }

  // Wind speed text next to the pole, near the bottom. Speed is always positive;
  // the sock direction already indicates whether the wind is blowing left or right.
  const textX = poleX + (gameState.cannonSide === 'left' ? 28 : -28);
  const textY = canvas.height - 8;
  drawOutlinedText(`${Math.abs(gameState.wind).toFixed(1)} mph`, textX, textY, 'bold 18px sans-serif');
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
 * Compute the base Y coordinate for a queue slot. Slot 0 is the bottom slot
 * directly above the cannon; higher indices stack upward.
 * @param {number} slot - Slot index.
 * @returns {number} Y coordinate.
 */
function getQueueSlotY(slot) {
  return canvas.height - 260 - slot * 70;
}

/**
 * Synchronize the animated display queue with the logical queue from C#.
 * Existing entries keep their current Y and animate to their new slot.
 * Missing entries are removed; new entries start above the stack and drop in.
 */
function updateDisplayQueue() {
  const startY = getQueueSlotY(0);
  const incoming = gameState.queue.slice();
  const outgoing = gameState.displayQueue.slice();
  const next = [];

  for (let i = 0; i < incoming.length; i++) {
    const entry = incoming[i];
    const prevIndex = outgoing.findIndex((item) => item.entry.name === entry.name);
    let currentY;
    if (prevIndex >= 0) {
      currentY = outgoing[prevIndex].currentY;
      outgoing.splice(prevIndex, 1);
    } else {
      // New entry: drop in from above the top slot.
      currentY = startY - i * 70 - 70;
    }
    next.push({ entry, targetY: getQueueSlotY(i), currentY });
  }

  gameState.displayQueue = next;
}

/**
 * Animate each display queue entry toward its target slot.
 * @param {number} dt - Seconds since last frame.
 */
function animateDisplayQueue(dt) {
  const speed = 480; // pixels per second toward target.
  for (const item of gameState.displayQueue) {
    const dy = item.targetY - item.currentY;
    const step = speed * dt;
    if (Math.abs(dy) <= step) {
      item.currentY = item.targetY;
    } else {
      item.currentY += Math.sign(dy) * step;
    }
  }
}

/**
 * Draw the player queue above the cannon.
 * The player currently being fired is animated down from the queue into the
 * cannon, while the remaining names stay in their slots and shift smoothly.
 */
function drawQueue() {
  const base = getCannonBasePos();
  const nameOffset = PROJECTILE_SIZE / 2 + 10;
  const isLeft = gameState.cannonSide === 'left';

  ctx.textBaseline = 'middle';
  for (const item of gameState.displayQueue) {
    const entry = item.entry;
    const img = getPlayerImage(entry);
    const iconX = base.x;
    const y = item.currentY;

    ctx.save();
    ctx.translate(iconX, y);
    ctx.globalAlpha = 0.9;
    ctx.drawImage(img, -PROJECTILE_SIZE / 2, -PROJECTILE_SIZE / 2, PROJECTILE_SIZE, PROJECTILE_SIZE);
    ctx.globalAlpha = 1.0;
    ctx.restore();

    const nameX = isLeft ? iconX + nameOffset : iconX - nameOffset;
    const align = isLeft ? 'left' : 'right';
    drawOutlinedText(entry.name, nameX, y, 'bold 20px sans-serif', align);
  }

  ctx.textBaseline = 'alphabetic';
}

/**
 * Draw text with white fill and black outline.
 * @param {string} text - Text.
 * @param {number} x - X.
 * @param {number} y - Y.
 * @param {string} font - Font string.
 * @param {CanvasTextAlign} [align='center'] - Horizontal alignment.
 */
function drawOutlinedText(text, x, y, font, align = 'center') {
  ctx.font = font;
  ctx.textAlign = align;
  ctx.lineWidth = 4;
  ctx.strokeStyle = '#000';
  ctx.strokeText(text, x, y);
  ctx.fillStyle = '#fff';
  ctx.fillText(text, x, y);
}

/**
 * Draw the player currently dropping into the cannon during the fuse.
 * The icon stays upright while it is loaded; rotation only begins once it
 * is fired as a projectile.
 * @param {number} dt - Seconds since last frame.
 */
function drawFiringEntry(dt) {
  if (!gameState.loadingEntry) return;

  const pivot = getCannonPivot();
  const entry = gameState.loadingEntry;
  const targetY = pivot.y;
  const speed = 600; // pixels per second.
  const step = speed * dt;
  const dy = targetY - entry.currentY;

  if (Math.abs(dy) <= step) {
    entry.currentY = targetY;
  } else {
    entry.currentY += Math.sign(dy) * step;
  }

  const x = gameState.cannonSide === 'left'
    ? pivot.x - 15
    : pivot.x + 15;
  const y = entry.currentY;

  const img = getPlayerImage(entry);
  ctx.save();
  ctx.translate(x, y);
  ctx.drawImage(img, -PROJECTILE_SIZE / 2, -PROJECTILE_SIZE / 2, PROJECTILE_SIZE, PROJECTILE_SIZE);
  ctx.restore();

  drawOutlinedText(entry.name, x, y - PROJECTILE_SIZE / 2 - 12, 'bold 20px sans-serif');

  // Once the loading entry reaches the cannon pivot, the fuse can fire it.
  if (Math.abs(entry.currentY - targetY) < 1 && !entry.ready) {
    entry.ready = true;
  }
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
 * @param {number} dt - Seconds since last frame.
 */
function updateAndDrawProjectiles(dt) {
  for (let i = gameState.projectiles.length - 1; i >= 0; i--) {
    const p = gameState.projectiles[i];
    p.time += dt;

    let x = p.startX + p.velocityX * p.time;
    let y = p.startY + p.velocityY * p.time + 0.5 * GRAVITY * p.time * p.time;

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
        startPause(3000);
      } else {
        startPause(2000);
      }

      // Report shot result back to Streamer.bot (hit or miss).
      reportShotEnded(p.name, score, p.platform);
    } else if (x < 0 || x > canvas.width) {
      // Off-screen miss: report it so Streamer.bot can fire the next player.
      reportShotEnded(p.name, -1, p.platform);
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
 * Compute the highest score among all landed shots on the target.
 * @returns {number} Highest score, or -1 if no one hit the target.
 */
function getHighScore() {
  let max = -1;
  for (const shot of gameState.landedShots) {
    if (shot.score > max) max = shot.score;
  }
  return max;
}

/**
 * Draw previously landed shots. Icons are right-side up.
 * Misses (score < 0) are faded to 40% opacity.
 * Hits that are not the current high score are faded to 60% opacity so the
 * leader stands out. Shots are drawn in score order so the highest score is
 * always on top.
 */
function drawLandedShots() {
  const highScore = getHighScore();
  // Sort by score ascending so the highest score is rendered last (on top).
  const shots = gameState.landedShots.slice().sort((a, b) => a.score - b.score);

  for (const shot of shots) {
    const img = getPlayerImage(shot);
    const isMiss = shot.score < 0;
    const isLeader = shot.score >= 0 && shot.score === highScore;
    let alpha;
    if (isLeader) {
      alpha = 1.0;
    } else {
      alpha = 0.4;
    }

    ctx.save();
    ctx.globalAlpha = alpha;
    ctx.drawImage(img, shot.x - PROJECTILE_SIZE / 2, shot.y - PROJECTILE_SIZE / 2, PROJECTILE_SIZE, PROJECTILE_SIZE);
    drawOutlinedText(shot.name, shot.x, shot.y - PROJECTILE_SIZE / 2 - 12, 'bold 22px sans-serif');
    if (shot.score >= 0) {
      drawOutlinedText(`${shot.score} pts`, shot.x, shot.y - PROJECTILE_SIZE / 2 - 40, 'bold 18px sans-serif');
    }
    ctx.restore();
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
  if (!streamerbotClient || typeof streamerbotClient.doAction !== 'function') {
    return;
  }

  const args = { userName: name, score, platform, shotId: gameState.currentShotId, status: 'finished' };
  // The StreamerbotClient expects an action object with a name (or id).
  // Passing a bare string is interpreted as an action id GUID, which fails.
  const actionRef = { name: 'cannon-shot-ended' };

  streamerbotClient.doAction(actionRef, args)
    .catch(() => {
      // Retry once after a short delay in case the request was dropped.
      setTimeout(() => streamerbotClient.doAction(actionRef, args).catch(() => {}), 500);
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
function animateFire(entry, shotId) {
  playAudioFile(gameState.audioPaths.fuse || 'assets/sounds/fuse.mp3');
  gameState.fuseStartTime = Date.now();
  gameState.firingEntry = entry;
  gameState.currentShotId = shotId || 0;
  // Aim the cannon barrel at the player's chosen angle during the fuse.
  gameState.cannonAngle = clamp(entry.angle, 1, 90);

  // Move the fired player from the display queue into the loading slot.
  // The icon will drop down to the cannon pivot without spinning.
  const displayIndex = gameState.displayQueue.findIndex((item) => item.entry.name === entry.name);
  let startY = getQueueSlotY(0);
  if (displayIndex >= 0) {
    const item = gameState.displayQueue.splice(displayIndex, 1)[0];
    startY = item.currentY;
  }
  gameState.loadingEntry = { ...entry, currentY: startY, ready: false };

  const timer = setInterval(() => {
    const elapsed = Date.now() - gameState.fuseStartTime;
    gameState.fuseProgress = elapsed / FUSE_DURATION;

    if (elapsed >= FUSE_DURATION) {
      clearInterval(timer);
      gameState.fuseProgress = 0;
      gameState.firingEntry = null;
      gameState.loadingEntry = null;
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
  // Scale power so 50% power reaches roughly two-thirds of the screen, but cap
  // total launch speed so high-power/high-angle shots do not fly off the top.
  const rawPower = entry.power * 16;
  const powerPx = Math.min(rawPower, MAX_LAUNCH_SPEED);

  // For the right-side cannon, the barrel SVG is flipped horizontally, so the
  // muzzle still points away from the pivot. Use the same angle sign and let
  // the side determine the horizontal direction.
  const sideDir = gameState.cannonSide === 'left' ? 1 : -1;
  const velocityX = sideDir * powerPx * Math.cos(angleRad);
  const velocityY = -powerPx * Math.sin(angleRad);

  const pivot = getCannonPivot();
  const barrelLength = 145;
  const startX = pivot.x + sideDir * Math.cos(angleRad) * barrelLength;
  const startY = pivot.y - Math.sin(angleRad) * barrelLength;

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
    rotationSpeed: (Math.random() * 3 + 1.5) * (Math.random() < 0.5 ? -1 : 1),
    bounces: 0,
    wind: gameState.wind
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
let debugOverlayEnabled = true;

function debugOverlay(message) {
  if (!debugOverlayEnabled) return;

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
 * Also reads debug=1 to enable the on-page overlay.
 * @returns {{host: string, port: number, password: string|undefined}}
 */
function getConnectionSettings() {
  const params = new URLSearchParams(window.location.search);
  debugOverlayEnabled = params.get('debug') === '1';
  const el = document.getElementById('debugOverlay');
  if (el) el.style.display = debugOverlayEnabled ? 'block' : 'none';
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
    return;
  }

  const settings = getConnectionSettings();

  const clientOptions = {
    host: settings.host,
    port: settings.port,
    endpoint: '/',
    autoReconnect: true,
    onConnect: () => {
      // Mark connection time and clear stale local state so old queue data doesn't flash.
      gameState.connectedAt = Date.now();
      gameState.queue = [];
      gameState.landedShots = [];
      gameState.projectiles = [];

      // Notify Streamer.bot that the browser has loaded.
      streamerbotClient.doAction({ name: 'cannon-browser-loaded' }).catch(() => {});
    },
    onDisconnect: () => {},
    onError: () => {},
    onData: () => {}
  };

  if (settings.password) {
    clientOptions.password = settings.password;
  }

  streamerbotClient = new StreamerbotClient(clientOptions);

  streamerbotClient.on('General.Custom', (payload) => {
    // Streamer.bot wraps the broadcast in an envelope; our actual data is in payload.data.
    const data = payload && typeof payload === 'object' && 'data' in payload ? payload.data : payload;
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
      gameState.setupReceived = true;
      break;

    case 'wind':
      gameState.wind = clamp(data.wind || 0, -20, 20);
      break;

    case 'fire':
      if (!gameState.paused && gameState.projectiles.length === 0 && data.player) {
        const player = normalizePlayer(data.player);
        animateFire(player, data.shotId);
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
 * Uses time-based animation so the game runs at the same speed regardless of
 * the browser source frame rate.
 */
let lastFrameTime = 0;
function gameLoop(timestamp) {
  if (!lastFrameTime) lastFrameTime = timestamp;
  const dt = Math.min((timestamp - lastFrameTime) / 1000, 0.1); // seconds, clamped to avoid huge jumps.
  lastFrameTime = timestamp;

  // Handle pause expiration.
  if (gameState.paused && Date.now() >= gameState.pauseEnd) {
    endPause();
    gameState.scoreText = null;
  }

  animateDisplayQueue(dt);

  // Clear the canvas.
  ctx.clearRect(0, 0, canvas.width, canvas.height);

  drawTarget();
  drawCannonBase();
  drawCannonBarrel();

  if (gameState.fuseProgress > 0 && gameState.fuseProgress < 1) {
    drawFuse(gameState.fuseProgress);
  }

  drawFiringEntry(dt);

  if (!gameState.paused) {
    updateAndDrawProjectiles(dt);
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
    requestAnimationFrame(gameLoop);
    document.addEventListener('click', unlockAudio, { once: true });
    document.addEventListener('touchstart', unlockAudio, { once: true });
  });
} else {
  connectStreamerbot();
  requestAnimationFrame(gameLoop);
  document.addEventListener('click', unlockAudio, { once: true });
  document.addEventListener('touchstart', unlockAudio, { once: true });
}
