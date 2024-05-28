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
        ? new UserSpecifiedDisplay(modal, document, options.maxRetries)
        : new DefaultReconnectDisplay(options.dialogId, document, this._logger);
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
    for (let i = 0; options.maxRetries === undefined || i < options.maxRetries; i++) {
      let retryInterval: number;
      if (typeof(options.retryIntervalMilliseconds) === 'function') {
        const computedRetryInterval = options.retryIntervalMilliseconds(i);
        if (computedRetryInterval === null || computedRetryInterval === undefined) {
          break;
        }
        retryInterval = computedRetryInterval;
      } else {
        retryInterval = i === 0 && options.retryIntervalMilliseconds > ReconnectionProcess.MaximumFirstRetryInterval
          ? ReconnectionProcess.MaximumFirstRetryInterval
          : options.retryIntervalMilliseconds;
      }

      await this.runTimer(retryInterval, /* intervalMs */ 1000, remainingMs => {
        this.reconnectDisplay.update(i + 1, Math.round(remainingMs / 1000));
      });

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

  private async runTimer(totalTimeMs: number, intervalMs: number, callback: (remainingMs: number) => void): Promise<void> {
    if (totalTimeMs <= 0) {
      callback(0);
      return;
    }

    let lastTime = Date.now();
    let timeoutId: unknown;
    let resolveTimerPromise: () => void;

    callback(totalTimeMs);

    const step = () => {
      if (this.isDisposed) {
        // Stop invoking the callback after disposal.
        resolveTimerPromise();
        return;
      }

      const currentTime = Date.now();
      const deltaTime = currentTime - lastTime;
      lastTime = currentTime;

      // Get the number of steps that should have passed have since the last
      // call to "step". We expect this to be 1 in most cases, but it may
      // be higher if something causes the timeout to get significantly
      // delayed (e.g., the browser sleeps the tab).
      const simulatedSteps = Math.max(1, Math.floor(deltaTime / intervalMs));
      const simulatedTime = intervalMs * simulatedSteps;

      totalTimeMs -= simulatedTime;
      if (totalTimeMs < Number.EPSILON) {
        callback(0);
        resolveTimerPromise();
        return;
      }

      const nextTimeout = Math.min(totalTimeMs, intervalMs - (deltaTime - simulatedTime));
      callback(totalTimeMs);
      timeoutId = setTimeout(step, nextTimeout);
    };

    const stepIfDocumentIsVisible = () => {
      // If the document becomes visible while the timeout is running, immediately
      // invoke the callback.
      if (document.visibilityState === 'visible') {
        clearTimeout(timeoutId as number);
        callback(0);
        resolveTimerPromise();
      }
    };

    timeoutId = setTimeout(step, intervalMs);

    document.addEventListener('visibilitychange', stepIfDocumentIsVisible);
    await new Promise<void>(resolve => resolveTimerPromise = resolve);
    document.removeEventListener('visibilitychange', stepIfDocumentIsVisible);
  }
}
