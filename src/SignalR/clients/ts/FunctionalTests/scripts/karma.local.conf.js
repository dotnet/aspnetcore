const os = require('os');

try {
    // Karma configuration for a local run (the default)
    const createKarmaConfig = require("./karma.base.conf");
    const fs = require("fs");
    const which = require("which");

    // Bring in the launchers directly to detect browsers
    const ChromeHeadlessBrowser = require("karma-chrome-launcher")["launcher:ChromeHeadless"][1];
    const ChromiumHeadlessBrowser = require("karma-chrome-launcher")["launcher:ChromiumHeadless"][1];
    const FirefoxHeadlessBrowser = require("karma-firefox-launcher")["launcher:FirefoxHeadless"][1];
    const FirefoxDeveloperHeadlessBrowser = require("karma-firefox-launcher")["launcher:FirefoxDeveloperHeadless"][1];
    const EdgeBrowser = require("karma-edge-launcher")["launcher:Edge"][1];
    const SafariBrowser = require("karma-safari-launcher")["launcher:Safari"][1];
    const IEBrowser = require("karma-ie-launcher")["launcher:IE"][1];

    let browsers = [];

    function browserExists(path) {
      // On linux, the browsers just return the command, not a path, so we need to check if it exists.
      if (process.platform === "linux") {
        return !!which.sync(path, { nothrow: true });
      } else {
        return fs.existsSync(path);
      }
    }

    function tryAddBrowser(name, b) {
      var path = b.DEFAULT_CMD[process.platform];
      if (b.ENV_CMD && process.env[b.ENV_CMD]) {
        path = process.env[b.ENV_CMD];
      }
      console.log(`Checking for ${name} at ${path}...`);

      if (path && browserExists(path)) {
        console.log(`Located ${name} at ${path}.`);
        browsers.push(name);
        return true;
      }
      else {
        console.log(`Unable to locate ${name}. Skipping.`);
        return false;
      }
    }

    // We use the launchers themselves to figure out if the browser exists. It's a bit sneaky, but it works.
    tryAddBrowser("ChromeHeadlessNoSandbox", ChromeHeadlessBrowser.prototype);
    tryAddBrowser("ChromiumHeadlessIgnoreCert", ChromiumHeadlessBrowser.prototype);

    if (os.platform() !== 'darwin') {
      if (!tryAddBrowser("FirefoxHeadless", FirefoxHeadlessBrowser.prototype)) {
        tryAddBrowser("FirefoxDeveloperHeadless", FirefoxDeveloperHeadlessBrowser.prototype);
      }
    } else {
        // https://bugzilla.mozilla.org/show_bug.cgi?id=1871366
        // https://bugzilla.mozilla.org/show_bug.cgi?id=1871447
        // It looks like some Entitlements issue with Firefox and macOS, additionally, it seems 'firefox-bin' is being removed which is what the karma firefox launcher uses by default
        tryAddBrowser("FirefoxHeadlessMac", FirefoxHeadlessBrowser.prototype);
    }

    // We need to receive an argument from the caller, but globals don't seem to work, so we use an environment variable.
    if (process.env.ASPNETCORE_SIGNALR_TEST_ALL_BROWSERS === "true") {
      tryAddBrowser("Edge", EdgeBrowser.prototype);
      tryAddBrowser("IE", IEBrowser.prototype);
      tryAddBrowser("Safari", SafariBrowser.prototype);
    }

    module.exports = createKarmaConfig({
      browsers,
      customLaunchers: {
        ChromeHeadlessNoSandbox: {
          base: 'ChromeHeadless',

          // Ignore cert errors to allow our test cert to work (NEVER do this outside of testing)
          // ChromeHeadless runs about 10x slower on Windows 7 machines without the --proxy switches below. Why? ¯\_(ツ)_/¯
          flags: ["--no-sandbox", "--proxy-server='direct://'", "--proxy-bypass-list=*", "--allow-insecure-localhost", "--ignore-certificate-errors"]
        },
        ChromiumHeadlessIgnoreCert: {
          base: 'ChromiumHeadless',

          // Ignore cert errors to allow our test cert to work (NEVER do this outside of testing)
          flags: ["--allow-insecure-localhost", "--ignore-certificate-errors"]
        },
        FirefoxHeadlessMac: {
          base: 'FirefoxHeadless',

          command: '/Applications/FireFox.app/Contents/MacOS/firefox'
        }
      },
    });
} catch (e) {
    console.error(e);
}
