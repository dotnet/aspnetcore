export function parseArgs(args: string[]): any {
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
