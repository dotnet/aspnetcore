// Node built-in test – Carousel data-api href sanitize timing (pass if < LIMIT)
const test   = require('node:test');
const assert = require('node:assert/strict');
const path   = require('node:path');

const BOOTSTRAP_PATH =
  process.env.BOOTSTRAP_PATH ||
  path.resolve(__dirname, '../dist/js/bootstrap.js');

const N     = parseInt(process.env.LENGTH || '100000', 10);
const LIMIT = parseInt(process.env.LIMIT  || '2000',   10);

test('Carousel data-api href sanitize timing (pass if < LIMIT)', () => {
  const handlers = Object.create(null);

  function wrap(raw) {
    return {
      on(event, selectorOrHandler, maybeHandler) {
        const h = typeof maybeHandler === 'function'
          ? maybeHandler
          : (typeof selectorOrHandler === 'function' ? selectorOrHandler : null);
        if (event && h) handlers[event] = h;
        return this;
      },
      find() { return { hasClass: () => false, data() { return {}; } }; },
      data() { return {}; },
      attr(name) { return raw && name === 'href' ? raw._href : null; },
    };
  }
  function $(x) { return wrap(x); }
  $.fn = { jquery: '3.4.1' };
  $.extend = Object.assign;

  global.window = global;
  global.document = {};
  global.jQuery = global.$ = $;

  require(BOOTSTRAP_PATH);

  const click =
    handlers['click.bs.carousel.data-api'] ||
    handlers['click.bs.carousel'];
  assert.equal(typeof click, 'function', 'failed to capture carousel handler');

  const cases = [
    { name: 'nul',        s: '\u0000'.repeat(N) + '\u0000' },
    { name: 'digits\\n@', s: '1'.repeat(N) + '\n@' },
  ];

  let worst = 0;
  for (const { name, s } of cases) {
    const t0 = Date.now();
    try { click.call({ _href: s }, { preventDefault(){} }); } catch {}
    const ms = Date.now() - t0;
    worst = Math.max(worst, ms);
    console.log(`[carousel] ${name.padEnd(10)} len=${s.length} -> ${ms} ms`);
  }
  console.log(`[carousel] worst = ${worst} ms (limit=${LIMIT})`);

  // 修复后应当 < LIMIT；否则失败
  assert.ok(worst < LIMIT, `too slow: ${worst}ms (>= ${LIMIT}ms)`);
});
