// App state: one object, minimal accessors, and a render callback so views update on change.
const state = {
  game: null,        // last game object from API
  selected: null,    // selected square index (0-63) or null
  me: null,          // my player id (1-255)
};

const listeners = new Set();

function onChange(fn) { listeners.add(fn); }

function set(patch) {
  Object.assign(state, patch);
  listeners.forEach(fn => fn(state));
}

function selectSquare(pos) { set({ selected: pos === state.selected ? null : pos }); }

module.exports = { state, set, onChange, selectSquare };
