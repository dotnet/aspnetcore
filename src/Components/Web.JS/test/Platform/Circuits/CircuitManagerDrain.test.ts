// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { expect, test, describe, beforeEach } from '@jest/globals';
import { CircuitManager } from '../../../src/Platform/Circuits/CircuitManager';
import { resolveOptions } from '../../../src/Platform/Circuits/CircuitStartOptions';

async function flushPromises(): Promise<void> {
  for (let i = 0; i < 10; i++) {
    await Promise.resolve();
  }
}

describe('CircuitManager.waitForActiveStreamsToDrain', () => {
  let circuit: CircuitManager;

  beforeEach(() => {
    const options = resolveOptions({});
    circuit = new CircuitManager({} as never, '', options, { log: () => { /* no-op */ } } as never);
  });

  test('resolves when the abort signal fires even if streams are still active', async () => {
    // Simulate an in-flight tracked operation so the drain wait would otherwise block.
    const untrack = (circuit as unknown as { trackActiveStream(): () => void }).trackActiveStream();
    const controller = new AbortController();

    let resolved = false;
    const drainPromise = circuit.waitForActiveStreamsToDrain(controller.signal).then(() => { resolved = true; });
    await flushPromises();
    expect(resolved).toBe(false);

    // Tab became visible again -> auto-pause aborts the drain wait.
    controller.abort();
    await drainPromise;
    expect(resolved).toBe(true);

    // The still-active stream finishing afterwards must not throw or revive anything.
    untrack();
    await flushPromises();
  });

  test('stale untrack from a previous connection does not resolve a new drain wait early', async () => {
    const internal = circuit as unknown as {
      trackActiveStream(): () => void;
      resetActiveStreams(): void;
    };

    // A stream from the old connection is in flight; keep its untrack closure around.
    const staleUntrack = internal.trackActiveStream();

    // The connection drops: onclose -> resetActiveStreams clears the tracker set and
    // resolves any pending drain waiters. The old stream's untrack closure survives
    // because it is held by the SignalR stream subscription, not by the cleared maps.
    internal.resetActiveStreams();

    // After reconnecting, a brand-new tracked operation starts and auto-pause begins
    // a fresh drain wait that must block until that new operation completes.
    const newUntrack = internal.trackActiveStream();

    let resolved = false;
    const drainPromise = circuit.waitForActiveStreamsToDrain().then(() => { resolved = true; });
    await flushPromises();
    expect(resolved).toBe(false);

    // The old connection's stream finally errors/completes and fires its stale untrack.
    // It removes only its own (already-cleared) token, so it must NOT drain the new wait.
    staleUntrack();
    await flushPromises();
    expect(resolved).toBe(false);

    // Only the genuine new operation completing should drain.
    newUntrack();
    await drainPromise;
    expect(resolved).toBe(true);
  });

  test('a never-completing tracker from a previous connection does not block a fresh drain', async () => {
    const internal = circuit as unknown as {
      trackActiveStream(): () => void;
      resetActiveStreams(): void;
    };

    // An old-connection stream is in flight and will never fire its untrack (the connection
    // died before the stream completed). Without clearing the tracker set on reset, this
    // stale tracker would keep the set non-empty and hang every future drain wait.
    internal.trackActiveStream();

    internal.resetActiveStreams();

    let resolved = false;
    circuit.waitForActiveStreamsToDrain().then(() => { resolved = true; });
    await flushPromises();
    expect(resolved).toBe(true);
  });
});
