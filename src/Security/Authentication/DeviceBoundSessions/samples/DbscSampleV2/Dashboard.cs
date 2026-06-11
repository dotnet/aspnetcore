// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace DbscSampleV2;

/// <summary>The dark-themed single-page debug dashboard served at <c>/</c>.</summary>
public static class Dashboard
{
    public const string Html =
"""
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width, initial-scale=1" />
<title>DBSC Debug Console</title>
<style>
  :root {
    --bg:#0d1117; --panel:#161b22; --panel2:#1c2330; --border:#30363d;
    --text:#c9d1d9; --muted:#8b949e; --accent:#58a6ff; --green:#3fb950;
    --red:#f85149; --amber:#d29922; --purple:#bc8cff; --teal:#39c5cf;
  }
  * { box-sizing:border-box; }
  body {
    margin:0; height:100vh; display:flex; gap:1px; background:var(--border);
    color:var(--text); font:13px/1.5 'Segoe UI',system-ui,sans-serif;
  }
  .col { background:var(--bg); overflow-y:auto; height:100vh; }
  .left { width:380px; min-width:340px; padding:18px; }
  .right { flex:1; display:flex; flex-direction:column; }
  h1 { font-size:18px; margin:0 0 2px; }
  h1 .v { color:var(--accent); }
  .sub { color:var(--muted); font-size:12px; margin-bottom:16px; }
  .card { background:var(--panel); border:1px solid var(--border); border-radius:8px; padding:14px; margin-bottom:14px; }
  .card h2 { font-size:12px; text-transform:uppercase; letter-spacing:.6px; color:var(--muted); margin:0 0 10px; }
  label { display:block; font-size:12px; color:var(--muted); margin:8px 0 4px; }
  input[type=text], input[type=number] {
    width:100%; padding:8px 10px; background:var(--bg); color:var(--text);
    border:1px solid var(--border); border-radius:6px; font-size:13px;
  }
  input:focus { outline:none; border-color:var(--accent); }
  .row { display:flex; gap:8px; }
  .row > * { flex:1; }
  button, .btn {
    display:inline-block; text-align:center; cursor:pointer; padding:8px 12px; font-size:13px;
    background:var(--panel2); color:var(--text); border:1px solid var(--border);
    border-radius:6px; text-decoration:none; margin-top:8px; transition:.12s;
  }
  button:hover, .btn:hover { border-color:var(--accent); color:#fff; }
  .btn.primary { background:var(--accent); color:#0d1117; border-color:var(--accent); font-weight:600; }
  .btn.primary:hover { filter:brightness(1.1); color:#0d1117; }
  .btn.danger:hover { border-color:var(--red); color:var(--red); }
  .status-line { display:flex; align-items:center; gap:8px; font-size:14px; }
  .dot { width:9px; height:9px; border-radius:50%; background:var(--red); box-shadow:0 0 6px var(--red); }
  .dot.on { background:var(--green); box-shadow:0 0 6px var(--green); }
  .kv { display:flex; justify-content:space-between; font-size:12px; padding:3px 0; border-bottom:1px dashed var(--border); }
  .kv:last-child { border-bottom:none; }
  .kv b { color:var(--text); font-weight:600; }
  .kv span { color:var(--muted); font-family:Consolas,monospace; }
  .muted { color:var(--muted); font-size:12px; }
  .cookie { background:var(--bg); border:1px solid var(--border); border-radius:6px; padding:8px; margin-top:8px; font-size:12px; }
  .cookie .nm { font-family:Consolas,monospace; color:var(--teal); word-break:break-all; }
  .legend span { display:inline-block; margin:3px 8px 0 0; font-size:11px; }
  .swatch { display:inline-block; width:9px; height:9px; border-radius:2px; margin-right:4px; vertical-align:middle; }

  .right-head { display:flex; align-items:center; gap:12px; padding:12px 16px; background:var(--panel); border-bottom:1px solid var(--border); }
  .right-head h2 { margin:0; font-size:14px; }
  .right-head .count { color:var(--muted); font-size:12px; }
  .right-head .spacer { flex:1; }
  .toggle { display:flex; align-items:center; gap:6px; font-size:12px; color:var(--muted); cursor:pointer; }
  .log { flex:1; overflow-y:auto; padding:10px 14px; }
  .entry { border:1px solid var(--border); border-left-width:3px; border-radius:6px; margin-bottom:7px; background:var(--panel); overflow:hidden; }
  .entry.register { border-left-color:var(--purple); }
  .entry.refresh  { border-left-color:var(--accent); }
  .entry.api      { border-left-color:var(--teal); }
  .entry.auth     { border-left-color:var(--amber); }
  .entry.page     { border-left-color:var(--muted); }
  .ehead { display:flex; align-items:center; gap:10px; padding:8px 10px; cursor:pointer; }
  .ehead:hover { background:var(--panel2); }
  .ehead .time { color:var(--muted); font-family:Consolas,monospace; font-size:11px; }
  .ehead .method { font-weight:700; min-width:42px; }
  .ehead .path { flex:1; font-family:Consolas,monospace; color:var(--text); word-break:break-all; }
  .badge { font-size:11px; padding:1px 7px; border-radius:10px; font-family:Consolas,monospace; }
  .badge.cat { background:var(--panel2); color:var(--muted); }
  .badge.s2 { background:rgba(63,185,80,.15); color:var(--green); }
  .badge.s3 { background:rgba(88,166,255,.15); color:var(--accent); }
  .badge.s4 { background:rgba(210,153,34,.15); color:var(--amber); }
  .badge.s5 { background:rgba(248,81,73,.15); color:var(--red); }
  .ebody { display:none; padding:8px 12px 12px; border-top:1px solid var(--border); background:var(--bg); }
  .entry.open .ebody { display:block; }
  .sec { margin-top:10px; }
  .sec h4 { margin:0 0 4px; font-size:11px; text-transform:uppercase; letter-spacing:.5px; color:var(--accent); }
  .sec.challenge h4 { color:var(--purple); }
  .sec.proof h4 { color:var(--teal); }
  pre { margin:4px 0 0; padding:8px; background:#0a0e14; border:1px solid var(--border); border-radius:6px;
        font:11px/1.45 Consolas,monospace; color:#9db3c7; overflow-x:auto; white-space:pre-wrap; word-break:break-word; }
  .pill { display:inline-block; font-size:10px; padding:1px 6px; border-radius:8px; margin-left:6px; }
  .pill.ok { background:rgba(63,185,80,.15); color:var(--green); }
  .pill.bad { background:rgba(248,81,73,.15); color:var(--red); }
  .empty { color:var(--muted); text-align:center; padding:40px; font-size:13px; }
</style>
</head>
<body>
  <div class="col left">
    <h1>DBSC <span class="v">Debug Console</span></h1>
    <div class="sub">Device Bound Session Credentials &mdash; live protocol inspector</div>

    <div class="card">
      <h2>Session</h2>
      <div class="status-line"><span id="dot" class="dot"></span><span id="who">Not signed in</span></div>
      <div class="kv" style="margin-top:8px"><b>Session TTL</b><span id="ttlNow">&mdash;</span></div>
    </div>

    <div class="card">
      <h2>Sign in</h2>
      <div id="loginError" style="display:none;margin-bottom:8px;padding:8px;border-radius:6px;background:rgba(220,50,50,.15);color:var(--red);font-size:12px"></div>
      <form method="post" action="/login">
        <label>Username</label>
        <input type="text" name="username" value="alice" />
        <label>Bound session TTL (seconds)</label>
        <input type="number" name="ttl" id="ttlInput" value="30" min="1" max="86400" />
        <button type="submit" class="btn primary" style="width:100%">Sign in &amp; register</button>
      </form>
      <div class="muted" style="margin-top:8px">TTL controls how long each short-lived bound cookie lasts before a device-key refresh is required.</div>
    </div>

    <div class="card">
      <h2>Actions</h2>
      <div class="row">
        <a class="btn" href="/signout">Sign out</a>
        <a class="btn danger" href="/clear">Clear all &amp; restart</a>
      </div>
      <button class="btn" style="width:100%" onclick="clearLog()">Clear log</button>
      <label class="toggle" style="margin-top:10px"><input type="checkbox" id="autoPing" /> Auto-call <code>/api/time</code> every 4s</label>
      <button class="btn" style="width:100%" onclick="ping()">Call /api/time now</button>
    </div>

    <div class="card" id="cookiesCard">
      <h2>Decoded cookies</h2>
      <div id="cookies"><div class="muted">No DBSC cookies present.</div></div>
    </div>

    <div class="card legend">
      <h2>Legend</h2>
      <span><span class="swatch" style="background:var(--purple)"></span>register</span>
      <span><span class="swatch" style="background:var(--accent)"></span>refresh</span>
      <span><span class="swatch" style="background:var(--teal)"></span>api</span>
      <span><span class="swatch" style="background:var(--amber)"></span>auth</span>
      <span><span class="swatch" style="background:var(--muted)"></span>page</span>
    </div>
  </div>

  <div class="col right">
    <div class="right-head">
      <h2>Live exchange log</h2>
      <span class="count" id="count">0 entries</span>
      <span class="spacer"></span>
      <label class="toggle"><input type="checkbox" id="follow" checked /> Follow</label>
    </div>
    <div class="log" id="log"><div class="empty">Waiting for traffic&hellip; sign in to begin.</div></div>
  </div>

<script>
let lastId = 0;
let firstLoad = true;
const logEl = document.getElementById('log');

// Show a login-failure banner when redirected back with ?loginError=<user>, then
// clean the URL so a refresh doesn't keep showing it.
(function(){
  const u = new URLSearchParams(location.search);
  const failed = u.get('loginError');
  if (failed !== null) {
    const el = document.getElementById('loginError');
    el.textContent = "Login failed: user '" + failed + "' is not authorized. You are still signed out.";
    el.style.display = 'block';
    history.replaceState(null, '', location.pathname);
  }
})();

function esc(s){ return String(s==null?'':s).replace(/[&<>]/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;'}[c])); }
function statusClass(s){ return 's' + Math.floor(s/100); }

async function refreshState(){
  try {
    const r = await fetch('/debug/state'); const st = await r.json();
    document.getElementById('dot').className = 'dot' + (st.authenticated ? ' on' : '');
    document.getElementById('who').textContent = st.authenticated ? ('Signed in as ' + st.user) : 'Not signed in';
    document.getElementById('ttlNow').textContent = st.ttlSeconds + 's';
    if (firstLoad) { document.getElementById('ttlInput').value = st.ttlSeconds; firstLoad = false; }
    renderCookies(st.cookies);
  } catch(e) {}
}

function renderCookies(cookies){
  const el = document.getElementById('cookies');
  if (!cookies || !cookies.length) { el.innerHTML = '<div class="muted">No DBSC cookies present.</div>'; return; }
  el.innerHTML = cookies.map(c => {
    let d = '';
    if (c.decoded) {
      const it = c.decoded.items || {};
      const rows = [];
      if (c.decoded.principal) rows.push(kv('principal', c.decoded.principal));
      if (it.DbscSessionId) rows.push(kv('sessionId', it.DbscSessionId));
      if (it.DbscAlgorithm) rows.push(kv('alg', it.DbscAlgorithm));
      if (c.decoded.expiresUtc) rows.push(kv('expires', c.decoded.expiresUtc));
      if (it.DbscPublicKeyJwk) rows.push('<pre>'+esc(it.DbscPublicKeyJwk)+'</pre>');
      d = rows.join('');
    } else {
      d = '<div class="muted">encrypted (no ticket decode)</div>';
    }
    return '<div class="cookie"><div class="nm">'+esc(c.name)+'</div>'
         + '<div class="muted">'+esc(c.scheme||'')+' &middot; '+c.valueLength+' bytes</div>'+d+'</div>';
  }).join('');
}
function kv(k,v){ return '<div class="kv"><b>'+esc(k)+'</b><span>'+esc(v)+'</span></div>'; }

async function pollLog(){
  // Long-poll loop: the request blocks on the server until the log changes or ~60s elapses,
  // then we immediately reconnect. No fixed-interval polling, so idle traffic is near zero.
  while (true) {
    try {
      const r = await fetch('/debug/log?since=' + lastId);
      const data = await r.json();
      if (data.lastId < lastId) {
        lastId = 0; logEl.innerHTML = '<div class="empty">Log cleared.</div>';
        document.getElementById('count').textContent = '0 entries';
      } else if (data.entries && data.entries.length) {
        if (lastId === 0) logEl.innerHTML = '';
        for (const e of data.entries) logEl.appendChild(renderEntry(e));
        lastId = data.lastId;
        document.getElementById('count').textContent = logEl.querySelectorAll('.entry').length + ' entries';
        if (document.getElementById('follow').checked) logEl.scrollTop = logEl.scrollHeight;
      }
    } catch(e) {
      // Network blip / server restart: back off briefly before reconnecting.
      await new Promise(res => setTimeout(res, 2000));
    }
  }
}

function renderEntry(e){
  const div = document.createElement('div');
  div.className = 'entry ' + e.category;
  const body = renderBody(e);
  div.innerHTML =
    '<div class="ehead" onclick="this.parentNode.classList.toggle(\'open\')">'
    + '<span class="time">'+esc(e.time)+'</span>'
    + '<span class="method">'+esc(e.method)+'</span>'
    + '<span class="path">'+esc(e.path)+'</span>'
    + '<span class="badge '+statusClass(e.status)+'">'+e.status+'</span>'
    + '<span class="badge cat">'+esc(e.category)+'</span>'
    + '</div>'
    + (body ? '<div class="ebody">'+body+'</div>' : '');
  return div;
}

function challengeHtml(ch){
  if (!ch) return '';
  const pill = ch.valid ? '<span class="pill ok">decrypted</span>' : '<span class="pill bad">decode failed</span>';
  let rows = '';
  if (ch.valid) {
    rows = kv('kind', ch.kind) + kv('claimUid', ch.claimUid||'') + (ch.sessionId ? kv('sessionId', ch.sessionId) : '');
  } else if (ch.error) {
    rows = '<pre style="color:var(--red)">'+esc(ch.error)+'</pre>';
  }
  return '<div class="sec challenge"><h4>Challenge (data-protected) '+pill+'</h4>'+rows+'</div>';
}

function renderBody(e){
  let h = '';
  if (e.proof) {
    h += '<div class="sec proof"><h4>Proof JWT '
       + (e.proof.signaturePresent ? '<span class="pill ok">signed</span>' : '<span class="pill bad">unsigned</span>') + '</h4>'
       + '<pre>header: ' + esc(JSON.stringify(e.proof.header, null, 2)) + '\npayload: ' + esc(JSON.stringify(e.proof.payload, null, 2)) + '</pre>';
    if (e.proof.decodedJti) h += '<div class="muted" style="margin-top:6px">jti decodes to:</div>' + challengeHtml(e.proof.decodedJti).replace('<h4>Challenge (data-protected)', '<h4>Answered challenge');
    h += '</div>';
  }
  if (e.registrationHeader) h += '<div class="sec"><h4>Secure-Session-Registration</h4><pre>'+esc(e.registrationHeader)+'</pre></div>';
  if (e.challengeHeader) {
    h += '<div class="sec"><h4>Secure-Session-Challenge (issued)</h4><pre>'+esc(e.challengeHeader)+'</pre></div>';
    h += challengeHtml(e.decodedChallenge);
  }
  if (e.sessionConfig) h += '<div class="sec proof"><h4>Session Instruction (JSON body) <span class="pill ok">response</span></h4><pre>'+esc(JSON.stringify(e.sessionConfig, null, 2))+'</pre></div>';
  h += cookieSection('Request cookies', e.requestCookies);
  h += cookieSection('Set-Cookie', e.setCookies);
  return h;
}

function cookieSection(title, list){
  if (!list || !list.length) return '';
  const items = list.map(c => {
    let inner = '<div class="nm">'+esc(c.name)+'</div><div class="muted">'+esc(c.scheme||'(app)')+' &middot; '+c.valueLength+' bytes'
              + (c.deleted ? ' &middot; <span style="color:var(--red)">deleted</span>' : '') + '</div>';
    if (c.attributes) inner += '<div class="muted">'+esc(c.attributes)+'</div>';
    if (c.decoded) {
      const it = c.decoded.items || {};
      inner += '<pre>' + esc(JSON.stringify({ principal:c.decoded.principal, expiresUtc:c.decoded.expiresUtc, items:it }, null, 2)) + '</pre>';
    }
    return '<div class="cookie">'+inner+'</div>';
  }).join('');
  return '<div class="sec"><h4>'+esc(title)+'</h4>'+items+'</div>';
}

async function clearLog(){ await fetch('/debug/clearlog', {method:'POST'}); lastId = 0; logEl.innerHTML = '<div class="empty">Log cleared.</div>'; document.getElementById('count').textContent = '0 entries'; }
async function ping(){ try { await fetch('/api/time'); } catch(e) {} }

setInterval(refreshState, 1500); refreshState();
pollLog(); // single self-looping long-poll; must NOT be wrapped in setInterval
setInterval(() => { if (document.getElementById('autoPing').checked) ping(); }, 4000);
</script>
</body>
</html>
""";
}
