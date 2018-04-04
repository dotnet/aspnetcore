const pollIntervalMs = 500;
const maxPollDurationMs = 10 * 1000;

export function enableLiveReloading(endpointUri: string) {
  new ReloadContext(endpointUri).start();
}

class ReloadContext {
  private _websocketUri: string;
  private _didConnect = false;
  private _pollUntilConnectedThenReload = false;
  private _stopPollingAtTime: Date | null = null;

  constructor(endpointUri: string) {
    this._websocketUri = toAbsoluteWebSocketUri(endpointUri);
  }

  start() {
    if (typeof WebSocket !== 'undefined') {
      this._attemptToConnect(/* delay */ 0);
    } else {
      console.log('Browser does not support WebSocket, so live reloading will be disabled.');
    }
  }

  private _attemptToConnect(delayMs: number) {
    setTimeout(() => {
      const source = new WebSocket(this._websocketUri);
      source.onopen = event => this._onOpen();
      source.onmessage = event => this._onMessage(event.data);
      source.onerror = event => this._onError();
      source.onclose = event => this._onClose();
    }, delayMs);
  }

  private _onOpen() {
    this._didConnect = true;

    if (this._pollUntilConnectedThenReload) {
      reloadNow();
    }
  }

  private _onMessage(data: string) {
    if (data === 'reload') {
      reloadNow();
    }
  }

  private _onClose() {
    if (this._didConnect) {
      // Looks like the server is being recycled (or possibly just shut down, but in the
      // absence of a graceful shutdown we have no way to tell the difference)
      // Wait until the server appears to be back, then reload
      this._pollUntilConnectedThenReload = true;
      this._stopPollingAtTime = new Date(new Date().valueOf() + maxPollDurationMs);
      this._attemptToConnect(/* delay */ pollIntervalMs);
    }
  }

  private _onError() {
    if (!this._didConnect) {
      if (this._pollUntilConnectedThenReload) {
        if (new Date() < this._stopPollingAtTime!) {
          // Continue polling
          this._attemptToConnect(/* delay */ pollIntervalMs);
        }
      } else {
        console.error(`The client app was compiled with live reloading enabled, but could not open `
          + ` a WebSocket connection to the server at ${this._websocketUri}\n`
          + `To fix this inconsistency, either run the server in development mode, or compile the `
          + `client app in Release configuration.`);
      }
    }
  }
}

function toAbsoluteWebSocketUri(uri: string) {
  const baseUri = document.baseURI;
  if (baseUri) {
    const lastSlashPos = baseUri.lastIndexOf('/');
    const prefix = baseUri.substr(0, lastSlashPos);
    uri = prefix + uri;
  }

  // Scheme must be ws: or wss:
  return uri.replace(/^http/, 'ws');
}

function reloadNow() {
  location.reload();
}