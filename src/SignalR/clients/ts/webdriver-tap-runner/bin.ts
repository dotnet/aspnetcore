import * as path from "path";

import * as yargs from "yargs";

import * as _debug from "debug";
import { run } from "./lib";
const debug = _debug("webdriver-tap-runner:bin");

const argv = yargs
    .option("url", { demand: true, description: "The URL of the server to test against" })
    .option("name", { demand: true, description: "The name of the test run" })
    .option("browser", { alias: "b", default: "chrome", description: "The browser to use (only 'chrome' is supported right now)" })
    .option("webdriver-port", { default: 9515, description: "The port on which to launch the WebDriver server", number: true })
    .option("chrome-driver-log", { })
    .option("chrome-driver-log-verbose", { })
    .argv;

run(argv.name, {
    browser: argv.browser,
    chromeDriverLogFile: argv["chrome-driver-log"],
    chromeVerboseLogging: !!argv["chrome-driver-log-verbose"],
    url: argv.url,
    webdriverPort: argv["webdriver-port"],
}).then((failures) => process.exit(failures));
