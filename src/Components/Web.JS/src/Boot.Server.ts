import '@microsoft/dotnet-js-interop';
import './GlobalExports';
import * as signalR from '@microsoft/signalr';
import { MessagePackHubProtocol } from '@microsoft/signalr-protocol-msgpack';
import { showErrorNotification } from './BootErrors';
import { shouldAutoStart } from './BootCommon';
import { RenderQueue } from './Platform/Circuits/RenderQueue';
import { ConsoleLogger } from './Platform/Logging/Loggers';
import { LogLevel, Logger } from './Platform/Logging/Logger';
import { discoverComponents, CircuitDescriptor } from './Platform/Circuits/CircuitManager';
import { setEventDispatcher } from './Rendering/RendererEventDispatcher';
import { resolveOptions, BlazorOptions } from './Platform/Circuits/BlazorOptions';
import { DefaultReconnectionHandler } from './Platform/Circuits/DefaultReconnectionHandler';
import { attachRootComponentToLogicalElement } from './Rendering/Renderer';

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

  const components = discoverComponents(document);
  const circuit = new CircuitDescriptor(components);

  const initialConnection = await initializeConnection(options, logger, circuit);
  const circuitStarted = await circuit.startCircuit(initialConnection);
  if (!circuitStarted) {
    logger.log(LogLevel.Error, 'Failed to start the circuit.');
    return;
  }

  const reconnect = async (existingConnection?: signalR.HubConnection): Promise<boolean> => {
    if (renderingFailed) {
      // We can't reconnect after a failure, so exit early.
      return false;
    }

    const reconnection = existingConnection || await initializeConnection(options, logger, circuit);
    if (!(await circuit.reconnect(reconnection))) {
      logger.log(LogLevel.Information, 'Reconnection attempt to the circuit was rejected by the server. This may indicate that the associated state is no longer available on the server.');
      return false;
    }

    options.reconnectionHandler!.onConnectionUp();

    return true;
  };

  window.addEventListener(
    'unload',
    () => {
      const data = new FormData();
      const circuitId = circuit.circuitId!;
      data.append('circuitId', circuitId);
      navigator.sendBeacon('_blazor/disconnect', data);
    },
    false
  );

  window['Blazor'].reconnect = reconnect;

  logger.log(LogLevel.Information, 'Blazor server-side application started.');
}

async function initializeConnection(options: BlazorOptions, logger: Logger, circuit: CircuitDescriptor): Promise<signalR.HubConnection> {
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

  // Configure navigation via SignalR
  window['Blazor']._internal.navigationManager.listenForNavigationEvents((uri: string, intercepted: boolean): Promise<void> => {
    return connection.send('OnLocationChanged', uri, intercepted);
  });

  connection.on('JS.AttachComponent', (componentId, selector) => attachRootComponentToLogicalElement(0, circuit.resolveElement(selector), componentId));
  connection.on('JS.BeginInvokeJS', DotNet.jsCallDispatcher.beginInvokeJSFromDotNet);
  connection.on('JS.EndInvokeDotNet', (args: string) => DotNet.jsCallDispatcher.endInvokeDotNetFromJS(...(JSON.parse(args) as [string, boolean, unknown])));

  const renderQueue = RenderQueue.getOrCreate(logger);
  connection.on('JS.RenderBatch', (batchId: number, batchData: Uint8Array) => {
    logger.log(LogLevel.Debug, `Received render batch with id ${batchId} and ${batchData.byteLength} bytes.`);
    renderQueue.processBatch(batchId, batchData, connection);
  });

  connection.onclose(error => !renderingFailed && options.reconnectionHandler!.onConnectionDown(options.reconnectionOptions, error));
  connection.on('JS.Error', error => {
    renderingFailed = true;
    unhandledError(connection, error, logger);
    showErrorNotification();
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
