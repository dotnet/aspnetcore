(function () {
  // Note: there are multiple copies of this file throughout the repo. If you're editing it, please look for
  // other seleniumworkaround.js files and keep them all in sync.
  const logs = [];

  const defaultLog = console.log;
  console.log = function () {
    defaultLog.apply(console, arguments);
    logs.push(Array.from(arguments).join(' '));
  }

  const defaultError = console.error;
  console.error = function () {
    defaultError.apply(console, arguments);
    logs.push(Array.from(arguments).join(' '));
  }

  const defaultWarn = console.warn;
  console.warn = function () {
    defaultWarn.apply(console, arguments);
    logs.push(Array.from(arguments).join(' '));
  }

  window.getBrowserLogs = () => logs;
})();

