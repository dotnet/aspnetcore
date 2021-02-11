import * as http from "http";
import * as path from "path";
import { promisify } from "util";

import { ChildProcess, spawn, SpawnOptions } from "child_process";
import * as chromedriver from "chromedriver";
import { EOL } from "os";
import { Builder, logging, WebDriver, WebElement } from "selenium-webdriver";
import { Options as ChromeOptions } from "selenium-webdriver/chrome";
import { Readable, Writable } from "stream";

import { delay, flushEntries, getEntryContent, getLogEntry, isComplete, waitForElement } from "./utils";

import * as _debug from "debug";
const debug = _debug("webdriver-tap-runner:bin");


export interface RunnerOptions {
    browser: string;
    url: string;
    chromeBinaryPath?: string,
    chromeDriverLogFile?: string;
    chromeVerboseLogging?: boolean;
    output?: Writable;
    webdriverPort: number;
}

function applyBrowserSettings(options: RunnerOptions, builder: Builder) {
    if (options.browser === "chrome") {
        const chromeOptions = new ChromeOptions();
        chromeOptions.headless();

        // If we're root, we need to disable the sandbox.
        if (process.getuid && process.getuid() === 0) {
            chromeOptions.addArguments("--no-sandbox");
        }

        if (options.chromeBinaryPath) {
            debug(`Using Chrome Binary Path: ${options.chromeBinaryPath}`);
            chromeOptions.setChromeBinaryPath(options.chromeBinaryPath);
        }

        builder.setChromeOptions(chromeOptions);
    }
}

function writeToDebug(name: string) {
    const writer = _debug(name);
    let lastLine: string;
    return (chunk: Buffer | string) => {
        const str = chunk.toString();
        const lines = str.split(/\r?\n/g);
        const lastLineComplete = str.endsWith("\n");

        if (lines.length > 0 && lastLine) {
            lines[0] = lastLine + lines[0];
        }

        const end = lastLineComplete ? lines.length : (lines.length - 1)
        for (let i = 0; i < end; i += 1) {
            writer(lines[i]);
        }
        if (lastLineComplete && lines.length > 0) {
            lastLine = lines[lines.length - 1];
        }
    }
}

let driverInstance: ChildProcess;
function startDriver(browser: string, port: number) {
    let processName: string;
    if (browser === "chrome") {
        processName = path.basename(chromedriver.path);
        driverInstance = spawn(chromedriver.path, [`--port=${port}`]);
    } else {
        throw new Error(`Unsupported browser: ${browser}`);
    }

    // Capture output
    driverInstance.stdout.on("data", writeToDebug(`webdriver-tap-runner:${processName}:stdout`));
    driverInstance.stderr.on("data", writeToDebug(`webdriver-tap-runner:${processName}:stderr`));
}

function stopDriver(browser: string) {
    if (driverInstance && !driverInstance.killed) {
        debug("Killing WebDriver...");
        driverInstance.kill();
        debug("Killed WebDriver");
    }
}

function pingUrl(url: string): Promise<void> {
    return new Promise<void>((resolve, reject) => {
        const request = http.request(url);
        request.on("response", (resp: http.IncomingMessage) => {
            if (resp.statusCode >= 400) {
                reject(new Error(`Received ${resp.statusCode} ${resp.statusMessage} from server`));
            } else {
                resolve();
            }
        });
        request.on("error", (error) => reject(error));
        request.end();
    });
}

async function pingWithRetry(url: string): Promise<boolean> {
    for (let i = 0; i < 5; i += 1) {
        try {
            debug(`Pinging URL: ${url}`);
            await pingUrl(url);
            return true;
        } catch (e) {
            debug(`Error reaching server: '${e}', retrying...`);
            await delay(100);
        }
    }
    debug("Retry limit exhausted");
    return false;
}

export async function run(runName: string, options: RunnerOptions): Promise<number> {
    const output = options.output || (process.stdout as Writable);

    debug(`Using WebDriver port: ${options.webdriverPort}`);

    startDriver(options.browser, options.webdriverPort);

    // Wait for the server to start
    const serverUrl = `http://localhost:${options.webdriverPort}`;
    if (!await pingWithRetry(`${serverUrl}/status`)) {
        console.log("WebDriver did not start in time.");
        process.exit(1);
    }

    try {
        // Shut selenium down when we shut down.
        process.on("exit", () => {
            stopDriver(options.browser);
        });

        // Build WebDriver
        const builder = new Builder()
            .usingServer(serverUrl);

        // Set the browser
        debug(`Using '${options.browser}' browser`);
        builder.forBrowser(options.browser);

        applyBrowserSettings(options, builder);

        // Build driver
        const driver = builder.build();

        let failureCount = 0;
        try {
            // Navigate to the URL
            debug(`Navigating to ${options.url}`);
            await driver.get(options.url);

            // Wait for the TAP results list
            const listElement = await waitForElement(driver, "__tap_list");

            output.write(`TAP version 13${EOL}`);
            output.write(`# ${runName}${EOL}`);

            // Process messages until the test run is complete
            let index = 0;
            while (!await isComplete(listElement)) {
                const entry = await getLogEntry(index, listElement);
                if (entry) {
                    index += 1;
                    const content = await getEntryContent(entry);
                    if (content.startsWith("not ok")) {
                        failureCount += 1;
                    }
                    output.write(content + EOL);
                }
            }

            // Flush any remaining messages
            await flushEntries(index, listElement, (entry) => {
                if (entry.startsWith("not ok")) {
                    failureCount += 1;
                }
                output.write(entry + EOL);
            });

        } finally {
            // Shut down
            debug("Shutting WebDriver down...");
            await driver.quit();
        }

        // We're done!
        debug("Test run complete");
        return failureCount;
    } finally {
        debug("Shutting Selenium server down...");
        stopDriver(options.browser);
    }
}