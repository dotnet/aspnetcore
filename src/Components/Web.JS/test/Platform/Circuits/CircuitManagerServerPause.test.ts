// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { expect, test, describe, beforeEach, afterEach } from '@jest/globals';
import { CircuitManager } from '../../../src/Platform/Circuits/CircuitManager';
import { resolveOptions, CircuitStartOptions } from '../../../src/Platform/Circuits/CircuitStartOptions';
import { JSEventRegistry } from '../../../src/Services/JSEventRegistry';
import { Blazor } from '../../../src/GlobalExports';
import { registerPauseDeferral } from '../../../src/Platform/Circuits/PauseDeferralRegistry';

interface InternalServerPause {
  handleServerInitiatedPause(): Promise<void>;
  pauseCircuit(externalSignal?: AbortSignal): Promise<boolean>;
  pause(remote?: boolean): Promise<boolean>;
}

describe('CircuitManager server-initiated pause deferral', () => {
  let options: CircuitStartOptions;
  let circuit: CircuitManager;
  let internal: InternalServerPause;
  let order: string[];
  let unregister: Array<() => void>;

  beforeEach(() => {
    options = resolveOptions({});
    circuit = new CircuitManager({} as never, '', options, { log: () => { /* no-op */ } } as never, new JSEventRegistry());
    internal = circuit as unknown as InternalServerPause;
    order = [];
    unregister = [];
    // The package-provided pause hook is absent in these tests, so the handler falls back
    // to the circuit's own pause(); stub it to record ordering instead of touching SignalR.
    internal.pause = () => {
      order.push('pause');
      return Promise.resolve(true);
    };
    // The deferral registry is module-internal (PauseDeferralRegistry); Boot wires
    // Blazor.pause.waitFor to register into it. Mirror that wiring here and track the unsubscribe
    // callbacks so each test cleans up the shared registry in afterEach.
    Blazor.pause = {
      waitFor(handler, opts) {
        const off = registerPauseDeferral(handler, opts?.source);
        unregister.push(off);
        return off;
      },
    };
    delete (Blazor as unknown as Record<string, unknown>).pauseCircuit;
  });

  afterEach(() => {
    unregister.forEach(off => off());
    delete (Blazor as unknown as Record<string, unknown>).pauseCircuit;
    delete (Blazor as unknown as Record<string, unknown>).pause;
  });

  test('awaits a registered server pause deferral to completion before pausing', async () => {
    let deferCompleted = false;
    Blazor.pause!.waitFor(async () => {
      order.push('defer-start');
      await Promise.resolve();
      deferCompleted = true;
      order.push('defer-end');
    }, { source: 'server' });

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

  test('server-initiated pause fires the server-scoped and untagged deferrals', async () => {
    Blazor.pause!.waitFor(() => { order.push('no-source'); }, undefined);
    Blazor.pause!.waitFor(() => { order.push('server'); }, { source: 'server' });
    Blazor.pause!.waitFor(() => { order.push('auto'); }, { source: 'auto' });

    await internal.handleServerInitiatedPause();

    expect(order).toContain('server');
    expect(order).toContain('no-source');
    expect(order).not.toContain('auto');
    expect(order[order.length - 1]).toBe('pause');
  });

  test('client/auto pause fires the untagged and non-server deferrals', async () => {
    Blazor.pause!.waitFor(() => { order.push('no-source'); }, undefined);
    Blazor.pause!.waitFor(() => { order.push('server'); }, { source: 'server' });
    Blazor.pause!.waitFor(() => { order.push('auto'); }, { source: 'auto' });

    await internal.pauseCircuit();

    expect(order).toContain('no-source');
    expect(order).toContain('auto');
    expect(order).not.toContain('server');
    expect(order[order.length - 1]).toBe('pause');
  });
});
