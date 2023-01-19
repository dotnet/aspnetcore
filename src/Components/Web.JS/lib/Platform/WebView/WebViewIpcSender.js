// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
import { trySerializeMessage } from './WebViewIpcCommon';
export function sendAttachPage(baseUrl, startUrl) {
    send('AttachPage', baseUrl, startUrl);
}
export function sendRenderCompleted(batchId, errorOrNull) {
    send('OnRenderCompleted', batchId, errorOrNull);
}
export function sendBeginInvokeDotNetFromJS(callId, assemblyName, methodIdentifier, dotNetObjectId, argsJson) {
    send('BeginInvokeDotNet', callId ? callId.toString() : null, assemblyName, methodIdentifier, dotNetObjectId || 0, argsJson);
}
export function sendEndInvokeJSFromDotNet(asyncHandle, succeeded, argsJson) {
    send('EndInvokeJS', asyncHandle, succeeded, argsJson);
}
export function sendByteArray(id, data) {
    const dataBase64Encoded = base64EncodeByteArray(data);
    send('ReceiveByteArrayFromJS', id, dataBase64Encoded);
}
function base64EncodeByteArray(data) {
    // Base64 encode a (large) byte array
    // Note `btoa(String.fromCharCode.apply(null, data as unknown as number[]));`
    // isn't sufficient as the `apply` over a large array overflows the stack.
    const charBytes = new Array(data.length);
    for (let i = 0; i < data.length; i++) {
        charBytes[i] = String.fromCharCode(data[i]);
    }
    const dataBase64Encoded = btoa(charBytes.join(''));
    return dataBase64Encoded;
}
export function sendLocationChanged(uri, state, intercepted) {
    send('OnLocationChanged', uri, state, intercepted);
    return Promise.resolve(); // Like in Blazor Server, we only issue the notification here - there's no need to wait for a response
}
export function sendLocationChanging(callId, uri, state, intercepted) {
    send('OnLocationChanging', callId, uri, state, intercepted);
    return Promise.resolve(); // Like in Blazor Server, we only issue the notification here - there's no need to wait for a response
}
function send(messageType, ...args) {
    const serializedMessage = trySerializeMessage(messageType, args);
    if (serializedMessage) {
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        window.external.sendMessage(serializedMessage);
    }
}
//# sourceMappingURL=WebViewIpcSender.js.map