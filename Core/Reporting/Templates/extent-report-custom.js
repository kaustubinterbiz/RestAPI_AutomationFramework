(function () {
  var storageKey = 'extentReportTheme';

  function isDarkTheme() {
    return document.body.classList.contains('dark');
  }

  function applyTheme(dark) {
    document.body.classList.toggle('dark', dark);
    document.body.classList.toggle('standard', !dark);
    try {
      localStorage.setItem(storageKey, dark ? 'dark' : 'standard');
    } catch (e) { /* ignore */ }
    updateToggleLabel();
  }

  function updateToggleLabel() {
    var label = document.getElementById('extent-theme-toggle-label');
    if (label) {
      label.textContent = isDarkTheme() ? 'Light mode' : 'Dark mode';
    }
  }

  function readStoredTheme(defaultDark) {
    try {
      var stored = localStorage.getItem(storageKey);
      if (stored === 'dark') return true;
      if (stored === 'standard') return false;
    } catch (e) { /* ignore */ }
    return defaultDark;
  }

  function addThemeToggle(defaultDark) {
    var navRight = document.querySelector('.nav-right');
    if (!navRight || document.getElementById('extent-theme-toggle')) return;

    var li = document.createElement('li');
    li.className = 'm-r-10 extent-theme-toggle';
    li.id = 'extent-theme-toggle';
    li.innerHTML =
      '<a href="#" title="Toggle dark / light mode">' +
      '<span class="badge badge-secondary" id="extent-theme-toggle-label">Dark mode</span>' +
      '</a>';

    li.addEventListener('click', function (e) {
      e.preventDefault();
      applyTheme(!isDarkTheme());
    });

    navRight.insertBefore(li, navRight.firstChild);
    applyTheme(readStoredTheme(defaultDark));
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', function () {
      addThemeToggle(window.extentReportDefaultDark === true);
    });
  } else {
    addThemeToggle(window.extentReportDefaultDark === true);
  }
})();
