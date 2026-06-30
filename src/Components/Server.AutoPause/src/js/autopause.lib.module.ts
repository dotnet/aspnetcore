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
  if (enabled === undefined) {
    return;
  }
  config = {
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

  if (!config?.enabled) {
    return;
  }

  const mgr = new AutoPauseManager(config, blazor);
  manager = mgr;
  mgr.start();
}

export function afterServerStarted(blazor: BlazorLike): void {
  afterWebStarted(blazor);
}
