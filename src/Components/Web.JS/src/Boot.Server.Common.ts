// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { DotNet } from '@microsoft/dotnet-js-interop';
import { Blazor } from './GlobalExports';
import { HubConnectionBuilder, HubConnection, HttpTransportType } from '@microsoft/signalr';
import { MessagePackHubProtocol } from '@microsoft/signalr-protocol-msgpack';
import { showErrorNotification } from './BootErrors';
import { RenderQueue } from './Platform/Circuits/RenderQueue';
import { ConsoleLogger } from './Platform/Logging/Loggers';
import { LogLevel, Logger } from './Platform/Logging/Logger';
import { CircuitDescriptor } from './Platform/Circuits/CircuitManager';
import { resolveOptions, CircuitStartOptions } from './Platform/Circuits/CircuitStartOptions';
import { DefaultReconnectionHandler } from './Platform/Circuits/DefaultReconnectionHandler';
import { attachRootComponentToLogicalElement } from './Rendering/Renderer';
import { discoverPersistedState, ServerComponentDescriptor } from './Services/ComponentDescriptorDiscovery';
import { sendJSDataStream } from './Platform/Circuits/CircuitStreamingInterop';
import { fetchAndInvokeInitializers } from './JSInitializers/JSInitializers.Server';
import { WebRendererId } from './Rendering/WebRendererId';
import { RootComponentManager } from './Services/RootComponentManager';
import { detachWebRendererInterop } from './Rendering/WebRendererInteropMethods';
import { CircuitDotNetCallDispatcher } from './Platform/Circuits/CircuitDotNetCallDispatcher';

let renderingFailed = false;
let started = false;
let startCircuitPromise: Promise<boolean> | undefined;
let connection: HubConnection;
let circuit: CircuitDescriptor;
let dotNetDispatcher: CircuitDotNetCallDispatcher;
let dispatcher: DotNet.ICallDispatcher;
let renderQueue: RenderQueue;
let options: CircuitStartOptions;
let logger: ConsoleLogger;
let afterRenderCallback: (() => void) | undefined;

export function setCircuitOptions(circuitUserOptions?: Partial<CircuitStartOptions>) {
  if (options) {
    throw new Error('Circuit options have already been configured.');
  }

  options = resolveOptions(circuitUserOptions);
}

export async function startServer(components: RootComponentManager<ServerComponentDescriptor>): Promise<void> {
  if (started) {
    throw new Error('Blazor Server has already started.');
  }

  started = true;

  // Establish options to be used
  logger = new ConsoleLogger(options.logLevel);

  const jsInitializer = await fetchAndInvokeInitializers(options);

  Blazor.reconnect = async (existingConnection?: HubConnection): Promise<boolean> => {
    if (renderingFailed) {
      // We can't reconnect after a failure, so exit early.
      return false;
    }

    const reconnection = existingConnection || await initializeConnection(logger, circuit);
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

  // Configure navigation via SignalR
  Blazor._internal.navigationManager.listenForNavigationEvents((uri: string, state: string | undefined, intercepted: boolean): Promise<void> => {
    return connection.send('OnLocationChanged', uri, state, intercepted);
  }, (callId: number, uri: string, state: string | undefined, intercepted: boolean): Promise<void> => {
    return connection.send('OnLocationChanging', callId, uri, state, intercepted);
  });

  Blazor._internal.forceCloseConnection = () => connection.stop();
  Blazor._internal.sendJSDataStream = (data: ArrayBufferView | Blob, streamId: number, chunkSize: number) => sendJSDataStream(connection, data, streamId, chunkSize);

  const didCircuitStart = await startCircuit(components);
  if (!didCircuitStart) {
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

export function startCircuit(components: RootComponentManager<ServerComponentDescriptor>): Promise<boolean> {
  if (!started) {
    throw new Error('Cannot start the circuit until Blazor Server has started.');
  }

  startCircuitPromise ??= (async () => {
    const appState = discoverPersistedState(document);
    renderQueue = new RenderQueue(logger);
    circuit = new CircuitDescriptor(components, appState || '');
    dotNetDispatcher = new CircuitDotNetCallDispatcher(() => connection);
    dispatcher = DotNet.attachDispatcher(dotNetDispatcher);

    const initialConnection = await initializeConnection(logger, circuit);
    const circuitStarted = await circuit.startCircuit(initialConnection);
    if (!circuitStarted) {
      logger.log(LogLevel.Error, 'Failed to start the circuit.');
      return false;
    }
    return true;
  })();

  return startCircuitPromise;
}

export function hasStartedServer(): boolean {
  return started;
}

export function isCircuitActive(): boolean {
  return startCircuitPromise !== undefined;
}

export function attachCircuitAfterRenderCallback(callback: typeof afterRenderCallback) {
  if (afterRenderCallback) {
    throw new Error('A Blazor Server after render batch callback was already attached.');
  }

  afterRenderCallback = callback;
}

export function disposeCircuit() {
  if (startCircuitPromise === undefined) {
    return;
  }

  startCircuitPromise = undefined;

  // We dispose the .NET dispatcher to prevent it from being used in the future.
  // This avoids cases where, for example, .NET object references from a
  // disconnected circuit start pointing to .NET objects for a new circuit.
  dotNetDispatcher.dispose();

  connection.stop();

  detachWebRendererInterop(WebRendererId.Server);
}

async function initializeConnection(logger: Logger, circuit: CircuitDescriptor): Promise<HubConnection> {
  const hubProtocol = new MessagePackHubProtocol();
  (hubProtocol as unknown as { name: string }).name = 'blazorpack';

  const connectionBuilder = new HubConnectionBuilder()
    .withUrl('_blazor')
    .withHubProtocol(hubProtocol);

  options.configureSignalR(connectionBuilder);

  const newConnection = connectionBuilder.build();

  newConnection.on('JS.AttachComponent', (componentId, selector) => attachRootComponentToLogicalElement(WebRendererId.Server, circuit.resolveElement(selector, componentId), componentId, false));
  newConnection.on('JS.BeginInvokeJS', dispatcher.beginInvokeJSFromDotNet.bind(dispatcher));
  newConnection.on('JS.EndInvokeDotNet', dispatcher.endInvokeDotNetFromJS.bind(dispatcher));
  newConnection.on('JS.ReceiveByteArray', dispatcher.receiveByteArray.bind(dispatcher));

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

    dispatcher.supplyDotNetStream(streamId, readableStream);
  });

  newConnection.on('JS.RenderBatch', async (batchId: number, batchData: Uint8Array) => {
    logger.log(LogLevel.Debug, `Received render batch with id ${batchId} and ${batchData.byteLength} bytes.`);
    await renderQueue.processBatch(batchId, batchData, newConnection);
    afterRenderCallback?.();
  });

  newConnection.on('JS.EndLocationChanging', Blazor._internal.navigationManager.endLocationChanging);

  newConnection.onclose(error => isCircuitActive() && !renderingFailed && options.reconnectionHandler!.onConnectionDown(options.reconnectionOptions, error));
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
