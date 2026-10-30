// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { afterEach, describe, expect, jest, test } from '@jest/globals';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { CircuitManager } from '../../../src/Platform/Circuits/CircuitManager';
import { resolveOptions } from '../../../src/Platform/Circuits/CircuitStartOptions';
import { JSEventRegistry } from '../../../src/Services/JSEventRegistry';
import { isRendererAttached } from '../../../src/Rendering/WebRendererInteropMethods';
import { WebRendererId } from '../../../src/Rendering/WebRendererId';

describe('CircuitManager reconnect', () => {
  afterEach(() => {
    jest.restoreAllMocks();
  });

  test('stops the connection when ConnectCircuit throws', async () => {
    const error = new Error('ConnectCircuit failed');
    let stopped = false;
    const connection = {
      state: 'Connected',
      invoke: () => Promise.reject(error),
      stop: () => {
        stopped = true;
        connection.state = 'Disconnected';
        return Promise.resolve();
      },
    };
    const manager = Object.create(CircuitManager.prototype) as {
      _circuitId: string;
      _connection: { state: string };
      startConnection: () => Promise<typeof connection>;
      reconnect: () => Promise<boolean>;
    };
    manager._circuitId = 'test-circuit-id';
    manager._connection = { state: 'Disconnected' };
    manager.startConnection = () => Promise.resolve(connection);

    await expect(manager.reconnect()).rejects.toBe(error);

    expect(stopped).toBe(true);
    expect(connection.state).toBe('Disconnected');
  });

  test('stops the connection when ResumeCircuit throws', async () => {
    const error = new Error('ResumeCircuit failed');
    let stopped = false;
    const connection = {
      state: 'Connected',
      invoke: () => Promise.reject(error),
      stop: () => {
        stopped = true;
        connection.state = 'Disconnected';
        return Promise.resolve();
      },
    };
    const manager = new CircuitManager(
      {} as never,
      '',
      resolveOptions({ circuitHandlers: [] }),
      { log: () => { /* no-op */ } } as never,
      new JSEventRegistry(),
    );
    const internal = manager as unknown as {
      _circuitId: string;
      _connection: typeof connection;
      _pausingState: { transitionTo(value: boolean): void };
    };
    internal._circuitId = 'test-circuit-id';
    internal._connection = connection;
    internal._pausingState.transitionTo(true);

    await expect(manager.resume()).rejects.toBe(error);

    expect(stopped).toBe(true);
    expect(connection.state).toBe('Disconnected');
  });
  test.each(['onCircuitClosed', 'onCircuitOpened'] as const)(
    'does not stop the connection when %s throws',
    async (callback) => {
      const callbackError = new Error(`${callback} failed`);
      const connection = {
        state: 'Connected',
        invoke: jest.fn(async () => 'new-circuit-id'),
        stop: jest.fn(async () => { }),
      };
      const manager = new CircuitManager(
        {} as never,
        '',
        resolveOptions({
          circuitHandlers: [{
            [callback]: () => { throw callbackError; },
          }],
        }),
        { log: () => { } } as never,
        new JSEventRegistry(),
      );
      const internal = manager as unknown as {
        _circuitId: string;
        _connection: typeof connection;
        _pausingState: { transitionTo(value: boolean): void };
      };
      internal._circuitId = 'test-circuit-id';
      internal._connection = connection;
      internal._pausingState.transitionTo(true);

      await expect(manager.resume()).rejects.toBe(callbackError);

      expect(connection.invoke).toHaveBeenCalledTimes(
        callback === 'onCircuitOpened' ? 1 : 0,
      );
      expect(connection.stop).not.toHaveBeenCalled();
      expect(connection.state).toBe('Connected');
    },
  );

  test('onclose does not throw when renderer interop is already detached', async () => {
    let closeHandler: ((error?: Error) => void) | undefined;
    const connection = {
      on: jest.fn(),
      onclose: jest.fn((handler: (error?: Error) => void) => { closeHandler = handler; }),
      start: () => Promise.resolve(),
    } as unknown as HubConnection;
    jest.spyOn(HubConnectionBuilder.prototype, 'build').mockReturnValue(connection);

    const onConnectionDown = jest.fn();
    const manager = new CircuitManager(
      {} as never,
      '',
      resolveOptions({
        circuitHandlers: [],
        reconnectionHandler: { onConnectionDown, onConnectionUp: () => { } },
      }),
      { log: () => { } } as never,
      new JSEventRegistry(),
    );

    await (manager as unknown as { startConnection(): Promise<HubConnection> }).startConnection();

    // Simulate the renderer interop already being detached (e.g. after ConnectCircuit returned false).
    expect(isRendererAttached(WebRendererId.Server)).toBe(false);

    // The onclose handler must not throw even though the renderer interop is no longer registered,
    // so connection-down bookkeeping still runs.
    expect(() => closeHandler!(new Error('connection closed'))).not.toThrow();
    expect(onConnectionDown).toHaveBeenCalledTimes(1);
  });
});
