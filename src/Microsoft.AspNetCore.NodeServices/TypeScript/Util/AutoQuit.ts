import * as fs from 'fs';
import * as path from 'path';

export function autoQuitOnFileChange(rootDir: string, extensions: string[]) {
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
