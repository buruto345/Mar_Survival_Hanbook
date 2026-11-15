// Small browser helpers used by the Handbook pages.
window.mhui = {
  scrollTo: function (id) { var e = document.getElementById(id); if (e) e.scrollIntoView({ behavior: 'smooth', block: 'start' }); },
  bmGet: function () { try { return localStorage.getItem('mh.bm') || ''; } catch (e) { return ''; } },
  bmSet: function (v) { try { localStorage.setItem('mh.bm', v); } catch (e) { } }
};
