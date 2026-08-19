// Board rendering. Each function does one thing: map a piece, build one square,
// build the grid, handle one click, render the whole board into a container.
const PIECES = {
  1: '♔', 2: '♙', 3: '♗', 4: '♞', 5: '♖', 6: '♕',
  7: '♚', 8: '♟', 9: '♝', 10: '♘', 11: '♜', 12: '♛',
};

function fileAt(pos) { return pos % 8; }
function rowAt(pos) { return Math.floor(pos / 8); }

function pieceGlyph(piece) { return PIECES[piece] || ''; }

function isLight(pos) { return (fileAt(pos) + rowAt(pos)) % 2 === 0; }

// Build one <div class="square"> for position.
function buildSquare(game, pos) {
  const div = document.createElement('div');
  div.className = 'square ' + (isLight(pos) ? 'light' : 'dark');
  div.dataset.pos = pos;
  const piece = game.board[pos];
  if (piece) div.textContent = pieceGlyph(piece);
  return div;
}

// Build the 8x8 grid. board[0] is the top-left square.
function buildBoard(game) {
  const grid = document.createElement('div');
  grid.className = 'board';
  for (let pos = 0; pos < 64; pos++) grid.appendChild(buildSquare(game, pos));
  return grid;
}

// Reflect the current selection by toggling .sel on matching squares.
function markSelection(boardEl, selected) {
  boardEl.querySelectorAll('.sel').forEach((x) => x.classList.remove('sel'));
  if (selected !== null && selected !== undefined) {
    const sq = boardEl.querySelector('[data-pos="' + selected + '"]');
    if (sq) sq.classList.add('sel');
  }
}

// Full click handler: select, deselect, or send a move.
async function handleClick(state, api, onMoved, pos) {
  const sel = state.selected;
  if (sel === null) { state.selectSquare(pos); return; }
  if (sel === pos) { state.selectSquare(pos); return; }
  state.selectSquare(null);
  const r = await api.move(state.game.id, sel, pos, 0);
  if (r.ok) onMoved(r.data);
  else showStatus((r.data && r.data.message) || 'Invalid move', true);
}

function showStatus(msg, isError) {
  const el = document.getElementById('status');
  if (!el) return;
  el.textContent = msg;
  el.className = 'status' + (isError ? ' error' : '');
}

// Render game into #containerId, wiring clicks through state.
function render(containerId, state, api, onMoved) {
  const host = document.getElementById(containerId);
  if (!host) return null;
  const el = buildBoard(state.game);
  el.addEventListener('click', (e) => {
    const sq = e.target.closest('.square');
    if (!sq) return;
    handleClick(state, api, onMoved, Number(sq.dataset.pos));
  });
  markSelection(el, state.selected);
  host.replaceChildren(el);
  return el;
}

module.exports = { PIECES, buildBoard, buildSquare, render, markSelection, showStatus };
