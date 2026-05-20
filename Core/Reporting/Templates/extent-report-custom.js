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

  /* -- Extent Spark charts: redraw when dashboard becomes visible -- */
  function destroyExtentCharts() {
    if (typeof Chart === 'undefined' || !Chart.instances) return;
    Object.keys(Chart.instances).forEach(function (id) {
      try {
        Chart.instances[id].destroy();
      } catch (e) { /* ignore */ }
    });
  }

  function randomColor() {
    var letters = '0123456789ABCDEF';
    var color = '#';
    for (var i = 0; i < 6; i++) {
      color += letters[Math.floor(Math.random() * 16)];
    }
    return color;
  }

  function drawExtentDashboardCharts() {
    if (typeof Chart === 'undefined' || typeof statusGroup === 'undefined') return;

    var dashboard = document.querySelector('.dashboard-view');
    if (!dashboard || dashboard.classList.contains('d-none')) return;

    destroyExtentCharts();

    var chartOptions = {
      responsive: true,
      maintainAspectRatio: false,
      legend: {
        position: 'right',
        labels: {
          boxWidth: 10,
          fontSize: 11,
          lineHeight: 1,
          fontFamily: ['Source Sans Pro', 'Segoe UI', 'Arial'],
          padding: 1,
          filter: function (legendItem, data) {
            return data.datasets[0].data[legendItem.index] !== 0;
          }
        }
      },
      cutoutPercentage: 65
    };

    function drawDoughnut(canvasId, values, colors, labels) {
      var canvas = document.getElementById(canvasId);
      if (!canvas) return;
      new Chart(canvas.getContext('2d'), {
        type: 'doughnut',
        data: {
          datasets: [{
            borderColor: 'transparent',
            data: values,
            backgroundColor: colors
          }],
          labels: labels
        },
        options: chartOptions
      });
    }

    drawDoughnut(
      'parent-analysis',
      [statusGroup.passParent, statusGroup.failParent, statusGroup.warningParent, statusGroup.skipParent],
      ['#00af00', '#F7464A', '#FDB45C', '#ff9900'],
      ['Pass', 'Fail', 'Warning', 'Skip']
    );

    if (statusGroup.childCount > 0) {
      drawDoughnut(
        'child-analysis',
        [statusGroup.passChild, statusGroup.failChild, statusGroup.warningChild, statusGroup.skipChild, statusGroup.infoChild],
        ['#00af00', '#F7464A', '#FDB45C', '#ff9900', '#46BFBD'],
        ['Pass', 'Fail', 'Warning', 'Skip', 'Info']
      );
    }

    if (statusGroup.grandChildCount > 0) {
      drawDoughnut(
        'grandchild-analysis',
        [statusGroup.passGrandChild, statusGroup.failGrandChild, statusGroup.warningGrandChild, statusGroup.skipGrandChild, statusGroup.infoGrandChild],
        ['#00af00', '#F7464A', '#FDB45C', '#ff9900', '#46BFBD'],
        ['Pass', 'Fail', 'Warning', 'Skip', 'Info']
      );
    }

    if (typeof timeline !== 'undefined') {
      var timelineCanvas = document.getElementById('timeline');
      if (timelineCanvas) {
        var datasets = [];
        for (var key in timeline) {
          if (Object.prototype.hasOwnProperty.call(timeline, key)) {
            datasets.push({
              label: key,
              data: [timeline[key]],
              backgroundColor: randomColor(),
              borderWidth: 1
            });
          }
        }
        new Chart(timelineCanvas.getContext('2d'), {
          type: 'horizontalBar',
          data: { datasets: datasets },
          options: {
            responsive: true,
            maintainAspectRatio: false,
            tooltips: { mode: 'point' },
            scales: {
              xAxes: [{ stacked: true, gridLines: false }],
              yAxes: [{ stacked: true, gridLines: false, barThickness: 25 }]
            },
            legend: { display: false }
          }
        });
      }
    }
  }

  function scheduleDashboardCharts() {
    setTimeout(drawExtentDashboardCharts, 80);
  }

  function hookDashboardCharts() {
    var originalToggle = window.toggleView;
    window.toggleView = function (view) {
      if (typeof originalToggle === 'function') {
        originalToggle(view);
      }
      if (view === 'dashboard-view') {
        scheduleDashboardCharts();
      }
    };

    var dashboardNav = document.getElementById('nav-dashboard');
    if (dashboardNav) {
      dashboardNav.addEventListener('click', scheduleDashboardCharts);
    }
  }

  function onReady() {
    addThemeToggle(window.extentReportDefaultDark === true);
    hookDashboardCharts();
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', onReady);
  } else {
    onReady();
  }
})();
