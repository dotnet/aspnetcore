// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { expect, jest, test, describe, beforeEach, afterEach } from '@jest/globals';
import { AutoPauseManager } from '../../../src/Platform/Circuits/AutoPauseManager';
import { AutoPauseOptions } from '../../../src/Platform/Circuits/CircuitStartOptions';
import { NullLogger } from '../../../src/Platform/Logging/Loggers';

type PauseRequestedCallback = (signal: AbortSignal) => void | Promise<void>;

function setVisibility(state: 'visible' | 'hidden'): void {
  Object.defineProperty(document, 'visibilityState', {
    configurable: true,
    get: () => state,
  });
  document.dispatchEvent(new Event('visibilitychange'));
}

async function flushPromises(): Promise<void> {
  // Yield several microtask turns so chained `await`s inside the manager resolve
  // even while fake timers are active.
  for (let i = 0; i < 10; i++) {
    await Promise.resolve();
  }
}

describe('AutoPauseManager', () => {
  const defaultOptions: AutoPauseOptions = { enabled: true, hiddenDelayMilliseconds: 1000 };

  let pauseCircuit: jest.Mock<() => Promise<boolean>>;
  let resumeCircuit: jest.Mock<() => Promise<boolean>>;
  let manager: AutoPauseManager | undefined;

  beforeEach(() => {
    jest.useFakeTimers();
    setVisibility('visible');
    pauseCircuit = jest.fn<() => Promise<boolean>>().mockResolvedValue(true);
    resumeCircuit = jest.fn<() => Promise<boolean>>().mockResolvedValue(true);
    manager = undefined;
  });

  afterEach(() => {
    manager?.dispose();
    jest.useRealTimers();
  });

  function create(
    onPauseRequested?: PauseRequestedCallback,
    options: AutoPauseOptions = defaultOptions,
  ): AutoPauseManager {
    manager = new AutoPauseManager(options, pauseCircuit, resumeCircuit, NullLogger.instance);
    if (onPauseRequested) {
      manager.register(onPauseRequested);
    }
    return manager;
  }

  test('does not pause while page is visible', async () => {
    create();
    jest.advanceTimersByTime(5000);
    await flushPromises();
    expect(pauseCircuit).not.toHaveBeenCalled();
  });

  test('pauses after the configured hidden delay', async () => {
    create();
    setVisibility('hidden');

    jest.advanceTimersByTime(999);
    await flushPromises();
    expect(pauseCircuit).not.toHaveBeenCalled();

    jest.advanceTimersByTime(1);
    await flushPromises();
    expect(pauseCircuit).toHaveBeenCalledTimes(1);
  });

  test('does not pause when page becomes visible before delay elapses', async () => {
    create();
    setVisibility('hidden');
    jest.advanceTimersByTime(500);
    setVisibility('visible');
    jest.advanceTimersByTime(5000);
    await flushPromises();
    expect(pauseCircuit).not.toHaveBeenCalled();
  });

  test('respects a custom hiddenDelayMilliseconds', async () => {
    create(undefined, { enabled: true, hiddenDelayMilliseconds: 50 });
    setVisibility('hidden');
    jest.advanceTimersByTime(50);
    await flushPromises();
    expect(pauseCircuit).toHaveBeenCalledTimes(1);
  });

  test('awaits onPauseRequested before pausing', async () => {
    const order: string[] = [];
    const onPauseRequested: PauseRequestedCallback = async () => {
      order.push('callback-start');
      await Promise.resolve();
      order.push('callback-end');
    };
    pauseCircuit.mockImplementation(async () => {
      order.push('pause');
      return true;
    });

    create(onPauseRequested);
    setVisibility('hidden');
    jest.advanceTimersByTime(1000);
    await flushPromises();
    await flushPromises();

    expect(order).toEqual(['callback-start', 'callback-end', 'pause']);
  });

  test('passes an AbortSignal to onPauseRequested', async () => {
    const onPauseRequested = jest.fn<PauseRequestedCallback>(async () => { /* noop */ });
    create(onPauseRequested);
    setVisibility('hidden');
    jest.advanceTimersByTime(1000);
    await flushPromises();

    expect(onPauseRequested).toHaveBeenCalledTimes(1);
    const [signal] = onPauseRequested.mock.calls[0];
    expect(signal).toBeInstanceOf(AbortSignal);
  });

  test('unregister prevents handler from being called on subsequent pause', async () => {
    const handler = jest.fn<PauseRequestedCallback>(async () => { /* noop */ });
    create();
    manager!.register(handler);

    // First pause: handler is called
    setVisibility('hidden');
    jest.advanceTimersByTime(1000);
    await flushPromises();
    expect(handler).toHaveBeenCalledTimes(1);

    // Resume and unregister
    setVisibility('visible');
    await flushPromises();
    manager!.unregister(handler);

    // Second pause: handler is NOT called
    setVisibility('hidden');
    jest.advanceTimersByTime(1000);
    await flushPromises();
    expect(handler).toHaveBeenCalledTimes(1);
  });

  test('aborts the signal and skips pausing if page becomes visible during callback', async () => {
    let capturedSignal: AbortSignal | undefined;
    let resolveCallback: (() => void) | undefined;
    const callbackPromise = new Promise<void>(r => { resolveCallback = r; });
    const onPauseRequested: PauseRequestedCallback = (signal) => {
      capturedSignal = signal;
      return callbackPromise;
    };

    create(onPauseRequested);
    setVisibility('hidden');
    jest.advanceTimersByTime(1000);
    await flushPromises();

    setVisibility('visible');
    expect(capturedSignal?.aborted).toBe(true);

    resolveCallback!();
    await flushPromises();

    expect(pauseCircuit).not.toHaveBeenCalled();
  });

  test('resumes circuit when page becomes visible after auto-pause', async () => {
    create();
    setVisibility('hidden');
    jest.advanceTimersByTime(1000);
    await flushPromises();
    expect(pauseCircuit).toHaveBeenCalledTimes(1);

    setVisibility('visible');
    await flushPromises();
    expect(resumeCircuit).toHaveBeenCalledTimes(1);
  });

  test('does not resume when visibility flips but no auto-pause has happened', async () => {
    create();
    setVisibility('hidden');
    setVisibility('visible');
    await flushPromises();
    expect(resumeCircuit).not.toHaveBeenCalled();
  });

  test('handles rapid hide/show cycles without pausing', async () => {
    create();
    for (let i = 0; i < 5; i++) {
      setVisibility('hidden');
      jest.advanceTimersByTime(100);
      setVisibility('visible');
      jest.advanceTimersByTime(100);
    }
    await flushPromises();
    expect(pauseCircuit).not.toHaveBeenCalled();
    expect(resumeCircuit).not.toHaveBeenCalled();
  });

  test('starts hidden timer immediately if page starts hidden', async () => {
    setVisibility('hidden');
    create();
    jest.advanceTimersByTime(1000);
    await flushPromises();
    expect(pauseCircuit).toHaveBeenCalledTimes(1);
  });

  test('dispose stops the hidden timer and prevents pausing', async () => {
    create();
    setVisibility('hidden');
    manager!.dispose();
    jest.advanceTimersByTime(5000);
    await flushPromises();
    expect(pauseCircuit).not.toHaveBeenCalled();
  });

  test('dispose aborts an in-flight onPauseRequested', async () => {
    let capturedSignal: AbortSignal | undefined;
    let resolveCallback: (() => void) | undefined;
    const callbackPromise = new Promise<void>(r => { resolveCallback = r; });
    const onPauseRequested: PauseRequestedCallback = (signal) => {
      capturedSignal = signal;
      return callbackPromise;
    };

    create(onPauseRequested);
    setVisibility('hidden');
    jest.advanceTimersByTime(1000);
    await flushPromises();

    manager!.dispose();
    expect(capturedSignal?.aborted).toBe(true);

    resolveCallback!();
    await flushPromises();
    expect(pauseCircuit).not.toHaveBeenCalled();
  });

  test('does not double-pause if visibility flips while pause is in flight', async () => {
    let resolvePause: ((value: boolean) => void) | undefined;
    pauseCircuit.mockImplementation(() => new Promise<boolean>(r => { resolvePause = r; }));

    create();
    setVisibility('hidden');
    jest.advanceTimersByTime(1000);
    await flushPromises();
    expect(pauseCircuit).toHaveBeenCalledTimes(1);

    setVisibility('visible');
    setVisibility('hidden');
    jest.advanceTimersByTime(1000);
    await flushPromises();

    expect(pauseCircuit).toHaveBeenCalledTimes(1);

    resolvePause!(true);
    await flushPromises();
  });

  test('auto-resumes when tab becomes visible during in-flight pauseCircuit (ghost-pause race)', async () => {
    let resolvePause: ((value: boolean) => void) | undefined;
    pauseCircuit.mockImplementation(() => new Promise<boolean>(r => { resolvePause = r; }));

    create();
    setVisibility('hidden');
    jest.advanceTimersByTime(1000);
    await flushPromises();
    expect(pauseCircuit).toHaveBeenCalledTimes(1);

    // Tab becomes visible while pauseCircuit is still in-flight.
    // onVisibilityChanged fires but _autoPaused is false, so it can't resume yet.
    setVisibility('visible');
    await flushPromises();
    expect(resumeCircuit).not.toHaveBeenCalled();

    // pauseCircuit completes — the manager must detect the tab is visible and auto-resume.
    resolvePause!(true);
    await flushPromises();
    expect(resumeCircuit).toHaveBeenCalledTimes(1);
  });
});
