import { ReconnectionHandler, ReconnectionOptions } from './BlazorOptions';
import { ReconnectDisplay } from './ReconnectDisplay';
import { DefaultReconnectDisplay } from './DefaultReconnectDisplay';
import { UserSpecifiedDisplay } from './UserSpecifiedDisplay';
import { Logger, LogLevel } from '../Logging/Logger';

export class DefaultReconnectionHandler implements ReconnectionHandler {
  private readonly _logger: Logger;
  private readonly _reconnectCallback: () => Promise<boolean>;
  private _currentReconnectionProcess: ReconnectionProcess | null = null;
  private _reconnectionDisplay?: ReconnectDisplay;

  constructor(logger: Logger, overrideDisplay?: ReconnectDisplay, reconnectCallback?: () => Promise<boolean>) {
    this._logger = logger;
    this._reconnectionDisplay = overrideDisplay;
    this._reconnectCallback = reconnectCallback || (() => window['Blazor'].reconnect());
  }

  onConnectionDown (options: ReconnectionOptions, error?: Error) {
    if (!this._reconnectionDisplay) {
      const modal = document.getElementById(options.dialogId);
      this._reconnectionDisplay = modal
          ? new UserSpecifiedDisplay(modal)
          : new DefaultReconnectDisplay(options.dialogId, document);
    }

    if (!this._currentReconnectionProcess) {
      this._currentReconnectionProcess = new ReconnectionProcess(options, this._logger, this._reconnectCallback, this._reconnectionDisplay!);
    }
  }

  onConnectionUp() {
    if (this._currentReconnectionProcess) {
      this._currentReconnectionProcess.dispose();
      this._currentReconnectionProcess = null;
    }
  }
};

class ReconnectionProcess {
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
      await this.delay(options.retryIntervalMilliseconds);
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
          break;
        }
        return;
      } catch (err) {
        // We got an exception so will try again momentarily
        this.logger.log(LogLevel.Error, err);
      }
    }

    this.reconnectDisplay.failed();
  }

  delay(durationMilliseconds: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, durationMilliseconds));
  }
}
