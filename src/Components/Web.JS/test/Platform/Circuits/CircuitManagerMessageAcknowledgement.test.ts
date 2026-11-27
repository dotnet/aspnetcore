// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { afterEach, describe, expect, jest, test } from '@jest/globals';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { CircuitManager } from '../../../src/Platform/Circuits/CircuitManager';
import { ReconnectionHandler, resolveOptions } from '../../../src/Platform/Circuits/CircuitStartOptions';
import { ConsoleLogger } from '../../../src/Platform/Logging/Loggers';
import { attachWebRendererInterop, detachWebRendererInterop, isRendererAttached } from '../../../src/Rendering/WebRendererInteropMethods';
import { WebRendererId } from '../../../src/Rendering/WebRendererId';
import { JSEventRegistry } from '../../../src/Services/JSEventRegistry';
import { internalFunctions as navigationManagerFunctions, navigateTo } from '../../../src/Services/NavigationManager';

function createConnectionWithMockedTransport() {
  const connection = new HubConnectionBuilder().withUrl('http://localhost/_blazor').build();
  let closeCallback: ((error?: Error) => void) | undefined;
  let state = HubConnectionState.Connected;

  jest.spyOn(HubConnectionBuilder.prototype, 'build').mockReturnValue(connection);
  jest.spyOn(connection, 'state', 'get').mockImplementation(() => state);
  jest.spyOn(connection, 'start').mockResolvedValue();
  jest.spyOn(connection, 'send').mockResolvedValue();
  jest.spyOn(connection, 'onclose').mockImplementation((callback: (error?: Error) => void) => {
    closeCallback = callback;
  });

  return {
    connection,
    setState: (newState: HubConnectionState) => { state = newState; },
    triggerClose: (error?: Error) => closeCallback?.(error),
  };
}

function createCircuit(reconnectionHandler?: ReconnectionHandler) {
  return new CircuitManager(
    {
      initialComponents: [],
      resolveRootComponent: () => { throw new Error('There are no root components in this test.'); },
    },
    '',
    resolveOptions({ reconnectionHandler }),
    new ConsoleLogger(LogLevel.None),
    new JSEventRegistry());
}

describe('CircuitManager navigation acknowledgement', () => {
  afterEach(() => {
    if (isRendererAttached(WebRendererId.Server)) {
      detachWebRendererInterop(WebRendererId.Server);
    }
    jest.clearAllTimers();
    jest.useRealTimers();
    jest.restoreAllMocks();
  });

  test.each([
    {
      name: 'location changed',
      send: (circuit: CircuitManager) => circuit.sendLocationChanged('/next', 'state', true),
      method: 'OnLocationChanged',
      args: ['/next', 'state', true],
    },
    {
      name: 'location changing',
      send: (circuit: CircuitManager) => circuit.sendLocationChanging(42, '/next', 'state', true),
      method: 'OnLocationChanging',
      args: [42, '/next', 'state', true],
    },
  ])('monitors $name without blocking navigation completion', async ({ send, method, args }) => {
    jest.useFakeTimers();
    const { connection } = createConnectionWithMockedTransport();
    const invoke = jest.spyOn(connection, 'invoke').mockResolvedValue('test-circuit');
    const onConnectionDown = jest.fn();
    const circuit = createCircuit({ onConnectionDown, onConnectionUp: jest.fn() });
    await circuit.start();

    const acknowledgement = Promise.withResolvers<void>();
    invoke.mockClear().mockReturnValue(acknowledgement.promise);

    await send(circuit);

    expect(invoke).toHaveBeenCalledWith(method, ...args);
    expect(connection.send).not.toHaveBeenCalled();

    acknowledgement.resolve();
    await Promise.resolve();
    jest.advanceTimersByTime(2000);
    expect(onConnectionDown).not.toHaveBeenCalled();
  });

  test('starts reconnection immediately at timeout even when connection.stop stalls, and ignores late onclose', async () => {
    jest.useFakeTimers();
    const { connection, setState, triggerClose } = createConnectionWithMockedTransport();
    const invoke = jest.spyOn(connection, 'invoke').mockResolvedValue('test-circuit');
    const stopDeferred = Promise.withResolvers<void>();
    jest.spyOn(connection, 'stop').mockImplementation(() => {
      setState(HubConnectionState.Disconnecting);
      return stopDeferred.promise;
    });
    const onConnectionDown = jest.fn();
    const circuit = createCircuit({ onConnectionDown, onConnectionUp: jest.fn() });
    await circuit.start();

    invoke.mockClear().mockReturnValue(new Promise<void>(() => { /* stalled send */ }));
    await circuit.sendLocationChanged('/next', undefined, true);

    jest.advanceTimersByTime(2000);

    expect(connection.stop).toHaveBeenCalledTimes(1);
    expect(onConnectionDown).toHaveBeenCalledTimes(1);

    setState(HubConnectionState.Disconnected);
    triggerClose();
    stopDeferred.resolve();
    await Promise.resolve();
    expect(onConnectionDown).toHaveBeenCalledTimes(1);
  });

  test('rapid Back followed by internal navigation preserves history index when OnLocationChanged ack is pending', async () => {
    jest.useFakeTimers();
    const { connection } = createConnectionWithMockedTransport();
    const invoke = jest.spyOn(connection, 'invoke').mockResolvedValue('test-circuit');
    const circuit = createCircuit({ onConnectionDown: jest.fn(), onConnectionUp: jest.fn() });
    await circuit.start();

    attachWebRendererInterop(WebRendererId.Server, {} as never);
    navigationManagerFunctions.enableNavigationInterception(WebRendererId.Server);
    navigationManagerFunctions.listenForNavigationEvents(
      WebRendererId.Server,
      (uri, state, intercepted) => circuit.sendLocationChanged(uri, state, intercepted),
      (callId, uri, state, intercepted) => circuit.sendLocationChanging(callId, uri, state, intercepted),
    );

    const pendingAcks: Array<ReturnType<typeof Promise.withResolvers<void>>> = [];
    invoke.mockClear().mockImplementation(() => {
      const deferred = Promise.withResolvers<void>();
      pendingAcks.push(deferred);
      return deferred.promise;
    });

    const initialIndex = (history.state as { _index?: number } | null)?._index ?? 0;
    navigateTo('/page-1', false);
    await Promise.resolve();
    expect((history.state as { _index?: number })._index).toBe(initialIndex + 1);

    // Simulate browser Back to initialIndex while OnLocationChanged remains pending.
    history.replaceState({ _index: initialIndex }, '', '/');
    window.dispatchEvent(new PopStateEvent('popstate', { state: { _index: initialIndex } }));
    await Promise.resolve();

    // Immediately navigate internally to /page-2 before the Back acknowledgement completes.
    navigateTo('/page-2', false);
    await Promise.resolve();
    expect((history.state as { _index?: number })._index).toBe(initialIndex + 1);

    // Settle pending acknowledgements and ensure currentHistoryIndex stays at initialIndex + 1.
    for (const ack of pendingAcks) {
      ack.resolve();
    }
    await Promise.resolve();

    // Guarded Back to initialIndex should compute delta = -1 (and revert with +1), not -2.
    navigationManagerFunctions.setHasLocationChangingListeners(WebRendererId.Server, true);
    const historyGoSpy = jest.spyOn(history, 'go').mockImplementation((delta?: number) => {
      if (delta === -1) {
        history.replaceState({ _index: initialIndex }, '', '/');
      } else if (delta === 1) {
        history.replaceState({ _index: initialIndex + 1 }, '', '/page-2');
      }
      window.dispatchEvent(new PopStateEvent('popstate', { state: history.state }));
    });

    history.replaceState({ _index: initialIndex }, '', '/');
    window.dispatchEvent(new PopStateEvent('popstate', { state: { _index: initialIndex } }));
    await Promise.resolve();

    expect(historyGoSpy).toHaveBeenCalledWith(1);
    expect(historyGoSpy).not.toHaveBeenCalledWith(2);
    navigationManagerFunctions.endLocationChanging(1, false);
    await Promise.resolve();
  });
});
