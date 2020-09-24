(function () {
  const logs = [];

  console.defaultLog = console.log.bind(console);
  console.log = function () {
    console.defaultLog.apply(console, arguments);
    logs.push(Array.from(arguments).join('\n'));
  }

  console.defaultError = console.error.bind(console);
  console.error = function () {
    console.defaultError.apply(console, arguments);
    logs.push(Array.from(arguments).join('\n'));
  }

  console.defaultWarn = console.warn.bind(console);
  console.warn = function () {
    console.defaultWarn.apply(console, arguments);
    logs.push(Array.from(arguments).join('\n'));
  }

  window.getBrowserLogs = () => logs;
})();

