// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { expect, test, describe, jest } from '@jest/globals';
import { DefaultAsyncValidationTracker } from '../../src/Validation/AsyncValidationTracker';
import { ValidatableElement, ValidationResult } from '../../src/Validation/ValidationTypes';

function createTracker(): DefaultAsyncValidationTracker {
  return new DefaultAsyncValidationTracker();
}

function makeInput(name: string): HTMLInputElement {
  const el = document.createElement('input');
  el.name = name;
  el.type = 'text';
  return el;
}

function deferred<T>(): { promise: Promise<T>; resolve: (v: T) => void; reject: (e: unknown) => void } {
  let resolve!: (v: T) => void;
  let reject!: (e: unknown) => void;
  const promise = new Promise<T>((res, rej) => { resolve = res; reject = rej; });
  return { promise, resolve, reject };
}

describe('DefaultAsyncValidationTracker', () => {

  // ─────────────────────────────────────────────────────
  //  Basic pending state
  // ─────────────────────────────────────────────────────

  test('hasPending returns false when empty', () => {
    const tracker = createTracker();
    expect(tracker.hasPending()).toBe(false);
  });

  test('tracking a promise makes hasPending return true', () => {
    const tracker = createTracker();
    const input = makeInput('email');
    const { promise } = deferred<ValidationResult>();

    tracker.track(input, 'remote', promise, () => {});

    expect(tracker.hasPending()).toBe(true);
  });

  test('tracking adds validation-pending CSS class', () => {
    const tracker = createTracker();
    const input = makeInput('email');
    const { promise } = deferred<ValidationResult>();

    tracker.track(input, 'remote', promise, () => {});

    expect(input.classList.contains('validation-pending')).toBe(true);
  });

  // ─────────────────────────────────────────────────────
  //  Promise resolution
  // ─────────────────────────────────────────────────────

  test('resolving promise removes pending state', async () => {
    const tracker = createTracker();
    const input = makeInput('email');
    const { promise, resolve } = deferred<ValidationResult>();

    tracker.track(input, 'remote', promise, () => {});
    resolve(true);
    await promise;

    // Let microtask settle
    await new Promise(r => setTimeout(r, 0));

    expect(tracker.hasPending()).toBe(false);
  });

  test('resolving promise removes CSS class', async () => {
    const tracker = createTracker();
    const input = makeInput('email');
    const { promise, resolve } = deferred<ValidationResult>();

    tracker.track(input, 'remote', promise, () => {});
    resolve(true);
    await promise;
    await new Promise(r => setTimeout(r, 0));

    expect(input.classList.contains('validation-pending')).toBe(false);
  });

  test('resolving promise calls onResolved callback', async () => {
    const tracker = createTracker();
    const input = makeInput('email');
    const { promise, resolve } = deferred<ValidationResult>();
    const onResolved = jest.fn();

    tracker.track(input, 'remote', promise, onResolved);
    resolve(true);
    await promise;
    await new Promise(r => setTimeout(r, 0));

    expect(onResolved).toHaveBeenCalledTimes(1);
  });

  test('rejecting promise also clears pending and calls onResolved', async () => {
    const tracker = createTracker();
    const input = makeInput('email');
    const { promise, reject } = deferred<ValidationResult>();
    const onResolved = jest.fn();

    tracker.track(input, 'remote', promise, onResolved);
    reject(new Error('network failure'));
    try { await promise; } catch { /* expected */ }
    await new Promise(r => setTimeout(r, 0));

    expect(tracker.hasPending()).toBe(false);
    expect(input.classList.contains('validation-pending')).toBe(false);
    expect(onResolved).toHaveBeenCalledTimes(1);
  });

  // ─────────────────────────────────────────────────────
  //  Multiple elements
  // ─────────────────────────────────────────────────────

  test('multiple elements tracked independently', async () => {
    const tracker = createTracker();
    const email = makeInput('email');
    const phone = makeInput('phone');
    const d1 = deferred<ValidationResult>();
    const d2 = deferred<ValidationResult>();

    tracker.track(email, 'remote', d1.promise, () => {});
    tracker.track(phone, 'remote', d2.promise, () => {});

    expect(tracker.hasPending()).toBe(true);

    // Resolve first
    d1.resolve(true);
    await d1.promise;
    await new Promise(r => setTimeout(r, 0));

    expect(tracker.hasPending()).toBe(true); // phone still pending
    expect(email.classList.contains('validation-pending')).toBe(false);
    expect(phone.classList.contains('validation-pending')).toBe(true);

    // Resolve second
    d2.resolve(true);
    await d2.promise;
    await new Promise(r => setTimeout(r, 0));

    expect(tracker.hasPending()).toBe(false);
  });

  test('multiple validators per element tracked independently', async () => {
    const tracker = createTracker();
    const input = makeInput('email');
    const d1 = deferred<ValidationResult>();
    const d2 = deferred<ValidationResult>();

    tracker.track(input, 'remote', d1.promise, () => {});
    tracker.track(input, 'uniqueCheck', d2.promise, () => {});

    // Both pending — element should have CSS class
    expect(tracker.hasPending()).toBe(true);
    expect(input.classList.contains('validation-pending')).toBe(true);

    // Resolve first validator
    d1.resolve(true);
    await d1.promise;
    await new Promise(r => setTimeout(r, 0));

    // Still pending (second validator)
    expect(tracker.hasPending()).toBe(true);
    expect(input.classList.contains('validation-pending')).toBe(true);

    // Resolve second
    d2.resolve(true);
    await d2.promise;
    await new Promise(r => setTimeout(r, 0));

    // Now fully clear
    expect(tracker.hasPending()).toBe(false);
    expect(input.classList.contains('validation-pending')).toBe(false);
  });

  // ─────────────────────────────────────────────────────
  //  Version tracking / staleness
  // ─────────────────────────────────────────────────────

  test('re-tracking same element+validator supersedes previous promise', async () => {
    const tracker = createTracker();
    const input = makeInput('email');
    const d1 = deferred<ValidationResult>();
    const d2 = deferred<ValidationResult>();
    const onResolved1 = jest.fn();
    const onResolved2 = jest.fn();

    tracker.track(input, 'remote', d1.promise, onResolved1);
    tracker.track(input, 'remote', d2.promise, onResolved2); // supersedes d1

    // Resolve the OLD promise — should be ignored (stale)
    d1.resolve(true);
    await d1.promise;
    await new Promise(r => setTimeout(r, 0));

    expect(onResolved1).not.toHaveBeenCalled(); // stale, ignored
    expect(tracker.hasPending()).toBe(true); // d2 still pending

    // Resolve the NEW promise — should be accepted
    d2.resolve('error');
    await d2.promise;
    await new Promise(r => setTimeout(r, 0));

    expect(onResolved2).toHaveBeenCalledTimes(1);
    expect(tracker.hasPending()).toBe(false);
  });

  test('CSS class does not flicker when re-tracking same element', () => {
    const tracker = createTracker();
    const input = makeInput('email');
    const d1 = deferred<ValidationResult>();
    const d2 = deferred<ValidationResult>();

    tracker.track(input, 'remote', d1.promise, () => {});

    // Re-track with new promise (simulates typing rapidly)
    tracker.track(input, 'remote', d2.promise, () => {});

    // CSS class should still be present (never removed between tracks)
    expect(input.classList.contains('validation-pending')).toBe(true);
  });

  // ─────────────────────────────────────────────────────
  //  clear()
  // ─────────────────────────────────────────────────────

  test('clear() removes all pending state and CSS', () => {
    const tracker = createTracker();
    const email = makeInput('email');
    const phone = makeInput('phone');

    tracker.track(email, 'remote', deferred<ValidationResult>().promise, () => {});
    tracker.track(phone, 'remote', deferred<ValidationResult>().promise, () => {});

    tracker.clear();

    expect(tracker.hasPending()).toBe(false);
    expect(email.classList.contains('validation-pending')).toBe(false);
    expect(phone.classList.contains('validation-pending')).toBe(false);
  });

  test('clear(element) removes only that element', () => {
    const tracker = createTracker();
    const email = makeInput('email');
    const phone = makeInput('phone');

    tracker.track(email, 'remote', deferred<ValidationResult>().promise, () => {});
    tracker.track(phone, 'remote', deferred<ValidationResult>().promise, () => {});

    tracker.clear(email);

    expect(tracker.hasPending()).toBe(true); // phone still pending
    expect(email.classList.contains('validation-pending')).toBe(false);
    expect(phone.classList.contains('validation-pending')).toBe(true);
  });

  test('clear() causes subsequent stale resolutions to be ignored', async () => {
    const tracker = createTracker();
    const input = makeInput('email');
    const { promise, resolve } = deferred<ValidationResult>();
    const onResolved = jest.fn();

    tracker.track(input, 'remote', promise, onResolved);
    tracker.clear();

    // Resolve after clear — should be ignored
    resolve(true);
    await promise;
    await new Promise(r => setTimeout(r, 0));

    expect(onResolved).not.toHaveBeenCalled();
  });

  // ─────────────────────────────────────────────────────
  //  createSignal()
  // ─────────────────────────────────────────────────────

  test('createSignal returns an AbortSignal', () => {
    const tracker = createTracker();
    const input = makeInput('email');

    const signal = tracker.createSignal(input, 'remote');

    expect(signal).toBeInstanceOf(AbortSignal);
    expect(signal.aborted).toBe(false);
  });

  test('createSignal aborts previous signal for same element+validator', () => {
    const tracker = createTracker();
    const input = makeInput('email');

    const signal1 = tracker.createSignal(input, 'remote');
    const signal2 = tracker.createSignal(input, 'remote');

    expect(signal1.aborted).toBe(true);
    expect(signal2.aborted).toBe(false);
  });

  test('createSignal does not abort signal for different validator', () => {
    const tracker = createTracker();
    const input = makeInput('email');

    const signal1 = tracker.createSignal(input, 'remote');
    const signal2 = tracker.createSignal(input, 'uniqueCheck');

    expect(signal1.aborted).toBe(false);
    expect(signal2.aborted).toBe(false);
  });

  test('createSignal does not abort signal for different element', () => {
    const tracker = createTracker();
    const email = makeInput('email');
    const phone = makeInput('phone');

    const signal1 = tracker.createSignal(email, 'remote');
    const signal2 = tracker.createSignal(phone, 'remote');

    expect(signal1.aborted).toBe(false);
    expect(signal2.aborted).toBe(false);
  });

  test('clear() aborts all outstanding signals', () => {
    const tracker = createTracker();
    const email = makeInput('email');
    const phone = makeInput('phone');

    const signal1 = tracker.createSignal(email, 'remote');
    const signal2 = tracker.createSignal(phone, 'remote');

    tracker.clear();

    expect(signal1.aborted).toBe(true);
    expect(signal2.aborted).toBe(true);
  });

  test('clear(element) aborts only that element signals', () => {
    const tracker = createTracker();
    const email = makeInput('email');
    const phone = makeInput('phone');

    const signal1 = tracker.createSignal(email, 'remote');
    const signal2 = tracker.createSignal(phone, 'remote');

    tracker.clear(email);

    expect(signal1.aborted).toBe(true);
    expect(signal2.aborted).toBe(false);
  });
});
