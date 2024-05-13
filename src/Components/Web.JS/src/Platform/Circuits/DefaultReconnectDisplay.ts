// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ReconnectDisplay } from './ReconnectDisplay';
import { Logger, LogLevel } from '../Logging/Logger';
import { Blazor } from '../../GlobalExports';

export class DefaultReconnectDisplay implements ReconnectDisplay {
  static readonly ReconnectOverlayClassName = 'components-reconnect-overlay';

  static readonly ReconnectDialogClassName = 'components-reconnect-dialog';

  static readonly ReconnectVisibleClassName = 'components-reconnect-visible';

  static readonly RippleGroupClassName = 'components-ripple-group';

  static readonly RippleCount = 2;

  static readonly ShowCheckInternetDelayMilliseconds = 5000;

  style: HTMLStyleElement;

  overlay: HTMLDivElement;

  dialog: HTMLDivElement;

  checkInternetElement: HTMLParagraphElement;

  showCheckInternetTimeout?: number;

  constructor(dialogId: string, private readonly maxRetries: number, private readonly document: Document, private readonly logger: Logger) {
    this.style = this.document.createElement('style');
    this.style.innerHTML = DefaultReconnectDisplay.Css;

    this.overlay = this.document.createElement('div');
    this.overlay.className = DefaultReconnectDisplay.ReconnectOverlayClassName;
    this.overlay.id = dialogId;

    this.dialog = this.document.createElement('div');
    this.dialog.className = DefaultReconnectDisplay.ReconnectDialogClassName;

    const rippleGroup = document.createElement('div');
    rippleGroup.className = DefaultReconnectDisplay.RippleGroupClassName;

    for (let i = 0; i < DefaultReconnectDisplay.RippleCount; i++) {
      const ripple = document.createElement('div');
      rippleGroup.appendChild(ripple);
    }

    const rejoiningMessage = document.createElement('p');
    rejoiningMessage.innerHTML = 'Rejoining the server...';

    this.checkInternetElement = document.createElement('p');
    this.checkInternetElement.innerHTML = 'Check your internet connection';

    const reloadButton = document.createElement('button');
    reloadButton.setAttribute('onclick', 'location.reload()');
    reloadButton.style.display = 'none';
    reloadButton.innerHTML = 'Reload';

    this.dialog.appendChild(rippleGroup);
    this.dialog.appendChild(rejoiningMessage);
    this.dialog.appendChild(this.checkInternetElement);
    this.dialog.appendChild(reloadButton);

    this.overlay.appendChild(this.dialog);
  }

  show(): void {
    if (!this.document.contains(this.overlay)) {
      this.document.body.appendChild(this.overlay);
    }

    if (!this.document.contains(this.style)) {
      this.document.body.appendChild(this.style);
    }

    this.overlay.classList.add(DefaultReconnectDisplay.ReconnectVisibleClassName);
    this.checkInternetElement.style.display = 'none';
    clearTimeout(this.showCheckInternetTimeout);
    this.showCheckInternetTimeout = setTimeout(() => {
      this.checkInternetElement.style.display = 'block';
    }, DefaultReconnectDisplay.ShowCheckInternetDelayMilliseconds) as unknown as number;
  }

  update(currentAttempt: number): void {
    // TODO
  }

  hide(): void {
    this.overlay.classList.remove(DefaultReconnectDisplay.ReconnectVisibleClassName);
    clearTimeout(this.showCheckInternetTimeout);
  }

  failed(): void {
    location.reload();
  }

  rejected(): void {
    location.reload();
  }

  static readonly Css = `
    .${this.ReconnectOverlayClassName} {
      position: absolute;
      top: 0;
      bottom: 0;
      left: 0;
      right: 0;
      z-index: 10000;
      display: none;
      animation: components-reconnect-fade-in;
    }

    .${this.ReconnectOverlayClassName}.${this.ReconnectVisibleClassName} {
      display: block;
    }

    .${this.ReconnectOverlayClassName}::before {
      content: '';
      background-color: rgba(0, 0, 0, 0.4);
      position: absolute;
      top: 0;
      bottom: 0;
      left: 0;
      right: 0;
      animation: components-reconnect-fadeInOpacity 0.5s ease-in-out;
      opacity: 1;
    }

    .${this.ReconnectOverlayClassName} p {
      margin: 0;
    }

    .${this.ReconnectOverlayClassName} button {
      border: 0;
      background-color: #6b9ed2;
      color: white;
      padding: 4px 24px;
      border-radius: 4px;
    }

    .${this.ReconnectOverlayClassName} button:hover {
      background-color: #3b6ea2;
    }

    .${this.ReconnectOverlayClassName} button:active {
      background-color: #6b9ed2;
    }

    .${this.ReconnectDialogClassName} {
      position: relative;
      background-color: white;
      width: 20rem;
      margin: 20vh auto;
      padding: 2rem;
      border-radius: 0.5rem;
      box-shadow: 0 3px 6px 2px rgba(0, 0, 0, 0.3);
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 1rem;
      opacity: 0;
      animation: components-reconnect-slideUp 1.5s cubic-bezier(.05, .89, .25, 1.02) 0.3s, components-reconnect-fadeInOpacity 0.5s ease-out 0.3s;
      animation-fill-mode: forwards;
      z-index: 10001;
    }

    .${this.RippleGroupClassName} {
      display: block;
      position: relative;
      width: 80px;
      height: 80px;
    }

    .${this.RippleGroupClassName} div {
      position: absolute;
      border: 3px solid #0087ff;
      opacity: 1;
      border-radius: 50%;
      animation: ${this.RippleGroupClassName} 1.5s cubic-bezier(0, 0.2, 0.8, 1) infinite;
    }

    .${this.RippleGroupClassName} div:nth-child(2) {
      animation-delay: -0.5s;
    }

    @keyframes ${this.RippleGroupClassName} {
      0% {
        top: 40px;
        left: 40px;
        width: 0;
        height: 0;
        opacity: 0;
      }

      4.9% {
        top: 40px;
        left: 40px;
        width: 0;
        height: 0;
        opacity: 0;
      }

      5% {
        top: 40px;
        left: 40px;
        width: 0;
        height: 0;
        opacity: 1;
      }

      100% {
        top: 0px;
        left: 0px;
        width: 80px;
        height: 80px;
        opacity: 0;
      }
    }

    @keyframes components-reconnect-fadeInOpacity {
      0% {
        opacity: 0;
      }

      100% {
        opacity: 1;
      }
    }

    @keyframes components-reconnect-slideUp {
      0% {
        transform: translateY(30px) scale(0.95);
      }

      100% {
        transform: translateY(0);
      }
    }
  `;
}

export class DefaultReconnectDisplayOld implements ReconnectDisplay {
  modal: HTMLDivElement;

  message: HTMLHeadingElement;

  button: HTMLButtonElement;

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

    this.message = this.document.createElement('h5') as HTMLHeadingElement;
    this.message.style.cssText = 'margin-top: 20px';

    this.button = this.document.createElement('button') as HTMLButtonElement;
    this.button.style.cssText = 'margin:5px auto 5px';
    this.button.textContent = 'Retry';

    const link = this.document.createElement('a');
    link.addEventListener('click', () => location.reload());
    link.textContent = 'reload';

    this.reloadParagraph = this.document.createElement('p') as HTMLParagraphElement;
    this.reloadParagraph.textContent = 'Alternatively, ';
    this.reloadParagraph.appendChild(link);

    this.modal.appendChild(this.message);
    this.modal.appendChild(this.button);
    this.modal.appendChild(this.reloadParagraph);

    this.loader = this.getLoader();

    this.message.after(this.loader);

    this.button.addEventListener('click', async () => {
      this.show();

      try {
        // reconnect will asynchronously return:
        // - true to mean success
        // - false to mean we reached the server, but it rejected the connection (e.g., unknown circuit ID)
        // - exception to mean we didn't reach the server (this can be sync or async)
        const successful = await Blazor.reconnect!();
        if (!successful) {
          this.rejected();
        }
      } catch (err: unknown) {
        // We got an exception, server is currently unavailable
        this.logger.log(LogLevel.Error, err as Error);
        this.failed();
      }
    });
  }

  show(): void {
    if (!this.document.contains(this.modal)) {
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

    const errorDescription = this.document.createTextNode('Reconnection failed. Try ');

    const link = this.document.createElement('a');
    link.textContent = 'reloading';
    link.setAttribute('href', '');
    link.addEventListener('click', () => location.reload());

    const errorInstructions = this.document.createTextNode(' the page if you\'re unable to reconnect.');

    this.message.replaceChildren(errorDescription, link, errorInstructions);
  }

  rejected(): void {
    this.button.style.display = 'none';
    this.reloadParagraph.style.display = 'none';
    this.loader.style.display = 'none';

    const errorDescription = this.document.createTextNode('Could not reconnect to the server. ');

    const link = this.document.createElement('a');
    link.textContent = 'Reload';
    link.setAttribute('href', '');
    link.addEventListener('click', () => location.reload());

    const errorInstructions = this.document.createTextNode(' the page to restore functionality.');

    this.message.replaceChildren(errorDescription, link, errorInstructions);
  }

  private getLoader(): HTMLDivElement {
    const loader = this.document.createElement('div');

    const loaderStyles = [
      'border: 0.3em solid #f3f3f3',
      'border-top: 0.3em solid #3498db',
      'border-radius: 50%',
      'width: 2em',
      'height: 2em',
      'display: inline-block',
    ];

    loader.style.cssText = loaderStyles.join(';');
    loader.animate([{ transform: 'rotate(0deg)' }, { transform: 'rotate(360deg)' }], {
      duration: 2000,
      iterations: Infinity,
    });

    return loader;
  }
}
