// Router: one path -> one page. No server-side routing; just replace <body>.
const PATHS = {
  '/': 'auth',
  '/auth': 'auth',
  '/settings': 'settings',
  '/new': 'new',
  '/game': 'game',
  '/games': 'games',
};

function pathToPage(path) {
  for (const [p, name] of Object.entries(PATHS)) {
    if (path === p || path === p + '/') return name;
    if (path.startsWith(p + '/')) return name;
  }
  return 'auth';
}

function loadPage(path, api, state) {
  const page = pathToPage(path);
  const me = state.state.me;
  document.body.innerHTML = '';
  const el = pages[page](api, me);
  document.body.appendChild(el);
}

function init(api, state) {
  if (api.isLoggedIn()) {
    state.set({ me: 1 }); // TODO: real lookup
    loadPage(window.location.pathname, api, state);
  } else {
    loadPage(window.location.pathname, api, state);
  }

  window.addEventListener('popstate', () => {
    loadPage(window.location.pathname, api, state);
  });
}

module.exports = { init, pathToPage };
