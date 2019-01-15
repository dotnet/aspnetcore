// When Node writes to stdout/strerr, we capture that and convert the lines into calls on the
// active .NET ILogger. But by default, stdout/stderr don't have any way of distinguishing
// linebreaks inside log messages from the linebreaks that delimit separate log messages,
// so multiline strings will end up being written to the ILogger as multiple independent
// log messages. This makes them very hard to make sense of, especially when they represent
// something like stack traces.
//
// To fix this, we intercept stdout/stderr writes, and replace internal linebreaks with a
// marker token. When .NET receives the lines, it converts the marker tokens back to regular
// linebreaks within the logged messages.
//
// Note that it's better to do the interception at the stdout/stderr level, rather than at
// the console.log/console.error (etc.) level, because this takes place after any native
// message formatting has taken place (e.g., inserting values for % placeholders).
const findInternalNewlinesRegex = /\n(?!$)/g;
const encodedNewline = '__ns_newline__';

encodeNewlinesWrittenToStream(process.stdout);
encodeNewlinesWrittenToStream(process.stderr);

function encodeNewlinesWrittenToStream(outputStream: NodeJS.WritableStream) {
    const origWriteFunction = outputStream.write;
    outputStream.write = <any>function (value: any) {
        // Only interfere with the write if it's definitely a string
        if (typeof value === 'string') {
            const argsClone = Array.prototype.slice.call(arguments, 0);
            argsClone[0] = encodeNewlinesInString(value);
            origWriteFunction.apply(this, argsClone);
        } else {
            origWriteFunction.apply(this, arguments);
        }
    };
}

function encodeNewlinesInString(str: string): string {
    return str.replace(findInternalNewlinesRegex, encodedNewline);
}
