function showError(msg) {
  document.getElementById('error').textContent = msg;
}

async function apiFetch(url, options) {
  try {
    const resp = await fetch(url, options);
    if (!resp.ok) {
      let text = await resp.text();
      try { const data = JSON.parse(text); text = data.error || text; } catch {}
      throw new Error(text);
    }
    return await resp.json();
  } catch (err) {
    showError(err.message);
    throw err;
  }
}

async function fetchUser() {
  showError('');
  const data = await apiFetch('/api/user');
  document.getElementById('user').textContent = 'User: ' + data.user;
}

function addSection(name = '') {
  const section = document.createElement('div');
  section.className = 'section';
  section.innerHTML = `<input class="section-name" placeholder="Section name" value="${name}">
      <div class="items"></div>
      <button class="add-item">Add Item</button>`;
  section.querySelector('.add-item').onclick = () => addItem(section.querySelector('.items'));
  document.getElementById('sections').appendChild(section);
}

function addItem(container, data) {
  const item = document.createElement('div');
  item.className = 'item';
  item.innerHTML = `<input class="item-label" placeholder="Question" value="${data?.label || ''}">
      <select class="item-type">
        <option value="checkbox">Checkbox</option>
        <option value="text">Text</option>
        <option value="number">Number</option>
        <option value="dropdown">Dropdown</option>
      </select>
      <input class="item-options" placeholder="Comma options" style="display:none">`;
  const typeSel = item.querySelector('.item-type');
  const optInput = item.querySelector('.item-options');
  if (data?.type) typeSel.value = data.type;
  if (data?.options) { optInput.value = data.options.join(','); optInput.style.display = 'inline'; }
  typeSel.onchange = () => {
    if (typeSel.value === 'dropdown') optInput.style.display = 'inline';
    else { optInput.style.display = 'none'; optInput.value = ''; }
  };
  container.appendChild(item);
}

async function loadTemplates() {
  showError('');
  const tpls = await apiFetch('/api/templates');
  const list = document.getElementById('templatesList');
  list.innerHTML = '';
  tpls.forEach(t => {
    const card = document.createElement('div');
    card.className = 'card';
    const title = document.createElement('div');
    title.textContent = t.name;
    const btn = document.createElement('button');
    btn.textContent = 'Create Check';
    btn.onclick = () => createCheck(t.id);
    card.appendChild(title);
    card.appendChild(btn);
    list.appendChild(card);
  });
}

async function loadChecks() {
  showError('');
  const checks = await apiFetch('/api/checks');
  const list = document.getElementById('checksList');
  list.innerHTML = '';
  checks.forEach(c => {
    const card = document.createElement('div');
    card.className = 'card';
    card.textContent = c.id + ' (' + c.status + ')';
    list.appendChild(card);
  });
}

async function saveTemplate() {
  showError('');
  const name = document.getElementById('tmplName').value.trim();
  if (!name) return;
  const sections = [];
  document.querySelectorAll('#sections .section').forEach(secEl => {
    const sName = secEl.querySelector('.section-name').value.trim();
    if (!sName) return;
    const sec = { id: crypto.randomUUID(), name: sName, items: [] };
    secEl.querySelectorAll('.items .item').forEach(it => {
      const label = it.querySelector('.item-label').value.trim();
      if (!label) return;
      const type = it.querySelector('.item-type').value;
      const options = it.querySelector('.item-options').value
        .split(',').map(o => o.trim()).filter(o => o);
      const item = { id: crypto.randomUUID(), label, type };
      if (options.length) item.options = options;
      sec.items.push(item);
    });
    sections.push(sec);
  });
  await apiFetch('/api/templates', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name, sections })
  });
  document.getElementById('tmplName').value = '';
  document.getElementById('sections').innerHTML = '';
  loadTemplates();
}

async function createCheck(tid) {
  showError('');
  const templates = await apiFetch('/api/templates');
  const t = templates.find(x => x.id === tid);
  if (!t) return;
  await apiFetch('/api/checks', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ templateUniqueId: t.dataStorageUniqueId, templateSnapshot: t, checkedElements: [], answers: [] })
  });
  loadChecks();
}

async function loadLog() {
  showError('');
  const data = await apiFetch('/api/log');
  document.getElementById('logText').textContent = data.log || '';
}

function applyTheme(theme) {
  document.documentElement.dataset.theme = theme;
  document.getElementById('themeToggle').checked = theme === 'dark';
  localStorage.setItem('theme', theme);
}

function showPage(page) {
  document.querySelectorAll('main .page').forEach(p => {
    p.hidden = p.dataset.page !== page;
  });
  document.querySelectorAll('nav a.nav-item').forEach(a => {
    a.classList.toggle('active', a.getAttribute('href') === '#' + page);
  });
  if (page === 'log') loadLog();
}

function init() {
  document.getElementById('addSection').onclick = () => addSection();
  document.getElementById('saveTemplate').onclick = saveTemplate;
  document.getElementById('refreshLog').onclick = loadLog;
  document.getElementById('themeToggle').addEventListener('change', e => {
    applyTheme(e.target.checked ? 'dark' : 'light');
  });
  window.addEventListener('hashchange', () => {
    showPage(location.hash.substring(1) || 'templates');
  });
  const savedTheme = localStorage.getItem('theme') || (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
  applyTheme(savedTheme);
  showPage(location.hash.substring(1) || 'templates');
  fetchUser();
  loadTemplates();
  loadChecks();
  loadLog();
}

document.addEventListener('DOMContentLoaded', init);
