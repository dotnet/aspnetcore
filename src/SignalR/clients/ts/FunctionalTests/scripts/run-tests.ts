import { ChildProcess, exec, spawn } from "child_process";
import { EOL } from "os";
import { Readable } from "stream";

import * as _fs from "fs";
import * as path from "path";
import { promisify } from "util";

import * as karma from "karma";

import * as _debug from "debug";

const debug = _debug("signalr-functional-tests:run");

const ARTIFACTS_DIR = path.resolve(__dirname, "..", "..", "..", "..", "artifacts");
const LOGS_DIR = path.resolve(ARTIFACTS_DIR, "logs");

// Promisify things from fs we want to use.
const fs = {
    createWriteStream: _fs.createWriteStream,
    exists: promisify(_fs.exists),
    mkdir: promisify(_fs.mkdir),
};

process.on("unhandledRejection", (reason) => {
    console.error(`Unhandled promise rejection: ${reason}`);
    process.exit(1);
});

// Don't let us hang the build. If this process takes more than 10 minutes, we're outta here
setTimeout(() => {
    console.error("Bail out! Tests took more than 10 minutes to run. Aborting.");
    process.exit(1);
}, 1000 * 60 * 10);

function waitForMatches(command: string, process: ChildProcess, regex: RegExp, matchCount: number): Promise<RegExpMatchArray> {
    return new Promise<string[]>((resolve, reject) => {
        const commandDebug = _debug(`${command}`);
        try {
            let lastLine = "";
            let results: string[] = null;

            async function onData(this: Readable, chunk: string | Buffer): Promise<void> {
                try {
                    chunk = chunk.toString();

                    // Process lines
                    let lineEnd = chunk.indexOf(EOL);
                    while (lineEnd >= 0) {
                        const chunkLine = lastLine + chunk.substring(0, lineEnd);
                        lastLine = "";

                        chunk = chunk.substring(lineEnd + EOL.length);
                        const res = regex.exec(chunkLine);
                        if (results == null && res != null) {
                            results = res;
                        } else if (res != null) {
                            results = Array<string>().concat(results, res);
                        }

                        // * 2 because each match will have the original line plus the match
                        if (results && results.length >= matchCount * 2) {
                            resolve(results);
                            return;
                        }

                        commandDebug(chunkLine);
                        lineEnd = chunk.indexOf(EOL);
                    }

                    lastLine = chunk.toString();
                } catch (e) {
                    this.removeAllListeners("data");
                    reject(e);
                }
            }

            process.on("close", async (code, signal) => {
                console.log(`${command} process exited with code: ${code}`);
                global.process.exit(1);
            });

            process.stdout.on("data", onData.bind(process.stdout));
            process.stderr.on("data", (chunk) => {
                onData.bind(process.stderr)(chunk);
                console.error(`${command} | ${chunk.toString()}`);
            });
        } catch (e) {
            reject(e);
        }
    });
}

let configuration = "Debug";
let spec: string;
let sauce = false;
let allBrowsers = false;
let noColor = false;

for (let i = 2; i < process.argv.length; i += 1) {
    switch (process.argv[i]) {
        case "--configuration":
            i += 1;
            configuration = process.argv[i];
            break;
        case "-v":
        case "--verbose":
            _debug.enable("signalr-functional-tests:*");
            break;
        case "-vv":
        case "--very-verbose":
            _debug.enable("*");
            break;
        case "--spec":
            i += 1;
            spec = process.argv[i];
            break;
        case "--sauce":
            sauce = true;
            console.log("Running on SauceLabs.");
            break;
        case "-a":
        case "--all-browsers":
            allBrowsers = true;
            break;
        case "--no-color":
            noColor = true;
            break;
    }
}

const configFile = sauce ?
    path.resolve(__dirname, "karma.sauce.conf.js") :
    path.resolve(__dirname, "karma.local.conf.js");
debug(`Loading Karma config file: ${configFile}`);

// Gross but it works
process.env.ASPNETCORE_SIGNALR_TEST_ALL_BROWSERS = allBrowsers ? "true" : null;
const config = (karma as any).config.parseConfig(configFile);

if (sauce) {
    let failed = false;

    if (!process.env.SAUCE_USERNAME) {
        failed = true;
        console.error("Required environment variable 'SAUCE_USERNAME' is missing!");
    }

    if (!process.env.SAUCE_ACCESS_KEY) {
        failed = true;
        console.error("Required environment variable 'SAUCE_ACCESS_KEY' is missing!");
        process.exit(1);
    }

    if (failed) {
        process.exit(1);
    }
}

function runKarma(karmaConfig) {
    return new Promise<karma.TestResults>((resolve, reject) => {
        const server = new karma.Server(karmaConfig);
        server.on("run_complete", (browsers, results) => {
            return resolve(results);
        });
        server.start();
    });
}

function runJest(httpsUrl: string, httpUrl: string) {
    const jestPath = path.resolve(__dirname, "..", "..", "common", "node_modules", "jest", "bin", "jest.js");
    const configPath = path.resolve(__dirname, "..", "func.jest.config.js");

    console.log("Starting Node tests using Jest.");
    return new Promise<number>((resolve, reject) => {
        const logStream = fs.createWriteStream(path.resolve(__dirname, "..", "..", "..", "..", "artifacts", "logs", "node.functionaltests.log"));
        // Use NODE_TLS_REJECT_UNAUTHORIZED to allow our test cert to be used by the Node tests (NEVER use this environment variable outside of testing)
        const p = exec(`"${process.execPath}" "${jestPath}" --config "${configPath}"`, { env: { SERVER_URL: `${httpsUrl};${httpUrl}`, NODE_TLS_REJECT_UNAUTHORIZED: 0 }, timeout: 200000, maxBuffer: 10 * 1024 * 1024 },
            (error: any, stdout, stderr) => {
                console.log("Finished Node tests.");
                if (error) {
                    console.log(error.message);
                    return resolve(error.code);
                }
                return resolve(0);
            });
        p.stdout.pipe(logStream);
        p.stderr.pipe(logStream);
    });
}

(async () => {
    try {
        const serverPath = path.resolve(__dirname, "..", "bin", configuration, "netcoreapp3.0", "FunctionalTests.dll");

        debug(`Launching Functional Test Server: ${serverPath}`);
        let desiredServerUrl = "https://127.0.0.1:0;http://127.0.0.1:0";

        if (sauce) {
            // SauceLabs can only proxy certain ports for Edge and Safari.
            // https://wiki.saucelabs.com/display/DOCS/Sauce+Connect+Proxy+FAQS
            desiredServerUrl = "http://127.0.0.1:9000";
        }

        const dotnet = spawn("dotnet", [serverPath], {
            env: {
                ...process.env,
                ["ASPNETCORE_URLS"]: desiredServerUrl,
            },
        });

        function cleanup() {
            if (dotnet && !dotnet.killed) {
                console.log("Terminating dotnet process");
                dotnet.kill();
            }
        }

        const logStream = fs.createWriteStream(path.resolve(__dirname, "..", "..", "..", "..", "artifacts", "logs", "ts.functionaltests.dotnet.log"));
        dotnet.stdout.pipe(logStream);

        process.on("SIGINT", cleanup);
        process.on("exit", cleanup);

        debug("Waiting for Functional Test Server to start");
        const matches = await waitForMatches("dotnet", dotnet, /Now listening on: (https?:\/\/[^\/]+:[\d]+)/, 2);
        const httpsUrl = matches[1];
        const httpUrl = matches[3];
        debug(`Functional Test Server has started at ${httpsUrl} and ${httpUrl}`);

        debug(`Using SignalR Server: ${httpsUrl} and ${httpUrl}`);

        // Start karma server
        const conf = {
            ...config,
            singleRun: true,
        };

        // Set output directory for console log
        if (!await fs.exists(ARTIFACTS_DIR)) {
            await fs.mkdir(ARTIFACTS_DIR);
        }
        if (!await fs.exists(LOGS_DIR)) {
            await fs.mkdir(LOGS_DIR);
        }
        conf.browserConsoleLogOptions.path = path.resolve(LOGS_DIR, `browserlogs.console.${new Date().toISOString().replace(/:|\./g, "-")}`);

        if (noColor) {
            conf.colors = false;
        }

        // Pass server URL to tests
        conf.client.args = ["--server", `${httpsUrl};${httpUrl}`];

        const jestExit = await runJest(httpsUrl, httpUrl);

        // Check if we got any browsers
        let karmaExit;
        if (config.browsers.length === 0) {
            console.log("Unable to locate any suitable browsers. Skipping browser functional tests.");
        } else {
            karmaExit = (await runKarma(conf)).exitCode;
        }

        if (karmaExit) {
            console.log(`karma exit code: ${karmaExit}`);
        }
        console.log(`jest exit code: ${jestExit}`);

        process.exit(jestExit !== 0 ? jestExit : karmaExit);
    } catch (e) {
        console.error(e);
        process.exit(1);
    }
})();
