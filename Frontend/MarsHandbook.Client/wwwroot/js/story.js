// Scroll-driven Mars story: procedural 3D planet (Three.js) + per-section colour/pose morphing.
let raf = 0, renderer, cleanup = [];
const loadScript = src => new Promise((res, rej) => {
  if (window.THREE) return res();
  const s = document.createElement('script'); s.src = src; s.onload = res; s.onerror = rej; document.head.appendChild(s);
});
const lerp = (a, b, t) => a + (b - a) * t, sm = (a, b, x) => { const t = Math.min(1, Math.max(0, (x - a) / (b - a))); return t * t * (3 - 2 * t); };
const hex = h => [parseInt(h.slice(1, 3), 16), parseInt(h.slice(3, 5), 16), parseInt(h.slice(5, 7), 16)];

// Per-section look: background colour + planet pose (x,y in view units, scale, dim)
const SEC = [
  { bg: '#050607', x: .52, y: .0, s: 1.25, dim: 1 },
  { bg: '#1c0b06', x: -.5, y: .0, s: 1.7, dim: 1 },
  { bg: '#e9dccb', x: .62, y: -.12, s: .85, dim: .55 },
  { bg: '#ff5a1f', x: -.62, y: .08, s: .62, dim: .8 },
  { bg: '#050607', x: 0, y: .05, s: 2.0, dim: .9 },
].map(o => ({ ...o, c: hex(o.bg) }));

const VERT = `varying vec3 vP; varying vec3 vN; void main(){ vP=position; vN=normalize(normalMatrix*normal); gl_Position=projectionMatrix*modelViewMatrix*vec4(position,1.); }`;
const FRAG = `
varying vec3 vP; varying vec3 vN; uniform vec3 uLight; uniform float uDim;
float h(vec3 p){ return fract(sin(dot(p,vec3(127.1,311.7,74.7)))*43758.5453); }
float n(vec3 p){ vec3 i=floor(p), f=fract(p); f=f*f*(3.-2.*f);
  return mix(mix(mix(h(i),h(i+vec3(1,0,0)),f.x),mix(h(i+vec3(0,1,0)),h(i+vec3(1,1,0)),f.x),f.y),
             mix(mix(h(i+vec3(0,0,1)),h(i+vec3(1,0,1)),f.x),mix(h(i+vec3(0,1,1)),h(i+vec3(1,1,1)),f.x),f.y),f.z); }
float fbm(vec3 p){ float a=.5,s=0.; for(int i=0;i<6;i++){ s+=a*n(p); p*=2.03; a*=.5; } return s; }
void main(){
  vec3 p=normalize(vP); float t=fbm(p*2.6); float t2=fbm(p*9.+3.);
  vec3 dark=vec3(.23,.09,.05), mid=vec3(.62,.27,.13), light=vec3(.88,.57,.35);
  vec3 c=mix(dark,mid,smoothstep(.3,.6,t)); c=mix(c,light,smoothstep(.55,.8,t)*.8); c*=.85+.3*t2;
  c=mix(c,dark*.65,smoothstep(.45,.3,fbm(p*1.4+9.))*.65);
  float pole=smoothstep(.84,.95,abs(p.y)+(t-.5)*.08); c=mix(c,vec3(.93,.95,.98),pole);
  vec3 N=normalize(vN); float d=max(dot(N,normalize(uLight)),0.); c*=.07+d*1.2;
  float rim=pow(1.-max(dot(N,vec3(0.,0.,1.)),0.),3.); c+=vec3(.95,.48,.28)*rim*.4*(.25+d);
  gl_FragColor=vec4(c*uDim,1.);
}`;
const AFRAG = `varying vec3 vN; void main(){ float i=pow(.74-dot(normalize(vN),vec3(0.,0.,1.)),3.); gl_FragColor=vec4(1.,.5,.3,1.)*max(i,0.)*.9; }`;

export async function initStory(root, canvas) {
  if (typeof root === 'string') root = document.querySelector(root);
  if (typeof canvas === 'string') canvas = document.querySelector(canvas);
  if (!root || !canvas) return;
  const secs = [...root.querySelectorAll('[data-sec]')];
  let ok = true;
  try { await loadScript('https://cdnjs.cloudflare.com/ajax/libs/three.js/r128/three.min.js'); } catch { ok = false; }
  if (!window.THREE) ok = false;
  const reveal = new IntersectionObserver(es => es.forEach(e => { if (e.isIntersecting) { e.target.classList.add('in'); count(e.target); } }), { root, threshold: .35 });
  secs.forEach(s => reveal.observe(s)); cleanup.push(() => reveal.disconnect());
  function count(sec) {
    sec.querySelectorAll('[data-count]').forEach(el => {
      if (el.dataset.done) return; el.dataset.done = 1;
      const to = parseFloat(el.dataset.count), dec = (el.dataset.count.split('.')[1] || '').length, t0 = performance.now();
      const tick = n => { const f = Math.min(1, (n - t0) / 1400), v = to * (1 - Math.pow(1 - f, 3)); el.textContent = v.toFixed(dec); if (f < 1) requestAnimationFrame(tick); };
      requestAnimationFrame(tick);
    });
  }
  let prog = 0, mx = 0, my = 0;
  const onScroll = () => { prog = root.scrollTop / Math.max(1, root.scrollHeight - root.clientHeight) * (secs.length - 1); };
  root.addEventListener('scroll', onScroll, { passive: true }); onScroll();
  root.addEventListener('mousemove', e => { mx = e.clientX / innerWidth - .5; my = e.clientY / innerHeight - .5; });
  const pose = { x: SEC[0].x, y: SEC[0].y, s: SEC[0].s, dim: 1, c: SEC[0].c.slice() };
  function target() {
    const i = Math.min(secs.length - 2, Math.floor(prog)), f = prog - i, t = sm(.55, 1, f), a = SEC[i], b = SEC[i + 1];
    return { x: lerp(a.x, b.x, t), y: lerp(a.y, b.y, t), s: lerp(a.s, b.s, t), dim: lerp(a.dim, b.dim, t), c: a.c.map((v, k) => lerp(v, b.c[k], t)) };
  }
  if (!ok) { // no WebGL / CDN: still do the colour story
    const loop = () => { const g = target(); root.style.background = `rgb(${g.c.map(Math.round)})`; raf = requestAnimationFrame(loop); };
    root.classList.add('no3d'); loop(); return;
  }
  const THREE = window.THREE;
  renderer = new THREE.WebGLRenderer({ canvas, antialias: true, alpha: true });
  renderer.setPixelRatio(Math.min(2, devicePixelRatio));
  const scene = new THREE.Scene(), cam = new THREE.PerspectiveCamera(38, 1, .1, 100); cam.position.z = 5;
  const mat = new THREE.ShaderMaterial({ vertexShader: VERT, fragmentShader: FRAG, uniforms: { uLight: { value: new THREE.Vector3(-.7, .45, .8) }, uDim: { value: 1 } } });
  const planet = new THREE.Mesh(new THREE.SphereGeometry(1, 96, 96), mat);
  const atmo = new THREE.Mesh(new THREE.SphereGeometry(1.14, 64, 64), new THREE.ShaderMaterial({ vertexShader: VERT, fragmentShader: AFRAG, blending: THREE.AdditiveBlending, side: THREE.BackSide, transparent: true, depthWrite: false }));
  const grp = new THREE.Group(); grp.add(planet, atmo); grp.rotation.z = .44; scene.add(grp);
  const sp = new Float32Array(900); for (let i = 0; i < 900; i++) sp[i] = (Math.random() - .5) * (i % 3 === 2 ? 20 : 30);
  const sg = new THREE.BufferGeometry(); sg.setAttribute('position', new THREE.BufferAttribute(sp, 3));
  const stars = new THREE.Points(sg, new THREE.PointsMaterial({ color: 0xffffff, size: .035, transparent: true, opacity: .8 })); scene.add(stars);
  const resize = () => { const w = canvas.clientWidth, h = canvas.clientHeight; renderer.setSize(w, h, false); cam.aspect = w / h; cam.updateProjectionMatrix(); };
  resize(); window.addEventListener('resize', resize); cleanup.push(() => window.removeEventListener('resize', resize));
  const t0 = performance.now(); let lastP = prog;
  const frame = now => {
    const g = target(), k = .08, w = canvas.clientWidth / canvas.clientHeight, spread = Math.min(2.3, 1.15 * w);
    pose.x = lerp(pose.x, g.x, k); pose.y = lerp(pose.y, g.y, k); pose.s = lerp(pose.s, g.s, k); pose.dim = lerp(pose.dim, g.dim, k);
    pose.c = pose.c.map((v, i) => lerp(v, g.c[i], .12));
    root.style.background = `rgb(${pose.c.map(Math.round)})`;
    grp.position.set(pose.x * spread + mx * .15, pose.y * 2 - my * .1, 0); grp.scale.setScalar(pose.s);
    planet.rotation.y = (now - t0) * .00006 + prog * 1.9; planet.rotation.x = my * .12;
    mat.uniforms.uDim.value = pose.dim; stars.rotation.y = (now - t0) * .00001 + prog * .05; stars.material.opacity = .8 * (1 - sm(1.5, 2.5, prog) * (1 - sm(3, 3.6, prog)) * .95);
    renderer.render(scene, cam); raf = requestAnimationFrame(frame);
  };
  raf = requestAnimationFrame(frame);
}
export function disposeStory() {
  cancelAnimationFrame(raf); cleanup.forEach(f => f()); cleanup = [];
  if (renderer) { renderer.dispose(); renderer = null; }
}
