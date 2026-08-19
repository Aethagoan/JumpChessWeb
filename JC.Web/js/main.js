// Bootstrapper. One job: load all modules, then run router.
const api = require('./api.js');
const state = require('./state.js');
const board = require('./board.js');
const pages = require('./pages.js');
const router = require('./router.js');

// Smoke test.
function test() {
  console.log('API:', api);
  console.log('State:', state);
  console.log('Board:', board);
  console.log('Pages:', pages);
  console.log('Router:', router);
}

module.exports = { api, state, board, pages, router, test };
