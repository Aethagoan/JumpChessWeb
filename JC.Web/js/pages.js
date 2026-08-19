// Page views. Each function builds one page: auth, games list, settings, new game, game board.
const TITLES = {
  auth: 'JumpChess – Log In',
  games: 'JumpChess – Current Games',
  settings: 'JumpChess – Settings',
  new: 'JumpChess – New Game',
  game: 'JumpChess – Board',
};

// Auth page: username/password form.
function authPage(api) {
  const form = document.createElement('form');
  form.className = 'page auth';
  form.innerHTML = `
    <h1>${TITLES.auth}</h1>
    <input id="auth-user" type="text" placeholder="Username" required>
    <input id="auth-pass" type="password" placeholder="Password" required>
    <button id="auth-btn">Log In</button>
    <a id="auth-link" href="/settings">Settings</a>
  `;
  form.querySelector('#auth-btn').onclick = async () => {
    const u = form.querySelector('#auth-user').value;
    const p = form.querySelector('#auth-pass').value;
    const r = await api.login(u, p);
    if (r.ok) {
      document.body.innerHTML = gamesPage(api);
    } else {
      showStatus((r.data && r.data.message) || 'Login failed', true);
    }
  };
  return form;
}

// Settings page: show my ID, logout, link to new game.
function settingsPage(api, me) {
  const el = document.createElement('div');
  el.className = 'page settings';
  el.innerHTML = `
    <h1>${TITLES.settings}</h1>
    <p>Your ID: <span id="me-id">${me || 'unknown'}</span></p>
    <button id="logout-btn">Log Out</button>
    <a id="new-game-link" href="/new">New Game</a>
  `;
  el.querySelector('#logout-btn').onclick = () => {
    api.logout();
    document.body.innerHTML = authPage(api);
  };
  return el;
}

// New game page: create a game, then load the board.
async function newGamePage(api, me) {
  const el = document.createElement('div');
  el.className = 'page new';
  el.innerHTML = `
    <h1>${TITLES.new}</h1>
    <form id="new-form">
      <label><input type="checkbox" id="undo-check"> Enable Undo</label>
      <label>White vetoes: <input type="number" id="wv" value="0" min="0" max="9"></label>
      <label>Black vetoes: <input type="number" id="bv" value="0" min="0" max="9"></label>
      <button type="submit">Create</button>
    </form>
    <p id="new-status"></p>
  `;
  el.querySelector('#new-form').onsubmit = async (e) => {
    e.preventDefault();
    const undo = el.querySelector('#undo-check').checked;
    const wv = Number(el.querySelector('#wv').value) || 0;
    const bv = Number(el.querySelector('#bv').value) || 0;
    const r = await api.createGame(me, me, undo, wv, bv);
    if (r.ok) {
      const game = r.data;
      document.body.innerHTML = gamePage(api, game, me);
    } else {
      showStatus((r.data && r.data.message) || 'Failed to create game', true);
    }
  };
  return el;
}

// Game board page: render board, undo, veto buttons.
function gamePage(api, game, me) {
  const el = document.createElement('div');
  el.className = 'page game';
  el.innerHTML = `
    <h1>${TITLES.game}</h1>
    <div id="board"></div>
    <div id="controls">
      <button id="undo-btn" disabled>Undo</button>
      <button id="veto-btn" disabled>Veto</button>
      <a id="back-link" href="/games">Games</a>
    </div>
    <p id="status"></p>
  `;
  const boardEl = document.getElementById('board');
  const undoBtn = el.querySelector('#undo-btn');
  const vetoBtn = el.querySelector('#veto-btn');
  const statusEl = el.querySelector('#status');

  function updateButtons() {
    undoBtn.disabled = game.moves.length === 0 || !game.allowUndo;
    vetoBtn.disabled = !game.whiteToMove || !game.whiteVetoesPerTurn || game.whiteVetoesUsedThisTurn === game.whiteVetoesPerTurn;
    statusEl.textContent = 'Status: ' + game.gameStatus;
  }

  updateButtons();

  undoBtn.onclick = async () => {
    const r = await api.undoMove(game.id, me);
    if (r.ok) {
      api.getGame(game.id).then(res => {
        if (res.ok) {
          const g = res.data;
          boardEl.replaceChildren();
          boardEl.appendChild(bd.buildBoard(g));
          bd.markSelection(boardEl, null);
          updateButtons();
        }
      });
    } else showStatus((r.data && r.data.message) || 'Undo failed', true);
  };

  vetoBtn.onclick = async () => {
    const r = await api.vetoMove(game.id, me);
    if (r.ok) {
      api.getGame(game.id).then(res => {
        if (res.ok) {
          const g = res.data;
          boardEl.replaceChildren();
          boardEl.appendChild(bd.buildBoard(g));
          updateButtons();
        }
      });
    } else showStatus((r.data && r.data.message) || 'Veto failed', true);
  };

  bd.render('board', game, null, api, () => {
    updateButtons();
  });

  el.querySelector('#back-link').href = '/games';
  return el;
}

// Games list page: list all games, link to each.
async function gamesPage(api) {
  const el = document.createElement('div');
  el.className = 'page games';
  el.innerHTML = `<h1>${TITLES.games}</h1><ul id="game-list"></ul><p id="game-status"></p>`;
  const list = el.querySelector('#game-list');
  const status = el.querySelector('#game-status');

  const r = await api.getGame(null); // list all
  if (r.ok) {
    const games = r.data || [];
    games.forEach(g => {
      const li = document.createElement('li');
      li.innerHTML = `<a href="/game/${g.id}">Game ${g.id}</a>`;
      list.appendChild(li);
    });
  } else {
    status.textContent = 'No games found';
  }

  return el;
}

function showStatus(msg, isError) {
  const el = document.getElementById('status');
  if (!el) return;
  el.textContent = msg;
  el.className = 'status' + (isError ? ' error' : '');
}

module.exports = { authPage, settingsPage, newGamePage, gamePage, gamesPage, showStatus };
