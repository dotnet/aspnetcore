// Limit dependencies to core Node modules. This means the code in this file has to be very low-level and unattractive,
// but simplifies things for the consumer of this module.
import * as fs from 'fs';
import * as net from 'net';
import * as path from 'path';
import * as readline from 'readline';
import { Duplex } from 'stream';
import * as virtualConnectionServer from './VirtualConnections/VirtualConnectionServer';

// Webpack doesn't support dynamic requires for files not present at compile time, so grab a direct
// reference to Node's runtime 'require' function.
const dynamicRequire: (name: string) => any = eval('require');
const parsedArgs = parseArgs(process.argv);
if (parsedArgs.watch) {
    autoQuitOnFileChange(process.cwd(), parsedArgs.watch.split(','));
}

// Signal to the .NET side when we're ready to accept invocations
const server = net.createServer().on('listening', () => {
    console.log('[Microsoft.AspNetCore.NodeServices:Listening]');
});

// Each virtual connection represents a separate invocation
virtualConnectionServer.createInterface(server).on('connection', (connection: Duplex) => {
    readline.createInterface(connection, null).on('line', line => {
        try {
            // Get a reference to the function to invoke
            const invocation = JSON.parse(line) as RpcInvocation;
            const invokedModule = dynamicRequire(path.resolve(process.cwd(), invocation.moduleName));
            const invokedFunction = invocation.exportedFunctionName ? invokedModule[invocation.exportedFunctionName] : invokedModule;

            // Actually invoke it, passing the callback followed by any supplied args
            const invocationCallback = (errorValue, successValue) => {
                connection.end(JSON.stringify({
                    result: successValue,
                    errorMessage: errorValue && (errorValue.message || errorValue),
                    errorDetails: errorValue && (errorValue.stack || null)
                }));
            };
            invokedFunction.apply(null, [invocationCallback].concat(invocation.args));
        } catch (ex) {
            connection.end(JSON.stringify({
                errorMessage: ex.message,
                errorDetails: ex.stack
            }));
        }
    });
});

// Begin listening now. The underlying transport varies according to the runtime platform.
// On Windows it's Named Pipes; on Linux/OSX it's Domain Sockets.
const useWindowsNamedPipes = /^win/.test(process.platform);
const listenAddress = (useWindowsNamedPipes ? '\\\\.\\pipe\\' : '/tmp/') + parsedArgs.pipename;
server.listen(listenAddress);

function autoQuitOnFileChange(rootDir: string, extensions: string[]) {
    // Note: This will only work on Windows/OS X, because the 'recursive' option isn't supported on Linux.
    // Consider using a different watch mechanism (though ideally without forcing further NPM dependencies).
    fs.watch(rootDir, { persistent: false, recursive: true } as any, (event, filename) => {
        var ext = path.extname(filename);
        if (extensions.indexOf(ext) >= 0) {
            console.log('Restarting due to file change: ' + filename);
            process.exit(0);
        }
    });
}

function parseArgs(args: string[]): any {
    // Very simplistic parsing which is sufficient for the cases needed. We don't want to bring in any external
    // dependencies (such as an args-parsing library) to this file.
    const result = {};
    let currentKey = null;
    args.forEach(arg => {
        if (arg.indexOf('--') === 0) {
            const argName = arg.substring(2);
            result[argName] = undefined;
            currentKey = argName;
        } else if (currentKey) {
            result[currentKey] = arg;
            currentKey = null;
        }
    });

    return result;
}

interface RpcInvocation {
    moduleName: string;
    exportedFunctionName: string;
    args: any[];
}
