// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { isFocusedElementEdited, initEditedTracking } from './DomFocus';
import { isMediaPlaying, isPictureInPictureActive, queryWebLockHeld } from './FreezeBlockers';

export interface AutoPauseConfig {
  enabled: boolean;
  hiddenDelayMilliseconds: number;
}

// Uses the public pause/resume and activity APIs, plus same-version internal circuit disposal.
export interface BlazorActivityHost {
  _internal?: { circuitEvents?: EventTarget };
  pauseCircuit?: (signal?: AbortSignal) => Promise<boolean>;
  resumeCircuit?: () => Promise<boolean>;
  addEventListener?: (type: 'circuitactivitychanged', handler: (ev: { busy: boolean }) => void) => void;
  removeEventListener?: (type: 'circuitactivitychanged', handler: (ev: { busy: boolean }) => void) => void;
}

const becameVisibleReason = Symbol('auto-pause:became-visible');

export class AutoPauseManager {
  private readonly _config: AutoPauseConfig;

  private readonly _blazor: BlazorActivityHost;

  private readonly _visibilityListener: () => void;

  private readonly _activityListener: (ev: { busy: boolean }) => void;

  private readonly _circuitEvents: EventTarget | undefined;

  private readonly _circuitDisposedListener: () => void;

  private _busy = false;

  private _idleResolvers: Array<() => void> = [];

  private _hiddenTimerId: ReturnType<typeof setTimeout> | undefined;

  private _activeAbortController: AbortController | undefined;

  private _pauseInFlight = false;

  private _autoPaused = false;

  private _circuitVersion = 0;

  private _disposed = false;

  public constructor(config: AutoPauseConfig, blazor: BlazorActivityHost) {
    this._config = config;
    this._blazor = blazor;
    this._circuitEvents = blazor._internal?.circuitEvents;

    this._visibilityListener = () => this.onVisibilityChanged();
    this._activityListener = (ev) => this.onActivityChanged(ev.busy);
    this._circuitDisposedListener = () => this.onCircuitDisposed();
  }

  public start(): void {
    initEditedTracking();
    this._blazor.addEventListener?.('circuitactivitychanged', this._activityListener);
    this._circuitEvents?.addEventListener('disposed', this._circuitDisposedListener);
    document.addEventListener('visibilitychange', this._visibilityListener);

    if (document.visibilityState === 'hidden') {
      this.startHiddenTimer();
    }
  }

  public dispose(): void {
    if (this._disposed) {
      return;
    }
    this._disposed = true;
    document.removeEventListener('visibilitychange', this._visibilityListener);
    this._blazor.removeEventListener?.('circuitactivitychanged', this._activityListener);
    this._circuitEvents?.removeEventListener('disposed', this._circuitDisposedListener);
    this.clearHiddenTimer();
    this._activeAbortController?.abort();
    this._activeAbortController = undefined;
    const resolvers = this._idleResolvers.splice(0);
    for (const resolve of resolvers) {
      resolve();
    }
  }

  private onCircuitDisposed(): void {
    this._circuitVersion++;
    this._autoPaused = false;
    // A replacement started while hidden waits for the next visibility change to auto-pause.
    this.clearHiddenTimer();
    this._activeAbortController?.abort();
    this.onActivityChanged(false);
  }

  private onActivityChanged(busy: boolean): void {
    this._busy = busy;
    if (!busy) {
      const resolvers = this._idleResolvers.splice(0);
      for (const resolve of resolvers) {
        resolve();
      }
    }
  }

  // Replaces the circuit's removed waitForActiveStreamsToDrain: resolves when the circuit
  // reports it is idle (or immediately if it already is), honoring `signal` so a visibility
  // change can cancel the wait instead of pausing and then immediately auto-resuming.
  private whenIdle(signal: AbortSignal): Promise<void> {
    if (!this._busy || signal.aborted) {
      return Promise.resolve();
    }
    return new Promise<void>(resolve => {
      const cleanup = () => {
        const idx = this._idleResolvers.indexOf(onIdle);
        if (idx >= 0) {
          this._idleResolvers.splice(idx, 1);
        }
        signal.removeEventListener('abort', onAbort);
      };
      const onIdle = () => {
        cleanup();
        resolve();
      };
      const onAbort = () => {
        cleanup();
        resolve();
      };
      this._idleResolvers.push(onIdle);
      signal.addEventListener('abort', onAbort, { once: true });
    });
  }

  private onVisibilityChanged(): void {
    if (this._disposed) {
      return;
    }

    if (document.visibilityState === 'hidden') {
      this.startHiddenTimer();
    } else {
      // Becoming visible cancels any pending pause and aborts an in-flight
      // wind-down so the developer can short-circuit a slow callback.
      this.clearHiddenTimer();
      this._activeAbortController?.abort(becameVisibleReason);
      this._activeAbortController = undefined;
      if (this._autoPaused) {
        this._autoPaused = false;
        this.resumeNow();
      }
    }
  }

  private startHiddenTimer(): void {
    if (this._hiddenTimerId !== undefined || this._autoPaused || this._pauseInFlight) {
      return;
    }
    this._hiddenTimerId = setTimeout(() => {
      this._hiddenTimerId = undefined;
      this.pauseNow();
    }, this._config.hiddenDelayMilliseconds);
  }

  private clearHiddenTimer(): void {
    if (this._hiddenTimerId !== undefined) {
      clearTimeout(this._hiddenTimerId);
      this._hiddenTimerId = undefined;
    }
  }

  private shouldAbortBeforePausing(controller: AbortController): boolean {
    return controller.signal.aborted
      || document.visibilityState !== 'hidden'
      || this._disposed;
  }

  private async pauseNow(): Promise<void> {
    if (this._disposed || document.visibilityState !== 'hidden' || this._autoPaused || this._pauseInFlight) {
      return;
    }

    const controller = new AbortController();
    const circuitVersion = this._circuitVersion;
    this._activeAbortController = controller;
    this._pauseInFlight = true;

    try {
      if (!(await this.deferIfBlocked(controller, isFocusedElementEdited))) {
        return;
      }

      if (!(await this.deferIfBlocked(controller, isMediaPlaying))) {
        return;
      }

      if (!(await this.deferIfBlocked(controller, isPictureInPictureActive, notify => {
        const handler = () => notify();
        document.addEventListener('leavepictureinpicture', handler, true);
        return () => document.removeEventListener('leavepictureinpicture', handler, true);
      }))) {
        return;
      }

      if (!(await this.deferIfBlocked(controller, queryWebLockHeld))) {
        return;
      }

      await this.whenIdle(controller.signal);

      if (this.shouldAbortBeforePausing(controller)) {
        return;
      }

      const paused = await this._blazor.pauseCircuit?.(controller.signal) ?? false;
      if (paused && circuitVersion === this._circuitVersion) {
        this._autoPaused = true;

        if (document.visibilityState !== 'hidden') {
          this._autoPaused = false;
          this.resumeNow();
        }
      }
    } catch {
      // Pausing is best-effort; failures leave the circuit running.
    } finally {
      this._pauseInFlight = false;
      if (this._activeAbortController === controller) {
        this._activeAbortController = undefined;
      }
      // If the tab is still hidden (e.g. it flipped visible then hidden again during an
      // aborted attempt), reschedule so we don't get stuck unpaused with no timer.
      if (!this._disposed && circuitVersion === this._circuitVersion && !this._autoPaused && document.visibilityState === 'hidden') {
        this.startHiddenTimer();
      }
    }
  }

  private async resumeNow(): Promise<void> {
    const circuitVersion = this._circuitVersion;
    try {
      const resumed = await this._blazor.resumeCircuit?.() ?? false;
      if (!resumed && circuitVersion === this._circuitVersion) {
        // Resume was rejected; keep treating the circuit as paused so a later visible
        // transition retries.
        this._autoPaused = true;
      }
    } catch {
      if (circuitVersion === this._circuitVersion) {
        this._autoPaused = true;
      }
    }
  }

  private async deferIfBlocked(
    controller: AbortController,
    isBlocked: () => boolean | Promise<boolean>,
    subscribeClearEvent?: (notify: () => void) => () => void,
  ): Promise<boolean> {
    if (controller.signal.aborted) {
      return false;
    }
    if (!(await isBlocked())) {
      return !controller.signal.aborted;
    }
    if (controller.signal.aborted) {
      return false;
    }

    const cleared = await new Promise<boolean>(resolve => {
      const pollIntervalMs = 500;
      let pollId: ReturnType<typeof setInterval> | undefined;
      let unsubscribe: (() => void) | undefined;

      const cleanup = () => {
        controller.signal.removeEventListener('abort', onAbort);
        unsubscribe?.();
        if (pollId !== undefined) {
          clearInterval(pollId);
        }
      };
      const onAbort = () => {
        cleanup();
        resolve(false);
      };
      controller.signal.addEventListener('abort', onAbort);

      const checkAndResolve = async () => {
        if (!(await isBlocked())) {
          cleanup();
          resolve(true);
        }
      };

      if (subscribeClearEvent) {
        unsubscribe = subscribeClearEvent(() => {
          checkAndResolve();
        });
      } else {
        pollId = setInterval(() => {
          checkAndResolve();
        }, pollIntervalMs);
      }
    });

    return cleared;
  }
}
