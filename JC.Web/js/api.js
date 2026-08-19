// API layer. Every function does one thing: build a fetch call or parse a reply.
const BASE = '/api';
const TOKEN_KEY = 'jc_token';

function getToken() { return localStorage.getItem(TOKEN_KEY) || ''; }

function setToken(token) {
  if (token) localStorage.setItem(TOKEN_KEY, token);
  else localStorage.removeItem(TOKEN_KEY);
}

function headers(extra) {
  const h = Object.assign({ 'Content-Type': 'application/json' }, extra || {});
  const t = getToken();
  if (t) h['Authorization'] = 'Bearer ' + t;
  return h;
}

function url(path) { return BASE + path; }

async function send(method, path, body) {
  return fetch(url(path), {
    method,
    headers: headers(),
    body: body === undefined ? undefined : JSON.stringify(body),
  });
}

// One request that never throws: returns {ok, status, data}.
async function request(method, path, body) {
  const res = await send(method, path, body);
  let data = null;
  try { data = await res.json(); } catch (_) { /* empty body */ }
  return { ok: res.ok, status: res.status, data };
}

function isLoggedIn() { return !!getToken(); }

// --- auth ---
async function login(username, password) {
  const r = await request('POST', '/auth/login', { Username: username, Password: password });
  if (r.ok) setToken(r.data.token);
  return r;
}

function logout() { setToken(''); }

// --- games ---
function createGame(w, b, undo, wv, bv) {
  return request('POST', '/games', { WhitePlayer: w, BlackPlayer: b, EnableUndo: undo, WhiteVetoes: wv, BlackVetoes: bv });
}

async function getGame(id) {
  if (id === null) return request('GET', '/games'); // list all
  return request('GET', '/games/' + id);
}

async function getGameStatus(id) { return request('GET', '/games/' + id + '/status'); }

async function move(id, from, to, promo) {
  return request('POST', '/games/' + id + '/move', { From: from, To: to, PromotionPiece: promo });
}

async function undoMove(id, playerId) {
  return request('POST', '/games/' + id + '/undo?playerId=' + playerId);
}

async function vetoMove(id, playerId) {
  return request('POST', '/games/' + id + '/veto?playerId=' + playerId);
}

async function getPlayerName(id) { return request('GET', '/players/' + id); }

module.exports = {
  getToken, setToken, isLoggedIn, url,
  login, logout,
  createGame, getGame, getGameStatus, move, undoMove, vetoMove, getPlayerName,
};
