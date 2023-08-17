// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { DotNet } from '@microsoft/dotnet-js-interop';
import { HubConnection } from '@microsoft/signalr';

export class CircuitDotNetCallDispatcher implements DotNet.DotNetCallDispatcher {
  private _disposed = false;

  constructor(private readonly getCurrentConnection: () => HubConnection) {
  }

  beginInvokeDotNetFromJS(callId: number, assemblyName: string | null, methodIdentifier: string, dotNetObjectId: number | null, argsJson: string): void {
    this.throwIfDisposed();
    const connection = this.getCurrentConnection();
    connection.send('BeginInvokeDotNetFromJS', callId ? callId.toString() : null, assemblyName, methodIdentifier, dotNetObjectId || 0, argsJson);
  }

  endInvokeJSFromDotNet(asyncHandle: number, succeeded: boolean, argsJson: any): void {
    this.throwIfDisposed();
    const connection = this.getCurrentConnection();
    connection.send('EndInvokeJSFromDotNet', asyncHandle, succeeded, argsJson);
  }

  sendByteArray(id: number, data: Uint8Array): void {
    this.throwIfDisposed();
    const connection = this.getCurrentConnection();
    connection.send('ReceiveByteArray', id, data);
  }

  dispose() {
    this._disposed = true;
  }

  private throwIfDisposed() {
    if (this._disposed) {
      throw new Error('The circuit associated with this dispatcher is no longer available.');
    }
  }
}
