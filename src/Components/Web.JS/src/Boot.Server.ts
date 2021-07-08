import { DotNet } from '@microsoft/dotnet-js-interop';
import { Blazor } from './GlobalExports';
import { HubConnectionBuilder, HubConnection } from '@microsoft/signalr';
import { MessagePackHubProtocol } from '@microsoft/signalr-protocol-msgpack';
import { showErrorNotification } from './BootErrors';
import { shouldAutoStart } from './BootCommon';
import { RenderQueue } from './Platform/Circuits/RenderQueue';
import { ConsoleLogger } from './Platform/Logging/Loggers';
import { LogLevel, Logger } from './Platform/Logging/Logger';
import { CircuitDescriptor } from './Platform/Circuits/CircuitManager';
import { setEventDispatcher } from './Rendering/Events/EventDispatcher';
import { resolveOptions, CircuitStartOptions } from './Platform/Circuits/CircuitStartOptions';
import { DefaultReconnectionHandler } from './Platform/Circuits/DefaultReconnectionHandler';
import { attachRootComponentToLogicalElement } from './Rendering/Renderer';
import { discoverComponents, discoverPersistedState, ServerComponentDescriptor } from './Services/ComponentDescriptorDiscovery';
import { InputFile } from './InputFile';
import { sendJSDataStream } from './Platform/Circuits/CircuitStreamingInterop';

let renderingFailed = false;
let started = false;

async function boot(userOptions?: Partial<CircuitStartOptions>): Promise<void> {
  if (started) {
    throw new Error('Blazor has already started.');
  }
  started = true;

  // Establish options to be used
  const options = resolveOptions(userOptions);
  const logger = new ConsoleLogger(options.logLevel);
  Blazor.defaultReconnectionHandler = new DefaultReconnectionHandler(logger);
  Blazor._internal.InputFile = InputFile;

  options.reconnectionHandler = options.reconnectionHandler || Blazor.defaultReconnectionHandler;
  logger.log(LogLevel.Information, 'Starting up Blazor server-side application.');

  const components = discoverComponents(document, 'server') as ServerComponentDescriptor[];
  const appState = discoverPersistedState(document);
  const circuit = new CircuitDescriptor(components, appState || '');


  const initialConnection = await initializeConnection(options, logger, circuit);
  const circuitStarted = await circuit.startCircuit(initialConnection);
  if (!circuitStarted) {
    logger.log(LogLevel.Error, 'Failed to start the circuit.');
    return;
  }

  const reconnect = async (existingConnection?: HubConnection): Promise<boolean> => {
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

  let disconnectSent = false;
  const cleanup = () => {
    if (!disconnectSent) {
      const data = new FormData();
      const circuitId = circuit.circuitId!;
      data.append('circuitId', circuitId);
      disconnectSent = navigator.sendBeacon('_blazor/disconnect', data);
    }
  };

  Blazor.disconnect = cleanup;

  window.addEventListener('unload', cleanup, { capture: false, once: true });

  Blazor.reconnect = reconnect;

  logger.log(LogLevel.Information, 'Blazor server-side application started.');
}

async function initializeConnection(options: CircuitStartOptions, logger: Logger, circuit: CircuitDescriptor): Promise<HubConnection> {
  const hubProtocol = new MessagePackHubProtocol();
  (hubProtocol as unknown as { name: string }).name = 'blazorpack';

  const connectionBuilder = new HubConnectionBuilder()
    .withUrl('_blazor')
    .withHubProtocol(hubProtocol);

  options.configureSignalR(connectionBuilder);

  const connection = connectionBuilder.build();
  const textEncoder = new TextEncoder();

  setEventDispatcher((descriptor, args) => {
    connection.send('DispatchBrowserEvent',
      textEncoder.encode(JSON.stringify([descriptor, args]))
    );
  });

  // Configure navigation via SignalR
  Blazor._internal.navigationManager.listenForNavigationEvents((uri: string, intercepted: boolean): Promise<void> => {
    return connection.send('OnLocationChanged', uri, intercepted);
  });

  connection.on('JS.AttachComponent', (componentId, selector) => attachRootComponentToLogicalElement(0, circuit.resolveElement(selector), componentId, false));
  connection.on('JS.BeginInvokeJS', DotNet.jsCallDispatcher.beginInvokeJSFromDotNet);
  connection.on('JS.EndInvokeDotNet', DotNet.jsCallDispatcher.endInvokeDotNetFromJS);
  connection.on('JS.ReceiveByteArray', DotNet.jsCallDispatcher.receiveByteArray);

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

  Blazor._internal.forceCloseConnection = () => connection.stop();

  Blazor._internal.sendJSDataStream = (data: ArrayBufferView, streamId: string, chunkSize: number) => sendJSDataStream(connection, data, streamId, chunkSize);

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
    sendByteArray: (id: number, data: Uint8Array): void => {
      connection.send('ReceiveByteArray', id, data);
    },
  });

  return connection;
}

function unhandledError(connection: HubConnection, err: Error, logger: Logger): void {
  logger.log(LogLevel.Error, err);

  // Disconnect on errors.
  //
  // Trying to call methods on the connection after its been closed will throw.
  if (connection) {
    connection.stop();
  }
}

Blazor.start = boot;

if (shouldAutoStart()) {
  boot();
}
