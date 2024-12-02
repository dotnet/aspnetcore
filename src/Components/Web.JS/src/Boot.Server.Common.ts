// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Blazor } from './GlobalExports';
import { ConsoleLogger } from './Platform/Logging/Loggers';
import { LogLevel } from './Platform/Logging/Logger';
import { CircuitManager } from './Platform/Circuits/CircuitManager';
import { resolveOptions, CircuitStartOptions } from './Platform/Circuits/CircuitStartOptions';
import { DefaultReconnectionHandler } from './Platform/Circuits/DefaultReconnectionHandler';
import { discoverServerPersistedState, ServerComponentDescriptor } from './Services/ComponentDescriptorDiscovery';
import { fetchAndInvokeInitializers } from './JSInitializers/JSInitializers.Server';
import { RootComponentManager } from './Services/RootComponentManager';
import { WebRendererId } from './Rendering/WebRendererId';

let initializersPromise: Promise<void> | undefined;
let appState: string;
let circuit: CircuitManager;
let options: CircuitStartOptions;
let logger: ConsoleLogger;
let serverStartPromise: Promise<void>;
let circuitStarting: Promise<boolean> | undefined;

export function setCircuitOptions(initializersReady: Promise<Partial<CircuitStartOptions>>) {
  if (options) {
    throw new Error('Circuit options have already been configured.');
  }

  initializersPromise = setOptions(initializersReady);

  async function setOptions(initializers: Promise<Partial<CircuitStartOptions>>): Promise<void> {
    const configuredOptions = await initializers;
    options = resolveOptions(configuredOptions);
  }
}

export function startServer(components: RootComponentManager<ServerComponentDescriptor>): Promise<void> {
  if (serverStartPromise !== undefined) {
    throw new Error('Blazor Server has already started.');
  }

  serverStartPromise = new Promise(startServerCore.bind(null, components));

  return serverStartPromise;
}

async function startServerCore(components: RootComponentManager<ServerComponentDescriptor>, resolve: () => void, _: any) {
  await initializersPromise;
  const jsInitializer = await fetchAndInvokeInitializers(options);

  appState = discoverServerPersistedState(document) || '';
  logger = new ConsoleLogger(options.logLevel);
  circuit = new CircuitManager(components, appState, options, logger);

  logger.log(LogLevel.Information, 'Starting up Blazor server-side application.');

  Blazor.reconnect = async () => {
    if (circuit.didRenderingFail()) {
      // We can't reconnect after a failure, so exit early.
      return false;
    }

    if (!(await circuit.reconnect())) {
      logger.log(LogLevel.Information, 'Reconnection attempt to the circuit was rejected by the server. This may indicate that the associated state is no longer available on the server.');
      return false;
    }

    return true;
  };

  Blazor.defaultReconnectionHandler = new DefaultReconnectionHandler(logger);
  options.reconnectionHandler = options.reconnectionHandler || Blazor.defaultReconnectionHandler;

  // Configure navigation via SignalR
  Blazor._internal.navigationManager.listenForNavigationEvents(WebRendererId.Server, (uri: string, state: string | undefined, intercepted: boolean): Promise<void> => {
    return circuit.sendLocationChanged(uri, state, intercepted);
  }, (callId: number, uri: string, state: string | undefined, intercepted: boolean): Promise<void> => {
    return circuit.sendLocationChanging(callId, uri, state, intercepted);
  });

  Blazor._internal.forceCloseConnection = () => circuit.disconnect();
  Blazor._internal.sendJSDataStream = (data: ArrayBufferView | Blob, streamId: number, chunkSize: number) => circuit.sendJsDataStream(data, streamId, chunkSize);

  const circuitStarted = await circuit.start();
  if (!circuitStarted) {
    logger.log(LogLevel.Error, 'Failed to start the circuit.');
    resolve();
    return;
  }

  const cleanup = () => {
    circuit.sendDisconnectBeacon();
  };

  Blazor.disconnect = cleanup;

  window.addEventListener('unload', cleanup, { capture: false, once: true });

  logger.log(LogLevel.Information, 'Blazor server-side application started.');

  jsInitializer.invokeAfterStartedCallbacks(Blazor);
  resolve();
}

export async function startCircuit(): Promise<boolean> {
  if (!serverStartPromise) {
    throw new Error('Cannot start the circuit until Blazor Server has started.');
  }

  if (circuit && !circuit.isDisposedOrDisposing()) {
    return true;
  }

  // We might be starting a circuit already, if that's the case, we can just wait for that
  // to finish.
  if (circuitStarting) {
    return await circuitStarting;
  }

  await serverStartPromise;

  if (circuit && circuit.didRenderingFail()) {
    // We can't start a new circuit after a rendering failure because the renderer
    // might be in an invalid state.
    return false;
  }

  if (circuit && circuit.isDisposedOrDisposing()) {
    // If the current circuit is no longer available, create a new one.
    appState = discoverServerPersistedState(document) || '';
    circuit = new CircuitManager(circuit.getRootComponentManager(), appState, options, logger);
  }

  // Start the circuit. If the circuit has already started, this will return the existing
  // circuit start promise.
  circuitStarting = circuit.start();
  // Once we are done, we need to clear the circuitStartingPromise to ensure that if the circuit gets
  // disposed later on, we are able to start a new one.
  clearCircuitStarting(circuitStarting);
  return circuitStarting;
}

async function clearCircuitStarting(circuitStartingPromise: Promise<boolean>) {
  await circuitStartingPromise;
  if (circuitStarting === circuitStartingPromise) {
    circuitStarting = undefined;
  }
}

export function hasStartedServer(): boolean {
  return serverStartPromise !== undefined;
}

export async function disposeCircuit() {
  await circuit?.dispose();
}

export function isCircuitAvailable(): boolean {
  return circuit && !circuit.isDisposedOrDisposing();
}

export function updateServerRootComponents(operations: string): Promise<void> | undefined {
  if (circuit && !circuit.isDisposedOrDisposing()) {
    return circuit.updateRootComponents(operations);
  } else {
    scheduleWhenReady(operations);
  }
}
async function scheduleWhenReady(operations: string) {
  await serverStartPromise;
  if (await startCircuit()) {
    return circuit.updateRootComponents(operations);
  }
}
