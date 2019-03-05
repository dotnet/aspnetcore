import '@dotnet/jsinterop';
import './GlobalExports';
import * as signalR from '@aspnet/signalr';
import { MessagePackHubProtocol } from '@aspnet/signalr-protocol-msgpack';
import { OutOfProcessRenderBatch } from './Rendering/RenderBatch/OutOfProcessRenderBatch';
import { internalFunctions as uriHelperFunctions } from './Services/UriHelper';
import { renderBatch } from './Rendering/Renderer';
import { fetchBootConfigAsync, loadEmbeddedResourcesAsync } from './BootCommon';
import { CircuitHandler } from './Platform/Circuits/CircuitHandler';
import { AutoReconnectCircuitHandler } from './Platform/Circuits/AutoReconnectCircuitHandler';

async function boot() {
  const circuitHandlers: CircuitHandler[] = [ new AutoReconnectCircuitHandler() ];
  window['Blazor'].circuitHandlers = circuitHandlers;

  // In the background, start loading the boot config and any embedded resources
  const embeddedResourcesPromise = fetchBootConfigAsync().then(bootConfig => {
    return loadEmbeddedResourcesAsync(bootConfig);
  });

  const initialConnection = await initializeConnection(circuitHandlers);

  // Ensure any embedded resources have been loaded before starting the app
  await embeddedResourcesPromise;
  const circuitId = await initialConnection.invoke<string>(
    'StartCircuit',
    uriHelperFunctions.getLocationHref(),
    uriHelperFunctions.getBaseURI()
  );

  window['Blazor'].reconnect = async () => {
    const reconnection = await initializeConnection(circuitHandlers);
    if (!(await reconnection.invoke<Boolean>('ConnectCircuit', circuitId))) {
      return false;
    }

    circuitHandlers.forEach(h => h.onConnectionUp && h.onConnectionUp());
    return true;
  };

  circuitHandlers.forEach(h => h.onConnectionUp && h.onConnectionUp());
}

async function initializeConnection(circuitHandlers: CircuitHandler[]): Promise<signalR.HubConnection> {
  const connection = new signalR.HubConnectionBuilder()
    .withUrl('_blazor')
    .withHubProtocol(new MessagePackHubProtocol())
    .configureLogging(signalR.LogLevel.Information)
    .build();

  connection.on('JS.BeginInvokeJS', DotNet.jsCallDispatcher.beginInvokeJSFromDotNet);
  connection.on('JS.RenderBatch', (browserRendererId: number, renderId: number, batchData: Uint8Array) => {
    try {
      renderBatch(browserRendererId, new OutOfProcessRenderBatch(batchData));
      connection.send('OnRenderCompleted', renderId, null);
    } catch (ex) {
      // If there's a rendering exception, notify server *and* throw on client
      connection.send('OnRenderCompleted', renderId, ex.toString());
      throw ex;
    }
  });

  connection.onclose(error => circuitHandlers.forEach(h => h.onConnectionDown && h.onConnectionDown(error)));
  connection.on('JS.Error', error => unhandledError(connection, error));

  window['Blazor']._internal.forceCloseConnection = () => connection.stop();

  try {
    await connection.start();
  } catch (ex) {
    unhandledError(connection, ex);
  }

  DotNet.attachDispatcher({
    beginInvokeDotNetFromJS: (callId, assemblyName, methodIdentifier, dotNetObjectId, argsJson) => {
      connection.send('BeginInvokeDotNetFromJS', callId ? callId.toString() : null, assemblyName, methodIdentifier, dotNetObjectId || 0, argsJson);
    }
  });

  return connection;
}

function unhandledError(connection: signalR.HubConnection, err: Error) {
  console.error(err);

  // Disconnect on errors.
  //
  // Trying to call methods on the connection after its been closed will throw.
  if (connection) {
    connection.stop();
  }
}

boot();
