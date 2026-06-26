// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { expect, test, describe, beforeEach } from '@jest/globals';
import { CircuitManager } from '../../../src/Platform/Circuits/CircuitManager';
import { resolveOptions } from '../../../src/Platform/Circuits/CircuitStartOptions';
import { JSEventRegistry } from '../../../src/Services/JSEventRegistry';

interface InternalActivity {
  trackActivity(): () => void;
  resetActivity(): void;
}

describe('CircuitManager activity tracking', () => {
  let registry: JSEventRegistry;
  let events: boolean[];
  let internal: InternalActivity;

  beforeEach(() => {
    registry = new JSEventRegistry();
    events = [];
    registry.addEventListener('circuitactivitychanged', ev => events.push(ev.busy));
    const options = resolveOptions({});
    const circuit = new CircuitManager({} as never, '', options, { log: () => { /* no-op */ } } as never, registry);
    internal = circuit as unknown as InternalActivity;
  });

  test('dispatches busy=true on the first operation and busy=false when the last completes', () => {
    const untrack = internal.trackActivity();
    expect(events).toEqual([true]);
    untrack();
    expect(events).toEqual([true, false]);
  });

  test('only the edges dispatch for nested/overlapping operations', () => {
    const a = internal.trackActivity();
    const b = internal.trackActivity();
    expect(events).toEqual([true]);
    a();
    expect(events).toEqual([true]);
    b();
    expect(events).toEqual([true, false]);
  });

  test('a release closure is idempotent', () => {
    const untrack = internal.trackActivity();
    untrack();
    untrack();
    expect(events).toEqual([true, false]);
  });

  test('resetActivity drains a never-completing operation and dispatches idle', () => {
    internal.trackActivity();
    expect(events).toEqual([true]);
    internal.resetActivity();
    expect(events).toEqual([true, false]);
  });

  test('resetActivity with no active operations does not dispatch', () => {
    internal.resetActivity();
    expect(events).toEqual([]);
  });

  test('a stale release from a previous connection does not corrupt the reconnected circuit', () => {
    // An operation from the old connection is in flight; keep its untrack closure.
    const staleUntrack = internal.trackActivity();
    expect(events).toEqual([true]);

    // The connection drops: onclose -> resetActivity clears trackers and reports idle.
    internal.resetActivity();
    expect(events).toEqual([true, false]);

    // After reconnect, a brand-new operation starts a fresh busy cycle.
    const newUntrack = internal.trackActivity();
    expect(events).toEqual([true, false, true]);

    // The old connection's stream finally errors/completes and fires its stale untrack.
    // Its token was already cleared by resetActivity, so it must NOT dispatch a spurious
    // idle nor remove a token belonging to the still-busy reconnected circuit.
    staleUntrack();
    expect(events).toEqual([true, false, true]);

    // Only the genuine new operation completing reports idle.
    newUntrack();
    expect(events).toEqual([true, false, true, false]);
  });
});
