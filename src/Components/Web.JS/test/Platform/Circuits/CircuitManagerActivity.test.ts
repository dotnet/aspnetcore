// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { expect, test, describe, beforeEach } from '@jest/globals';
import { CircuitManager } from '../../../src/Platform/Circuits/CircuitManager';
import { resolveOptions } from '../../../src/Platform/Circuits/CircuitStartOptions';
import { JSEventRegistry } from '../../../src/Services/JSEventRegistry';

interface InternalActivity {
  changeActivity(delta: number): void;
  handleConnectionUp(): void;
  handleConnectionDown(): void;
}

function newCircuit(registry: JSEventRegistry): InternalActivity {
  const options = resolveOptions({});
  const circuit = new CircuitManager({} as never, '', options, { log: () => { /* no-op */ } } as never, registry);
  return circuit as unknown as InternalActivity;
}

describe('CircuitManager activity tracking', () => {
  let registry: JSEventRegistry;
  let events: boolean[];
  let internal: InternalActivity;

  beforeEach(() => {
    registry = new JSEventRegistry();
    events = [];
    registry.addEventListener('circuitactivitychanged', ev => events.push(ev.busy));
    internal = newCircuit(registry);
    // Activity is only reported while connected; simulate the connection up-edge.
    internal.handleConnectionUp();
  });

  test('dispatches busy=true on the first operation and busy=false when the last completes', () => {
    internal.changeActivity(1);
    expect(events).toEqual([true]);
    internal.changeActivity(-1);
    expect(events).toEqual([true, false]);
  });

  test('only the edges dispatch for nested/overlapping operations', () => {
    internal.changeActivity(1);
    internal.changeActivity(1);
    expect(events).toEqual([true]);
    internal.changeActivity(-1);
    expect(events).toEqual([true]);
    internal.changeActivity(-1);
    expect(events).toEqual([true, false]);
  });

  test('overlapping decrements below zero do not spuriously re-dispatch', () => {
    internal.changeActivity(1);
    internal.changeActivity(-1);
    expect(events).toEqual([true, false]);
    // An extra decrement (e.g. a contract violation) must not dispatch again.
    internal.changeActivity(-1);
    expect(events).toEqual([true, false]);
  });

  test('no activity is reported before the connection up-edge', () => {
    const registry2 = new JSEventRegistry();
    const events2: boolean[] = [];
    registry2.addEventListener('circuitactivitychanged', ev => events2.push(ev.busy));
    const internal2 = newCircuit(registry2);

    // Without a preceding handleConnectionUp(), operations must not dispatch busy.
    internal2.changeActivity(1);
    internal2.changeActivity(-1);
    expect(events2).toEqual([]);
  });

  test('disconnect reports idle for a never-completing operation', () => {
    internal.changeActivity(1);
    expect(events).toEqual([true]);
    internal.handleConnectionDown();
    expect(events).toEqual([true, false]);
  });

  test('disconnect with no active operations does not dispatch', () => {
    internal.handleConnectionDown();
    expect(events).toEqual([]);
  });

  test('a stale stream release that fires while disconnected does not corrupt the reconnected circuit', () => {
    // An operation from the old connection is in flight.
    internal.changeActivity(1);
    expect(events).toEqual([true]);

    // The connection drops: onclose -> handleConnectionDown reports idle. The operation count
    // is intentionally NOT zeroed here.
    internal.handleConnectionDown();
    expect(events).toEqual([true, false]);
    internal.changeActivity(-1);
    expect(events).toEqual([true, false]);

    // Reconnect: the up-edge zeroes the count, discarding anything left over.
    internal.handleConnectionUp();

    // A brand-new operation starts a fresh, correct busy/idle cycle.
    internal.changeActivity(1);
    expect(events).toEqual([true, false, true]);
    internal.changeActivity(-1);
    expect(events).toEqual([true, false, true, false]);
  });

  test('orphaned operations from a dropped connection are discarded on reconnect', () => {
    internal.changeActivity(1);
    expect(events).toEqual([true]);

    internal.handleConnectionDown();
    expect(events).toEqual([true, false]);

    internal.handleConnectionUp();
    internal.changeActivity(1);
    expect(events).toEqual([true, false, true]);
    internal.changeActivity(-1);
    expect(events).toEqual([true, false, true, false]);
  });
});
