/**
 * SPA-like page navigation for sidebar links.
 * Intercepts clicks, fetches pages via AJAX, swaps only the content area
 * with a smooth fade transition. Sidebar and layout stay untouched.
 */
(function () {
  'use strict';

  const TRANSITION_MS = 120;
  const contentBody = document.querySelector('.content-body');
  if (!contentBody) return;

  const sidebarMenu = document.querySelector('.sidebar-menu');
  let isNavigating = false;

  // ── helpers ──

  function getCsrfToken() {
    const meta = document.querySelector('meta[name="RequestVerificationToken"]');
    return meta ? meta.content : '';
  }

  function extractContent(doc) {
    const main = doc.getElementById('main-content');
    return {
      html: main ? main.innerHTML : null,
      title: doc.title,
    };
  }

  function updateActiveLink(url) {
    if (!sidebarMenu) return;
    const path = new URL(url, location.origin).pathname;
    sidebarMenu.querySelectorAll('.sidebar-link').forEach(function (link) {
      const linkPath = new URL(link.href, location.origin).pathname;
      link.classList.toggle('active', linkPath === path);
    });
  }

  /** Remove old page-specific scripts so they can be re-executed. */
  function cleanupOldScripts() {
    contentBody.querySelectorAll('[data-page-script]').forEach(function (el) {
      el.remove();
    });
  }

  /** Inject <script> tags from fetched HTML so inline JS re-runs. */
  function injectScripts(html) {
    const tmp = document.createElement('div');
    tmp.innerHTML = html;
    tmp.querySelectorAll('script').forEach(function (old) {
      const s = document.createElement('script');
      if (old.src) {
        s.src = old.src;
      } else {
        s.textContent = old.textContent;
      }
      s.setAttribute('data-page-script', '');
      if (old.hasAttribute('csp-nonce')) {
        s.setAttribute('csp-nonce', old.getAttribute('csp-nonce'));
      }
      document.body.appendChild(s);
    });
  }

  // ── navigation ──

  function navigateTo(url, pushState) {
    if (isNavigating) return;
    if (pushState === undefined) pushState = true;
    isNavigating = true;

    // fade out
    contentBody.style.transition = 'opacity ' + TRANSITION_MS + 'ms ease';
    contentBody.style.opacity = '0';

    setTimeout(function () {
      fetch(url, {
        headers: { 'X-Requested-With': 'XMLHttpRequest' },
        credentials: 'same-origin',
      })
        .then(function (resp) {
          if (!resp.ok) throw new Error(resp.status);
          return resp.text();
        })
        .then(function (raw) {
          var parser = new DOMParser();
          var doc = parser.parseFromString(raw, 'text/html');
          var data = extractContent(doc);

          if (!data.html) {
            // no content found, full reload as fallback
            location.href = url;
            return;
          }

          // swap content
          cleanupOldScripts();
          contentBody.innerHTML = data.html;
          injectScripts(data.html);
          document.title = data.title;

          if (pushState) {
            history.pushState({ path: url }, '', url);
          }

          updateActiveLink(url);

          // scroll content area to top
          contentBody.scrollTop = 0;
          var mainContent = document.querySelector('.main-content');
          if (mainContent) mainContent.scrollTop = 0;

          // fade in
          requestAnimationFrame(function () {
            contentBody.style.opacity = '1';
          });

          isNavigating = false;
        })
        .catch(function () {
          // on any error, do a full page load
          location.href = url;
        });
    }, TRANSITION_MS);
  }

  // ── event listeners ──

  // Intercept sidebar link clicks
  if (sidebarMenu) {
    sidebarMenu.addEventListener('click', function (e) {
      var link = e.target.closest('.sidebar-link');
      if (!link) return;

      // ignore if modifier key held (ctrl+click, etc.)
      if (e.ctrlKey || e.metaKey || e.shiftKey || e.altKey) return;

      // ignore if same page
      var linkPath = new URL(link.href, location.origin).pathname;
      var currentPath = location.pathname;
      if (linkPath === currentPath) {
        e.preventDefault();
        return;
      }

      e.preventDefault();
      navigateTo(link.getAttribute('href'), true);
    });
  }

  // Handle browser back/forward
  window.addEventListener('popstate', function (e) {
    if (e.state && e.state.path) {
      navigateTo(e.state.path, false);
    } else {
      navigateTo(location.href, false);
    }
  });

  // Mark initial state
  history.replaceState({ path: location.href }, '', location.href);
})();
