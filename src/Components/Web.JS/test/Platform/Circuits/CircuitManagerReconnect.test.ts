// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { afterEach, beforeEach, describe, expect, jest, test } from '@jest/globals';
import { HubConnection, HubConnectionBuilder, HubConnectionState, JsonHubProtocol, NullLogger } from '@microsoft/signalr';
import { TestConnection } from '../../../../../SignalR/clients/ts/signalr/tests/TestConnection';
import { CircuitManager } from '../../../src/Platform/Circuits/CircuitManager';
import { resolveOptions } from '../../../src/Platform/Circuits/CircuitStartOptions';
import { JSEventRegistry } from '../../../src/Services/JSEventRegistry';
import { attachWebRendererInterop, detachWebRendererInterop, isRendererAttached } from '../../../src/Rendering/WebRendererInteropMethods';
import { WebRendererId } from '../../../src/Rendering/WebRendererId';

interface InternalCircuitManager {
  _circuitId: string;
  _connection: HubConnection;
  _pausingState: { transitionTo(value: boolean): void };
  _activePauseDeferral?: AbortController;
  _persistedCircuitState?: { components: string, applicationState: string };
}

describe('CircuitManager failed circuit invocations', () => {
  let options: ReturnType<typeof resolveOptions>;
  let manager: CircuitManager;
  let internal: InternalCircuitManager;
  const connections: HubConnection[] = [];

  function createConnection() {
    const transport = new TestConnection();
    const connection = (HubConnection as unknown as {
      create(transport: TestConnection, logger: typeof NullLogger.instance, protocol: JsonHubProtocol): HubConnection;
    }).create(transport, NullLogger.instance, new JsonHubProtocol());
    connections.push(connection);
    const invocationSent = Promise.withResolvers<void>();
    const send = transport.send.bind(transport);
    jest.spyOn(transport, 'send').mockImplementation(async data => {
      await send(data);
      if (transport.lastInvocationId !== null) {
        invocationSent.resolve();
      }
    });
    jest.spyOn(HubConnectionBuilder.prototype, 'build').mockReturnValue(connection);
    return { connection, transport, invocationSent: invocationSent.promise };
  }

  beforeEach(() => {
    options = resolveOptions({
      circuitHandlers: [],
      reconnectionHandler: { onConnectionDown: jest.fn(), onConnectionUp: jest.fn() },
    });
    manager = new CircuitManager(
      {} as never,
      '',
      options,
      { log: () => { /* no-op */ } } as never,
      new JSEventRegistry(),
    );
    internal = manager as unknown as InternalCircuitManager;
    internal._circuitId = 'test-circuit-id';
    internal._connection = { state: HubConnectionState.Disconnected } as HubConnection;
    internal._pausingState.transitionTo(true);
  });

  afterEach(async () => {
    for (const connection of connections) {
      await connection.stop();
    }
    connections.length = 0;
    if (isRendererAttached(WebRendererId.Server)) {
      detachWebRendererInterop(WebRendererId.Server);
    }
    jest.restoreAllMocks();
  });

  test.each(['reconnect', 'resume'] as const)('stops the connection and permits retry when %s invocation fails', async operation => {
    const { connection, transport, invocationSent } = createConnection();
    const stop = jest.spyOn(connection, 'stop');
    const deferral = new AbortController();
    internal._activePauseDeferral = deferral;
    const result = manager[operation]();
    const rejected = expect(result).rejects.toThrow('Intentional hub failure');
    await invocationSent;

    expect(transport.parsedSentData.find(message => message.invocationId)?.target)
      .toBe(operation === 'reconnect' ? 'ConnectCircuit' : 'ResumeCircuit');
    transport.receive({ type: 3, invocationId: transport.lastInvocationId, error: 'Intentional hub failure' });
    await rejected;

    expect(stop).toHaveBeenCalledTimes(1);
    expect(connection.state).toBe(HubConnectionState.Disconnected);
    expect(deferral.signal.aborted).toBe(true);
    expect(options.reconnectionHandler!.onConnectionDown).toHaveBeenCalledTimes(1);
    expect(options.reconnectionHandler!.onConnectionUp).not.toHaveBeenCalled();

    const retry = createConnection();
    const retried = manager[operation]();
    await retry.invocationSent;
    retry.transport.receive({ type: 3, invocationId: retry.transport.lastInvocationId, result: operation === 'reconnect' ? true : 'new-circuit-id' });
    await expect(retried).resolves.toBe(true);
    expect(retry.connection.state).toBe(HubConnectionState.Connected);
    expect(options.reconnectionHandler!.onConnectionUp).toHaveBeenCalledTimes(1);
  });

  test('retains client-persisted state after a failed resume until a retry succeeds', async () => {
    const initial = createConnection();
    const connected = manager.reconnect();
    await initial.invocationSent;
    initial.transport.receive({ type: 3, invocationId: initial.transport.lastInvocationId, result: true });
    await expect(connected).resolves.toBe(true);

    const state = { components: '[{"type":"test-component"}]', applicationState: 'persisted-application-state' };
    initial.transport.receive({
      type: 1,
      target: 'JS.SavePersistedState',
      arguments: ['test-circuit-id', state.components, state.applicationState],
    });
    expect(internal._persistedCircuitState).toEqual(state);
    await initial.connection.stop();

    const first = createConnection();
    const resumed = manager.resume();
    const rejected = expect(resumed).rejects.toThrow('Intentional resume failure');
    await first.invocationSent;
    const firstInvocation = first.transport.parsedSentData.find(message => message.target === 'ResumeCircuit');
    expect(firstInvocation.arguments.slice(3)).toEqual([state.components, state.applicationState]);
    first.transport.receive({ type: 3, invocationId: first.transport.lastInvocationId, error: 'Intentional resume failure' });
    await rejected;

    const retry = createConnection();
    const retried = manager.resume();
    await retry.invocationSent;
    const retryInvocation = retry.transport.parsedSentData.find(message => message.target === 'ResumeCircuit');
    retry.transport.receive({ type: 3, invocationId: retry.transport.lastInvocationId, result: 'new-circuit-id' });
    await expect(retried).resolves.toBe(true);
    expect(retryInvocation.arguments).toEqual(firstInvocation.arguments);
    expect(internal._persistedCircuitState).toBeUndefined();
  });

  test('a late failed invocation stops its connection without tearing down a newer successful reconnect', async () => {
    const first = createConnection();
    const firstResult = manager.reconnect();
    const rejected = expect(firstResult).rejects.toThrow('Late hub failure');
    const second = createConnection();
    const secondResult = manager.reconnect();
    await Promise.all([first.invocationSent, second.invocationSent]);
    second.transport.receive({ type: 3, invocationId: second.transport.lastInvocationId, result: true });
    await expect(secondResult).resolves.toBe(true);

    const interop = {} as Parameters<typeof attachWebRendererInterop>[1];
    attachWebRendererInterop(WebRendererId.Server, interop);
    const deferral = new AbortController();
    internal._activePauseDeferral = deferral;
    first.transport.receive({ type: 3, invocationId: first.transport.lastInvocationId, error: 'Late hub failure' });
    await rejected;

    expect(first.connection.state).toBe(HubConnectionState.Disconnected);
    expect(second.connection.state).toBe(HubConnectionState.Connected);
    expect(internal._connection).toBe(second.connection);
    expect(isRendererAttached(WebRendererId.Server)).toBe(true);
    expect(deferral.signal.aborted).toBe(false);
    expect(options.reconnectionHandler!.onConnectionDown).not.toHaveBeenCalled();
    expect(options.reconnectionHandler!.onConnectionUp).toHaveBeenCalledTimes(1);
  });

  test.each(['reconnect', 'resume'] as const)('does not stop the connection when %s is rejected without an exception', async operation => {
    const { connection, transport, invocationSent } = createConnection();
    const stop = jest.spyOn(connection, 'stop');
    const result = manager[operation]();
    await invocationSent;
    transport.receive({ type: 3, invocationId: transport.lastInvocationId, result: operation === 'reconnect' ? false : '' });

    await expect(result).resolves.toBe(false);
    expect(stop).not.toHaveBeenCalled();
    expect(connection.state).toBe(HubConnectionState.Connected);
    expect(options.reconnectionHandler!.onConnectionUp).not.toHaveBeenCalled();
  });

  test.each(['reconnect', 'resume'] as const)('preserves the %s invocation error if cleanup fails', async operation => {
    const { connection, transport, invocationSent } = createConnection();
    const stop = jest.spyOn(connection, 'stop').mockRejectedValueOnce(new Error('Cleanup failed'));
    const result = manager[operation]();
    const rejected = expect(result).rejects.toThrow('Original hub failure');
    await invocationSent;
    transport.receive({ type: 3, invocationId: transport.lastInvocationId, error: 'Original hub failure' });
    await rejected;
    expect(stop).toHaveBeenCalledTimes(1);
  });
});
