import { receiveEvent } from './BenchmarkEvents.js';

export class BlazorApp {
  constructor() {
    this._frame = document.createElement('iframe');
    document.body.appendChild(this._frame);
  }

  get window() {
    return this._frame.contentWindow;
  }

  async start() {
    this._frame.src = 'blazor-frame.html';
    await receiveEvent('Rendered index.cshtml');
  }

  navigateTo(url) {
    this.window.Blazor.navigateTo(url);
  }

  dispose() {
    document.body.removeChild(this._frame);
  }
}
