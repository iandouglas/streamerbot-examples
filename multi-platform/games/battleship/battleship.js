'use strict';

// ============================================================================
// Battleship - browser renderer
// All game logic lives in Streamer.bot C# actions.
// This page is purely a renderer + audio player that listens for WebSocket events.
// ============================================================================

// --- Canvas layers ---
const waterCanvas = document.getElementById('waterCanvas');
const shipCanvas  = document.getElementById('shipCanvas');
const fogCanvas   = document.getElementById('fogCanvas');
const hudCanvas   = document.getElementById('hudCanvas');
const waterCtx = waterCanvas.getContext('2d');
const shipCtx  = shipCanvas.getContext('2d');
const fogCtx   = fogCanvas.getContext('2d');
const hudCtx   = hudCanvas.getContext('2d');

// --- Audio ---
let audioContext = null;
let audioUnlocked = false;

function initAudio() {
  try {
    if (!audioContext) audioContext = new (window.AudioContext || window.webkitAudioContext)();
    if (audioContext.state === 'suspended') audioContext.resume().catch(() => {});
  } catch (e) { /* fallback to HTMLAudioElement */ }
}

function unlockAudio() {
  if (audioUnlocked) return;
  audioUnlocked = true;
  initAudio();
  const silent = new Audio('data:audio/wav;base64,UklGRigAAABXQVZFZm10IBAAAAABAAEAQB8AAEAfAAABAAgAZGF0YQQAAAAAAA==');
  silent.play().catch(() => {});
}

function playAudioFile(path) {
  initAudio();
  const audio = new Audio(path);
  audio.play().catch(() => {});
  return audio;
}

// --- Image cache ---
const imageCache = {};

function loadImage(key, src) {
  if (!imageCache[key]) {
    const img = new Image();
    img.onload = () => {
      debugOverlay(`image loaded: ${key} (${src})`);
      // Redraw all layers once images are available
      if (gameState.initialized) {
        drawWater();
        drawShipLayer();
        drawFog();
      }
    };
    img.onerror = () => {
      debugOverlay(`IMAGE FAILED: ${key} (${src})`);
      setStatus(`Image failed: ${src}`);
    };
    img.src = src;
    imageCache[key] = img;
  }
  return imageCache[key];
}

// --- Sprite paths ---
const SPRITES = {
  water: 'assets/images/water.png',
  fog: 'assets/images/fog.png',
  pegWhite: 'assets/images/peg-white.svg',
  pegRed: 'assets/images/peg-red.svg',
  mine: 'assets/images/mine.png',
  bomber: 'assets/images/bomber.png',
  bomb: 'assets/images/bomb.svg',
  ships: [
    'assets/images/carrier-horiz.png',
    'assets/images/battleship-horiz.png',
    'assets/images/cruiser-horiz.png',
    'assets/images/submarine-horiz.png',
    'assets/images/destroyer-horiz.png'
  ]
};

// Preload critical images at startup
loadImage('bomb', SPRITES.bomb);
loadImage('bomber', SPRITES.bomber);
loadImage('mine', SPRITES.mine);
loadImage('water', SPRITES.water);
loadImage('fog', SPRITES.fog);

const SHIP_NAMES = ['Carrier', 'Battleship', 'Cruiser', 'Submarine', 'Destroyer'];
const SHIP_SIZES = [5, 4, 3, 3, 2];

// --- Game state ---
const gameState = {
  initialized: false,
  mode: 'normal',
  gridSize: 10,
  boardSize: 1080,
  boardOffsetX: 420,
  boardOffsetY: 0,
  cellSize: 108,
  round: 0,
  endsAt: 0,
  duration: 30,
  collecting: false,
  coords: [],
  players: {},
  mutedPlayers: [],
  ships: [],
  mines: [],
  shots: {},
  crosshair: { row: 0, col: 0, visible: false, blinkPhase: 0 },
  bomber: null,
  audioPlaying: null,
  phase: 'idle',
  gameEnded: false
};

// --- Board geometry ---
const LABEL_GUTTER = 80; // pixels reserved for coordinate labels at top and left

function recalcGeometry() {
  // Reserve gutter on top and left for labels; board fills remaining space
  const availWidth = 1920 - 420 - 420 - LABEL_GUTTER; // minus left/right panels minus left gutter
  const availHeight = 1080 - LABEL_GUTTER;             // minus top gutter
  const maxBoardSize = Math.min(availWidth, availHeight);
  gameState.cellSize = Math.floor(maxBoardSize / gameState.gridSize);
  const actualBoardSize = gameState.cellSize * gameState.gridSize;
  // Center the board in the space between the panels, offset down by the gutter
  const centerX = 420 + (1920 - 420 - 420) / 2;
  gameState.boardOffsetX = Math.floor(centerX - actualBoardSize / 2) + Math.floor(LABEL_GUTTER / 2);
  gameState.boardOffsetY = LABEL_GUTTER + Math.floor((1080 - LABEL_GUTTER - actualBoardSize) / 2);
}

function cellToPixel(row, col) {
  return {
    x: gameState.boardOffsetX + col * gameState.cellSize,
    y: gameState.boardOffsetY + row * gameState.cellSize
  };
}

function cellCenter(row, col) {
  return {
    x: gameState.boardOffsetX + col * gameState.cellSize + gameState.cellSize / 2,
    y: gameState.boardOffsetY + row * gameState.cellSize + gameState.cellSize / 2
  };
}

// --- Drawing helpers ---
function drawOutlinedText(ctx, text, x, y, font, align) {
  ctx.font = font;
  ctx.textAlign = align || 'center';
  ctx.lineWidth = 4;
  ctx.strokeStyle = '#000';
  ctx.strokeText(text, x, y);
  ctx.fillStyle = '#fff';
  ctx.fillText(text, x, y);
}

// --- Layer 1: Water + grid ---
function drawWater() {
  waterCtx.clearRect(0, 0, 1920, 1080);

  const waterImg = loadImage('water', SPRITES.water);
  const cs = gameState.cellSize;

  for (let row = 0; row < gameState.gridSize; row++) {
    for (let col = 0; col < gameState.gridSize; col++) {
      const p = cellToPixel(row, col);
      waterCtx.drawImage(waterImg, p.x, p.y, cs, cs);
    }
  }

  // Grid lines only — labels are drawn on the HUD layer every frame
  waterCtx.strokeStyle = 'rgba(255,255,255,0.3)';
  waterCtx.lineWidth = 1;
  for (let i = 0; i <= gameState.gridSize; i++) {
    const x = gameState.boardOffsetX + i * cs;
    const y = gameState.boardOffsetY + i * cs;
    waterCtx.beginPath();
    waterCtx.moveTo(x, gameState.boardOffsetY);
    waterCtx.lineTo(x, gameState.boardOffsetY + gameState.gridSize * cs);
    waterCtx.stroke();
    waterCtx.beginPath();
    waterCtx.moveTo(gameState.boardOffsetX, y);
    waterCtx.lineTo(gameState.boardOffsetX + gameState.gridSize * cs, y);
    waterCtx.stroke();
  }
}

// --- Layer 2: Ships, pegs, mines ---

/**
 * Draw a full ship sprite across all its cells.
 * Ship sprites are horizontally oriented (wider than tall).
 * For horizontal ship placement, draw as-is.
 * For vertical ship placement, rotate 90° clockwise.
 * The sprite is stretched to fit the ship's cell area.
 */
function drawFullShip(sprite, ship, cs) {
  if (!sprite || !sprite.complete || sprite.naturalWidth === 0) return;

  const isVertical = ship.cells.length > 1 && ship.cells[0].col === ship.cells[1].col;
  const shipLen = ship.cells.length;
  const minRow = Math.min(...ship.cells.map(c => c.row));
  const minCol = Math.min(...ship.cells.map(c => c.col));
  const p = cellToPixel(minRow, minCol);

  // Carrier stays full width; other ships are 20% skinnier, centered in the cell
  const isCarrier = ship.id === 0;
  const thickness = isCarrier ? cs : cs * 0.8;
  const thicknessOffset = (cs - thickness) / 2;

  if (!isVertical) {
    // Horizontal ship: sprite is horizontal, draw as-is
    shipCtx.drawImage(sprite, p.x, p.y + thicknessOffset, shipLen * cs, thickness);
  } else {
    // Vertical ship: rotate 90° clockwise
    shipCtx.save();
    shipCtx.translate(p.x + cs, p.y);
    shipCtx.rotate(Math.PI / 2);
    // After +90° rotation: x-axis points down, y-axis points right
    // drawImage width = screen height (length), height = screen width (thickness)
    shipCtx.drawImage(sprite, thicknessOffset, 0, shipLen * cs, thickness);
    shipCtx.restore();
  }
}

function drawShipLayer() {
  shipCtx.clearRect(0, 0, 1920, 1080);
  const cs = gameState.cellSize;

  // Draw sunk ships (full sprite) with red pegs only on actually-hit cells
  for (const ship of gameState.ships) {
    if (ship.sunk) {
      const sprite = loadImage('ship-' + ship.id, SPRITES.ships[ship.id]);
      drawFullShip(sprite, ship, cs);

      // Red peg only on cells that were actually hit during play
      const peg = loadImage('peg-red', SPRITES.pegRed);
      const pegSize = Math.floor(cs * 0.6);
      for (const cell of ship.cells) {
        if (!cell.hit) continue;
        const p = cellToPixel(cell.row, cell.col);
        shipCtx.drawImage(peg, p.x + (cs - pegSize) / 2, p.y + (cs - pegSize) / 2, pegSize, pegSize);
      }
    }
  }

  // Draw pegs and mines for shot cells
  for (const key in gameState.shots) {
    const shot = gameState.shots[key];
    const p = cellToPixel(shot.row, shot.col);
    const iconSize = Math.floor(cs * 0.6);

    if (shot.result === 'miss') {
      const peg = loadImage('peg-white', SPRITES.pegWhite);
      shipCtx.drawImage(peg, p.x + (cs - iconSize) / 2, p.y + (cs - iconSize) / 2, iconSize, iconSize);
    } else if (shot.result === 'hit' && !shot.shipSunk) {
      const peg = loadImage('peg-red', SPRITES.pegRed);
      shipCtx.drawImage(peg, p.x + (cs - iconSize) / 2, p.y + (cs - iconSize) / 2, iconSize, iconSize);
    } else if (shot.result === 'mine') {
      const mine = loadImage('mine', SPRITES.mine);
      shipCtx.drawImage(mine, p.x + (cs - iconSize) / 2, p.y + (cs - iconSize) / 2, iconSize, iconSize);
    }
  }

  // At game end, reveal all unhit mines on the board
  if (gameState.gameEnded) {
    const mineIcon = loadImage('mine', SPRITES.mine);
    const iconSize = Math.floor(cs * 0.6);
    for (const m of gameState.mines) {
      const key = m.row + ',' + m.col;
      if (gameState.shots[key]) continue; // already drawn above
      const p = cellToPixel(m.row, m.col);
      shipCtx.drawImage(mineIcon, p.x + (cs - iconSize) / 2, p.y + (cs - iconSize) / 2, iconSize, iconSize);
    }
  }
}

// --- Layer 3: Fog ---
function drawFog() {
  fogCtx.clearRect(0, 0, 1920, 1080);
  if (gameState.gameEnded) return;

  const fogImg = loadImage('fog', SPRITES.fog);
  const cs = gameState.cellSize;

  for (let row = 0; row < gameState.gridSize; row++) {
    for (let col = 0; col < gameState.gridSize; col++) {
      const key = row + ',' + col;
      if (gameState.shots[key]) continue;
      const p = cellToPixel(row, col);
      fogCtx.drawImage(fogImg, p.x, p.y, cs, cs);
    }
  }
}

// --- Layer 4: HUD (crosshair, bomber, bomb) ---
function drawHud(dt) {
  hudCtx.clearRect(0, 0, 1920, 1080);

  // Coordinate labels — drawn every frame on the HUD layer so they're always visible
  if (gameState.initialized) {
    const cs = gameState.cellSize;
    const labelSize = Math.floor(cs * 0.4);
    hudCtx.font = `bold ${labelSize}px 'Courier New', monospace`;
    hudCtx.textAlign = 'center';
    hudCtx.textBaseline = 'middle';
    hudCtx.lineWidth = 5;
    hudCtx.strokeStyle = '#000';
    hudCtx.fillStyle = '#ffff00';

    // Column numbers — centered in the gutter above the board
    const labelY = Math.floor(gameState.boardOffsetY / 2);
    for (let col = 0; col < gameState.gridSize; col++) {
      const cx = gameState.boardOffsetX + col * cs + cs / 2;
      hudCtx.strokeText(String(col + 1), cx, labelY);
      hudCtx.fillText(String(col + 1), cx, labelY);
    }

    // Row letters — centered in the gutter to the left of the board
    const labelX = Math.floor(gameState.boardOffsetX - LABEL_GUTTER / 2);
    for (let row = 0; row < gameState.gridSize; row++) {
      const cy = gameState.boardOffsetY + row * cs + cs / 2;
      hudCtx.strokeText(String.fromCharCode(65 + row), labelX, cy);
      hudCtx.fillText(String.fromCharCode(65 + row), labelX, cy);
    }
  }

  // Crosshair lines
  if (gameState.crosshair.visible) {
    const cs = gameState.cellSize;
    const cx = gameState.boardOffsetX + gameState.crosshair.col * cs + cs / 2;
    const cy = gameState.boardOffsetY + gameState.crosshair.row * cs + cs / 2;

    let alpha = 1.0;
    if (gameState.crosshair.blinkPhase > 0) {
      alpha = Math.sin(gameState.crosshair.blinkPhase * Math.PI * 6) * 0.5 + 0.5;
    }

    hudCtx.strokeStyle = `rgba(255, 0, 0, ${alpha})`;
    hudCtx.lineWidth = 4;

    // Vertical line
    hudCtx.beginPath();
    hudCtx.moveTo(cx, gameState.boardOffsetY);
    hudCtx.lineTo(cx, gameState.boardOffsetY + gameState.gridSize * cs);
    hudCtx.stroke();

    // Horizontal line
    hudCtx.beginPath();
    hudCtx.moveTo(gameState.boardOffsetX, cy);
    hudCtx.lineTo(gameState.boardOffsetX + gameState.gridSize * cs, cy);
    hudCtx.stroke();

    // Intersection circle
    hudCtx.strokeStyle = `rgba(255, 50, 50, ${alpha})`;
    hudCtx.lineWidth = 3;
    hudCtx.beginPath();
    hudCtx.arc(cx, cy, cs * 0.45, 0, Math.PI * 2);
    hudCtx.stroke();
  }

  // Bomber animation
  if (gameState.bomber) {
    updateBomber(dt);
    drawBomber();
  }
}

// --- Live averaging ---
function computeAverage() {
  if (gameState.coords.length === 0) return null;
  let rowSum = 0, colSum = 0;
  for (const c of gameState.coords) {
    rowSum += c.row;
    colSum += c.col;
  }
  // Use round-half-up to match the game's intended averaging rule
  const avgRow = Math.floor(rowSum / gameState.coords.length + 0.5);
  const avgCol = Math.floor(colSum / gameState.coords.length + 0.5);
  return {
    row: Math.max(0, Math.min(gameState.gridSize - 1, avgRow)),
    col: Math.max(0, Math.min(gameState.gridSize - 1, avgCol))
  };
}

// --- Bomber animation ---
function startBomber(targetRow, targetCol, audioPath, onComplete) {
  const target = cellCenter(targetRow, targetCol);
  const cs = gameState.cellSize;
  const gridSize = gameState.gridSize;

  const midCol = Math.floor(gridSize / 2);
  const midRow = Math.floor(gridSize / 2);

  let startX, startY;
  const margin = 100;

  if (targetCol < midCol) {
    startX = gameState.boardOffsetX + gridSize * cs + margin;
    if (targetRow < midRow) {
      startY = gameState.boardOffsetY + gridSize * cs + margin;
    } else {
      startY = gameState.boardOffsetY - margin;
    }
  } else {
    startX = gameState.boardOffsetX - margin;
    if (targetRow < midRow) {
      startY = gameState.boardOffsetY + gridSize * cs + margin;
    } else {
      startY = gameState.boardOffsetY - margin;
    }
  }

  const dx = target.x - startX;
  const dy = target.y - startY;
  const angle = Math.atan2(dy, dx);

  // Plane reaches target at 7s, is past it by 8s
  const arriveTime = 7.0;

  gameState.bomber = {
    startX, startY, targetX: target.x, targetY: target.y,
    angle, elapsed: 0, phase: 'flying',
    targetRow, targetCol, audioPath, onComplete,
    arriveTime,
    bombDropped: false, bomb: null,
    opacity: 1.0
  };

  if (audioPath) {
    gameState.audioPlaying = playAudioFile(audioPath);
  }
}

function updateBomber(dt) {
  const b = gameState.bomber;
  if (!b) return;
  b.elapsed += dt;

  if (b.phase === 'flying') {
    if (b.elapsed >= b.arriveTime) {
      b.phase = 'fading';
      b.fadeStart = b.elapsed;
    }
  }

  // Drop bomb at 8s — plane is already past the target, bomb sound starts in audio
  if (!b.bombDropped && b.elapsed >= 7.5) {
    b.bombDropped = true;
    const center = cellCenter(b.targetRow, b.targetCol);
    b.bomb = { elapsed: 0, fallTime: 1.75, centerX: center.x, centerY: center.y };
  }

  if (b.phase === 'fading') {
    const fadeElapsed = b.elapsed - b.fadeStart;
    b.opacity = Math.max(0, 1.0 - fadeElapsed / 3.0);

    const fadeDist = gameState.cellSize * 4;
    const moveDir = { x: Math.cos(b.angle), y: Math.sin(b.angle) };
    b.targetX += moveDir.x * (fadeDist / 3.0) * dt;
    b.targetY += moveDir.y * (fadeDist / 3.0) * dt;

    if (b.elapsed >= 11.0) {
      b.phase = 'done';
    }
  }

  // Update bomb — spirals inward within the target cell over 1.75s
  if (b.bomb) {
    b.bomb.elapsed += dt;
    if (b.bomb.elapsed >= b.bomb.fallTime) {
      b.bomb = null;
    }
  }

  if (b.phase === 'done') {
    const cb = b.onComplete;
    gameState.bomber = null;
    if (cb) cb();
  }
}

function drawBomber() {
  const b = gameState.bomber;
  if (!b) return;

  const bomberImg = loadImage('bomber', SPRITES.bomber);
  const bomberSize = gameState.cellSize * 1.575;

  let bx, by;
  if (b.phase === 'flying') {
    const t = b.elapsed / b.arriveTime;
    bx = b.startX + (b.targetX - b.startX) * t;
    by = b.startY + (b.targetY - b.startY) * t;
  } else {
    bx = b.targetX;
    by = b.targetY;
  }

  hudCtx.save();
  hudCtx.globalAlpha = b.opacity;
  hudCtx.translate(bx, by);
  hudCtx.rotate(b.angle + Math.PI / 2);
  hudCtx.drawImage(bomberImg, -bomberSize / 2, -bomberSize / 2, bomberSize, bomberSize);
  hudCtx.restore();

  if (b.bomb) {
    const bombImg = loadImage('bomb', SPRITES.bomb);
    const progress = Math.min(1, b.bomb.elapsed / b.bomb.fallTime);
    const cs = gameState.cellSize;
    // Spiral: 2 full rotations, radius shrinks from 0.4 to 0 of cell size
    const angle = progress * Math.PI * 4; // 2 full rotations
    const radius = cs * 0.4 * (1 - progress);
    const bx = b.bomb.centerX + Math.cos(angle) * radius;
    const by = b.bomb.centerY + Math.sin(angle) * radius;
    // Size shrinks from 0.5 to 0.05 of cell size
    const bombSize = cs * (0.5 - progress * 0.45);
    if (bombSize < 1) return;
    hudCtx.save();
    hudCtx.globalAlpha = 1.0; // always 100% opacity
    hudCtx.drawImage(bombImg, bx - bombSize / 2, by - bombSize / 2, bombSize, bombSize);
    hudCtx.restore();
  }
}

// --- Leaderboard ---
function updateLeaderboard() {
  const el = document.getElementById('leaderboard');
  if (!el) return;

  const maxPlayers = 10;
  const players = Object.values(gameState.players).sort((a, b) => b.coordCount - a.coordCount).slice(0, maxPlayers);

  let html = '<div class="heading">Top 10 Players</div>';
  const showMuted = gameState.mode !== 'easy';
  for (const p of players) {
    const iconSrc = `assets/images/emote-${p.platform || 'twitch'}.png`;
    const mutedClass = gameState.mutedPlayers.some(m => m.user === p.user && m.platform === p.platform) ? ' muted' : '';
    const statsLine = showMuted
      ? `coords: ${p.coordCount}, muted: ${p.muteCount}`
      : `coords: ${p.coordCount}`;
    html += `<div class="entry${mutedClass}">
      <div class="row1">
        <img class="platform-icon" src="${iconSrc}" onerror="this.style.display='none'">
        <span class="name">${escapeHtml(p.user)}</span>
      </div>
      <div class="stats">${statsLine}</div>
    </div>`;
  }
  el.innerHTML = html;
}

function escapeHtml(s) {
  const div = document.createElement('div');
  div.textContent = s;
  return div.innerHTML;
}

// --- Right panel: ship roster, mines, round info ---
function updateRightPanel() {
  const shipRoster = document.getElementById('shipRoster');
  if (shipRoster) {
    let html = '';
    for (let i = 0; i < gameState.ships.length; i++) {
      const ship = gameState.ships[i];
      const sunkClass = ship.sunk ? ' sunk' : '';
      html += `<div class="ship-entry${sunkClass}">
        <img class="ship-icon" src="${SPRITES.ships[i]}" alt="">
        <div class="ship-label">${SHIP_NAMES[i]} (${SHIP_SIZES[i]})</div>
      </div>`;
    }
    shipRoster.innerHTML = html;
  }

  const mineRoster = document.getElementById('mineRoster');
  if (mineRoster) {
    if (gameState.mines.length === 0) {
      mineRoster.innerHTML = '';
    } else {
      let html = '';
      let hitCount = 0;
      for (let i = 0; i < gameState.mines.length; i++) {
        const hit = gameState.mines[i].hit ? ' hit' : '';
        if (gameState.mines[i].hit) hitCount++;
        html += `<div class="mine-icon${hit}"><img src="${SPRITES.mine}" alt=""></div>`;
      }
      mineRoster.innerHTML = `<div class="mine-header">Mines: ${hitCount}/${gameState.mines.length}</div><div class="mine-grid">` + html + `</div>`;
    }
  }

  document.getElementById('modeLabel').textContent = gameState.mode.toUpperCase();
  document.getElementById('roundNumber').textContent = `Round ${gameState.round}`;
}

function updateCountdown() {
  const el = document.getElementById('countdown');
  if (!el) return;
  if (!gameState.collecting || gameState.endsAt === 0) {
    el.textContent = '';
    el.classList.remove('urgent');
    return;
  }
  const remaining = Math.max(0, Math.ceil((gameState.endsAt - Date.now()) / 1000));
  el.textContent = remaining;
  if (remaining <= 5) el.classList.add('urgent');
  else el.classList.remove('urgent');
}

// --- WebSocket client ---
let streamerbotClient = null;

function getConnectionSettings() {
  const params = new URLSearchParams(window.location.search);
  const debug = params.get('debug') === '1';
  const el = document.getElementById('debugOverlay');
  if (el) el.style.display = debug ? 'block' : 'none';
  return {
    host: params.get('host') || '127.0.0.1',
    port: parseInt(params.get('port') || '8080', 10),
    password: params.get('password') || undefined,
    debug
  };
}

function setStatus(msg) {
  const el = document.getElementById('status');
  if (el) el.textContent = msg;
}

function clearStatus() {
  const el = document.getElementById('status');
  if (el) el.textContent = '';
}

function connectStreamerbot() {
  if (typeof StreamerbotClient === 'undefined') {
    setStatus('StreamerbotClient not found — check assets/js/streamerbot-client.js');
    return;
  }

  const settings = getConnectionSettings();

  const clientOptions = {
    host: settings.host,
    port: settings.port,
    endpoint: '/',
    autoReconnect: true,
    onConnect: () => {
      debugOverlay('Connected to Streamer.bot');
      setStatus('Connected. Requesting game state...');
      streamerbotClient.doAction({ name: 'battleship-browser-loaded' })
        .then(() => { debugOverlay('browser-loaded action called successfully'); })
        .catch((err) => {
          debugOverlay(`browser-loaded action FAILED: ${err}`);
          setStatus('Failed to call battleship-browser-loaded action');
        });
    },
    onDisconnect: () => { debugOverlay('Disconnected from Streamer.bot'); setStatus('Disconnected from Streamer.bot'); },
    onError: () => { setStatus('WebSocket error'); },
    onData: () => {}
  };

  if (settings.password) clientOptions.password = settings.password;
  streamerbotClient = new StreamerbotClient(clientOptions);

  streamerbotClient.on('General.Custom', (payload) => {
    debugOverlay(`WS raw payload: ${JSON.stringify(payload).substring(0, 200)}`);
    const data = payload && typeof payload === 'object' && 'data' in payload ? payload.data : payload;
    debugOverlay(`WS extracted data: ${JSON.stringify(data).substring(0, 200)}`);
    handleEvent(data);
  });
}

function reportBomberComplete(round) {
  if (!streamerbotClient || typeof streamerbotClient.doAction !== 'function') {
    debugOverlay('reportBomberComplete: no streamerbotClient');
    return;
  }
  debugOverlay('reportBomberComplete: calling battleship-bomber-complete');
  streamerbotClient.doAction({ name: 'battleship-bomber-complete' }, { round })
    .then(() => { debugOverlay('reportBomberComplete: success'); })
    .catch((err) => { debugOverlay(`reportBomberComplete: FAILED: ${err}`); });
}

// --- Debug overlay ---
let debugOverlayEnabled = false;
function debugOverlay(msg) {
  if (!debugOverlayEnabled) return;
  const el = document.getElementById('debugOverlay');
  if (!el) return;
  const line = `[${new Date().toLocaleTimeString()}] ${msg}`;
  el.textContent = `${line}\n${el.textContent}`;
  const lines = el.textContent.split('\n').slice(0, 40);
  el.textContent = lines.join('\n');
}

// --- Event handler ---
function handleEvent(data) {
  if (!data || !data.event) {
    debugOverlay('handleEvent: data or data.event is null');
    setStatus('Received WebSocket data but no event field found');
    return;
  }
  debugOverlay(`Received event: ${data.event}`);

  switch (data.event) {
    case 'setup':
      handleSetup(data);
      break;
    case 'round-start':
      handleRoundStart(data);
      break;
    case 'coord':
      handleCoord(data);
      break;
    case 'round-end':
      handleRoundEnd(data);
      break;
    case 'shot-resolve':
      handleShotResolve(data);
      break;
    case 'game-end':
      handleGameEnd(data);
      break;
    default:
      break;
  }
}

function handleSetup(data) {
  setStatus('');
  debugOverlay(`setup: mode=${data.mode} gridSize=${data.gridSize} ships=${(data.ships||[]).length} mines=${(data.mines||[]).length}`);
  gameState.mode = data.mode || 'normal';
  gameState.gridSize = data.gridSize || 10;
  gameState.boardSize = 1080;
  recalcGeometry();
  gameState.ships = (data.ships || []).map((s, i) => ({
    id: i,
    name: s.name || SHIP_NAMES[i] || 'Ship',
    cells: s.cells || [],
    sunk: false
  }));
  gameState.mines = (data.mines || []).map(m => ({ row: m.row, col: m.col, hit: false }));
  gameState.shots = {};
  gameState.coords = [];
  gameState.players = {};
  gameState.mutedPlayers = [];
  gameState.round = 0;
  gameState.gameEnded = false;
  gameState.initialized = true;
  gameState.phase = 'setup';

  drawWater();
  drawShipLayer();
  drawFog();
  updateRightPanel();
  updateLeaderboard();

  // Play game start audio only on initial setup, not on browser-loaded resend
  if (data.playAudio !== false)
    playAudioFile('assets/sounds/__game-start.mp3');
  debugOverlay(`Game setup: mode=${gameState.mode}, grid=${gameState.gridSize}x${gameState.gridSize}, playAudio=${data.playAudio !== false}`);
}

function handleRoundStart(data) {
  gameState.round = data.round || (gameState.round + 1);
  gameState.endsAt = data.endsAt || (Date.now() + (data.duration || 30) * 1000);
  gameState.duration = data.duration || 30;
  gameState.collecting = true;
  gameState.coords = [];
  gameState.mutedPlayers = data.mutedPlayers || [];
  gameState.crosshair.visible = false; // hidden until first coordinate arrives
  gameState.crosshair.blinkPhase = 0;
  gameState.phase = 'collecting';

  updateRightPanel();
  updateLeaderboard();
  debugOverlay(`Round ${gameState.round} started, collecting for ${gameState.duration}s`);
}

function handleCoord(data) {
  if (!gameState.collecting) return;

  const row = data.row;
  const col = data.col;
  const user = data.user;
  const platform = data.platform;

  gameState.coords.push({ row, col, user, platform });

  // Track player stats
  const key = `${platform}:${user}`;
  if (!gameState.players[key]) {
    gameState.players[key] = { user, platform, coordCount: 0, muteCount: 0 };
  }
  gameState.players[key].coordCount++;

  // Update crosshair from live average
  const avg = computeAverage();
  if (avg) {
    gameState.crosshair.row = avg.row;
    gameState.crosshair.col = avg.col;
    gameState.crosshair.visible = true;
  }

  updateLeaderboard();
}

function handleRoundEnd(data) {
  gameState.collecting = false;
  gameState.crosshair.row = data.row;
  gameState.crosshair.col = data.col;
  gameState.crosshair.blinkPhase = 0.01; // start blinking

  // Blink for ~2 seconds, then hide crosshair
  setTimeout(() => {
    gameState.crosshair.visible = false;
    gameState.crosshair.blinkPhase = 0;
  }, 2000);

  debugOverlay(`Round ended, firing on ${String.fromCharCode(65 + data.row)}-${data.col + 1}`);
}

function handleShotResolve(data) {
  const row = data.row;
  const col = data.col;
  const key = row + ',' + col;
  const result = data.result;

  // Update mine and ship state immediately, but DON'T draw or add to shots yet
  // — pegs/ship reveals happen after the bomber animation completes (10s in)
  if (result === 'mine') {
    for (const m of gameState.mines) {
      if (m.row === row && m.col === col) {
        m.hit = true;
        break;
      }
    }
    const muted = data.mutedPlayers || [];
    for (const m of muted) {
      const pkey = `${m.platform}:${m.user}`;
      if (gameState.players[pkey]) {
        gameState.players[pkey].muteCount++;
      }
    }
  }

  if (data.shipSunk && data.shipId !== undefined) {
    const ship = gameState.ships[data.shipId];
    if (ship) {
      ship.sunk = true;
      for (const cell of ship.cells) cell.hit = true;
    }
  }

  updateRightPanel();
  updateLeaderboard();

  // Start bomber animation for hit/miss/mine results
  if (result === 'hit' || result === 'miss' || result === 'mine') {
    // Always play the hit/miss/mine audio during the bomber animation
    const audioPath = getAudioPath(result, false); // never play ship-sunk during bomber
    const bomberAudioEl = playAudioFile(audioPath);
    startBomber(row, col, null, () => {
      // NOW reveal the peg/mine/ship after the bomber animation completes
      if (result === 'hit' || result === 'miss' || result === 'mine')
        gameState.shots[key] = { row, col, result, shipSunk: !!data.shipSunk };
      drawShipLayer();
      drawFog();

      // Wait for the bomber audio to finish before proceeding
      if (data.shipSunk) {
        // Play sunk audio after the hit audio finishes
        const playSunkAudio = () => {
          const sunkAudio = 'assets/sounds/__ship-sunk.mp3';
          const sunkAudioEl = playAudioFile(sunkAudio);
          if (sunkAudioEl && sunkAudioEl.onended !== undefined) {
            sunkAudioEl.onended = () => reportBomberComplete(gameState.round);
          } else {
            setTimeout(() => reportBomberComplete(gameState.round), 10000);
          }
        };

        if (bomberAudioEl && bomberAudioEl.onended !== undefined) {
          bomberAudioEl.onended = playSunkAudio;
        } else {
          setTimeout(playSunkAudio, 10000);
        }
      } else {
        // No ship sunk — wait for the bomber audio to finish
        if (bomberAudioEl && bomberAudioEl.onended !== undefined) {
          bomberAudioEl.onended = () => reportBomberComplete(gameState.round);
        } else {
          setTimeout(() => reportBomberComplete(gameState.round), 12000);
        }
      }
    });
  } else {
    // No airplane animation for no-coords, repeat, game-win, game-lost
    const audioPath = getAudioPath(result, data.shipSunk);
    if (audioPath) {
      gameState.audioPlaying = playAudioFile(audioPath);
      if (gameState.audioPlaying) {
        gameState.audioPlaying.onended = () => {
          reportBomberComplete(gameState.round);
        };
      } else {
        setTimeout(() => reportBomberComplete(gameState.round), 10000);
      }
    } else {
      reportBomberComplete(gameState.round);
    }
  }

  debugOverlay(`Shot resolved: ${result} at ${String.fromCharCode(65 + row)}-${col + 1}`);
}

function handleGameEnd(data) {
  gameState.gameEnded = true;
  gameState.collecting = false;
  gameState.crosshair.visible = false;

  // Reveal full board — show ship sprites but don't add pegs to unhit cells
  if (data.ships) {
    for (let i = 0; i < data.ships.length; i++) {
      const s = data.ships[i];
      if (gameState.ships[i]) {
        // Mark as sunk so the full sprite is drawn, but don't mark unhit cells as hit
        gameState.ships[i].sunk = true;
      }
    }
  }
  if (data.mines) {
    for (const m of data.mines) {
      const existing = gameState.mines.find(mm => mm.row === m.row && mm.col === m.col);
      if (existing) existing.hit = true;
    }
  }

  drawShipLayer();
  drawFog(); // fog cleared because gameEnded = true

  const audioPath = data.result === 'win'
    ? 'assets/sounds/__game-win.mp3'
    : 'assets/sounds/__game-lost.mp3';
  playAudioFile(audioPath);

  debugOverlay(`Game ended: ${data.result}`);
}

function getAudioPath(result, shipSunk) {
  switch (result) {
    case 'hit':       return shipSunk ? 'assets/sounds/__ship-sunk.mp3' : 'assets/sounds/__ship-hit.mp3';
    case 'miss':      return 'assets/sounds/__miss.mp3';
    case 'mine':      return 'assets/sounds/__mine-hit.mp3';
    case 'no-coords': return 'assets/sounds/__no-coords.mp3';
    case 'repeat':    return 'assets/sounds/__repeat-coordinates.mp3';
    case 'win':       return 'assets/sounds/__game-win.mp3';
    case 'lost':      return 'assets/sounds/__game-lost.mp3';
    default:          return null;
  }
}

// --- Main loop ---
let lastFrameTime = 0;
function gameLoop(timestamp) {
  if (!lastFrameTime) lastFrameTime = timestamp;
  const dt = Math.min((timestamp - lastFrameTime) / 1000, 0.1);
  lastFrameTime = timestamp;

  // Update blink phase
  if (gameState.crosshair.blinkPhase > 0) {
    gameState.crosshair.blinkPhase += dt;
    if (gameState.crosshair.blinkPhase > 2.0) {
      gameState.crosshair.blinkPhase = 0;
    }
  }

  updateCountdown();
  drawHud(dt);

  requestAnimationFrame(gameLoop);
}

// --- Start ---
function startApp() {
  const settings = getConnectionSettings();
  debugOverlayEnabled = settings.debug;
  setStatus('Connecting to Streamer.bot...');
  connectStreamerbot();
  requestAnimationFrame(gameLoop);
  document.addEventListener('click', unlockAudio, { once: true });
  document.addEventListener('touchstart', unlockAudio, { once: true });
  document.addEventListener('keydown', (e) => {
    if (e.key === 'd' || e.key === 'D') {
      debugOverlayEnabled = !debugOverlayEnabled;
      const el = document.getElementById('debugOverlay');
      if (el) el.style.display = debugOverlayEnabled ? 'block' : 'none';
    }
  });
}

if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', startApp);
} else {
  startApp();
}