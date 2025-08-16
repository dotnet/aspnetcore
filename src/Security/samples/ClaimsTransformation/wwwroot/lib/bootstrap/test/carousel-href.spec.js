// Node built-in test (no mocha) – Carousel data-api
const test = require('node:test');
const assert = require('node:assert/strict');

// 设 EXPECT_FAST=1 时启用“<1000ms”断言；未修复时默认只打印时间
const EXPECT_FAST = process.env.EXPECT_FAST === '1';

test('Carousel data-api href sanitize timing', () => {
  // 极简 jQuery stub：按事件名记录 handler
  const handlers = Object.create(null);
  function wrap(raw) {
    return {
      on(eventName, selectorOrHandler, maybeHandler) {
        const h = typeof maybeHandler === 'function' ? maybeHandler
              : (typeof selectorOrHandler === 'function' ? selectorOrHandler : null);
        if (eventName && h) handlers[eventName] = h;
        return this;
      },
      find() { return { hasClass: () => false }; },
      data() { return {}; },
      attr(name) { return raw && name === 'href' ? raw._href : null; }
    };
  }
  function $(x) { return wrap(x); }
  $.fn = { jquery: '3.4.1' };
  $.extend = Object.assign;
  global.window = global;
  global.document = {};
  global.jQuery = global.$ = $;

  // 载入“本地（未修复/修复后）”的 bootstrap.js
  require('../dist/js/bootstrap.js');

  const click = handlers['click.bs.carousel.data-api'] || handlers['click.bs.carousel'];
  assert.equal(typeof click, 'function', 'failed to capture carousel handler');

  const N = 100000;
  const cases = [
    { name: 'nul',        s: '\u0000'.repeat(N) + '\u0000' },
    { name: 'digits\\n@', s: '1'.repeat(N) + '\n@' },
    //{ name: '=#...@\\r',  s: '=' + '#'.repeat(N) + '@\r' },
    //{ name: '=#*@\\r',    s: '=#'.repeat(N) + '@\r' },
  ];

  for (const { name, s } of cases) {
    const t0 = Date.now();
    try { click.call({ _href: s }, { preventDefault(){} }); } catch {}
    const ms = Date.now() - t0;
    console.log(`[carousel] ${name.padEnd(9)} len=${s.length} -> ${ms} ms`);
    if (EXPECT_FAST) assert.ok(ms < 2000, `too slow: ${ms}ms`);
  }
});
