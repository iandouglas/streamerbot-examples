"use strict";

const StreamerbotClient = window.Streamerbot && window.Streamerbot.Client
    ? window.Streamerbot.Client
    : null;

let sb = null;
let connected = false;

let gameState = {
    grid: [],
    difficulty: 'easy',
    rows: 6,
    cols: 7,
    currentPlayer: 1,
    turnCount: 0,
    phase: 'idle',
    players: []
};

let timerInterval = null;
let timerEndsAt = 0;

function $(id) { return document.getElementById(id); }

function initGrid(difficulty) {
    const gridEl = $('grid');
    const labelsEl = $('columnLabels');
    const rows = 6;
    const cols = difficulty === 'extreme' ? 11 : 7;

    gridEl.className = `grid ${difficulty}`;
    gridEl.innerHTML = '';
    labelsEl.className = `column-labels ${difficulty}`;
    labelsEl.innerHTML = '';

    // Column number labels (1-based) above the grid
    for (let col = 0; col < cols; col++) {
        const label = document.createElement('div');
        label.className = 'column-label';
        label.textContent = String(col + 1);
        label.dataset.col = col;
        labelsEl.appendChild(label);
    }

    for (let row = 0; row < rows; row++) {
        for (let col = 0; col < cols; col++) {
            const cell = document.createElement('div');
            cell.className = 'cell';
            cell.dataset.row = row;
            cell.dataset.col = col;
            gridEl.appendChild(cell);
        }
    }

    gameState.rows = rows;
    gameState.cols = cols;
    return { rows, cols };
}

function highlightColumns(cols) {
    const labels = document.querySelectorAll('.column-label');
    labels.forEach(label => {
        const col = parseInt(label.dataset.col, 10);
        label.classList.toggle('highlighted', Array.isArray(cols) && cols.includes(col));
    });
}

function updateVoteTally(tally) {
    const labels = document.querySelectorAll('.column-label');
    if (!labels.length) return;

    const counts = tally && tally.counts ? tally.counts : [];
    const total = tally && tally.total ? tally.total : 0;
    const maxCount = counts.length ? Math.max(...counts, 1) : 1;

    labels.forEach(label => {
        const col = parseInt(label.dataset.col, 10);
        const count = counts[col] || 0;
        label.classList.toggle('leading', count > 0 && count === maxCount);

        // Append/replace a vote-count badge under the number.
        let badge = label.querySelector('.vote-count');
        if (count > 0) {
            if (!badge) {
                badge = document.createElement('div');
                badge.className = 'vote-count';
                label.appendChild(badge);
            }
            badge.textContent = count;
        } else if (badge) {
            badge.remove();
        }
    });

    // Update the total votes display in the voting section.
    const totalEl = $('votingSection').querySelector('.vote-total');
    if (totalEl) totalEl.textContent = total > 0 ? `${total} vote${total === 1 ? '' : 's'}` : '';
}

function clearVoteTally() {
    document.querySelectorAll('.column-label .vote-count').forEach(b => b.remove());
    document.querySelectorAll('.column-label.leading').forEach(l => l.classList.remove('leading'));
    const totalEl = $('votingSection').querySelector('.vote-total');
    if (totalEl) totalEl.textContent = '';
}

function updateGrid(gridData) {
    if (!gridData || !Array.isArray(gridData)) return;
    const cells = document.querySelectorAll('.cell');
    cells.forEach(cell => {
        const row = parseInt(cell.dataset.row, 10);
        const col = parseInt(cell.dataset.col, 10);
        const value = gridData[row] && gridData[row][col] !== undefined ? gridData[row][col] : 0;

        cell.classList.remove('player1', 'player2', 'dropping');
        if (value === 1) cell.classList.add('player1');
        else if (value === 2) cell.classList.add('player2');
    });
}

function updateTurnInfo(currentPlayer, turnCount) {
    const turnEl = $('currentTurn');
    if (currentPlayer === 1) {
        turnEl.innerHTML = '<span style="color: #ff5e57">Chat (Red)</span>';
    } else if (currentPlayer === 2) {
        turnEl.innerHTML = '<span style="color: #f9d423">AI (Yellow)</span>';
    } else {
        turnEl.textContent = 'Waiting for players...';
    }
    $('turnCount').textContent = turnCount || 0;
}

function updateDifficulty(difficulty) {
    const el = $('difficultyDisplay');
    el.textContent = difficulty.charAt(0).toUpperCase() + difficulty.slice(1);
    el.className = `difficulty-badge difficulty-${difficulty}`;
}

function updatePlayerList(players) {
    if (!players || players.length === 0) {
        $('playerList').textContent = 'None';
        return;
    }
    const names = players.map(p => p.displayName || p.name || p.userKey).join(', ');
    $('playerList').textContent = names;
}

function startTimer(remainingMs) {
    if (timerInterval) clearInterval(timerInterval);
    timerEndsAt = Date.now() + (remainingMs || 30000);
    updateTimerDisplay();
    timerInterval = setInterval(() => {
        const remaining = timerEndsAt - Date.now();
        if (remaining <= 0) {
            clearInterval(timerInterval);
            timerInterval = null;
            $('timerDisplay').textContent = '00:00';
        } else {
            updateTimerDisplay(remaining);
        }
    }, 250);
}

function updateTimerDisplay(remainingMs) {
    if (typeof remainingMs === 'undefined') remainingMs = timerEndsAt - Date.now();
    const totalSec = Math.max(0, Math.floor(remainingMs / 1000));
    const min = Math.floor(totalSec / 60);
    const sec = totalSec % 60;
    const el = $('timerDisplay');
    el.textContent = `${String(min).padStart(2, '0')}:${String(sec).padStart(2, '0')}`;
    el.classList.toggle('urgent', totalSec <= 10 && totalSec > 0);
}

function showWinner(result, winningCells) {
    const overlay = $('winnerOverlay');
    const title = $('winnerTitle');
    const msg = $('winnerMessage');

    if (result === 'draw') {
        title.textContent = "It's a Draw!";
        title.className = 'winner-title winner-tie';
        msg.textContent = 'The board is full.';
    } else if (result === 'human') {
        title.textContent = 'Chat Wins!';
        title.className = 'winner-title winner-player1';
        msg.textContent = 'Great teamwork!';
    } else if (result === 'ai') {
        title.textContent = 'AI Wins!';
        title.className = 'winner-title winner-player2';
        msg.textContent = 'Better luck next time.';
    } else {
        title.textContent = 'Game Over';
        title.className = 'winner-title winner-tie';
        msg.textContent = '';
    }

    if (winningCells && winningCells.length > 0) {
        const cells = document.querySelectorAll('.cell');
        winningCells.forEach(pos => {
            const idx = pos.row * gameState.cols + pos.col;
            if (cells[idx]) cells[idx].classList.add('winning');
        });
    }

    setTimeout(() => overlay.classList.add('active'), 400);
}

function resetDisplay() {
    $('winnerOverlay').classList.remove('active');
    $('playerList').textContent = 'None';
    $('turnCount').textContent = '0';
    $('timerDisplay').textContent = '--:--';
    $('currentTurn').textContent = 'Waiting for players...';
    highlightColumns([]);
    clearVoteTally();
    document.querySelectorAll('.cell').forEach(c => {
        c.classList.remove('player1', 'player2', 'winning', 'dropping');
    });
}

function hideVotingSection(hide) {
    $('votingSection').classList.toggle('hidden', hide);
}

function setVotingLabel(text) {
    const el = $('votingSection').querySelector('.voting-label');
    if (el) el.textContent = text;
}

function handleMessage(data) {
    if (!data) return;
    const evt = data.event || data.type;

    switch (evt) {
        case 'setup': {
            gameState.difficulty = data.difficulty || 'easy';
            gameState.cols = data.cols || (gameState.difficulty === 'extreme' ? 11 : 7);
            gameState.rows = data.rows || 6;
            initGrid(gameState.difficulty);
            updateDifficulty(gameState.difficulty);
            hideVotingSection(false);
            setVotingLabel('Time to !join:');
            clearVoteTally();
            if (data.joinEndsAt) {
                startTimer(data.joinEndsAt - Date.now());
            } else if (data.joinSeconds) {
                startTimer(data.joinSeconds * 1000);
            }
            $('status').textContent = `Game started (${gameState.difficulty}) - waiting for players to !join`;
            break;
        }

        case 'player-joined': {
            if (!gameState.players) gameState.players = [];
            gameState.players.push({ userKey: data.userKey, displayName: data.displayName });
            updatePlayerList(gameState.players);
            $('status').textContent = `${data.displayName} joined (${data.joinCount} player(s))`;
            break;
        }

        case 'phase': {
            gameState.phase = data.phase;
            const remainingMs = data.remainingMs || 0;

            if (data.phase === 'join') {
                hideVotingSection(false);
                setVotingLabel('Time to !join:');
                highlightColumns([]);
                clearVoteTally();
                if (data.remainingMs) startTimer(data.remainingMs);
                $('status').textContent = `Join window open: ${Math.ceil((data.remainingMs || 0) / 1000)}s left to !join`;
            } else if (data.phase === 'voting') {
                hideVotingSection(false);
                setVotingLabel('Vote for column:');
                highlightColumns([]);
                updateVoteTally(data.voteTally);
                startTimer(data.remainingMs);
                updateTurnInfo(1, gameState.turnCount);
                $('status').textContent = 'Chat voting open - type a column number';
            } else if (data.phase === 'tiebreak') {
                hideVotingSection(false);
                setVotingLabel('Tiebreak vote:');
                highlightColumns(data.finalists || []);
                updateVoteTally(data.voteTally);
                startTimer(data.remainingMs);
                $('status').textContent = 'Tiebreak vote!';
            } else if (data.phase === 'ai') {
                hideVotingSection(true);
                highlightColumns([]);
                clearVoteTally();
                if (timerInterval) { clearInterval(timerInterval); timerInterval = null; }
                updateTurnInfo(2, gameState.turnCount);
                $('status').textContent = 'AI is thinking...';
            } else if (data.phase === 'animating') {
                highlightColumns([]);
                clearVoteTally();
                $('status').textContent = 'Dropping piece...';
            } else if (data.phase === 'game_end') {
                hideVotingSection(true);
                highlightColumns([]);
                clearVoteTally();
                if (timerInterval) { clearInterval(timerInterval); timerInterval = null; }
            }
            break;
        }

        case 'move': {
            // Re-fetch grid from the payload if present, otherwise animate the single move
            if (data.grid) {
                updateGrid(data.grid);
            } else if (data.row !== undefined && data.col !== undefined) {
                const cells = document.querySelectorAll('.cell');
                const idx = data.row * gameState.cols + data.col;
                if (cells[idx]) {
                    cells[idx].classList.add('dropping');
                    cells[idx].classList.add(data.player === 1 ? 'player1' : 'player2');
                }
            }
            gameState.turnCount = (gameState.turnCount || 0) + 1;
            updateTurnInfo(data.player, gameState.turnCount);
            break;
        }

        case 'game-over': {
            showWinner(data.result, data.winningCells);
            $('status').textContent = 'Game over.';
            setTimeout(resetDisplay, 8000);
            break;
        }

        case 'game-end': {
            // Cancelled or no-players
            resetDisplay();
            $('status').textContent = 'Game cancelled.';
            break;
        }
    }
}

function connect() {
    if (!StreamerbotClient) {
        $('status').textContent = 'Streamerbot client library not found.';
        return;
    }

    try {
        sb = new StreamerbotClient({
            address: 'localhost',
            port: 8080,
            subscribe: { General: ['Custom'] }
        });
    } catch (e) {
        sb = new StreamerbotClient();
    }

    sb.on('open', () => {
        connected = true;
        $('status').textContent = 'Connected to Streamer.bot.';
    });

    sb.on('close', () => {
        connected = false;
        $('status').textContent = 'Disconnected. Reconnecting...';
        setTimeout(connect, 3000);
    });

    sb.on('error', (err) => {
        $('status').textContent = 'Connection error.';
        console.error('SB error', err);
    });

    sb.on('General.Custom', (data) => {
        // Streamer.bot wraps the broadcast JSON in payload.data
        handleMessage(data && data.data ? data.data : data);
    });
}

window.addEventListener('load', () => {
    initGrid('easy');
    connect();
});