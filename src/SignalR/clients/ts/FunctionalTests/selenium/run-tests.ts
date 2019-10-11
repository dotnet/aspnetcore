import { ChildProcess, spawn } from "child_process";
import * as fs from "fs";
import { EOL } from "os";
import * as path from "path";
import { PassThrough, Readable } from "stream";

import { run } from "../../webdriver-tap-runner/lib";

import * as _debug from "debug";
const debug = _debug("signalr-functional-tests:run");

process.on("unhandledRejection", (reason) => {
    console.error(`Unhandled promise rejection: ${reason}`);
    process.exit(1);
});

// Don't let us hang the build. If this process takes more than 10 minutes, we're outta here
setTimeout(() => {
    console.error("Bail out! Tests took more than 10 minutes to run. Aborting.");
    process.exit(1);
}, 1000 * 60 * 10);

function waitForMatch(command: string, process: ChildProcess, regex: RegExp): Promise<RegExpMatchArray> {
    return new Promise<RegExpMatchArray>((resolve, reject) => {
        const commandDebug = _debug(`signalr-functional-tests:${command}`);
        try {
            let lastLine = "";

            async function onData(this: Readable, chunk: string | Buffer): Promise<void> {
                try {
                    chunk = chunk.toString();

                    // Process lines
                    let lineEnd = chunk.indexOf(EOL);
                    while (lineEnd >= 0) {
                        const chunkLine = lastLine + chunk.substring(0, lineEnd);
                        lastLine = "";

                        chunk = chunk.substring(lineEnd + EOL.length);

                        const results = regex.exec(chunkLine);
                        commandDebug(chunkLine);
                        if (results && results.length > 0) {
                            resolve(results);
                            return;
                        }
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
let chromePath: string;
let spec: string;

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
        case "--chrome":
            i += 1;
            chromePath = process.argv[i];
            break;
        case "--spec":
            i += 1;
            spec = process.argv[i];
            break;
    }
}

if (chromePath) {
    debug(`Using Google Chrome at: '${chromePath}'`);
}

(async () => {
    try {
        const serverPath = path.resolve(__dirname, "..", "bin", configuration, "netcoreapp2.1", "FunctionalTests.dll");

        debug(`Launching Functional Test Server: ${serverPath}`);
        const dotnet = spawn("dotnet", [serverPath], {
            env: {
                ...process.env,
                ["ASPNETCORE_URLS"]: "http://127.0.0.1:0"
            },
        });

        function cleanup() {
            if (dotnet && !dotnet.killed) {
                console.log("Terminating dotnet process");
                dotnet.kill();
            }
        }

        process.on("SIGINT", cleanup);
        process.on("exit", cleanup);

        debug("Waiting for Functional Test Server to start");
        const results = await waitForMatch("dotnet", dotnet, /Now listening on: (http:\/\/[^\/]+:[\d]+)/);
        debug(`Functional Test Server has started at ${results[1]}`);

        let url = results[1] + "?cacheBust=true";
        if (spec) {
            url += `&spec=${encodeURI(spec)}`;
        }

        debug(`Using server url: ${url}`);

        const failureCount = await run("SignalR Browser Functional Tests", {
            browser: "chrome",
            chromeBinaryPath: chromePath,
            output: process.stdout,
            url,
            webdriverPort: 9515,
        });
        process.exit(failureCount);
    } catch (e) {
        console.error("Error: " + e.toString());
        process.exit(1);
    }
})();
