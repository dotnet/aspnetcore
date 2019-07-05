import { CircuitHandler } from './CircuitHandler';
import { UserSpecifiedDisplay } from './UserSpecifiedDisplay';
import { DefaultReconnectDisplay } from './DefaultReconnectDisplay';
import { ReconnectDisplay } from './ReconnectDisplay';
import { ILogger, LogLevel } from '../Logging/ILogger';
export class AutoReconnectCircuitHandler implements CircuitHandler {
  public static readonly MaxRetries = 5;

  public static readonly RetryInterval = 3000;

  public static readonly DialogId = 'components-reconnect-modal';

  public reconnectDisplay: ReconnectDisplay;

  public logger: ILogger;

  public constructor(logger: ILogger) {
    this.logger = logger;
    this.reconnectDisplay = new DefaultReconnectDisplay(document);
    document.addEventListener('DOMContentLoaded', () => {
      const modal = document.getElementById(AutoReconnectCircuitHandler.DialogId);
      if (modal) {
        this.reconnectDisplay = new UserSpecifiedDisplay(modal);
      }
    });
  }

  public onConnectionUp(): void {
    this.reconnectDisplay.hide();
  }

  public delay(): Promise<void> {
    return new Promise((resolve) => setTimeout(resolve, AutoReconnectCircuitHandler.RetryInterval));
  }

  public async onConnectionDown(): Promise<void> {
    this.reconnectDisplay.show();

    for (let i = 0; i < AutoReconnectCircuitHandler.MaxRetries; i++) {
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
