import { EOL } from "os";
import * as _fs from "fs";
import { promisify } from "util";

// Promisify things from fs we want to use.
const fs = {
    createWriteStream: _fs.createWriteStream,
    exists: promisify(_fs.exists),
    mkdir: promisify(_fs.mkdir),
    appendFile: promisify(_fs.appendFile),
    readFile: promisify(_fs.readFile),
};

process.on("unhandledRejection", (reason) => {
    console.error(`Unhandled promise rejection: ${reason}`);
    process.exit(1);
});

let sauceUser = null;
let sauceKey = null;
let tunnelIdentifier = null;
let hostName = null;

for (let i = 0; i < process.argv.length; i += 1) {
    switch (process.argv[i]) {
        case "--sauce-user":
            i += 1;
            sauceUser = process.argv[i];
            break;
        case "--sauce-key":
            i += 1;
            sauceKey = process.argv[i];
            break;
        case "--sauce-tunnel":
            i += 1;
            tunnelIdentifier = process.argv[i];
            break;
        case "--use-hostname":
            i += 1;
            hostName = process.argv[i];
            break;
    }
}

const HOSTSFILE_PATH = process.platform === "win32" ? `${process.env.SystemRoot}\\System32\\drivers\\etc\\hosts` : null;

(async () => {

    if (hostName) {
        // Register a custom hostname in the hosts file (requires Admin, but AzDO agents run as Admin)
        // Used to work around issues in Sauce Labs.
        if (process.platform !== "win32") {
            throw new Error("Can't use '--use-hostname' on non-Windows platform.");
        }

        try {

            console.log(`Updating Hosts file (${HOSTSFILE_PATH}) to register host name '${hostName}'`);
            await fs.appendFile(HOSTSFILE_PATH, `${EOL}127.0.0.1 ${hostName}${EOL}`);

        } catch (error) {
            console.log(`Unable to update hosts file at ${HOSTSFILE_PATH}. Error: ${error}`);
        }
    }


    // Creates a persistent proxy tunnel using Sauce Connect.
    var sauceConnectLauncher = require('sauce-connect-launcher');

    sauceConnectLauncher({
        username: sauceUser,
        accessKey: sauceKey,
        tunnelIdentifier: tunnelIdentifier,
    }, function (err, sauceConnectProcess) {
        if (err) {
            console.error(err.message);
            return;
        }

        console.log("Sauce Connect ready");
    });
})();
