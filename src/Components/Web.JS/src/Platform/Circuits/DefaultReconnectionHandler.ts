// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ReconnectionHandler, ReconnectionOptions } from './CircuitStartOptions';
import { ReconnectDisplay } from './ReconnectDisplay';
import { DefaultReconnectDisplay } from './DefaultReconnectDisplay';
import { UserSpecifiedDisplay } from './UserSpecifiedDisplay';
import { Logger, LogLevel } from '../Logging/Logger';
import { Blazor } from '../../GlobalExports';

export class DefaultReconnectionHandler implements ReconnectionHandler {
  private readonly _logger: Logger;

  private readonly _reconnectCallback: () => Promise<boolean>;

  private _currentReconnectionProcess: ReconnectionProcess | null = null;

  private _reconnectionDisplay?: ReconnectDisplay;

  constructor(logger: Logger, overrideDisplay?: ReconnectDisplay, reconnectCallback?: () => Promise<boolean>) {
    this._logger = logger;
    this._reconnectionDisplay = overrideDisplay;
    this._reconnectCallback = reconnectCallback || Blazor.reconnect!;
  }

  onConnectionDown(options: ReconnectionOptions, _error?: Error): void {
    if (!this._reconnectionDisplay) {
      const modal = document.getElementById(options.dialogId);
      this._reconnectionDisplay = modal
        ? new UserSpecifiedDisplay(modal, options.maxRetries, document)
        : new DefaultReconnectDisplay(options.dialogId, options.maxRetries, document, this._logger);
    }

    if (!this._currentReconnectionProcess) {
      this._currentReconnectionProcess = new ReconnectionProcess(options, this._logger, this._reconnectCallback, this._reconnectionDisplay);
    }
  }

  onConnectionUp(): void {
    if (this._currentReconnectionProcess) {
      this._currentReconnectionProcess.dispose();
      this._currentReconnectionProcess = null;
    }
  }
}

class ReconnectionProcess {
  static readonly MaximumFirstRetryInterval = 3000;

  readonly reconnectDisplay: ReconnectDisplay;

  isDisposed = false;

  constructor(options: ReconnectionOptions, private logger: Logger, private reconnectCallback: () => Promise<boolean>, display: ReconnectDisplay) {
    this.reconnectDisplay = display;
    this.reconnectDisplay.show();
    this.attemptPeriodicReconnection(options);
  }

  public dispose() {
    this.isDisposed = true;
    this.reconnectDisplay.hide();
  }

  async attemptPeriodicReconnection(options: ReconnectionOptions) {
    for (let i = 0; i < options.maxRetries; i++) {
      this.reconnectDisplay.update(i + 1);

      const delayDuration = i === 0 && options.retryIntervalMilliseconds > ReconnectionProcess.MaximumFirstRetryInterval
        ? ReconnectionProcess.MaximumFirstRetryInterval
        : options.retryIntervalMilliseconds;
      await this.delay(delayDuration);

      if (this.isDisposed) {
        break;
      }

      try {
        // reconnectCallback will asynchronously return:
        // - true to mean success
        // - false to mean we reached the server, but it rejected the connection (e.g., unknown circuit ID)
        // - exception to mean we didn't reach the server (this can be sync or async)
        const result = await this.reconnectCallback();
        if (!result) {
          // If the server responded and refused to reconnect, stop auto-retrying.
          this.reconnectDisplay.rejected();
          return;
        }
        return;
      } catch (err: unknown) {
        // We got an exception so will try again momentarily
        this.logger.log(LogLevel.Error, err as Error);
      }
    }

    this.reconnectDisplay.failed();
  }

  delay(durationMilliseconds: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, durationMilliseconds));
  }
}
