// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { afterEach, describe, expect, jest, test } from '@jest/globals';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
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
        return '["testCircuitStartup.value"]';
      }

      return 'circuit-id';
    });
    const circuit = createCircuit(invoke);

    await expect(circuit.start()).resolves.toBe(true);

    expect(invoke.mock.calls.map(call => call[0])).toEqual([
      'GetStartupValueKeys',
      'StartCircuitWithStartupValues',
      'CompleteHostInitialization',
    ]);
    expect(invoke.mock.calls[1][0]).toBe('StartCircuitWithStartupValues');
    expect(JSON.parse(invoke.mock.calls[1][5] as string))
      .toEqual({ 'testCircuitStartup.value': 'expected' });
  });

  test('completes host initialization before reporting the circuit as opened', async () => {
    const events: string[] = [];
    const invoke = jest.fn(async (method: string, ..._args: unknown[]) => {
      events.push(method);
      return method === 'GetStartupValueKeys' ? '[]' : 'circuit-id';
    });
    const circuit = createCircuit(
      invoke,
      () => events.push('opened'),
      notify => {
        events.push('server completion');
        notify(true, null);
      });

    await expect(circuit.start()).resolves.toBe(true);

    expect(events).toEqual([
      'GetStartupValueKeys',
      'StartCircuitWithStartupValues',
      'CompleteHostInitialization',
      'server completion',
      'opened',
    ]);
  });

  test('waits for server completion after the trigger invocation returns', async () => {
    let notifyCompletion: ((succeeded: boolean, error: string | null) => void) | undefined;
    let startCompleted = false;
    const invoke = jest.fn(async (method: string, ..._args: unknown[]) =>
      method === 'GetStartupValueKeys' ? '[]' : 'circuit-id');
    const circuit = createCircuit(
      invoke,
      undefined,
      notify => {
        notifyCompletion = notify;
      });

    const startPromise = circuit.start().then(result => {
      startCompleted = true;
      return result;
    });
    await waitFor(() => notifyCompletion !== undefined);

    expect(invoke.mock.calls.map(call => call[0])).toEqual([
      'GetStartupValueKeys',
      'StartCircuitWithStartupValues',
      'CompleteHostInitialization',
    ]);
    expect(startCompleted).toBe(false);

    notifyCompletion!(true, null);
    await expect(startPromise).resolves.toBe(true);
  });

  test('rejects when server host initialization fails without falling back', async () => {
    const invoke = jest.fn(async (method: string, ..._args: unknown[]) =>
      method === 'GetStartupValueKeys' ? '[]' : 'circuit-id');
    const circuit = createCircuit(
      invoke,
      undefined,
      notify => notify(false, 'Initializer failed.'));

    await expect(circuit.start()).rejects.toThrow('Initializer failed.');
    expect(invoke.mock.calls.map(call => call[0])).toEqual([
      'GetStartupValueKeys',
      'StartCircuitWithStartupValues',
      'CompleteHostInitialization',
    ]);
  });

  test('rejects host initialization when the circuit fails before completion notification', async () => {
    const invoke = jest.fn(async (method: string, ..._args: unknown[]) =>
      method === 'GetStartupValueKeys' ? [] : 'circuit-id');
    const circuit = createCircuit(
      invoke,
      undefined,
      (_notify, notifyError) => notifyError('Initializer failed through circuit error.'));

    await expect(circuit.start()).rejects.toThrow('Initializer failed through circuit error.');
    expect(invoke.mock.calls.map(call => call[0])).toEqual([
      'GetStartupValueKeys',
      'StartCircuitWithStartupValues',
      'CompleteHostInitialization',
    ]);
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
        return '[]';
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
        return '["testCircuitStartup.value"]';
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
      'CompleteHostInitialization',
      'GetStartupValueKeys',
      'ResumeCircuitWithStartupValues',
      'CompleteHostInitialization',
    ]);
    expect(JSON.parse(invoke.mock.calls[4][6] as string))
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
        return '[]';
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
      'CompleteHostInitialization',
      'GetStartupValueKeys',
      'ResumeCircuitWithStartupValues',
    ]);
  });

  test('rejects resume when server host initialization fails without falling back', async () => {
    let initializationCount = 0;
    const invoke = jest.fn(async (method: string, ..._args: unknown[]) => {
      if (method === 'GetStartupValueKeys') {
        return '[]';
      }

      return method === 'ResumeCircuitWithStartupValues' ? 'resumed-circuit-id' : 'circuit-id';
    });
    const circuit = createCircuit(
      invoke,
      undefined,
      notify => {
        initializationCount++;
        notify(initializationCount === 1, initializationCount === 1 ? null : 'Resume initializer failed.');
      });
    await circuit.start();
    setPaused(circuit);

    await expect(circuit.resume()).rejects.toThrow('Resume initializer failed.');
    expect(invoke.mock.calls.map(call => call[0])).toEqual([
      'GetStartupValueKeys',
      'StartCircuitWithStartupValues',
      'CompleteHostInitialization',
      'GetStartupValueKeys',
      'ResumeCircuitWithStartupValues',
      'CompleteHostInitialization',
    ]);
  });
});

function createCircuit(
  invoke: (method: string, ...args: unknown[]) => Promise<unknown>,
  onCircuitOpened?: () => void,
  onHostInitialization: (
    notify: (succeeded: boolean, error: string | null) => void,
    notifyError: (error: string) => void,
  ) => void = notify => notify(true, null),
): CircuitManager {
  const handlers = new Map<string, (...args: any[]) => void>();
  const connection = {
    state: HubConnectionState.Connected,
    on: (method: string, handler: (...args: any[]) => void) => handlers.set(method, handler),
    onclose: () => { /* no-op */ },
    start: () => Promise.resolve(),
    stop: () => Promise.resolve(),
    send: () => Promise.resolve(),
    invoke: async (method: string, ...args: unknown[]) => {
      const result = await invoke(method, ...args);
      if (method === 'CompleteHostInitialization') {
        onHostInitialization(
          (succeeded, error) => handlers.get('JS.EndHostInitialization')!(succeeded, error),
          error => handlers.get('JS.Error')!(error));
      }
      return result;
    },
  } as unknown as HubConnection;
  jest.spyOn(HubConnectionBuilder.prototype, 'build').mockReturnValue(connection);
  const circuit = new CircuitManager(
    { initialComponents: [] } as never,
    '',
    resolveOptions({
      reconnectionHandler: {
        onConnectionDown: () => { /* no-op */ },
        onConnectionUp: () => { /* no-op */ },
      },
      circuitHandlers: onCircuitOpened ? [{ onCircuitOpened }] : [],
    }),
    { log: () => { /* no-op */ } } as never,
    new JSEventRegistry());
  return circuit;
}

function setPaused(circuit: CircuitManager): void {
  (circuit as unknown as InternalCircuitManager)._pausingState.transitionTo(true);
}

async function waitFor(condition: () => boolean): Promise<void> {
  for (let i = 0; i < 100 && !condition(); i++) {
    await new Promise(resolve => setTimeout(resolve, 0));
  }
  expect(condition()).toBe(true);
}
