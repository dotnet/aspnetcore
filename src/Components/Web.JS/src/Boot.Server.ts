import '@dotnet/jsinterop';
import './GlobalExports';
import * as signalR from '@aspnet/signalr';
import { MessagePackHubProtocol } from '@aspnet/signalr-protocol-msgpack';
import { shouldAutoStart } from './BootCommon';
import { RenderQueue } from './Platform/Circuits/RenderQueue';
import { ConsoleLogger } from './Platform/Logging/Loggers';
import { LogLevel, Logger } from './Platform/Logging/Logger';
import { discoverPrerenderedCircuits, startCircuit } from './Platform/Circuits/CircuitManager';
import { setEventDispatcher } from './Rendering/RendererEventDispatcher';
import { resolveOptions, BlazorOptions } from './Platform/Circuits/BlazorOptions';
import { DefaultReconnectionHandler } from './Platform/Circuits/DefaultReconnectionHandler';

let renderingFailed = false;
let started = false;

async function boot(userOptions?: Partial<BlazorOptions>): Promise<void> {
  if (started) {
    throw new Error('Blazor has already started.');
  }
  started = true;

  // Establish options to be used
  const options = resolveOptions(userOptions);
  const logger = new ConsoleLogger(options.logLevel);
  window['Blazor'].defaultReconnectionHandler = new DefaultReconnectionHandler(logger);
  options.reconnectionHandler = options.reconnectionHandler || window['Blazor'].defaultReconnectionHandler;
  logger.log(LogLevel.Information, 'Starting up blazor server-side application.');

  // Initialize statefully prerendered circuits and their components
  // Note: This will all be removed soon
  const initialConnection = await initializeConnection(options, logger);
  const circuits = discoverPrerenderedCircuits(document);
  for (let i = 0; i < circuits.length; i++) {
    const circuit = circuits[i];
    for (let j = 0; j < circuit.components.length; j++) {
      const component = circuit.components[j];
      component.initialize();
    }
  }

  const circuit = await startCircuit(initialConnection);
  if (!circuit) {
    logger.log(LogLevel.Information, 'No preregistered components to render.');
  }

  const reconnect = async (existingConnection?: signalR.HubConnection): Promise<boolean> => {
    if (renderingFailed) {
      // We can't reconnect after a failure, so exit early.
      return false;
    }
    const reconnection = existingConnection || await initializeConnection(options, logger);
    const results = await Promise.all(circuits.map(circuit => circuit.reconnect(reconnection)));

    if (reconnectionFailed(results)) {
      return false;
    }

    options.reconnectionHandler!.onConnectionUp();

    return true;
  };

  window['Blazor'].reconnect = reconnect;

  const reconnectTask = reconnect(initialConnection);

  if (circuit) {
    circuits.push(circuit);
  }

  await reconnectTask;

  logger.log(LogLevel.Information, 'Blazor server-side application started.');

  function reconnectionFailed(results: boolean[]): boolean {
    return !results.reduce((current, next) => current && next, true);
  }
}

async function initializeConnection(options: BlazorOptions, logger: Logger): Promise<signalR.HubConnection> {
  const hubProtocol = new MessagePackHubProtocol();
  (hubProtocol as unknown as { name: string }).name = 'blazorpack';

  const connectionBuilder = new signalR.HubConnectionBuilder()
    .withUrl('_blazor')
    .withHubProtocol(hubProtocol);

  options.configureSignalR(connectionBuilder);

  const connection = connectionBuilder.build();

  setEventDispatcher((descriptor, args) => {
    return connection.send('DispatchBrowserEvent', JSON.stringify(descriptor), JSON.stringify(args));
  });

  connection.on('JS.BeginInvokeJS', DotNet.jsCallDispatcher.beginInvokeJSFromDotNet);
  connection.on('JS.EndInvokeDotNet', (args: string) => DotNet.jsCallDispatcher.endInvokeDotNetFromJS(...(JSON.parse(args) as [string, boolean, unknown])));
  connection.on('JS.RenderBatch', (browserRendererId: number, batchId: number, batchData: Uint8Array) => {
    logger.log(LogLevel.Debug, `Received render batch for ${browserRendererId} with id ${batchId} and ${batchData.byteLength} bytes.`);

    const queue = RenderQueue.getOrCreateQueue(browserRendererId, logger);

    queue.processBatch(batchId, batchData, connection);
  });

  connection.onclose(error => !renderingFailed && options.reconnectionHandler!.onConnectionDown(options.reconnectionOptions, error));
  connection.on('JS.Error', error => {
    renderingFailed = true;
    unhandledError(connection, error, logger);
  });

  window['Blazor']._internal.forceCloseConnection = () => connection.stop();

  try {
    await connection.start();
  } catch (ex) {
    unhandledError(connection, ex, logger);
  }

  DotNet.attachDispatcher({
    beginInvokeDotNetFromJS: (callId, assemblyName, methodIdentifier, dotNetObjectId, argsJson): void => {
      connection.send('BeginInvokeDotNetFromJS', callId ? callId.toString() : null, assemblyName, methodIdentifier, dotNetObjectId || 0, argsJson);
    },
    endInvokeJSFromDotNet: (asyncHandle, succeeded, argsJson): void => {
      connection.send('EndInvokeJSFromDotNet', asyncHandle, succeeded, argsJson);
    },
  });

  return connection;
}

function unhandledError(connection: signalR.HubConnection, err: Error, logger: Logger): void {
  logger.log(LogLevel.Error, err);

  // Disconnect on errors.
  //
  // Trying to call methods on the connection after its been closed will throw.
  if (connection) {
    connection.stop();
  }
}

window['Blazor'].start = boot;

if (shouldAutoStart()) {
  boot();
}
