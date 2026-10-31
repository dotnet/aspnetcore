// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

const ipcMessagePrefix = '__bwv:';
let applicationIsTerminated = false;

export function trySerializeMessage(messageType: string, args: any[]): string | null {
  return applicationIsTerminated
    ? null
    : `${ipcMessagePrefix}${JSON.stringify([messageType, ...args])}`;
}

export function tryDeserializeMessage(message: string): IpcMessage | null {
  if (applicationIsTerminated || !message || !message.startsWith(ipcMessagePrefix)) {
    return null;
  }

  const messageAfterPrefix = message.substring(ipcMessagePrefix.length);
  const [messageType, ...args] = JSON.parse(messageAfterPrefix);
  return { messageType, args };
}

export function setApplicationIsTerminated(): void {
  // If there's an unhandled exception, we'll prevent the webview from doing anything else until
  // it reloads the page. This is equivalent to what happens in Blazor Server, and avoids anyone
  // taking a dependency on being able to continue interacting after a fatal error.
  applicationIsTerminated = true;
}

interface IpcMessage {
  messageType: string;
  args: unknown[];
}
