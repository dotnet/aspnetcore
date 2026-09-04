// Node built-in test – Collapse data-api href sanitize timing (fail if > LIMIT ms)
const test   = require('node:test');
const assert = require('node:assert/strict');
const path   = require('node:path');

const BOOTSTRAP_PATH =
  process.env.BOOTSTRAP_PATH ||
  path.resolve(__dirname, '../dist/js/bootstrap.js');

const N      = parseInt(process.env.LENGTH || '100000', 10);
const LIMIT  = parseInt(process.env.LIMIT  || '2000',   10); // 超过就失败

test('Collapse data-api href sanitize timing', () => {
  // 极简 jQuery stub：按事件名存 handler
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
    handlers['click.bs.collapse.data-api'] ||
    handlers['click.bs.collapse'];
  assert.equal(typeof click, 'function', 'failed to capture collapse handler');

  const cases = [
    { name: 'nul',        s: '\u0000'.repeat(N) + '\u0000' },
    { name: 'digits\\n@', s: '1'.repeat(N)    + '\n@'      },
  ];

  let worst = 0;
  for (const { name, s } of cases) {
    const t0 = Date.now();
    try { click.call({ _href: s }, { preventDefault(){} }); } catch {}
    const ms = Date.now() - t0;
    worst = Math.max(worst, ms);
    console.log(`[collapse] ${name.padEnd(10)} len=${s.length} -> ${ms} ms`);
  }
  console.log(`[collapse] worst = ${worst} ms (limit=${LIMIT})`);

  // 关键：超过 LIMIT 就判失败（红色 ✗）
  if (worst > LIMIT) {
    assert.fail(`too slow: ${worst}ms (> ${LIMIT}ms)`);
  }
});
