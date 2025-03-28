// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { trySerializeMessage } from './WebViewIpcCommon';

export function sendAttachPage(baseUrl: string, startUrl: string): void {
  send('AttachPage', baseUrl, startUrl);
}

export function sendRenderCompleted(batchId: number, errorOrNull: string | null): void {
  send('OnRenderCompleted', batchId, errorOrNull);
}

export function sendBeginInvokeDotNetFromJS(callId: number, assemblyName: string | null, methodIdentifier: string, dotNetObjectId: number | null, argsJson: string): void {
  send('BeginInvokeDotNet', callId ? callId.toString() : null, assemblyName, methodIdentifier, dotNetObjectId || 0, argsJson);
}

export function sendEndInvokeJSFromDotNet(asyncHandle: number, succeeded: boolean, argsJson: any): void {
  send('EndInvokeJS', asyncHandle, succeeded, argsJson);
}

export function sendByteArray(id: number, data: Uint8Array): void {
  const dataBase64Encoded = base64EncodeByteArray(data);
  send('ReceiveByteArrayFromJS', id, dataBase64Encoded);
}

function base64EncodeByteArray(data: Uint8Array) {
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

export function sendLocationChanged(uri: string, state: string | undefined, intercepted: boolean): Promise<void> {
  send('OnLocationChanged', uri, state, intercepted);
  return Promise.resolve(); // Like in Blazor Server, we only issue the notification here - there's no need to wait for a response
}

export function sendLocationChanging(callId: number, uri: string, state: string | undefined, intercepted: boolean): Promise<void> {
  send('OnLocationChanging', callId, uri, state, intercepted);
  return Promise.resolve(); // Like in Blazor Server, we only issue the notification here - there's no need to wait for a response
}

function send(messageType: string, ...args: unknown[]) {
  const serializedMessage = trySerializeMessage(messageType, args);
  if (serializedMessage) {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    (window.external as any).sendMessage(serializedMessage);
  }
}
