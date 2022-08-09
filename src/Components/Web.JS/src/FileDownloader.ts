// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export async function downloadFile(data: any, fileName: string) {
    // For Chromium browsers, show "Save As" dialog and then stream the data into the file without buffering it all in memory
    if (typeof (window as any).showSaveFilePicker === 'function')
    {
        // Show the "Save As" dialog
        let fileWriter;
        try {
            const fileHandle = await (window as any).showSaveFilePicker();
            // Create a FileSystemWritableFileStream to write to.
            fileWriter = await fileHandle.createWritable();
        } catch {
            // User pressed cancel, so abort the whole thing
            return;
        }

        var dataStream = new ReadableStream(data);
        const reader = dataStream.getReader();
        while (true) {
            const readResult = await reader.read();
            if (readResult.done) {
                break;
            }
            // Write the contents of the file to the stream.
            await fileWriter.write(readResult.value);
        }
        // Close the file and write the contents to disk
        await fileWriter.close();
    }
    else {
        // The following option works for all browsers
        const arrayBuffer = await data.arrayBuffer();
        const blob = new Blob([arrayBuffer]);
        const url = URL.createObjectURL(blob);
        const anchorElement = document.createElement('a');
        anchorElement.href = url;
        anchorElement.download = fileName;
        anchorElement.click();
        anchorElement.remove();
        URL.revokeObjectURL(url);
    }
}
