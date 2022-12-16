// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { DotNet } from '@microsoft/dotnet-js-interop';
import { Blazor } from './GlobalExports';
import { HubConnectionBuilder, HubConnection, HttpTransportType } from '@microsoft/signalr';
import { MessagePackHubProtocol } from '@microsoft/signalr-protocol-msgpack';
import { showErrorNotification } from './BootErrors';
import { shouldAutoStart } from './BootCommon';
import { RenderQueue } from './Platform/Circuits/RenderQueue';
import { ConsoleLogger } from './Platform/Logging/Loggers';
import { LogLevel, Logger } from './Platform/Logging/Logger';
import { CircuitDescriptor } from './Platform/Circuits/CircuitManager';
import { resolveOptions, CircuitStartOptions } from './Platform/Circuits/CircuitStartOptions';
import { DefaultReconnectionHandler } from './Platform/Circuits/DefaultReconnectionHandler';
import { attachRootComponentToLogicalElement } from './Rendering/Renderer';
import { discoverComponents, discoverPersistedState, ServerComponentDescriptor } from './Services/ComponentDescriptorDiscovery';
import { sendJSDataStream } from './Platform/Circuits/CircuitStreamingInterop';
import { fetchAndInvokeInitializers } from './JSInitializers/JSInitializers.Server';

let renderingFailed = false;
let started = false;
let connection: HubConnection;

async function boot(userOptions?: Partial<CircuitStartOptions>): Promise<void> {
  if (started) {
    throw new Error('Blazor has already started.');
  }
  started = true;

  // Establish options to be used
  const options = resolveOptions(userOptions);
  const jsInitializer = await fetchAndInvokeInitializers(options);

  const logger = new ConsoleLogger(options.logLevel);

  Blazor.reconnect = async (existingConnection?: HubConnection): Promise<boolean> => {
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
  Blazor.defaultReconnectionHandler = new DefaultReconnectionHandler(logger);

  options.reconnectionHandler = options.reconnectionHandler || Blazor.defaultReconnectionHandler;
  logger.log(LogLevel.Information, 'Starting up Blazor server-side application.');

  const components = discoverComponents(document, 'server') as ServerComponentDescriptor[];
  const appState = discoverPersistedState(document);
  const circuit = new CircuitDescriptor(components, appState || '');

  // Configure navigation via SignalR
  Blazor._internal.navigationManager.listenForNavigationEvents((uri: string, state: string | undefined, intercepted: boolean): Promise<void> => {
    return connection.send('OnLocationChanged', uri, state, intercepted);
  }, (callId: number, uri: string, state: string | undefined, intercepted: boolean): Promise<void> => {
    return connection.send('OnLocationChanging', callId, uri, state, intercepted);
  });

  Blazor._internal.forceCloseConnection = () => connection.stop();
  Blazor._internal.sendJSDataStream = (data: ArrayBufferView | Blob, streamId: number, chunkSize: number) => sendJSDataStream(connection, data, streamId, chunkSize);

  const initialConnection = await initializeConnection(options, logger, circuit);
  const circuitStarted = await circuit.startCircuit(initialConnection);
  if (!circuitStarted) {
    logger.log(LogLevel.Error, 'Failed to start the circuit.');
    return;
  }

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

  logger.log(LogLevel.Information, 'Blazor server-side application started.');

  jsInitializer.invokeAfterStartedCallbacks(Blazor);
}

async function initializeConnection(options: CircuitStartOptions, logger: Logger, circuit: CircuitDescriptor): Promise<HubConnection> {
  const hubProtocol = new MessagePackHubProtocol();
  (hubProtocol as unknown as { name: string }).name = 'blazorpack';

  const connectionBuilder = new HubConnectionBuilder()
    .withUrl('_blazor')
    .withHubProtocol(hubProtocol);

  options.configureSignalR(connectionBuilder);

  const newConnection = connectionBuilder.build();

  newConnection.on('JS.AttachComponent', (componentId, selector) => attachRootComponentToLogicalElement(0, circuit.resolveElement(selector), componentId, false));
  newConnection.on('JS.BeginInvokeJS', DotNet.jsCallDispatcher.beginInvokeJSFromDotNet);
  newConnection.on('JS.EndInvokeDotNet', DotNet.jsCallDispatcher.endInvokeDotNetFromJS);
  newConnection.on('JS.ReceiveByteArray', DotNet.jsCallDispatcher.receiveByteArray);

  newConnection.on('JS.BeginTransmitStream', (streamId: number) => {
    const readableStream = new ReadableStream({
      start(controller) {
        newConnection.stream('SendDotNetStreamToJS', streamId).subscribe({
          next: (chunk: Uint8Array) => controller.enqueue(chunk),
          complete: () => controller.close(),
          error: (err) => controller.error(err),
        });
      },
    });

    DotNet.jsCallDispatcher.supplyDotNetStream(streamId, readableStream);
  });

  const renderQueue = RenderQueue.getOrCreate(logger);
  newConnection.on('JS.RenderBatch', (batchId: number, batchData: Uint8Array) => {
    logger.log(LogLevel.Debug, `Received render batch with id ${batchId} and ${batchData.byteLength} bytes.`);
    renderQueue.processBatch(batchId, batchData, newConnection);
  });

  newConnection.on('JS.EndLocationChanging', Blazor._internal.navigationManager.endLocationChanging);

  newConnection.onclose(error => !renderingFailed && options.reconnectionHandler!.onConnectionDown(options.reconnectionOptions, error));
  newConnection.on('JS.Error', error => {
    renderingFailed = true;
    unhandledError(newConnection, error, logger);
    showErrorNotification();
  });

  try {
    await newConnection.start();
    connection = newConnection;
  } catch (ex: any) {
    unhandledError(newConnection, ex as Error, logger);

    if (ex.errorType === 'FailedToNegotiateWithServerError') {
      // Connection with the server has been interrupted, and we're in the process of reconnecting.
      // Throw this exception so it can be handled at the reconnection layer, and don't show the
      // error notification.
      throw ex;
    } else {
      showErrorNotification();
    }

    if (ex.innerErrors) {
      if (ex.innerErrors.some(e => e.errorType === 'UnsupportedTransportError' && e.transport === HttpTransportType.WebSockets)) {
        logger.log(LogLevel.Error, 'Unable to connect, please ensure you are using an updated browser that supports WebSockets.');
      } else if (ex.innerErrors.some(e => e.errorType === 'FailedToStartTransportError' && e.transport === HttpTransportType.WebSockets)) {
        logger.log(LogLevel.Error, 'Unable to connect, please ensure WebSockets are available. A VPN or proxy may be blocking the connection.');
      } else if (ex.innerErrors.some(e => e.errorType === 'DisabledTransportError' && e.transport === HttpTransportType.LongPolling)) {
        logger.log(LogLevel.Error, 'Unable to initiate a SignalR connection to the server. This might be because the server is not configured to support WebSockets. For additional details, visit https://aka.ms/blazor-server-websockets-error.');
      }
    }
  }

  // Check if the connection is established using the long polling transport,
  // using the `features.inherentKeepAlive` property only present with long polling.
  if ((newConnection as any).connection?.features?.inherentKeepAlive) {
    logger.log(LogLevel.Warning, 'Failed to connect via WebSockets, using the Long Polling fallback transport. This may be due to a VPN or proxy blocking the connection. To troubleshoot this, visit https://aka.ms/blazor-server-using-fallback-long-polling.');
  }

  DotNet.attachDispatcher({
    beginInvokeDotNetFromJS: (callId, assemblyName, methodIdentifier, dotNetObjectId, argsJson): void => {
      newConnection.send('BeginInvokeDotNetFromJS', callId ? callId.toString() : null, assemblyName, methodIdentifier, dotNetObjectId || 0, argsJson);
    },
    endInvokeJSFromDotNet: (asyncHandle, succeeded, argsJson): void => {
      newConnection.send('EndInvokeJSFromDotNet', asyncHandle, succeeded, argsJson);
    },
    sendByteArray: (id: number, data: Uint8Array): void => {
      newConnection.send('ReceiveByteArray', id, data);
    },
  });

  return newConnection;
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
