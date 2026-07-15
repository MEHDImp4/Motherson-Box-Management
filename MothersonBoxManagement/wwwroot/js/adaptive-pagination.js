(() => {
  const min = 5;
  const max = 100;
  let timer;

  function capacity(table) {
    const row = table.querySelector('tbody tr');
    const rowHeight = Math.max(44, row?.getBoundingClientRect().height || 56);
    const top = table.getBoundingClientRect().top;
    const footer = 120;
    return Math.max(min, Math.min(max, Math.floor((window.innerHeight - top - footer) / rowHeight)));
  }

  function sync() {
    document.querySelectorAll('[data-adaptive-pagination]').forEach((table) => {
      const size = capacity(table);
      const url = new URL(window.location.href);
      if (Number(url.searchParams.get('pageSize')) === size) return;
      url.searchParams.set('pageSize', String(size));
      url.searchParams.set('page', '1');
      window.location.replace(url.toString());
    });
  }

  document.addEventListener('DOMContentLoaded', () => {
    if (!document.querySelector('[data-adaptive-pagination]')) return;
    window.setTimeout(sync, 50);
    window.addEventListener('resize', () => {
      window.clearTimeout(timer);
      timer = window.setTimeout(sync, 250);
    });
  });
})();
