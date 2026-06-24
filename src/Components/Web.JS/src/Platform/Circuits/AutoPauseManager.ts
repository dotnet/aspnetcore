// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Logger, LogLevel } from '../Logging/Logger';
import { AutoPauseOptions } from './CircuitStartOptions';
import { isFocusedElementEdited } from '../../Rendering/DomFocus';
import { isMediaPlaying, isPictureInPictureActive, queryWebLockHeld } from '../../Rendering/FreezeBlockers';

const becameVisibleReason = Symbol('auto-pause:became-visible');

export class AutoPauseManager {
  private readonly _options: AutoPauseOptions;

  private readonly _pauseHandlers = new Set<(signal: AbortSignal) => void | Promise<void>>();

  private readonly _pauseCircuit: () => Promise<boolean>;

  private readonly _resumeCircuit: () => Promise<boolean>;

  private readonly _waitForActiveStreamsToDrain?: (signal: AbortSignal) => Promise<void>;

  private readonly _logger: Logger;

  private readonly _visibilityListener: () => void;

  private _hiddenTimerId: ReturnType<typeof setTimeout> | undefined;

  private _activeAbortController: AbortController | undefined;

  private _pauseInFlight = false;

  private _autoPaused = false;

  private _disposed = false;

  public constructor(
    options: AutoPauseOptions,
    pauseCircuit: () => Promise<boolean>,
    resumeCircuit: () => Promise<boolean>,
    logger: Logger,
    waitForActiveStreamsToDrain?: (signal: AbortSignal) => Promise<void>,
  ) {
    this._options = options;
    this._pauseCircuit = pauseCircuit;
    this._resumeCircuit = resumeCircuit;
    this._logger = logger;
    this._waitForActiveStreamsToDrain = waitForActiveStreamsToDrain;

    this._visibilityListener = () => this.onVisibilityChanged();
    document.addEventListener('visibilitychange', this._visibilityListener);

    if (document.visibilityState === 'hidden') {
      this.startHiddenTimer();
    }
  }

  public register(handler: (signal: AbortSignal) => void | Promise<void>): void {
    this._pauseHandlers.add(handler);
  }

  public unregister(handler: (signal: AbortSignal) => void | Promise<void>): void {
    this._pauseHandlers.delete(handler);
  }

  public async invokeHandlers(signal?: AbortSignal): Promise<void> {
    if (this._pauseInFlight && !signal) {
      return;
    }
    if (this._pauseHandlers.size > 0) {
      if (!signal) {
        this._activeAbortController?.abort('superseded by new pause request');
        const controller = new AbortController();
        this._activeAbortController = controller;
        signal = controller.signal;
      }
      await Promise.all([...this._pauseHandlers].map(fn => Promise.resolve(fn(signal!))));
    }
  }

  public dispose(): void {
    if (this._disposed) {
      return;
    }
    this._disposed = true;
    document.removeEventListener('visibilitychange', this._visibilityListener);
    this.clearHiddenTimer();
    this._activeAbortController?.abort();
    this._activeAbortController = undefined;
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
    this._logger.log(LogLevel.Trace, `Auto-pause: hidden timer started (${this._options.hiddenDelayMilliseconds}ms).`);
    this._hiddenTimerId = setTimeout(() => {
      this._hiddenTimerId = undefined;
      this.pauseNow();
    }, this._options.hiddenDelayMilliseconds);
  }

  private clearHiddenTimer(): void {
    if (this._hiddenTimerId !== undefined) {
      clearTimeout(this._hiddenTimerId);
      this._hiddenTimerId = undefined;
    }
  }

  private async pauseNow(): Promise<void> {
    if (this._disposed || document.visibilityState !== 'hidden' || this._autoPaused || this._pauseInFlight) {
      return;
    }

    const controller = new AbortController();
    this._activeAbortController = controller;
    this._pauseInFlight = true;

    try {
      if (!(await this.deferIfBlocked(
        controller, isFocusedElementEdited,
        'Pause deferred: waiting for edited element to lose focus.',
        'Pause resumed: tab became visible before focus cleared.'))) {
        return;
      }

      if (!(await this.deferIfBlocked(
        controller, isMediaPlaying,
        'Pause deferred: media playing.',
        'Pause resumed: tab became visible before media stopped.'))) {
        return;
      }

      if (!(await this.deferIfBlocked(
        controller, isPictureInPictureActive,
        'Pause deferred: picture-in-picture active.',
        'Pause resumed: tab became visible before picture-in-picture closed.',
        notify => {
          const handler = () => notify();
          document.addEventListener('leavepictureinpicture', handler, true);
          return () => document.removeEventListener('leavepictureinpicture', handler, true);
        }))) {
        return;
      }

      if (!(await this.deferIfBlocked(
        controller, queryWebLockHeld,
        'Pause deferred: web lock held.',
        'Pause resumed: tab became visible before web lock released.'))) {
        return;
      }

      await this.invokeHandlers(controller.signal);

      if (controller.signal.aborted || document.visibilityState !== 'hidden' || this._disposed) {
        this._logger.log(LogLevel.Trace, 'Auto-pause: aborted before pausing (tab became visible).');
        return;
      }

      if (this._waitForActiveStreamsToDrain) {
        await this._waitForActiveStreamsToDrain(controller.signal);

        if (controller.signal.aborted || document.visibilityState !== 'hidden' || this._disposed) {
          this._logger.log(LogLevel.Trace, 'Auto-pause: aborted before pausing (tab became visible during drain).');
          return;
        }
      }

      const paused = await this._pauseCircuit();
      if (paused) {
        this._autoPaused = true;
        this._logger.log(LogLevel.Information, 'Auto-pause: circuit paused after hidden timeout.');

        if (document.visibilityState !== 'hidden') {
          this._autoPaused = false;
          this.resumeNow();
        }
      }
    } catch (error) {
      this._logger.log(LogLevel.Error, `Auto-pause: failed to pause circuit: ${error}`);
    } finally {
      this._pauseInFlight = false;
      if (this._activeAbortController === controller) {
        this._activeAbortController = undefined;
      }
    }
  }

  private async resumeNow(): Promise<void> {
    try {
      await this._resumeCircuit();
      this._logger.log(LogLevel.Information, 'Auto-pause: circuit resumed after tab became visible.');
    } catch (error) {
      this._logger.log(LogLevel.Error, `Auto-pause: failed to resume circuit: ${error}`);
    }
  }

  private async deferIfBlocked(
    controller: AbortController,
    isBlocked: () => boolean | Promise<boolean>,
    deferLog: string,
    resumeLog: string,
    subscribeClearEvent?: (notify: () => void) => () => void,
  ): Promise<boolean> {
    if (controller.signal.aborted) {
      return false;
    }
    if (!(await isBlocked())) {
      return !controller.signal.aborted;
    }

    this._logger.log(LogLevel.Information, deferLog);

    const cleared = await new Promise<boolean>(resolve => {
      const pollIntervalMs = 500;
      let pollId: ReturnType<typeof setInterval> | undefined;
      let unsubscribe: (() => void) | undefined;

      const cleanup = (didClear: boolean) => {
        controller.signal.removeEventListener('abort', onAbort);
        unsubscribe?.();
        if (pollId !== undefined) {
          clearInterval(pollId);
        }
        resolve(didClear);
      };
      const onAbort = () => {
        if (controller.signal.reason === becameVisibleReason) {
          this._logger.log(LogLevel.Information, resumeLog);
        }
        cleanup(false);
      };
      controller.signal.addEventListener('abort', onAbort);

      const checkAndResolve = async () => {
        if (!(await isBlocked())) {
          cleanup(true);
        }
      };

      if (subscribeClearEvent) {
        unsubscribe = subscribeClearEvent(() => { checkAndResolve(); });
      } else {
        pollId = setInterval(() => { checkAndResolve(); }, pollIntervalMs);
      }
    });

    return cleared;
  }
}
