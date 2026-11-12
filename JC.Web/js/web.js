(function (root) {
  'use strict';

  var SETTINGS_KEY = 'jumpchess.settings';
  var TOKEN_KEY = 'jc_token';
  var defaults = { playerName: '', playerId: 1, playerKey: '', apiBaseUrl: window.location.origin };
  var pieces = { 1: 'l', 2: 'o', 3: 'v', 4: 'm', 5: 't', 6: 'w', 7: 'l', 8: 'o', 9: 'v', 10: 'm', 11: 't', 12: 'w' };
  var initialBoard = [11, 10, 9, 7, 0, 9, 10, 11, 8, 8, 8, 8, 8, 8, 8, 8].concat(
    new Array(32).fill(0),
    [2, 2, 2, 2, 2, 2, 2, 2, 5, 4, 3, 1, 0, 3, 4, 5]
  );

  function settings() {
    try { return Object.assign({}, defaults, JSON.parse(localStorage.getItem(SETTINGS_KEY) || '{}')); }
    catch (_) { return Object.assign({}, defaults); }
  }
  function saveSettings(value) { localStorage.setItem(SETTINGS_KEY, JSON.stringify(value)); }
  function baseUrl() {
    var value = settings().apiBaseUrl.replace(/\/+$/, '');
    return value.endsWith('/api') ? value : value + '/api';
  }
  function setStatus(id, message, error) {
    var el = document.getElementById(id);
    if (el) { el.textContent = message || ''; el.className = 'status' + (error ? ' error' : ''); }
  }
  async function request(method, path, body) {
    var headers = { 'Content-Type': 'application/json' };
    var token = localStorage.getItem(TOKEN_KEY);
    if (token) headers.Authorization = 'Bearer ' + token;
    try {
      var response = await fetch(baseUrl() + path, { method: method, headers: headers, body: body === undefined ? undefined : JSON.stringify(body) });
      var data = null;
      try { data = await response.json(); } catch (_) {}
      return { ok: response.ok, status: response.status, data: data };
    } catch (error) {
      return { ok: false, status: 0, data: { message: error.message || 'Unable to reach the server' } };
    }
  }
  async function login() {
    var s = settings();
    var result = await request('POST', '/auth/login', { Username: null, Password: s.playerKey, PlayerId: Number(s.playerId) });
    if (result.ok && result.data && result.data.token) localStorage.setItem(TOKEN_KEY, result.data.token);
    return result;
  }
  async function ensureLogin() {
    if (localStorage.getItem(TOKEN_KEY)) return { ok: true };
    return login();
  }
  function gameId() { return new URLSearchParams(window.location.search).get('id'); }
  function message(result, fallback) { return result.data && (result.data.message || result.data.Message) || fallback; }
  async function loadGames() {
    var auth = await ensureLogin();
    if (!auth.ok) return auth;
    var result = await request('GET', '/games');
    if (result.status === 401) {
      localStorage.removeItem(TOKEN_KEY);
      auth = await login();
      if (!auth.ok) return auth;
      result = await request('GET', '/games');
    }
    return result;
  }
  async function authorizedRequest(method, path, body) {
    var auth = await ensureLogin();
    if (!auth.ok) return auth;
    var result = await request(method, path, body);
    if (result.status === 401) {
      localStorage.removeItem(TOKEN_KEY);
      auth = await login();
      if (!auth.ok) return auth;
      result = await request(method, path, body);
    }
    return result;
  }
  function renderGames(container, games) {
    container.replaceChildren();
    (games || []).forEach(function (game) {
      var id = game.id || game.Id;
      var card = document.createElement('a');
      card.className = 'game-card';
      card.href = 'game.html?id=' + encodeURIComponent(id);
      var title = document.createElement('span');
      title.className = 'game-card-title';
      title.textContent = 'Game ' + id;
      var players = document.createElement('span');
      players.className = 'game-card-detail';
      players.textContent = 'White: ' + (game.whitePlayerID || game.WhitePlayerID) + ' · Black: ' + (game.blackPlayerID || game.BlackPlayerID);
      var status = document.createElement('span');
      status.className = 'game-card-detail';
      status.textContent = (game.whiteToMove || game.WhiteToMove ? 'White' : 'Black') + ' to move · ' + formatGameStatus(game.gameStatus || game.GameStatus);
      card.append(title, players, status);
      container.appendChild(card);
    });
  }
  function formatGameStatus(value) {
    var labels = ['In progress', 'White checkmate', 'Black checkmate', 'Stalemate', 'Draw'];
    return labels[Number(value)] || String(value || 'Unknown');
  }
  function renderBoard(game, selected, onMove) {
    var board = document.getElementById('board');
    var positions = game.board || game.Board;
    if (typeof positions === 'string') {
      try { positions = Array.from(atob(positions), function (character) { return character.charCodeAt(0); }); }
      catch (_) { positions = null; }
    }
    if (!Array.isArray(positions) || positions.length !== 64) positions = initialBoard;
    board.replaceChildren();
    for (let pos = 0; pos < 64; pos += 1) {
      var piece = positions[pos] || 0;
      var square = document.createElement('button');
      square.type = 'button';
      square.className = 'square ' + ((pos + Math.floor(pos / 8)) % 2 ? 'dark' : 'light') + (selected.value === pos ? ' selected' : '');
      square.dataset.position = pos;
      square.classList.add(piece >= 7 ? 'black-piece' : 'white-piece');
      square.textContent = pieces[piece] || '';
      square.addEventListener('click', function () {
        if (selected.value === null) { selected.value = pos; renderBoard(game, selected, onMove); }
        else if (selected.value === pos) { selected.value = null; renderBoard(game, selected, onMove); }
        else { var from = selected.value; selected.value = null; onMove(from, pos); }
      });
      board.appendChild(square);
    }
  }

  function initHome() {
    var s = settings();
    if (!s.playerName || !s.playerKey) setStatus('connection-status', 'Set up your player in Settings to connect.');
    else {
      setStatus('connection-status', 'Configured for ' + s.playerName + '.');
      loadGames().then(function (result) {
        if (result.ok) {
          renderGames(document.getElementById('home-games'), result.data);
          if (!result.data || !result.data.length) setStatus('home-games-status', 'No current games.');
        } else setStatus('home-games-status', message(result, 'Unable to load games.'), true);
      });
    }
  }
  function initSettings() {
    var form = document.getElementById('settings-form'), s = settings();
    Object.keys(s).forEach(function (key) { if (form.elements[key]) form.elements[key].value = s[key]; });
    form.addEventListener('submit', function (event) {
      event.preventDefault();
      var next = Object.assign({}, s, Object.fromEntries(new FormData(form).entries()));
      next.playerId = Number(next.playerId);
      saveSettings(next); localStorage.removeItem(TOKEN_KEY);
      setStatus('settings-status', 'Settings saved.');
    });
    document.getElementById('test-connection').addEventListener('click', async function () {
      var next = Object.assign({}, s, Object.fromEntries(new FormData(form).entries()));
      next.playerId = Number(next.playerId); saveSettings(next);
      var result = await ensureLogin();
      setStatus('settings-status', result.ok ? 'Connected to the server.' : message(result, 'Connection failed.'), !result.ok);
    });
  }
  async function initSetup() {
    var form = document.getElementById('setup-form');
    form.addEventListener('submit', async function (event) {
      event.preventDefault(); setStatus('setup-status', 'Creating game...');
      var values = Object.fromEntries(new FormData(form).entries()), s = settings(), white = values.color === 'white';
      var result = await authorizedRequest('POST', '/games', { WhitePlayer: white ? Number(s.playerId) : Number(values.opponentId), BlackPlayer: white ? Number(values.opponentId) : Number(s.playerId), EnableUndo: values.enableUndo === 'on', WhiteVetoes: Number(values.whiteVetoes) || 0, BlackVetoes: Number(values.blackVetoes) || 0 });
      if (result.ok && result.data && result.data.id) window.location.href = 'game.html?id=' + encodeURIComponent(result.data.id);
      else setStatus('setup-status', message(result, 'Game creation failed.'), true);
    });
  }
  async function initGames() {
    var games = await loadGames();
    var list = document.getElementById('games-list');
    if (!games.ok) { setStatus('games-status', message(games, 'Unable to load games.'), true); return; }
    renderGames(list, games.data);
    if (!list.children.length) setStatus('games-status', 'No games found.');
  }
  async function initGame() {
    var id = gameId(), selected = { value: null }, s = settings();
    if (!id) { setStatus('game-message', 'No game was selected.', true); return; }
    async function load() {
      var result = await authorizedRequest('GET', '/games/' + encodeURIComponent(id));
      if (!result.ok) { setStatus('game-message', message(result, 'Unable to load game.'), true); return; }
      var game = result.data; document.getElementById('game-title').textContent = 'Game ' + id;
      var whiteName = await request('GET', '/players/' + game.whitePlayerID);
      var blackName = await request('GET', '/players/' + game.blackPlayerID);
      var whiteLabel = whiteName.ok && whiteName.data ? whiteName.data.name || whiteName.data.Name : 'Player ' + game.whitePlayerID;
      var blackLabel = blackName.ok && blackName.data ? blackName.data.name || blackName.data.Name : 'Player ' + game.blackPlayerID;
      document.getElementById('game-players').textContent = whiteLabel + ' vs ' + blackLabel;
      setStatus('game-status', (game.whiteToMove ? 'White' : 'Black') + ' to move · ' + String(game.gameStatus || 'In progress'));
      renderBoard(game, selected, async function (from, to) {
        var move = await authorizedRequest('POST', '/games/' + encodeURIComponent(id) + '/move', { From: from, To: to, PromotionPiece: 0 });
        if (!move.ok) setStatus('game-message', message(move, 'Invalid move.'), true); else load();
      });
      document.getElementById('undo-game').disabled = !game.allowUndo;
      var isWhitePlayer = Number(s.playerId) === Number(game.whitePlayerID);
      var vetoes = isWhitePlayer ? game.whiteVetoesRemaining : game.blackVetoesRemaining;
      document.getElementById('veto-game').disabled = !vetoes || (game.whiteToMove !== isWhitePlayer);
      document.getElementById('veto-remaining').textContent = String(vetoes || 0) + ' remaining';
    }
    document.getElementById('refresh-game').addEventListener('click', load);
    document.getElementById('undo-game').addEventListener('click', async function () { var r = await authorizedRequest('POST', '/games/' + id + '/undo?playerId=' + s.playerId); if (!r.ok) setStatus('game-message', message(r, 'Undo failed.'), true); else load(); });
    document.getElementById('veto-game').addEventListener('click', async function () { var r = await authorizedRequest('POST', '/games/' + id + '/veto?playerId=' + s.playerId); if (!r.ok) setStatus('game-message', message(r, 'Veto failed.'), true); else load(); });
    load();
  }
  root.JumpChess = { initHome: initHome, initSettings: initSettings, initSetup: initSetup, initGames: initGames, initGame: initGame };
})(window);
