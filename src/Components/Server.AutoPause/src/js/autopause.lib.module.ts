// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { AutoPauseManager, AutoPauseConfig, BlazorActivityHost } from './AutoPauseManager';

type BlazorLike = BlazorActivityHost;

// The auto-pause configuration arrives as flat server [JsonExtensionData] keys on the
// circuit options, following the existing flat-options convention for browser config.
interface WebStartOptionsLike {
  circuit?: Record<string, unknown>;
}

let config: AutoPauseConfig | undefined;
let manager: AutoPauseManager | undefined;

export function beforeWebStart(options: WebStartOptionsLike): void {
  const enabled = options.circuit?.['autoPauseEnabled'] as boolean | undefined;
  config = enabled === undefined
    ? undefined
    : {
      enabled,
      hiddenDelayMilliseconds: options.circuit?.['autoPauseHiddenDelayMilliseconds'] as number | undefined ?? 120000,
    };
}

// The Blazor Server bundle uses the server-specific initializer names.
export function beforeServerStart(options: WebStartOptionsLike): void {
  beforeWebStart(options);
}

// Called by the framework once Blazor has started; activates auto-pause when AddAutoPause
// enabled it. A second call disposes the previous manager so listeners never accumulate.
export function afterWebStarted(blazor: BlazorLike): void {
  // Avoid stale listeners on restart.
  manager?.dispose();
  manager = undefined;
  delete (blazor as Record<string, unknown>).autoPause;

  if (!config?.enabled) {
    return;
  }

  const mgr = new AutoPauseManager(config, blazor);
  manager = mgr;
  mgr.start();

  const handle: AutoPauseHandle = {
    waitFor: (participant) => {
      mgr.registerPauseHandler(participant);
      return () => mgr.unregisterPauseHandler(participant);
    },
  };
  (blazor as Record<string, unknown>).autoPause = handle;
}

export function afterServerStarted(blazor: BlazorLike): void {
  afterWebStarted(blazor);
}

export interface AutoPauseHandle {
  // Defer or veto an automatic pause; the callback receives an AbortSignal that aborts
  // when the tab becomes visible again. Returns a function that unregisters the participant.
  waitFor(participant: (signal: AbortSignal) => void | Promise<void>): () => void;
}
