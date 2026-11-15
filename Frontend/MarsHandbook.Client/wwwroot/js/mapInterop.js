// Leaflet <-> Blazor bridge. Mars radius (km) used for all geodesy.
const R = 3389.5;
const rad = d => d * Math.PI / 180, deg = r => r * 180 / Math.PI;

const TREK = 'https://trek.nasa.gov/tiles/Mars/EQ';
const BASEMAPS = {
  mola:   { url: `${TREK}/Mars_MGS_MOLA_ClrShade_merge_global_463m/1.0.0/default/default028mm/{z}/{y}/{x}.jpg`, label: 'MOLA elevation', maxNative: 5 },
  viking: { url: `${TREK}/Mars_Viking_MDIM21_ClrMosaic_global_232m/1.0.0/default/default028mm/{z}/{y}/{x}.jpg`, label: 'Viking color mosaic', maxNative: 5 },
};

let map, dotnet, base, grid, pois, poiLayer, routeLine, vertexLayer, reachLayer;
let cursor = [10, 60], nightLayer, solarTimer, evaTimer, evaMarker, evaTrail;
let points = [], drawing = false, basemapKey = 'mola', loaded = 0, errors = 0;

function emitRoute() {
  dotnet?.invokeMethodAsync('OnRouteChanged', points.map(p => ({ lat: p[0], lng: p[1] })));
}
function status(s) { dotnet?.invokeMethodAsync('OnBasemapStatus', s); }

function offlineGrid() {
  const L_ = L.GridLayer.extend({
    createTile(c) {
      const t = document.createElement('canvas'); t.width = t.height = 256;
      const g = t.getContext('2d'), n = 2 ** c.z, step = 180 / n;
      g.fillStyle = '#2a1812'; g.fillRect(0, 0, 256, 256);
      const grd = g.createLinearGradient(0, 0, 256, 256);
      grd.addColorStop(0, 'rgba(201,104,60,.22)'); grd.addColorStop(1, 'rgba(90,40,24,.1)');
      g.fillStyle = grd; g.fillRect(0, 0, 256, 256);
      g.strokeStyle = 'rgba(255,200,160,.18)'; g.strokeRect(.5, .5, 255, 255);
      g.fillStyle = 'rgba(255,200,160,.35)'; g.font = '10px JetBrains Mono, monospace';
      const lat = 90 - c.y * step, lng = -180 + c.x * step;
      g.fillText(`${lat.toFixed(1)}°, ${lng.toFixed(1)}°`, 6, 14);
      return t;
    }
  });
  return new L_({ tileSize: 256, noWrap: true, maxZoom: 9 });
}

function makeBase(key) {
  if (key === 'grid') return offlineGrid();
  const b = BASEMAPS[key];
  loaded = 0; errors = 0;
  const l = L.tileLayer(b.url, {
    tileSize: 256, noWrap: true, minZoom: 0, maxNativeZoom: b.maxNative, maxZoom: 9,
    attribution: 'Imagery: NASA/JPL · Mars Trek · MOLA/Viking'
  });
  l.on('tileload', () => { loaded++; if (loaded === 1) status('ok'); });
  l.on('tileerror', () => { errors++; if (errors >= 6 && loaded === 0) { status('offline'); setBasemap('grid', true); } });
  return l;
}

export function initMap(el, ref) {
  dotnet = ref;
  map = L.map(el, { crs: L.CRS.EPSG4326, center: [10, 60], zoom: 1, minZoom: 0, maxZoom: 9, zoomControl: false, worldCopyJump: false, attributionControl: true });
  L.control.zoom({ position: 'bottomright' }).addTo(map);
  L.control.scale({ metric: true, imperial: false, position: 'bottomleft' }).addTo(map);
  base = makeBase(basemapKey).addTo(map);
  poiLayer = L.layerGroup().addTo(map);
  reachLayer = L.layerGroup().addTo(map);
  vertexLayer = L.layerGroup().addTo(map);
  routeLine = L.polyline([], { color: '#ff5a1f', weight: 2.5, className: 'route-flow' }).addTo(map);
  map.on('click', e => {
    if (!drawing) return;
    points.push([+e.latlng.lat.toFixed(5), +e.latlng.lng.toFixed(5)]);
    redraw(); emitRoute();
  });
  map.createPane('night'); map.getPane('night').style.zIndex = 380; map.getPane('night').style.pointerEvents = 'none';
  const xh = document.createElement('div'); xh.className = 'xh';
  xh.innerHTML = '<i class="h"></i><i class="v"></i>'; map.getContainer().appendChild(xh);
  map.on('mousemove', e => {
    cursor = [e.latlng.lat, e.latlng.lng];
    const c = document.getElementById('coords');
    if (c) c.textContent = `${e.latlng.lat.toFixed(3)}°  ${e.latlng.lng.toFixed(3)}°`;
    xh.style.opacity = 1; xh.firstChild.style.top = e.containerPoint.y + 'px'; xh.lastChild.style.left = e.containerPoint.x + 'px';
  });
  map.on('mouseout', () => { xh.style.opacity = 0; });
  drawNight(); solarTimer = setInterval(() => { drawNight(); updateSolar(); }, 1000); updateSolar();
  const fit = () => { map.invalidateSize(); };
  new ResizeObserver(fit).observe(map.getContainer());
  setTimeout(() => { fit(); map.setView([10, 60], 1, { animate: false }); }, 150);
}

function redraw() {
  routeLine.setLatLngs(points);
  vertexLayer.clearLayers();
  points.forEach((p, i) => {
    const m = L.marker(p, { icon: L.divIcon({ className: 'wp', html: `<span>${i === 0 ? 'H' : i}</span>`, iconSize: [22, 22] }), draggable: true });
    m.on('dragend', ev => { const ll = ev.target.getLatLng(); points[i] = [+ll.lat.toFixed(5), +ll.lng.toFixed(5)]; redraw(); emitRoute(); });
    m.addTo(vertexLayer);
  });
}

export function setBasemap(key, silent) {
  basemapKey = key;
  if (base) map.removeLayer(base);
  base = makeBase(key).addTo(map); base.bringToBack();
  if (!silent && key === 'grid') status('offline');
}
export function setPois(list, cb) {
  poiLayer.clearLayers();
  list.forEach(p => {
    const m = L.marker([p.lat, p.lng], { icon: L.divIcon({ className: 'poi poi-' + p.kind, html: '<i></i>', iconSize: [20, 20] }), title: p.name });
    m.bindTooltip(p.name, { direction: 'top', offset: [0, -8] });
    m.on('click', () => dotnet.invokeMethodAsync('OnPoiClicked', p.id));
    m.addTo(poiLayer);
  });
}
export function setDrawing(on) { drawing = on; map.getContainer().classList.toggle('drawing', on); }
export function setRoute(list) { points = list.map(p => [p.lat, p.lng]); redraw(); emitRoute(); }
export function clearRoute() { points = []; redraw(); emitRoute(); }
export function undoPoint() { points.pop(); redraw(); emitRoute(); }
export function flyTo(lat, lng, zoom, dur) { map.flyTo([lat, lng], zoom, { duration: dur || 1.4 }); }
export function setReach(lat, lng, km) {
  reachLayer.clearLayers();
  if (!(km > 0)) return;
  const ring = [], d = km / R, la = rad(lat), lo = rad(lng);
  for (let b = 0; b <= 360; b += 5) {
    const t = rad(b);
    const la2 = Math.asin(Math.sin(la) * Math.cos(d) + Math.cos(la) * Math.sin(d) * Math.cos(t));
    const lo2 = lo + Math.atan2(Math.sin(t) * Math.sin(d) * Math.cos(la), Math.cos(d) - Math.sin(la) * Math.sin(la2));
    ring.push([deg(la2), ((deg(lo2) + 540) % 360) - 180]);
  }
  L.polygon(ring, { color: '#ffffff', weight: 1, fillColor: '#ffffff', fillOpacity: .05, dashArray: '3 5', interactive: false }).addTo(reachLayer);
}
export function fitRoute() { if (points.length > 1) map.fitBounds(L.latLngBounds(points), { padding: [60, 60], maxZoom: 9 }); }
export function disposeMap() { stopEva(); clearInterval(solarTimer); nightLayer = null; if (map) { map.remove(); map = null; points = []; } dotnet = null; }


// ---------------- Mars solar clock (Allison & McEwen 2000, Mars24 algorithm) ----------------
export function marsSolar(now = new Date()) {
  const jdUT = now.getTime() / 86400000 + 2440587.5;
  const jdTT = jdUT + 69.184 / 86400;
  const dt = jdTT - 2451545.0;
  const M = (19.3871 + 0.52402073 * dt) % 360;
  const aFMS = 270.3871 + 0.524038496 * dt;
  const A = [0.0071, 0.0057, 0.0039, 0.0037, 0.0021, 0.0020, 0.0018];
  const T = [2.2353, 2.7543, 1.1177, 15.7866, 2.1354, 2.4694, 32.8493];
  const P = [49.409, 168.173, 191.837, 21.736, 15.704, 95.528, 49.095];
  let pbs = 0; for (let i = 0; i < 7; i++) pbs += A[i] * Math.cos(rad(0.985626 * dt / T[i] + P[i]));
  const vM = (10.691 + 3e-7 * dt) * Math.sin(rad(M)) + 0.623 * Math.sin(rad(2 * M)) + 0.050 * Math.sin(rad(3 * M)) + 0.005 * Math.sin(rad(4 * M)) + 0.0005 * Math.sin(rad(5 * M)) + pbs;
  const ls = ((aFMS + vM) % 360 + 360) % 360;
  const eot = 2.861 * Math.sin(rad(2 * ls)) - 0.071 * Math.sin(rad(4 * ls)) + 0.002 * Math.sin(rad(6 * ls)) - vM; // degrees
  const dec = deg(Math.asin(0.42565 * Math.sin(rad(ls)))) + 0.25 * Math.sin(rad(ls));
  const msd = (jdTT - 2451549.5) / 1.0274912517 + 44796.0 - 0.0009626;
  const mtc = ((msd % 1) + 1) % 1 * 24;
  let subLng = 15 * (12 - mtc) - eot; subLng = ((subLng + 540) % 360) - 180;
  return { ls, dec, eot, msd, mtc, subLng };
}
const hhmm = h => { h = ((h % 24) + 24) % 24; const H = Math.floor(h), m = Math.floor((h - H) * 60); return String(H).padStart(2, '0') + ':' + String(m).padStart(2, '0'); };
function sunElev(lat, lng, s) { return deg(Math.asin(Math.sin(rad(lat)) * Math.sin(rad(s.dec)) + Math.cos(rad(lat)) * Math.cos(rad(s.dec)) * Math.cos(rad(lng - s.subLng)))); }
function updateSolar() {
  const el = document.getElementById('solar'); if (!el) return;
  const s = marsSolar(), [lat, lng] = cursor, ltst = s.mtc + lng / 15 + s.eot / 15, el_ = sunElev(lat, lng, s);
  const phase = el_ > 6 ? 'DAY' : el_ > -6 ? 'TWILIGHT' : 'NIGHT';
  el.innerHTML = `SOL <b>${Math.floor(s.msd)}</b> · Ls <b>${s.ls.toFixed(0)}°</b> · LTST <b>${hhmm(ltst)}</b> <span class="ph ${phase.toLowerCase()}">${phase}</span> · SUN <b>${el_.toFixed(0)}°</b>`;
}
function drawNight() {
  if (!map) return;
  const s = marsSolar(); let dec = rad(s.dec); if (Math.abs(dec) < 0.002) dec = 0.002;
  const pts = [];
  for (let lng = -180; lng <= 180; lng += 3) pts.push([deg(Math.atan(-Math.cos(rad(lng - s.subLng)) / Math.tan(dec))), lng]);
  const edge = dec > 0 ? -90 : 90;
  const poly = [...pts, [edge, 180], [edge, -180]];
  if (!nightLayer) nightLayer = L.layerGroup().addTo(map);
  nightLayer.clearLayers();
  L.polygon(poly, { pane: 'night', stroke: false, fillColor: '#02030a', fillOpacity: .58, interactive: false }).addTo(nightLayer);
  L.polyline(pts, { pane: 'night', color: '#ffb36b', weight: 1, opacity: .45, interactive: false, className: 'terminator' }).addTo(nightLayer);
}

// ---------------- EVA simulation ----------------
const hav = (a, b) => { const dLa = rad(b[0] - a[0]), dLo = rad(b[1] - a[1]); const x = Math.sin(dLa / 2) ** 2 + Math.cos(rad(a[0])) * Math.cos(rad(b[0])) * Math.sin(dLo / 2) ** 2; return 2 * R * Math.asin(Math.min(1, Math.sqrt(x))); };
export function startEva(ms, outAndBack) {
  stopEva(); if (points.length < 2) return;
  const path = outAndBack ? [...points, ...points.slice(0, -1).reverse()] : points.slice();
  const seg = []; let tot = 0; for (let i = 1; i < path.length; i++) { const d = hav(path[i - 1], path[i]); seg.push(d); tot += d; }
  evaMarker = L.marker(path[0], { icon: L.divIcon({ className: 'astro', html: '<i></i>', iconSize: [26, 26] }), interactive: false, zIndexOffset: 2000 }).addTo(map);
  evaTrail = L.polyline([path[0]], { color: '#ffffff', weight: 3, opacity: .95, interactive: false }).addTo(map);
  const t0 = performance.now(); let lastEmit = 0, lastPan = 0;
  const step = now => {
    const f = Math.min(1, (now - t0) / ms); let d = f * tot, i = 0;
    while (i < seg.length - 1 && d > seg[i]) { d -= seg[i]; i++; }
    const a = path[i], b = path[i + 1] || path[i], t = seg[i] ? Math.min(1, d / seg[i]) : 1;
    const p = [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t];
    evaMarker.setLatLng(p); evaTrail.setLatLngs([...path.slice(0, i + 1), p]);
    if (now - lastPan > 450) { map.panTo(p, { animate: true, duration: .45, easeLinearity: 1, noMoveStart: true }); lastPan = now; }
    if (now - lastEmit > 100 || f === 1) { dotnet?.invokeMethodAsync('OnEvaProgress', f); lastEmit = now; }
    if (f < 1) evaTimer = requestAnimationFrame(step); else dotnet?.invokeMethodAsync('OnEvaDone');
  };
  evaTimer = requestAnimationFrame(step);
}
export function stopEva() {
  if (evaTimer) cancelAnimationFrame(evaTimer); evaTimer = null;
  [evaMarker, evaTrail].forEach(x => x && map && map.removeLayer(x)); evaMarker = evaTrail = null;
}

// ---------------- Intro starfield + planet ----------------
export function startStars(id) {
  const c = document.getElementById(id); if (!c) return;
  const g = c.getContext('2d'), dpr = Math.min(2, window.devicePixelRatio || 1);
  let w, h, mx = 0, my = 0;
  const stars = Array.from({ length: 280 }, () => ({ x: Math.random(), y: Math.random(), z: Math.random() * .9 + .1, p: Math.random() * 6 }));
  const resize = () => { const r = c.getBoundingClientRect(); w = c.width = r.width * dpr; h = c.height = r.height * dpr; };
  resize(); window.addEventListener('resize', resize);
  const host = c.parentElement;
  host.addEventListener('mousemove', e => { mx = e.clientX / innerWidth - .5; my = e.clientY / innerHeight - .5; });
  const draw = t => {
    if (!document.body.contains(c) || host.classList.contains('out')) { window.removeEventListener('resize', resize); return; }
    g.clearRect(0, 0, w, h);
    const R = Math.max(h, w * .55) * .62, cx = w * .8 - mx * 28 * dpr, cy = h * 1.02 - my * 18 * dpr;
    g.save();
    g.shadowColor = 'rgba(255,110,50,.45)'; g.shadowBlur = 70 * dpr;
    g.beginPath(); g.arc(cx, cy, R, 0, 7); g.fillStyle = '#220d06'; g.fill();
    g.shadowBlur = 0; g.clip();
    const gr = g.createRadialGradient(cx - R * .45, cy - R * .5, R * .05, cx, cy, R);
    gr.addColorStop(0, '#d2622f'); gr.addColorStop(.5, '#7a321a'); gr.addColorStop(1, '#100604');
    g.fillStyle = gr; g.fillRect(cx - R, cy - R, R * 2, R * 2);
    for (let i = 0; i < 18; i++) { // slow drifting dust bands
      const y = cy - R + (i / 18) * R * 2, off = Math.sin(t / 9000 + i) * R * .08;
      g.fillStyle = `rgba(${i % 2 ? '20,6,2' : '255,150,90'},${.012 + (i % 3) * .006})`;
      g.fillRect(cx - R + off, y, R * 2, R * .05);
    }
    g.restore();
    stars.forEach(s => {
      const x = ((s.x * w + mx * s.z * 40 * dpr + t * .004 * s.z) % w + w) % w, y = (s.y * h + my * s.z * 26 * dpr) % h;
      g.fillStyle = `rgba(255,255,255,${(.25 + .75 * Math.abs(Math.sin(t / 900 + s.p))) * s.z})`;
      g.fillRect(x, y, s.z * 1.6 * dpr, s.z * 1.6 * dpr);
    });
    requestAnimationFrame(draw);
  };
  requestAnimationFrame(draw);
}
