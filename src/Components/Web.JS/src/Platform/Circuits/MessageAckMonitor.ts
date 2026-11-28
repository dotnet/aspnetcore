// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { HttpError, HubConnection, HubConnectionState, LogLevel, TimeoutError } from '@microsoft/signalr';
import { ConsoleLogger } from '../Logging/Loggers';

interface HubConnectionWithSendProtocol {
  _sendWithProtocol?: (message: unknown) => Promise<void>;
}

export class MessageAckMonitor {
  private readonly _timers = new Set<ReturnType<typeof setTimeout>>();

  private readonly _deliveryFailures = new WeakSet<object>();

  private readonly _originalSendWithProtocol?: (message: unknown) => Promise<void>;

  private _disposed = false;

  public constructor(
    private readonly _connection: HubConnection,
    private readonly _timeout: number,
    private readonly _logger: ConsoleLogger,
    private readonly _onFailure?: (error: Error) => void,
  ) {
    const connection = this._connection as unknown as HubConnectionWithSendProtocol;
    if (typeof connection._sendWithProtocol === 'function') {
      const originalSendWithProtocol = connection._sendWithProtocol;
      this._originalSendWithProtocol = originalSendWithProtocol;
      connection._sendWithProtocol = (message: unknown): Promise<void> => {
        try {
          return originalSendWithProtocol.call(this._connection, message).catch((error: unknown) => {
            this.recordDeliveryFailure(error);
            throw error;
          });
        } catch (error) {
          this.recordDeliveryFailure(error);
          throw error;
        }
      };
    }
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
        if (this.isDeliveryFailure(error)) {
          this.fail(error instanceof Error ? error : new Error(String(error)));
        }
      },
    );
    return acknowledgement;
  }

  public dispose(): void {
    this._disposed = true;
    if (this._originalSendWithProtocol) {
      const connection = this._connection as unknown as HubConnectionWithSendProtocol;
      connection._sendWithProtocol = this._originalSendWithProtocol;
    }
    for (const timer of this._timers) {
      clearTimeout(timer);
    }
    this._timers.clear();
  }

  private clearTimer(timer: ReturnType<typeof setTimeout>): void {
    clearTimeout(timer);
    this._timers.delete(timer);
  }

  private recordDeliveryFailure(error: unknown): void {
    if (error && typeof error === 'object') {
      this._deliveryFailures.add(error);
    }
  }

  private isDeliveryFailure(error: unknown): boolean {
    if (error && typeof error === 'object' && this._deliveryFailures.has(error)) {
      return true;
    }

    return error instanceof HttpError || error instanceof TimeoutError || error instanceof TypeError;
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
      // Trigger local reconnection immediately rather than waiting for Long Polling cleanup DELETE timeouts.
      this._onFailure?.(error);
    }
  }
}
