// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { expect, test, describe, beforeEach, afterEach } from '@jest/globals';
import { CircuitManager } from '../../../src/Platform/Circuits/CircuitManager';
import { resolveOptions, CircuitStartOptions } from '../../../src/Platform/Circuits/CircuitStartOptions';
import { JSEventRegistry } from '../../../src/Services/JSEventRegistry';
import { Blazor } from '../../../src/GlobalExports';

interface InternalServerPause {
  handleServerInitiatedPause(): Promise<void>;
  pause(remote?: boolean): Promise<boolean>;
}

describe('CircuitManager server-initiated pause deferral', () => {
  let options: CircuitStartOptions;
  let circuit: CircuitManager;
  let internal: InternalServerPause;
  let order: string[];

  beforeEach(() => {
    options = resolveOptions({});
    circuit = new CircuitManager({} as never, '', options, { log: () => { /* no-op */ } } as never, new JSEventRegistry());
    internal = circuit as unknown as InternalServerPause;
    order = [];
    // The package-provided pause hook is absent in these tests, so the handler falls back
    // to the circuit's own pause(); stub it to record ordering instead of touching SignalR.
    internal.pause = () => {
      order.push('pause');
      return Promise.resolve(true);
    };
    delete (Blazor as unknown as Record<string, unknown>).pauseCircuit;
  });

  afterEach(() => {
    delete (Blazor as unknown as Record<string, unknown>).pauseCircuit;
  });

  test('awaits onPauseRequested to completion before pausing', async () => {
    let deferCompleted = false;
    options.onPauseRequested = async () => {
      order.push('defer-start');
      await Promise.resolve();
      deferCompleted = true;
      order.push('defer-end');
    };

    await internal.handleServerInitiatedPause();

    expect(deferCompleted).toBe(true);
    expect(order).toEqual([
      'defer-start',
      'defer-end',
      'pause',
    ]);
  });

  test('still pauses when no onPauseRequested deferral is registered', async () => {
    expect(options.onPauseRequested).toBeUndefined();

    await internal.handleServerInitiatedPause();

    expect(order).toEqual(['pause']);
  });

  test('routes the pause through Blazor.pauseCircuit when present', async () => {
    (Blazor as unknown as Record<string, unknown>).pauseCircuit = () => {
      order.push('pauseCircuit');
      return Promise.resolve(true);
    };
    options.onPauseRequested = () => {
      order.push('defer');
      return Promise.resolve();
    };

    await internal.handleServerInitiatedPause();

    expect(order).toEqual(['defer', 'pauseCircuit']);
  });
});
