import * as fs from 'fs';
import * as path from 'path';
const isWindows = /^win/.test(process.platform);

// On Windows, Node (still as of v8.1.3) has an issue whereby, when locating JavaScript modules
// on disk, it walks up the directory hierarchy to the disk root, testing whether each directory
// is a symlink or not. This fails with an exception if the process doesn't have permission to
// read those directories. This is a problem when hosting in full IIS, because in typical cases
// the process does not have read permission for higher-level directories.
//
// NodeServices itself works around this by injecting a patched version of Node's 'lstat' API that
// suppresses these irrelevant errors during module loads. This covers most scenarios, but isn't
// enough to make Webpack dev middleware work, because typical Webpack configs use loaders such as
// 'awesome-typescript-loader', which works by forking a child process to do some of its work. The
// child process does not get the patched 'lstat', and hence fails. It's an especially bad failure,
// because the Webpack compiler doesn't even surface the exception - it just never completes the
// compilation process, causing the application to hang indefinitely.
//
// Additionally, Webpack dev middleware will want to write its output to disk, which is also going
// to fail in a typical IIS process, because you won't have 'write' permission to the app dir by
// default. We have to actually write the build output to disk (and not purely keep it in the in-
// memory file system) because the server-side prerendering Node instance is a separate process
// that only knows about code changes when it sees the compiled files on disk change.
//
// In the future, we'll hopefully get Node to fix its underlying issue, and figure out whether VS
// could give 'write' access to the app dir when launching sites in IIS. But until then, disable
// Webpack dev middleware if we detect the server process doesn't have the necessary permissions.

export function hasSufficientPermissions() {
    if (isWindows) {
        return canReadDirectoryAndAllAncestors(process.cwd());
    } else {
        return true;
    }
}

function canReadDirectoryAndAllAncestors(dir: string): boolean {
    if (!canReadDirectory(dir)) {
        return false;
    }

    const parentDir = path.resolve(dir, '..');
    if (parentDir === dir) {
        // There are no more parent directories - we've reached the disk root
        return true;
    } else {
        return canReadDirectoryAndAllAncestors(parentDir);
    }
}

function canReadDirectory(dir: string): boolean {
    try {
        fs.statSync(dir);
        return true;
    } catch(ex) {
        return false;
    }
}
