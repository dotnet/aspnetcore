export function enableLiveReloading(endpointUri: string) {
  listenForReloadEvent(endpointUri);
}

function listenForReloadEvent(endpointUri: string) {
  if (!WebSocket) {
    console.log('Browser does not support WebSocket, so live reloading will be disabled.');
    return;
  }

  // First, connect to the endpoint
  const websocketUri = toAbsoluteWebSocketUri(endpointUri);
  const source = new WebSocket(websocketUri);
  let allowConnectionFailedErrorReporting = true;

  source.onopen = e => {
    allowConnectionFailedErrorReporting = false;
  };

  source.onerror = e => {
    if (allowConnectionFailedErrorReporting) {
      allowConnectionFailedErrorReporting = false;
      console.error(`The client app was compiled with live reloading enabled, but could not open `
        + ` a WebSocket connection to the server at ${websocketUri}\n`
        + `To fix this inconsistency, either run the server in development mode, or compile the `
        + `client app in Release configuration.`);
    }
  };

  // If we're notified that we should reload, then do so
  source.onmessage = e => {
    if (e.data === 'reload') {
      location.reload();
    }
  };
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
