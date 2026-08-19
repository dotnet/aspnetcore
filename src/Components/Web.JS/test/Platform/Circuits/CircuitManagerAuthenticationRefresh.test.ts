// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { afterEach, describe, expect, jest, test } from '@jest/globals';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { CircuitManager } from '../../../src/Platform/Circuits/CircuitManager';
import { resolveOptions } from '../../../src/Platform/Circuits/CircuitStartOptions';
import { JSEventRegistry } from '../../../src/Services/JSEventRegistry';

interface InternalCircuitManager {
  _connection?: HubConnection;
  startConnection(): Promise<HubConnection>;
}

describe('CircuitManager authentication refresh', () => {
  const circuits: CircuitManager[] = [];

  afterEach(async () => {
    await Promise.all(circuits.map(circuit => circuit.dispose()));
    circuits.length = 0;
    jest.restoreAllMocks();
  });

  test('enables authentication refresh before applying user configuration', async () => {
    const configuredOptions: unknown[] = [];
    jest.spyOn(HubConnectionBuilder.prototype, 'withAuthenticationRefresh')
      .mockImplementation(function (this: HubConnectionBuilder, options = {}) {
        configuredOptions.push(options);
        return this;
      });

    const connection = {
      on: jest.fn(),
      onclose: jest.fn(),
      start: () => Promise.resolve(),
    } as unknown as HubConnection;
    jest.spyOn(HubConnectionBuilder.prototype, 'build').mockReturnValue(connection);

    const options = resolveOptions({
      configureSignalR: builder => {
        builder.withAuthenticationRefresh({ enableAutoRefresh: false });
      },
    });
    const circuit = new CircuitManager(
      {} as never,
      '',
      options,
      { log: () => { /* no-op */ } } as never,
      new JSEventRegistry());
    circuits.push(circuit);

    await startCircuitConnection(circuit);

    expect(configuredOptions).toEqual([{}, { enableAutoRefresh: false }]);
  });

  test('refreshes authentication after an enhanced load', async () => {
    const refreshAuthentication = jest.fn(() => Promise.resolve(undefined));
    const connection = createConnection(refreshAuthentication);
    jest.spyOn(HubConnectionBuilder.prototype, 'build').mockReturnValue(connection);
    const eventRegistry = new JSEventRegistry();
    const circuit = createCircuit(eventRegistry);

    await startCircuitConnection(circuit);
    eventRegistry.dispatchEvent('enhancedload', {});
    await Promise.resolve();

    expect(refreshAuthentication).toHaveBeenCalledTimes(1);
  });

  test('refreshes authentication when the document becomes visible', async () => {
    const refreshAuthentication = jest.fn(() => Promise.resolve(undefined));
    const connection = createConnection(refreshAuthentication);
    jest.spyOn(HubConnectionBuilder.prototype, 'build').mockReturnValue(connection);
    const circuit = createCircuit(new JSEventRegistry());

    await startCircuitConnection(circuit);
    jest.spyOn(document, 'visibilityState', 'get').mockReturnValue('visible');
    document.dispatchEvent(new Event('visibilitychange'));
    await Promise.resolve();

    expect(refreshAuthentication).toHaveBeenCalledTimes(1);
  });

  test('does not refresh authentication when the document is hidden', async () => {
    const refreshAuthentication = jest.fn(() => Promise.resolve(undefined));
    const connection = createConnection(refreshAuthentication);
    jest.spyOn(HubConnectionBuilder.prototype, 'build').mockReturnValue(connection);
    const circuit = createCircuit(new JSEventRegistry());

    await startCircuitConnection(circuit);
    jest.spyOn(document, 'visibilityState', 'get').mockReturnValue('hidden');
    document.dispatchEvent(new Event('visibilitychange'));

    expect(refreshAuthentication).not.toHaveBeenCalled();
  });

  test('does not refresh authentication while disconnected', async () => {
    const refreshAuthentication = jest.fn(() => Promise.resolve(undefined));
    const connection = createConnection(refreshAuthentication);
    jest.spyOn(HubConnectionBuilder.prototype, 'build').mockReturnValue(connection);
    const eventRegistry = new JSEventRegistry();
    const circuit = createCircuit(eventRegistry);

    await startCircuitConnection(circuit);
    Object.defineProperty(connection, 'state', { configurable: true, value: HubConnectionState.Disconnected });
    eventRegistry.dispatchEvent('enhancedload', {});

    expect(refreshAuthentication).not.toHaveBeenCalled();
  });

  test('coalesces overlapping circuit authentication refreshes into one trailing refresh', async () => {
    const refreshCompletions: Array<() => void> = [];
    const refreshAuthentication = jest.fn(() => new Promise<void>(resolve => refreshCompletions.push(resolve)));
    const connection = createConnection(refreshAuthentication);
    jest.spyOn(HubConnectionBuilder.prototype, 'build').mockReturnValue(connection);
    const eventRegistry = new JSEventRegistry();
    const circuit = createCircuit(eventRegistry);

    await startCircuitConnection(circuit);
    eventRegistry.dispatchEvent('enhancedload', {});
    eventRegistry.dispatchEvent('enhancedload', {});
    eventRegistry.dispatchEvent('enhancedload', {});
    expect(refreshAuthentication).toHaveBeenCalledTimes(1);

    refreshCompletions[0]();
    await Promise.resolve();
    await Promise.resolve();
    expect(refreshAuthentication).toHaveBeenCalledTimes(2);

    refreshCompletions[1]();
    await Promise.resolve();
    await Promise.resolve();
    expect(refreshAuthentication).toHaveBeenCalledTimes(2);
  });

  function createConnection(refreshAuthentication: () => Promise<unknown>): HubConnection {
    return {
      state: HubConnectionState.Disconnected,
      on: jest.fn(),
      onclose: jest.fn(),
      start: jest.fn(function (this: HubConnection) {
        Object.defineProperty(this, 'state', { configurable: true, value: HubConnectionState.Connected });
        return Promise.resolve();
      }),
      refreshAuthentication,
    } as unknown as HubConnection;
  }

  async function startCircuitConnection(circuit: CircuitManager): Promise<void> {
    const internalCircuit = circuit as unknown as InternalCircuitManager;
    internalCircuit._connection = await internalCircuit.startConnection();
  }

  function createCircuit(eventRegistry: JSEventRegistry): CircuitManager {
    const circuit = new CircuitManager(
      {} as never,
      '',
      resolveOptions({}),
      { log: () => { /* no-op */ } } as never,
      eventRegistry);
    circuits.push(circuit);
    return circuit;
  }
});
