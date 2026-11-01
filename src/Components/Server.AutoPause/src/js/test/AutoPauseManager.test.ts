// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { expect, jest, test, describe, beforeEach, afterEach } from '@jest/globals';
import { AutoPauseManager, AutoPauseConfig, BlazorActivityHost } from '../AutoPauseManager';

function setVisibility(state: 'visible' | 'hidden'): void {
  Object.defineProperty(document, 'visibilityState', {
    configurable: true,
    get: () => state,
  });
  document.dispatchEvent(new Event('visibilitychange'));
}

const realSetTimeout = setTimeout;

async function flushPromises(): Promise<void> {
  // Drain the entire microtask queue (regardless of chained-await depth)
  await new Promise<void>(resolve => realSetTimeout(resolve, 0));
}

describe('AutoPauseManager', () => {
  const defaultConfig: AutoPauseConfig = { enabled: true, hiddenDelayMilliseconds: 1000 };

  let pauseCircuit: jest.Mock<(signal?: AbortSignal) => Promise<boolean>>;
  let resumeCircuit: jest.Mock<() => Promise<boolean>>;
  let activityListener: ((ev: { busy: boolean }) => void) | undefined;
  let host: BlazorActivityHost;
  let manager: AutoPauseManager | undefined;

  beforeEach(() => {
    jest.useFakeTimers();
    setVisibility('visible');
    pauseCircuit = jest.fn<(signal?: AbortSignal) => Promise<boolean>>().mockResolvedValue(true);
    resumeCircuit = jest.fn<() => Promise<boolean>>().mockResolvedValue(true);
    activityListener = undefined;
    host = {
      pauseCircuit,
      resumeCircuit,
      addEventListener: (_type, handler) => { activityListener = handler; },
      removeEventListener: (_type, handler) => { if (activityListener === handler) { activityListener = undefined; } },
    };
    manager = undefined;
  });

  afterEach(() => {
    manager?.dispose();
    jest.useRealTimers();
  });

  function create(config: AutoPauseConfig = defaultConfig): AutoPauseManager {
    manager = new AutoPauseManager(config, host);
    manager.start();
    return manager;
  }

  // Simulate the circuit raising 'circuitactivitychanged' through the Blazor host.
  function setBusy(busy: boolean): void {
    activityListener?.({ busy });
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
    create({ enabled: true, hiddenDelayMilliseconds: 50 });
    setVisibility('hidden');
    jest.advanceTimersByTime(50);
    await flushPromises();
    expect(pauseCircuit).toHaveBeenCalledTimes(1);
  });

  test('zero delay pauses immediately on hide', async () => {
    create({ enabled: true, hiddenDelayMilliseconds: 0 });
    setVisibility('hidden');
    jest.advanceTimersByTime(0);
    await flushPromises();
    expect(pauseCircuit).toHaveBeenCalledTimes(1);
  });

  test('negative delay pauses immediately on hide', async () => {
    create({ enabled: true, hiddenDelayMilliseconds: -1 });
    setVisibility('hidden');
    jest.advanceTimersByTime(0);
    await flushPromises();
    expect(pauseCircuit).toHaveBeenCalledTimes(1);
  });

  test('invokes pauseCircuit with an AbortSignal when the hidden delay elapses', async () => {
    create();
    setVisibility('hidden');
    jest.advanceTimersByTime(1000);
    await flushPromises();

    expect(pauseCircuit).toHaveBeenCalledTimes(1);
    const [signal] = pauseCircuit.mock.calls[0];
    expect(signal).toBeInstanceOf(AbortSignal);
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

  test('aborts the signal handed to pauseCircuit when the page becomes visible mid-pause', async () => {
    let capturedSignal: AbortSignal | undefined;
    let resolvePause: ((value: boolean) => void) | undefined;
    pauseCircuit.mockImplementation((signal) => {
      capturedSignal = signal;
      return new Promise<boolean>(r => { resolvePause = r; });
    });

    create();
    setVisibility('hidden');
    jest.advanceTimersByTime(1000);
    await flushPromises();

    expect(capturedSignal).toBeInstanceOf(AbortSignal);
    expect(capturedSignal!.aborted).toBe(false);

    setVisibility('visible');
    expect(capturedSignal!.aborted).toBe(true);

    resolvePause!(false);
    await flushPromises();
  });

  test('dispose aborts the signal handed to an in-flight pauseCircuit', async () => {
    let capturedSignal: AbortSignal | undefined;
    let resolvePause: ((value: boolean) => void) | undefined;
    pauseCircuit.mockImplementation((signal) => {
      capturedSignal = signal;
      return new Promise<boolean>(r => { resolvePause = r; });
    });

    create();
    setVisibility('hidden');
    jest.advanceTimersByTime(1000);
    await flushPromises();

    expect(capturedSignal).toBeInstanceOf(AbortSignal);
    manager!.dispose();
    expect(capturedSignal!.aborted).toBe(true);

    resolvePause!(false);
    await flushPromises();
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

  describe('circuit-activity drain cancellation', () => {
    test('pauses after the circuit reports idle when the tab stays hidden', async () => {
      create();
      // The circuit is busy with in-flight work when the pause is attempted.
      setBusy(true);

      setVisibility('hidden');
      jest.advanceTimersByTime(1000);
      await flushPromises();

      // The pause must wait for the circuit to become idle.
      expect(pauseCircuit).not.toHaveBeenCalled();

      // Work completes while the tab is still hidden -> the pause proceeds.
      setBusy(false);
      await flushPromises();
      expect(pauseCircuit).toHaveBeenCalledTimes(1);
    });

    test('abandons the pause when the tab becomes visible while waiting for the circuit to drain', async () => {
      create();
      setBusy(true);

      setVisibility('hidden');
      jest.advanceTimersByTime(1000);
      await flushPromises();

      expect(pauseCircuit).not.toHaveBeenCalled();

      // Tab becomes visible mid-drain -> the wait is aborted and the pause is abandoned.
      setVisibility('visible');
      await flushPromises();

      expect(pauseCircuit).not.toHaveBeenCalled();
      // Nothing was paused, so there is nothing to resume either (no modal flicker / churn).
      expect(resumeCircuit).not.toHaveBeenCalled();
    });
  });
});
