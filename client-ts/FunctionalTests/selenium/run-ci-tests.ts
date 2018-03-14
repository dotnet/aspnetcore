import { ChildProcess, spawn, spawnSync } from "child_process";
import { existsSync } from "fs";
import * as path from "path";

import * as tapTeamCity from "tap-teamcity";
import * as tee from "tee";

const teamcity = !!process.env.TEAMCITY_VERSION;

let force = process.env.ASPNETCORE_SIGNALR_FORCE_BROWSER_TESTS === "true";
let chromePath = process.env.ASPNETCORE_CHROME_PATH;

let configuration;
let verbose;

for (let i = 2; i < process.argv.length; i += 1) {
    switch (process.argv[i]) {
        case "-f":
        case "--force":
            force = true;
            break;

        case "--chrome":
            i += 1;
            chromePath = process.argv[i];
            break;

        case "--configuration":
            i += 1;
            configuration = process.argv[i];
            break;

        case "-v":
        case "--verbose":
            verbose = true;
            break;
    }
}

function failPrereq(error: string) {
    if (force) {
        console.error(`Browser functional tests cannot be run: ${error}`);
        process.exit(1);
    } else {
        console.log(`Skipping browser functional Tests: ${error}`);

        // Zero exit code because we don't want to fail the build.
        process.exit(0);
    }
}

function getChromeBinaryPath(): string {
    if (chromePath) {
        return chromePath;
    } else {
        switch (process.platform) {
            case "win32":
                // tslint:disable-next-line:no-string-literal
                return path.resolve(process.env.LOCALAPPDATA, "Google", "Chrome", "Application", "chrome.exe");
            case "darwin":
                return path.resolve("/", "Applications", "Google Chrome.app", "Contents", "MacOS", "Google Chrome");
            case "linux":
                return path.resolve("/", "usr", "bin", "google-chrome");
        }
    }
}

// Check prerequisites
const chromeBinary = getChromeBinaryPath();
if (!existsSync(chromeBinary)) {
    failPrereq(`Unable to locate Google Chrome at '${chromeBinary}'. Use the '--chrome' argument or 'ASPNETCORE_CHROME_PATH' environment variable to specify an alternate path`);
} else {
    console.log(`Using Google Chrome Browser from '${chromeBinary}`);
}

// Launch the tests
const args = ["test", "--", "--chrome", chromeBinary];
if (configuration) {
    args.push("--configuration");
    args.push(configuration);
}
if (verbose) {
    args.push("--verbose");
}
if (teamcity) {
    args.push("--raw");
}

const testProcess = spawn("npm", args, { cwd: path.resolve(__dirname, "..") });
testProcess.stderr.pipe(process.stderr);
if (teamcity) {
    testProcess.stdout.pipe(tapTeamCity()).pipe(process.stdout);
}

testProcess.stdout.pipe(process.stdout);
testProcess.on("close", (code) => process.exit(code));