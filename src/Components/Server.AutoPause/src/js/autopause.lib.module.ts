// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { AutoPauseManager, AutoPauseConfig, BlazorActivityHost } from './AutoPauseManager';

// Minimal shape of the Blazor global passed to afterWebStarted / afterServerStarted.
type BlazorLike = BlazorActivityHost;

// Minimal shape of the start options passed to beforeWebStart. The auto-pause
// configuration arrives as server [JsonExtensionData] spread onto the circuit options.
interface WebStartOptionsLike {
  circuit?: Record<string, unknown>;
}

let config: AutoPauseConfig | undefined;
let manager: AutoPauseManager | undefined;

// JS initializer: read the library's slice of the circuit configuration before start.
export function beforeWebStart(options: WebStartOptionsLike): void {
  config = options.circuit?.['autoPause'] as AutoPauseConfig | undefined;
}

// Blazor Server bundle uses the server-specific initializer name.
export function beforeServerStart(options: WebStartOptionsLike): void {
  beforeWebStart(options);
}

// JS initializer: once Blazor has started, wire up auto-pause if it was enabled.
export function afterWebStarted(blazor: BlazorLike): void {
  if (config?.enabled) {
    start(blazor, config);
  }
}

export function afterServerStarted(blazor: BlazorLike): void {
  afterWebStarted(blazor);
}

export interface AutoPauseHandle {
  // Defer or veto an automatic pause; the callback receives an AbortSignal that aborts
  // when the tab becomes visible again.
  waitFor(participant: (signal: AbortSignal) => void | Promise<void>): () => void;
  stop(): void;
}

// Explicit opt-in for developers who prefer `import('autopause.js')` over configuration.
export function start(blazor: BlazorLike, options?: AutoPauseConfig): AutoPauseHandle {
  manager?.dispose();
  const effective: AutoPauseConfig = options ?? config ?? { enabled: true, hiddenDelayMilliseconds: 120000 };
  const mgr = new AutoPauseManager(effective, blazor);
  manager = mgr;
  mgr.start();
  return {
    waitFor: (participant) => {
      mgr.registerPauseHandler(participant);
      return () => mgr.unregisterPauseHandler(participant);
    },
    stop: () => {
      mgr.dispose();
      if (manager === mgr) {
        manager = undefined;
      }
    },
  };
}
