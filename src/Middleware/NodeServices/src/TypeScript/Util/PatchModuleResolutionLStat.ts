import * as path from 'path';
const startsWith = (str: string, prefix: string) => str.substring(0, prefix.length) === prefix;
const appRootDir = process.cwd();

function patchedLStat(pathToStatLong: string, fsReqWrap?: any) {
    try {
        // If the lstat completes without errors, we don't modify its behavior at all
        return origLStat.apply(this, arguments);
    } catch(ex) {
        const shouldOverrideError =
            startsWith(ex.message, 'EPERM')                     // It's a permissions error
            && typeof appRootDirLong === 'string'
            && startsWith(appRootDirLong, pathToStatLong)       // ... for an ancestor directory
            && ex.stack.indexOf('Object.realpathSync ') >= 0;   // ... during symlink resolution

        if (shouldOverrideError) {
            // Fake the result to give the same result as an 'lstat' on the app root dir.
            // This stops Node failing to load modules just because it doesn't know whether
            // ancestor directories are symlinks or not. If there's a genuine file
            // permissions issue, it will still surface later when Node actually
            // tries to read the file.
            return origLStat.call(this, appRootDir, fsReqWrap);
        } else {
            // In any other case, preserve the original error
            throw ex;
        }
    }
};

// It's only necessary to apply this workaround on Windows
let appRootDirLong: string = null;
let origLStat: Function = null;
if (/^win/.test(process.platform)) {
    try {
        // Get the app's root dir in Node's internal "long" format (e.g., \\?\C:\dir\subdir)
        appRootDirLong = (path as any)._makeLong(appRootDir);

        // Actually apply the patch, being as defensive as possible
        const bindingFs = (process as any).binding('fs');
        origLStat = bindingFs.lstat;
        if (typeof origLStat === 'function') {
            bindingFs.lstat = patchedLStat;
        }
    } catch(ex) {
        // If some future version of Node throws (e.g., to prevent use of process.binding()),
        // don't apply the patch, but still let the application run.
    }
}
