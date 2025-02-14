// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Blazor } from '../../GlobalExports';
import { LogLevel, Logger } from '../Logging/Logger';
import { ReconnectDisplay } from './ReconnectDisplay';

export class DefaultReconnectDisplay implements ReconnectDisplay {
  static readonly ReconnectOverlayClassName = 'components-reconnect-overlay';

  static readonly ReconnectDialogClassName = 'components-reconnect-dialog';

  static readonly ReconnectVisibleClassName = 'components-reconnect-visible';

  static readonly RejoiningAnimationClassName = 'components-rejoining-animation';

  static readonly AnimationRippleCount = 2;

  style: HTMLStyleElement;

  overlay: HTMLDivElement;

  host: HTMLDivElement;

  dialog: HTMLDivElement;

  rejoiningAnimation: HTMLDivElement;

  reloadButton: HTMLButtonElement;

  status: HTMLParagraphElement;

  retryWhenDocumentBecomesVisible: () => void;

  constructor(dialogId: string, private readonly document: Document, private readonly logger: Logger) {
    this.style = this.document.createElement('style');
    this.style.innerHTML = DefaultReconnectDisplay.Css;

    this.overlay = this.document.createElement('div');
    this.overlay.className = DefaultReconnectDisplay.ReconnectOverlayClassName;

    this.host = this.document.createElement('div');
    this.host.id = dialogId;

    const shadow = this.host.attachShadow({ mode: 'open' });
    this.dialog = document.createElement('div');
    this.dialog.className = DefaultReconnectDisplay.ReconnectDialogClassName;
    shadow.appendChild(this.style);
    shadow.appendChild(this.overlay);

    this.rejoiningAnimation = document.createElement('div');
    this.rejoiningAnimation.className = DefaultReconnectDisplay.RejoiningAnimationClassName;

    for (let i = 0; i < DefaultReconnectDisplay.AnimationRippleCount; i++) {
      const ripple = document.createElement('div');
      this.rejoiningAnimation.appendChild(ripple);
    }

    this.status = document.createElement('p');
    this.status.innerHTML = '';

    this.reloadButton = document.createElement('button');
    this.reloadButton.style.display = 'none';
    this.reloadButton.innerHTML = 'Retry';
    this.reloadButton.addEventListener('click', this.retry.bind(this));

    this.dialog.appendChild(this.rejoiningAnimation);
    this.dialog.appendChild(this.status);
    this.dialog.appendChild(this.reloadButton);

    this.overlay.appendChild(this.dialog);

    this.retryWhenDocumentBecomesVisible = () => {
      if (this.document.visibilityState === 'visible') {
        this.retry();
      }
    };
  }

  show(): void {
    if (!this.document.contains(this.host)) {
      this.document.body.appendChild(this.host);
    }

    this.reloadButton.style.display = 'none';
    this.rejoiningAnimation.style.display = 'block';
    this.status.innerHTML = 'Rejoining the server...';
    this.host.style.display = 'block';
    this.overlay.classList.add(DefaultReconnectDisplay.ReconnectVisibleClassName);
  }

  update(currentAttempt: number, secondsToNextAttempt: number): void {
    if (currentAttempt === 1 || secondsToNextAttempt === 0) {
      this.status.innerHTML = 'Rejoining the server...';
    } else {
      const unitText = secondsToNextAttempt === 1 ? 'second' : 'seconds';
      this.status.innerHTML = `Rejoin failed... trying again in ${secondsToNextAttempt} ${unitText}`;
    }
  }

  hide(): void {
    this.host.style.display = 'none';
    this.overlay.classList.remove(DefaultReconnectDisplay.ReconnectVisibleClassName);
  }

  failed(): void {
    this.reloadButton.style.display = 'block';
    this.rejoiningAnimation.style.display = 'none';
    this.status.innerHTML = 'Failed to rejoin.<br />Please retry or reload the page.';
    this.document.addEventListener('visibilitychange', this.retryWhenDocumentBecomesVisible);
  }

  rejected(): void {
    // We have been able to reach the server, but the circuit is no longer available.
    // We'll reload the page so the user can continue using the app as quickly as possible.
    location.reload();
  }

  private async retry() {
    this.document.removeEventListener('visibilitychange', this.retryWhenDocumentBecomesVisible);
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
  }

  static readonly Css = `
    .${this.ReconnectOverlayClassName} {
      position: fixed;
      top: 0;
      bottom: 0;
      left: 0;
      right: 0;
      z-index: 10000;
      display: none;
      overflow: hidden;
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
      text-align: center;
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

    .${this.RejoiningAnimationClassName} {
      display: block;
      position: relative;
      width: 80px;
      height: 80px;
    }

    .${this.RejoiningAnimationClassName} div {
      position: absolute;
      border: 3px solid #0087ff;
      opacity: 1;
      border-radius: 50%;
      animation: ${this.RejoiningAnimationClassName} 1.5s cubic-bezier(0, 0.2, 0.8, 1) infinite;
    }

    .${this.RejoiningAnimationClassName} div:nth-child(2) {
      animation-delay: -0.5s;
    }

    @keyframes ${this.RejoiningAnimationClassName} {
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
