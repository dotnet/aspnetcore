import * as childProcess from 'child_process';
import * as path from 'path';
import * as readline from 'readline';
import { waitUntilPortState } from './ports';
const treeKill = require('tree-kill');
const crossSpawn: typeof childProcess.spawn = require('cross-spawn');
const defaultPort = 5000;
const defaultInterface = 'localhost';

export const defaultUrl = `http://localhost:${ defaultPort }`;

export enum AspNetCoreEnviroment {
    development,
    production
}

export class AspNetProcess {
    public static RunInMochaContext(cwd: string, mode: AspNetCoreEnviroment, dllToRun?: string) {
        // Set up mocha before/after callbacks so that a 'dotnet run' process exists
        // for the same duration as the context this is called inside
        let aspNetProcess: AspNetProcess;
        before(() => {
            aspNetProcess = new AspNetProcess(cwd, mode, dllToRun);
            return aspNetProcess.waitUntilListening();
        });
        after(() => aspNetProcess.dispose());
    }

    private _process: childProcess.ChildProcess;
    private _processHasExited: boolean;
    private _stdoutReader: readline.ReadLine;

    constructor(cwd: string, mode: AspNetCoreEnviroment, dllToRun?: string) {
        try {
            // Prepare env for child process. Note that it doesn't inherit parent's env vars automatically,
            // hence cloning process.env.
            const childProcessEnv = Object.assign({}, process.env);
            childProcessEnv.ASPNETCORE_ENVIRONMENT = mode === AspNetCoreEnviroment.development ? 'Development' : 'Production';

            const verbOrAssembly = dllToRun || 'run';
            console.log(`Running 'dotnet ${ verbOrAssembly }' in ${ cwd }`);
            this._process = crossSpawn('dotnet', [verbOrAssembly], { cwd: cwd, stdio: 'pipe', env: childProcessEnv });
            this._stdoutReader = readline.createInterface(this._process.stdout, null);

            // Echo stdout to the test process's own stdout
            this._stdoutReader.on('line', line => {
                console.log(`[dotnet] ${ line.toString() }`);
            });

            // Also echo stderr
            this._process.stderr.on('data', chunk => {
                console.log(`[dotnet ERROR] ${ chunk.toString() }`);
            });

            // Ensure the process isn't orphaned even if Node crashes before we're disposed
            process.on('exit', () => this._killAspNetProcess());

            // Also track whether it exited on its own already
            this._process.on('exit', () => {
                this._processHasExited = true;
            });
        } catch(ex) {
            console.log('ERROR: ' + ex.toString());
            throw ex;
        }
    }

    public waitUntilListening(): Promise<any> {
        return new Promise((resolve, reject) => {
            this._stdoutReader.on('line', (line: string) => {
                if (line.startsWith('Now listening on:')) {
                    resolve();
                }
            });
        });
    }

    public dispose(): Promise<any> {
        return new Promise((resolve, reject) => {
            this._killAspNetProcess(err => {
                if (err) {
                    reject(err);
                } else {
                    resolve();
                }
            });
        });
    }

    private _killAspNetProcess(callback?: (err: any) => void) {
        callback = callback || (() => {});
        if (!this._processHasExited) {
            // It's important to kill the whole tree, because 'dotnet run' launches a separate 'dotnet exec'
            // child process that would otherwise be left running
            treeKill(this._process.pid, 'SIGINT', err => {
                if (err) {
                    callback(err);
                } else {
                    // It's not enough just to send a SIGINT to ASP.NET. It will stay open for a moment, completing
                    // any outstanding requests. We have to wait for it really to be gone before continuing, otherwise
                    // the next test might be unable to start because of the port still being in use.
                    console.log(`Waiting until port ${ defaultPort } is closed...`);
                    waitUntilPortState(defaultPort, defaultInterface, /* isListening */ false, /* timeoutMs */ 15000, err => {
                        if (err) {
                            callback(err);
                        } else {
                            console.log(`Port ${ defaultPort } is now closed`);
                            callback(null);
                        }
                    });
                }
            });
        }
    }
}

export function publishProjectSync(sourceDir: string, outputDir: string) {
    // Workaround for: MSB4018: The "ResolvePublishAssemblies" task failed unexpectedly
    // TODO: Remove this when the framework issue is fixed
    const aspNetCore20PublishWorkaround = '/p:TargetManifestFiles=';

    childProcess.execSync(`dotnet publish -c Release -o ${ outputDir } ${ aspNetCore20PublishWorkaround }`, {
        cwd: sourceDir,
        stdio: 'inherit',
        encoding: 'utf8'
    });
}
