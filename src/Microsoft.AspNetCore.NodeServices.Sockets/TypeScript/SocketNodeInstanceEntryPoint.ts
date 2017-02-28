// Limit dependencies to core Node modules. This means the code in this file has to be very low-level and unattractive,
// but simplifies things for the consumer of this module.
import '../../Microsoft.AspNetCore.NodeServices/TypeScript/Util/OverrideStdOutputs';
import * as net from 'net';
import * as path from 'path';
import * as readline from 'readline';
import { Duplex } from 'stream';
import { parseArgs } from '../../Microsoft.AspNetCore.NodeServices/TypeScript/Util/ArgsUtil';
import { exitWhenParentExits } from '../../Microsoft.AspNetCore.NodeServices/TypeScript/Util/ExitWhenParentExits';
import * as virtualConnectionServer from './VirtualConnections/VirtualConnectionServer';

// Webpack doesn't support dynamic requires for files not present at compile time, so grab a direct
// reference to Node's runtime 'require' function.
const dynamicRequire: (name: string) => any = eval('require');

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

            // Prepare a callback for accepting non-streamed JSON responses
            let hasInvokedCallback = false;
            const invocationCallback = (errorValue, successValue) => {
                if (hasInvokedCallback) {
                    throw new Error('Cannot supply more than one result. The callback has already been invoked,'
                        + ' or the result stream has already been accessed');
                }

                hasInvokedCallback = true;
                connection.end(JSON.stringify({
                    result: successValue,
                    errorMessage: errorValue && (errorValue.message || errorValue),
                    errorDetails: errorValue && (errorValue.stack || null)
                }));
            };

            // Also support streamed binary responses
            Object.defineProperty(invocationCallback, 'stream', {
                enumerable: true,
                get: (): Duplex => {
                    hasInvokedCallback = true;
                    return connection;
                }
            });

            // Actually invoke it, passing through any supplied args
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
const parsedArgs = parseArgs(process.argv);
const listenAddress = (useWindowsNamedPipes ? '\\\\.\\pipe\\' : '/tmp/') + parsedArgs.listenAddress;
server.listen(listenAddress);

exitWhenParentExits(parseInt(parsedArgs.parentPid), /* ignoreSigint */ true);

interface RpcInvocation {
    moduleName: string;
    exportedFunctionName: string;
    args: any[];
}
