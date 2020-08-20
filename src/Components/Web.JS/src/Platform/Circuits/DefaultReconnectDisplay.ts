import { ReconnectDisplay } from './ReconnectDisplay';
import { Logger, LogLevel } from '../Logging/Logger';

export class DefaultReconnectDisplay implements ReconnectDisplay {
  modal: HTMLDivElement;

  message: HTMLHeadingElement;

  button: HTMLButtonElement;

  addedToDom: boolean = false;

  reloadParagraph: HTMLParagraphElement;

  loader: HTMLDivElement;

  constructor(dialogId: string, private readonly maxRetries: number, private readonly document: Document, private readonly logger: Logger) {
    this.modal = this.document.createElement('div');
    this.modal.id = dialogId;
    this.maxRetries = maxRetries;

    const modalStyles = [
      'position: fixed',
      'top: 0',
      'right: 0',
      'bottom: 0',
      'left: 0',
      'z-index: 1050',
      'display: none',
      'overflow: hidden',
      'background-color: #fff',
      'opacity: 0.8',
      'text-align: center',
      'font-weight: bold',
      'transition: visibility 0s linear 500ms',
    ];

    this.modal.style.cssText = modalStyles.join(';');
    this.modal.innerHTML = '<h5 style="margin-top: 20px"></h5><button style="margin:5px auto 5px">Retry</button><p>Alternatively, <a href>reload</a></p>';
    this.message = this.modal.querySelector('h5')!;
    this.button = this.modal.querySelector('button')!;
    this.reloadParagraph = this.modal.querySelector('p')!;
    this.loader = this.getLoader();
    
    this.message.after(this.loader);

    this.button.addEventListener('click', async () => {
      this.show();

      try {
        // reconnect will asynchronously return:
        // - true to mean success
        // - false to mean we reached the server, but it rejected the connection (e.g., unknown circuit ID)
        // - exception to mean we didn't reach the server (this can be sync or async)
        const successful = await window['Blazor'].reconnect();
        if (!successful) {
          this.rejected();
        }
      } catch (err) {
        // We got an exception, server is currently unavailable
        this.logger.log(LogLevel.Error, err);
        this.failed();
      }
    });
    this.reloadParagraph.querySelector('a')!.addEventListener('click', () => location.reload());
  }

  show(): void {
    if (!this.addedToDom) {
      this.addedToDom = true;
      this.document.body.appendChild(this.modal);
    }
    this.modal.style.display = 'block';
    this.loader.style.display = 'inline-block';
    this.button.style.display = 'none';
    this.reloadParagraph.style.display = 'none';
    this.message.textContent = 'Attempting to reconnect to the server...';

    // The visibility property has a transition so it takes effect after a delay.
    // This is to prevent it appearing momentarily when navigating away. For the
    // transition to take effect, we have to apply the visibility asynchronously.
    this.modal.style.visibility = 'hidden';
    setTimeout(() => {
      this.modal.style.visibility = 'visible';
    }, 0);
  }

  update(currentAttempt: number): void {
    this.message.textContent = `Attempting to reconnect to the server: ${currentAttempt} of ${this.maxRetries}`;
  }

  hide(): void {
    this.modal.style.display = 'none';
  }

  failed(): void {
    this.button.style.display = 'block';
    this.reloadParagraph.style.display = 'none';
    this.loader.style.display = 'none';
    this.message.innerHTML = 'Reconnection failed. Try <a href>reloading</a> the page if you\'re unable to reconnect.';
    this.message.querySelector('a')!.addEventListener('click', () => location.reload());
  }

  rejected(): void {
    this.button.style.display = 'none';
    this.reloadParagraph.style.display = 'none';
    this.loader.style.display = 'none';
    this.message.innerHTML = 'Could not reconnect to the server. <a href>Reload</a> the page to restore functionality.';
    this.message.querySelector('a')!.addEventListener('click', () => location.reload());
  }

  private getLoader(): HTMLDivElement {
    const loader = this.document.createElement('div');

    const loaderStyles = [
      'border: 0.3em solid #f3f3f3',
      'border-top: 0.3em solid #3498db',
      'border-radius: 50%',
      'width: 2em',
      'height: 2em',
      'display: inline-block'
    ];

    loader.style.cssText = loaderStyles.join(';');
    loader.animate([
      { transform: 'rotate(0deg)' },
      { transform: 'rotate(360deg)' }
    ], { 
      duration: 2000,
      iterations: Infinity
    });

    return loader;
  }
}
