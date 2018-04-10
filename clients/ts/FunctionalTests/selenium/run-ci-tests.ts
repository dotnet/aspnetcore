import { ChildProcess, spawn, spawnSync } from "child_process";
import { existsSync } from "fs";
import * as path from "path";

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
                let candidatePath = path.resolve(process.env["ProgramFiles(x86)"], "Google", "Chrome", "Application", "chrome.exe");
                if (!existsSync(candidatePath)) {
                    candidatePath = path.resolve(process.env.LOCALAPPDATA, "Google", "Chrome", "Application", "chrome.exe");
                }
                return candidatePath;
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

// Launch the tests (we know the CI already built, so run the 'test-only' script)
const args = ["run", "test-only", "--", "--raw", "--chrome", chromeBinary];
if (configuration) {
    args.push("--configuration");
    args.push(configuration);
}

    args.push("--verbose");


let command = "npm";

if (process.platform === "win32") {
    // NPM is a cmd file, and it's tricky to "spawn". Instead, we'll find the NPM js file and use process.execPath to locate node.exe and run it directly
    const npmPath = path.resolve(process.execPath, "..", "node_modules", "npm", "bin", "npm-cli.js");
    if (!existsSync(npmPath)) {
        failPrereq(`Unable to locate npm command line at '${npmPath}'`);
    }

    args.unshift(npmPath);
    command = process.execPath;
}

console.log(`running: ${command} ${args.join(" ")}`);

const testProcess = spawn(command, args, { cwd: path.resolve(__dirname, "..") });
testProcess.stderr.pipe(process.stderr);
testProcess.stdout.pipe(process.stdout);
testProcess.on("close", (code) => process.exit(code));
