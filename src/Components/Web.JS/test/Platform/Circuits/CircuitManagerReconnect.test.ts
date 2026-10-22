// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { describe, expect, test } from '@jest/globals';
import { CircuitManager } from '../../../src/Platform/Circuits/CircuitManager';
import { resolveOptions } from '../../../src/Platform/Circuits/CircuitStartOptions';
import { JSEventRegistry } from '../../../src/Services/JSEventRegistry';

describe('CircuitManager reconnect', () => {
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
});
