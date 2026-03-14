// Node built-in test – Carousel data-api href sanitize timing (fail if > LIMIT)
const test   = require('node:test');
const assert = require('node:assert/strict');
const path   = require('node:path');

const BOOTSTRAP_PATH =
  process.env.BOOTSTRAP_PATH ||
  path.resolve(__dirname, '../dist/js/bootstrap.js');

const N     = parseInt(process.env.LENGTH || '100000', 10);
const LIMIT = parseInt(process.env.LIMIT  || '2000',   10); // 超过即失败

test('Carousel data-api href sanitize timing (fail if > LIMIT)', () => {
  // 极简 jQuery stub：按事件名保存 handler
  const handlers = Object.create(null);

  function wrap(raw) {
    return {
      on(event, selectorOrHandler, maybeHandler) {
        const h = typeof maybeHandler === 'function'
          ? maybeHandler
          : (typeof selectorOrHandler === 'function' ? selectorOrHandler : null);
        if (event && h) handlers[event] = h;
        return this; // 链式 .on().on()
      },
      find() { return { hasClass: () => false, data() { return {}; } }; },
      data() { return {}; },
      attr(name) { return raw && name === 'href' ? raw._href : null; },
    };
  }
  function $(x) { return wrap(x); }
  $.fn = { jquery: '3.4.1' };
  $.extend = Object.assign;

  // 满足 bootstrap 自检
  global.window = global;
  global.document = {};
  global.jQuery = global.$ = $;

  // 载入本地 bootstrap（未修复/修复后的都可以）
  require(BOOTSTRAP_PATH);

  // 精确拿到 Carousel 的 data-api 处理器
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

  // 关键：超过 LIMIT 就判失败（红色 ✗）
  if (worst > LIMIT) {
    assert.fail(`too slow: ${worst}ms (> ${LIMIT}ms)`);
  }
});
