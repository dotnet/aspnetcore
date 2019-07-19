import { CircuitHandler } from './CircuitHandler';
import { UserSpecifiedDisplay } from './UserSpecifiedDisplay';
import { DefaultReconnectDisplay } from './DefaultReconnectDisplay';
import { ReconnectDisplay } from './ReconnectDisplay';
import { Logger, LogLevel } from '../Logging/Logger';
import { ReconnectionOptions } from './BlazorOptions';

export class AutoReconnectCircuitHandler implements CircuitHandler {
  public reconnectDisplay: ReconnectDisplay;

  public constructor(public options: ReconnectionOptions, public logger: Logger) {
    this.reconnectDisplay = new DefaultReconnectDisplay(options.dialogId, document);
    document.addEventListener('DOMContentLoaded', () => {
      const modal = document.getElementById(this.options.dialogId);
      if (modal) {
        this.reconnectDisplay = new UserSpecifiedDisplay(modal);
      }
    });
  }

  public onConnectionUp(): void {
    this.reconnectDisplay.hide();
  }

  public delay(): Promise<void> {
    return new Promise((resolve) => setTimeout(resolve, this.options.retryIntervalMilliseconds));
  }

  public async onConnectionDown(): Promise<void> {
    this.reconnectDisplay.show();

    for (let i = 0; i < this.options.maxRetries; i++) {
      await this.delay();
      try {
        const result = await window['Blazor'].reconnect();
        if (!result) {
          // If the server responded and refused to reconnect, stop auto-retrying.
          break;
        }
        return;
      } catch (err) {
        this.logger.log(LogLevel.Error, err);
      }
    }

    this.reconnectDisplay.failed();
  }
}
