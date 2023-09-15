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

let started = false;
let appState: string;
let circuit: CircuitManager;
let options: CircuitStartOptions;
let logger: ConsoleLogger;

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
  Blazor._internal.navigationManager.listenForNavigationEvents((uri: string, state: string | undefined, intercepted: boolean): Promise<void> => {
    return circuit.sendLocationChanged(uri, state, intercepted);
  }, (callId: number, uri: string, state: string | undefined, intercepted: boolean): Promise<void> => {
    return circuit.sendLocationChanging(callId, uri, state, intercepted);
  });

  Blazor._internal.forceCloseConnection = () => circuit.disconnect();
  Blazor._internal.sendJSDataStream = (data: ArrayBufferView | Blob, streamId: number, chunkSize: number) => circuit.sendJsDataStream(data, streamId, chunkSize);

  const jsInitializer = await fetchAndInvokeInitializers(options);

  const circuitStarted = await circuit.start();
  if (!circuitStarted) {
    logger.log(LogLevel.Error, 'Failed to start the circuit.');
    return;
  }

  const cleanup = () => {
    circuit.sendDisconnectBeacon();
  };

  Blazor.disconnect = cleanup;

  window.addEventListener('unload', cleanup, { capture: false, once: true });

  logger.log(LogLevel.Information, 'Blazor server-side application started.');

  jsInitializer.invokeAfterStartedCallbacks(Blazor);
}

export function startCircuit(): Promise<boolean> {
  if (!started) {
    throw new Error('Cannot start the circuit until Blazor Server has started.');
  }

  if (circuit.didRenderingFail()) {
    // We can't start a new circuit after a rendering failure because the renderer
    // might be in an invalid state.
    return Promise.resolve(false);
  }

  if (circuit.isDisposedOrDisposing()) {
    // If the current circuit is no longer available, create a new one.
    circuit = new CircuitManager(circuit.getRootComponentManager(), appState, options, logger);
  }

  // Start the circuit. If the circuit has already started, this will return the existing
  // circuit start promise.
  return circuit.start();
}

export function hasStartedServer(): boolean {
  return started;
}

export async function disposeCircuit() {
  await circuit?.dispose();
}

export function isCircuitAvailable(): boolean {
  return !circuit.isDisposedOrDisposing();
}
