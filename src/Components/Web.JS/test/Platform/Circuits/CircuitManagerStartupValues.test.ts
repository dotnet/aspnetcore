// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { afterEach, describe, expect, jest, test } from '@jest/globals';
import { HubConnection, HubConnectionState } from '@microsoft/signalr';
import { CircuitManager } from '../../../src/Platform/Circuits/CircuitManager';
import { resolveOptions } from '../../../src/Platform/Circuits/CircuitStartOptions';
import { JSEventRegistry } from '../../../src/Services/JSEventRegistry';

interface InternalCircuitManager {
  startConnection(): Promise<HubConnection>;
  _pausingState: {
    transitionTo(value: boolean): void;
  };
}

const globals = globalThis as unknown as Record<string, unknown>;

describe('CircuitManager startup values', () => {
  afterEach(() => {
    delete globals.testCircuitStartup;
    jest.restoreAllMocks();
  });

  test('queries keys and starts with evaluated startup values', async () => {
    globals.testCircuitStartup = { value: 'expected' };
    const invoke = jest.fn(async (method: string, ..._args: unknown[]) => {
      if (method === 'GetStartupValueKeys') {
        return ['testCircuitStartup.value'];
      }

      return 'circuit-id';
    });
    const circuit = createCircuit(invoke);

    await expect(circuit.start()).resolves.toBe(true);

    expect(invoke).toHaveBeenCalledTimes(2);
    expect(invoke.mock.calls[1][0]).toBe('StartCircuitWithStartupValues');
    expect(JSON.parse(invoke.mock.calls[1][5] as string))
      .toEqual({ 'testCircuitStartup.value': 'expected' });
  });

  test('uses legacy start only when the capability method is absent', async () => {
    const invoke = jest.fn(async (method: string, ..._args: unknown[]) => {
      if (method === 'GetStartupValueKeys') {
        throw new Error(
          "Failed to invoke 'GetStartupValueKeys' due to an error on the server. HubException: Method does not exist.");
      }

      return 'circuit-id';
    });
    const circuit = createCircuit(invoke);

    await expect(circuit.start()).resolves.toBe(true);

    expect(invoke.mock.calls.map(call => call[0])).toEqual(['GetStartupValueKeys', 'StartCircuit']);
  });

  test('does not fall back after startup-values capability succeeds', async () => {
    const invoke = jest.fn(async (method: string, ..._args: unknown[]) => {
      if (method === 'GetStartupValueKeys') {
        return [];
      }

      throw new Error('New start failed.');
    });
    const circuit = createCircuit(invoke);

    await expect(circuit.start()).rejects.toThrow('New start failed.');

    expect(invoke.mock.calls.map(call => call[0]))
      .toEqual(['GetStartupValueKeys', 'StartCircuitWithStartupValues']);
  });

  test('renegotiates and reevaluates startup values when resuming', async () => {
    globals.testCircuitStartup = { value: 'expected' };
    const invoke = jest.fn(async (method: string, ..._args: unknown[]) => {
      if (method === 'GetStartupValueKeys') {
        return ['testCircuitStartup.value'];
      }

      return method === 'ResumeCircuitWithStartupValues' ? 'resumed-circuit-id' : 'circuit-id';
    });
    const circuit = createCircuit(invoke);
    await circuit.start();
    globals.testCircuitStartup = { value: 'changed' };
    setPaused(circuit);

    await expect(circuit.resume()).resolves.toBe(true);

    expect(invoke.mock.calls.map(call => call[0])).toEqual([
      'GetStartupValueKeys',
      'StartCircuitWithStartupValues',
      'GetStartupValueKeys',
      'ResumeCircuitWithStartupValues',
    ]);
    expect(JSON.parse(invoke.mock.calls[3][6] as string))
      .toEqual({ 'testCircuitStartup.value': 'changed' });
  });

  test('uses legacy resume when startup values are unsupported', async () => {
    const invoke = jest.fn(async (method: string, ..._args: unknown[]) => {
      if (method === 'GetStartupValueKeys') {
        throw new Error(
          "Failed to invoke 'GetStartupValueKeys' due to an error on the server. HubException: Method does not exist.");
      }

      return method === 'ResumeCircuit' ? 'resumed-circuit-id' : 'circuit-id';
    });
    const circuit = createCircuit(invoke);
    await circuit.start();
    setPaused(circuit);

    await expect(circuit.resume()).resolves.toBe(true);

    expect(invoke.mock.calls.map(call => call[0]))
      .toEqual(['GetStartupValueKeys', 'StartCircuit', 'GetStartupValueKeys', 'ResumeCircuit']);
  });

  test('does not fall back when startup-values resume fails', async () => {
    const invoke = jest.fn(async (method: string, ..._args: unknown[]) => {
      if (method === 'GetStartupValueKeys') {
        return [];
      }

      if (method === 'ResumeCircuitWithStartupValues') {
        throw new Error('New resume failed.');
      }

      return 'circuit-id';
    });
    const circuit = createCircuit(invoke);
    await circuit.start();
    setPaused(circuit);

    await expect(circuit.resume()).rejects.toThrow('New resume failed.');

    expect(invoke.mock.calls.map(call => call[0])).toEqual([
      'GetStartupValueKeys',
      'StartCircuitWithStartupValues',
      'GetStartupValueKeys',
      'ResumeCircuitWithStartupValues',
    ]);
  });
});

function createCircuit(invoke: (method: string, ...args: unknown[]) => Promise<unknown>): CircuitManager {
  const connection = {
    state: HubConnectionState.Connected,
    invoke,
  } as unknown as HubConnection;
  const circuit = new CircuitManager(
    { initialComponents: [] } as never,
    '',
    resolveOptions({
      reconnectionHandler: {
        onConnectionDown: () => { /* no-op */ },
        onConnectionUp: () => { /* no-op */ },
      },
    }),
    { log: () => { /* no-op */ } } as never,
    new JSEventRegistry());
  (circuit as unknown as InternalCircuitManager).startConnection = () => Promise.resolve(connection);
  return circuit;
}

function setPaused(circuit: CircuitManager): void {
  (circuit as unknown as InternalCircuitManager)._pausingState.transitionTo(true);
}
