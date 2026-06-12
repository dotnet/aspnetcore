// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Logger, LogLevel } from '../Logging/Logger';
import { AutoPauseOptions, CircuitStartOptions } from './CircuitStartOptions';

export class AutoPauseManager {
  private readonly _options: AutoPauseOptions;

  private readonly _onPauseRequested?: CircuitStartOptions['onPauseRequested'];

  private readonly _pauseCircuit: () => Promise<boolean>;

  private readonly _resumeCircuit: () => Promise<boolean>;

  private readonly _logger: Logger;

  private readonly _visibilityListener: () => void;

  private _hiddenTimerId: ReturnType<typeof setTimeout> | undefined;

  private _activeAbortController: AbortController | undefined;

  private _pauseInFlight = false;

  private _autoPaused = false;

  private _disposed = false;

  public constructor(
    options: AutoPauseOptions,
    onPauseRequested: CircuitStartOptions['onPauseRequested'],
    pauseCircuit: () => Promise<boolean>,
    resumeCircuit: () => Promise<boolean>,
    logger: Logger,
  ) {
    this._options = options;
    this._onPauseRequested = onPauseRequested;
    this._pauseCircuit = pauseCircuit;
    this._resumeCircuit = resumeCircuit;
    this._logger = logger;

    this._visibilityListener = () => this.onVisibilityChanged();
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
      this._activeAbortController?.abort();
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
      if (this._onPauseRequested) {
        await this._onPauseRequested(controller.signal);
      }

      // The user may have returned to the tab while the callback was running;
      // skip the pause in that case.
      if (controller.signal.aborted || document.visibilityState !== 'hidden' || this._disposed) {
        this._logger.log(LogLevel.Trace, 'Auto-pause: aborted before pausing (tab became visible).');
        return;
      }

      const paused = await this._pauseCircuit();
      if (paused) {
        this._autoPaused = true;
        this._logger.log(LogLevel.Information, 'Auto-pause: circuit paused after hidden timeout.');
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
}
