import { CircuitHandler } from './CircuitHandler';
import { UserSpecifiedDisplay } from './UserSpecifiedDisplay';
import { DefaultReconnectDisplay } from './DefaultReconnectDisplay';
import { ReconnectDisplay } from './ReconnectDisplay';
export class AutoReconnectCircuitHandler implements CircuitHandler {
  static readonly MaxRetries = 5;
  static readonly RetryInterval = 3000;
  static readonly DialogId = 'components-reconnect-modal';
  reconnectDisplay: ReconnectDisplay;

  constructor() {
    this.reconnectDisplay = new DefaultReconnectDisplay(document);
    document.addEventListener('DOMContentLoaded', () => {
      const modal = document.getElementById(AutoReconnectCircuitHandler.DialogId);
      if (modal) {
        this.reconnectDisplay = new UserSpecifiedDisplay(modal);
      }
    });
  }
  onConnectionUp()  : void{
    this.reconnectDisplay.hide();
  }

  delay() : Promise<void>{
    return new Promise((resolve) => setTimeout(resolve, AutoReconnectCircuitHandler.RetryInterval));
  }

  async onConnectionDown() : Promise<void> {
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
        console.error(err);
      }
    }

    this.reconnectDisplay.failed();
  }
}
