function toggleTheme() {
  var current = document.documentElement.getAttribute('data-theme');
  var next = current === 'dark' ? 'light' : 'dark';
  document.documentElement.setAttribute('data-theme', next);
  localStorage.setItem('theme', next);
  _updateThemeIcon();
}

function _updateThemeIcon() {
  var theme = document.documentElement.getAttribute('data-theme');
  var btn = document.getElementById('themeToggle');
  if (btn) btn.textContent = theme === 'dark' ? '☀️' : '🌙';
}

document.addEventListener('DOMContentLoaded', _updateThemeIcon);
