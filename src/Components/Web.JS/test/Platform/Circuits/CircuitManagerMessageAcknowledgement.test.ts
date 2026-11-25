// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { afterEach, describe, expect, jest, test } from '@jest/globals';
import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { CircuitManager } from '../../../src/Platform/Circuits/CircuitManager';
import { resolveOptions } from '../../../src/Platform/Circuits/CircuitStartOptions';
import { ConsoleLogger } from '../../../src/Platform/Logging/Loggers';
import { JSEventRegistry } from '../../../src/Services/JSEventRegistry';

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
  ])('monitors $name and returns its acknowledgement promise', async ({ send, method, args }) => {
    jest.useFakeTimers();
    const connection = new HubConnectionBuilder().withUrl('http://localhost/_blazor').build();
    jest.spyOn(HubConnectionBuilder.prototype, 'build').mockReturnValue(connection);
    jest.spyOn(connection, 'state', 'get').mockReturnValue(HubConnectionState.Connected);
    jest.spyOn(connection, 'start').mockResolvedValue();
    jest.spyOn(connection, 'send').mockResolvedValue();
    const invoke = jest.spyOn(connection, 'invoke').mockResolvedValue('test-circuit');
    const circuit = new CircuitManager(
      {
        initialComponents: [],
        resolveRootComponent: () => { throw new Error('There are no root components in this test.'); },
      },
      '',
      resolveOptions(),
      new ConsoleLogger(LogLevel.None),
      new JSEventRegistry());
    await circuit.start();

    const acknowledgement = Promise.withResolvers<void>();
    invoke.mockClear().mockReturnValue(acknowledgement.promise);
    let completed = false;
    const sending = send(circuit).then(() => { completed = true; });

    expect(invoke).toHaveBeenCalledWith(method, ...args);
    expect(connection.send).not.toHaveBeenCalled();
    await Promise.resolve();
    expect(completed).toBe(false);
    acknowledgement.resolve();
    await sending;
    expect(completed).toBe(true);
  });
});
