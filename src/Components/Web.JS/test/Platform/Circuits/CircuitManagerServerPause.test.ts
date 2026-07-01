// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { expect, test, describe, beforeEach } from '@jest/globals';
import { CircuitManager } from '../../../src/Platform/Circuits/CircuitManager';
import { resolveOptions, CircuitStartOptions, CircuitHandler } from '../../../src/Platform/Circuits/CircuitStartOptions';
import { JSEventRegistry } from '../../../src/Services/JSEventRegistry';

interface InternalServerPause {
  handleServerInitiatedPause(): Promise<void>;
  pauseCircuit(externalSignal?: AbortSignal): Promise<boolean>;
  pause(remote?: boolean): Promise<boolean>;
}

describe('CircuitManager pause deferral', () => {
  let options: CircuitStartOptions;
  let circuit: CircuitManager;
  let internal: InternalServerPause;
  let order: string[];

  function onPausing(handler: CircuitHandler['onCircuitPausing']) {
    // Deferrals are the onCircuitPausing hooks of the registered circuit handlers.
    options.circuitHandlers.push({ onCircuitPausing: handler });
  }

  beforeEach(() => {
    // Pass a fresh circuitHandlers array so registrations don't leak across tests.
    options = resolveOptions({ circuitHandlers: [] });
    circuit = new CircuitManager({} as never, '', options, { log: () => { /* no-op */ } } as never, new JSEventRegistry());
    internal = circuit as unknown as InternalServerPause;
    order = [];
    // The package-provided pause hook is absent in these tests, so the handler falls back
    // to the circuit's own pause(); stub it to record ordering instead of touching SignalR.
    internal.pause = () => {
      order.push('pause');
      return Promise.resolve(true);
    };
  });

  test('awaits a registered pause deferral to completion before pausing', async () => {
    let deferCompleted = false;
    onPausing(async () => {
      order.push('defer-start');
      await Promise.resolve();
      deferCompleted = true;
      order.push('defer-end');
    });

    await internal.handleServerInitiatedPause();

    expect(deferCompleted).toBe(true);
    expect(order).toEqual([
      'defer-start',
      'defer-end',
      'pause',
    ]);
  });

  test('still pauses when no pause deferral is registered', async () => {
    await internal.handleServerInitiatedPause();

    expect(order).toEqual(['pause']);
  });

  test('server-initiated pause fires every registered deferral before pausing', async () => {
    onPausing(() => { order.push('a'); });
    onPausing(() => { order.push('b'); });

    await internal.handleServerInitiatedPause();

    expect(order).toContain('a');
    expect(order).toContain('b');
    expect(order[order.length - 1]).toBe('pause');
  });

  test('client/auto pause fires every registered deferral before pausing', async () => {
    onPausing(() => { order.push('a'); });
    onPausing(() => { order.push('b'); });

    await internal.pauseCircuit();

    expect(order).toContain('a');
    expect(order).toContain('b');
    expect(order[order.length - 1]).toBe('pause');
  });
});
