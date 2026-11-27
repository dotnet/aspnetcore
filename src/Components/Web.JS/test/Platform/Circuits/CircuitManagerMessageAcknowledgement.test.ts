// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { afterEach, describe, expect, jest, test } from '@jest/globals';
import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { CircuitManager } from '../../../src/Platform/Circuits/CircuitManager';
import { ReconnectionHandler, resolveOptions } from '../../../src/Platform/Circuits/CircuitStartOptions';
import { ConsoleLogger } from '../../../src/Platform/Logging/Loggers';
import { JSEventRegistry } from '../../../src/Services/JSEventRegistry';

const originalBuild = HubConnectionBuilder.prototype.build;

function createConnectionWithMockedTransport() {
  const connection = originalBuild.call(new HubConnectionBuilder().withUrl('http://localhost/_blazor'));
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

function createCircuit(reconnectionHandler?: ReconnectionHandler, logger?: ConsoleLogger) {
  return new CircuitManager(
    {
      initialComponents: [],
      resolveRootComponent: () => { throw new Error('There are no root components in this test.'); },
    },
    '',
    resolveOptions({ reconnectionHandler }),
    logger ?? new ConsoleLogger(LogLevel.None),
    new JSEventRegistry());
}

describe('CircuitManager navigation acknowledgement', () => {
  afterEach(() => {
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

  test('suppresses stale invocation cancellation error logs after connection replacement', async () => {
    jest.useFakeTimers();
    const first = createConnectionWithMockedTransport();
    const firstInvoke = jest.spyOn(first.connection, 'invoke').mockResolvedValue('test-circuit');
    jest.spyOn(first.connection, 'stop').mockImplementation(() => {
      first.setState(HubConnectionState.Disconnecting);
      return new Promise<void>(() => { /* stalled DELETE */ });
    });

    const logger = new ConsoleLogger(LogLevel.Error);
    const logSpy = jest.spyOn(logger, 'log').mockImplementation(() => { /* suppress console output */ });
    const circuit = createCircuit({ onConnectionDown: jest.fn(), onConnectionUp: jest.fn() }, logger);
    await circuit.start();

    const firstPendingAck = Promise.withResolvers<void>();
    firstInvoke.mockClear().mockReturnValue(firstPendingAck.promise);
    await circuit.sendLocationChanged('/next', undefined, true);

    jest.advanceTimersByTime(2000);

    // Replace the connection via reconnect before the old Long Polling cleanup finishes.
    const second = createConnectionWithMockedTransport();
    const secondInvoke = jest.spyOn(second.connection, 'invoke').mockResolvedValue(true);
    await expect(circuit.reconnect()).resolves.toBe(true);
    logSpy.mockClear();

    // When the old connection finally closes and rejects its pending invocation, no error is logged.
    first.setState(HubConnectionState.Disconnected);
    firstPendingAck.reject(new Error('Invocation canceled due to the underlying connection being closed.'));
    await Promise.resolve();
    await Promise.resolve();
    expect(logSpy).not.toHaveBeenCalled();

    // A server completion error on the current active connection is still logged at Error level.
    const hubError = new Error('Hub filter threw on active connection.');
    secondInvoke.mockClear().mockReturnValue(Promise.reject(hubError));
    await circuit.sendLocationChanged('/active', undefined, true);
    await Promise.resolve();
    await Promise.resolve();
    expect(logSpy).toHaveBeenCalledWith(LogLevel.Error, hubError);
  });
});
