// Smoke tests for the JS modules. Run with `node js.test.js` after `node -e "require('./js/api.js')"`.
const api = require('./js/api.js');
const state = require('./js/state.js');
const board = require('./js/board.js');
const pages = require('./js/pages.js');
const router = require('./js/router.js');
const main = require('./js/main.js');

console.assert(api && typeof api.login === 'function', 'api.login exists');
console.assert(api && typeof api.createGame === 'function', 'api.createGame exists');
console.assert(state && typeof state.set === 'function', 'state.set exists');
console.assert(board && typeof board.render === 'function', 'board.render exists');
console.assert(pages && typeof pages.authPage === 'function', 'pages.authPage exists');
console.assert(router && typeof router.init === 'function', 'router.init exists');
console.assert(main && typeof main.test === 'function', 'main.test exists');

// Run main test.
main.test();

console.log('All smoke tests passed.');
