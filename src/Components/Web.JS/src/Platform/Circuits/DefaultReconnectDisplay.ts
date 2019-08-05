import { ReconnectDisplay } from './ReconnectDisplay';

export class DefaultReconnectDisplay implements ReconnectDisplay {
  modal: HTMLDivElement;

  message: HTMLHeadingElement;

  button: HTMLButtonElement;

  addedToDom: boolean = false;

  reloadParagraph: HTMLParagraphElement;

  constructor(dialogId: string, private document: Document) {
    this.modal = this.document.createElement('div');
    this.modal.id = dialogId;

    const modalStyles = [
      'position: fixed',
      'top: 0',
      'right: 0',
      'bottom: 0',
      'left: 0',
      'z-index: 1000',
      'display: none',
      'overflow: hidden',
      'background-color: #fff',
      'opacity: 0.8',
      'text-align: center',
      'font-weight: bold',
    ];

    this.modal.style.cssText = modalStyles.join(';');
    this.modal.innerHTML = '<h5 style="margin-top: 20px"></h5><button style="margin:5px auto 5px">Retry?</button><p>Alternatively, <a href>reload</a></p>';
    this.message = this.modal.querySelector('h5')!;
    this.button = this.modal.querySelector('button')!;
    this.reloadParagraph = this.modal.querySelector('p')!;

    this.button.addEventListener('click', () => window['Blazor'].reconnect());
    this.reloadParagraph.querySelector('a')!.addEventListener('click', () => location.reload());
  }

  show(): void {
    if (!this.addedToDom) {
      this.addedToDom = true;
      this.document.body.appendChild(this.modal);
    }
    this.modal.style.display = 'block';
    this.button.style.display = 'none';
    this.reloadParagraph.style.display = 'none';
    this.message.textContent = 'Attempting to reconnect to the server...';
  }

  hide(): void {
    this.modal.style.display = 'none';
  }

  failed(): void {
    this.button.style.display = 'block';
    this.reloadParagraph.style.display = 'block';
    this.message.textContent = 'Failed to reconnect to the server.';
  }
}
