// Node built-in test (no mocha) – Collapse data-api
const test = require('node:test');
const assert = require('node:assert/strict');

const EXPECT_FAST = process.env.EXPECT_FAST === '1';

test('Collapse data-api href sanitize timing', () => {
  const handlers = Object.create(null);
  function wrap(raw) {
    return {
      on(eventName, selectorOrHandler, maybeHandler) {
        const h = typeof maybeHandler === 'function' ? maybeHandler
              : (typeof selectorOrHandler === 'function' ? selectorOrHandler : null);
        if (eventName && h) handlers[eventName] = h;
        return this;
      },
      each() { return this; }, // 避免深入插件逻辑
      data() { return {}; },
      attr(name) {
        if (!raw) return null;
        if (name === 'href') return raw._href || null;
        if (name === 'data-target') return raw._dt || null;
        return null;
      },
      find() { return { hasClass: () => false }; }
    };
  }
  function $(x) { return wrap(x); }
  $.fn = { jquery: '3.4.1' };
  $.extend = Object.assign;
  global.window = global;
  global.document = {};
  global.jQuery = global.$ = $;

  require('../dist/js/bootstrap.js');

  const click = handlers['click.bs.collapse.data-api'] || handlers['click.bs.collapse'];
  assert.equal(typeof click, 'function', 'failed to capture collapse handler');

  const N = 100000;
  const cases = [
    { name: 'nul',        s: '\u0000'.repeat(N) + '\u0000' },
    { name: 'digits\\n@', s: '1'.repeat(N) + '\n@' },
    //{ name: '=#...@\\r',  s: '=' + '#'.repeat(N) + '@\r' },
    //{ name: '=#*@\\r',    s: '=#'.repeat(N) + '@\r' },
  ];

  for (const { name, s } of cases) {
    const t0 = Date.now();
    try { click.call({ _href: s, _dt: null }, { preventDefault(){} }); } catch {}
    const ms = Date.now() - t0;
    console.log(`[collapse] ${name.padEnd(9)} len=${s.length} -> ${ms} ms`);
    if (EXPECT_FAST) assert.ok(ms < 2000, `too slow: ${ms}ms`);
  }
});
