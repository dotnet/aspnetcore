import { CircuitHandler } from './CircuitHandler';
export class AutoReconnectCircuitHandler implements CircuitHandler {
  modal: HTMLDivElement;
  message: Text;
  isConnected: boolean | null;

  constructor(private maxRetries: number = 5, private retryInterval: number = 3000) {
    this.modal = document.createElement('div');
    this.modal.className = 'modal';
    this.message = document.createTextNode('');
    this.modal.appendChild(this.message);
    document.addEventListener('DOMContentLoaded', () => document.body.appendChild(this.modal));
    this.isConnected = null;
  }
  onConnectionUp() {
    this.modal.style.display = 'none';
  }
  async onConnectionDown() {
    this.message.textContent = 'Attempting to reconnect to the server...';

    this.modal.style.display = 'block';
    const delay = () => new Promise((resolve) => setTimeout(resolve, this.retryInterval));
    for (let i = 0; i < this.maxRetries; i++) {
      await delay();
      try {
        await window['Blazor'].reconnect();
        break;
      } catch (err) {
        console.error(err);
      }
    }

    this.message.textContent = 'Failed to connect to server.';
  }
}
