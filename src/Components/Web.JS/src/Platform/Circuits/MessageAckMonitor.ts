// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { HubConnection, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { ConsoleLogger } from '../Logging/Loggers';

export class MessageAckMonitor {
  private readonly _timers = new Set<ReturnType<typeof setTimeout>>();

  private _disposed = false;

  public constructor(
    private readonly _connection: HubConnection,
    private readonly _timeout: number,
    private readonly _logger: ConsoleLogger,
  ) {
  }

  public send(method: string, ...args: unknown[]): Promise<void> {
    if (this._disposed) {
      throw new Error('Cannot send a message through a disposed acknowledgement monitor.');
    }

    if (this._timeout === 0) {
     return this._connection.send(method, ...args);
    }

    const timer = setTimeout(() => this.fail(new Error(`Server receipt of '${method}' was not acknowledged within ${this._timeout} milliseconds.`)), this._timeout);
    this._timers.add(timer);
    const acknowledgement = this._connection.invoke<void>(method, ...args);
    acknowledgement.then(
      () => this.clearTimer(timer),
      error => {
        this.clearTimer(timer);
        this.fail(error);
      },
    );
    return acknowledgement;
  }

  public dispose(): void {
    this._disposed = true;
    for (const timer of this._timers) {
      clearTimeout(timer);
    }
    this._timers.clear();
  }

  private clearTimer(timer: ReturnType<typeof setTimeout>): void {
    clearTimeout(timer);
    this._timers.delete(timer);
  }

  private fail(error: Error): void {
    if (this._disposed) {
      return;
    }

    this.dispose();
    if (this._connection.state === HubConnectionState.Connected) {
      this._logger.log(LogLevel.Warning, error);
      // Stop only the connection that owns this acknowledgement, never a replacement.
      this._connection.stop().catch(stopError => this._logger.log(LogLevel.Error, stopError));
    }
  }
}
