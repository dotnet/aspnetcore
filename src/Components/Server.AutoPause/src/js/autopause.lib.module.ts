// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { AutoPauseManager, AutoPauseConfig, BlazorActivityHost } from './AutoPauseManager';

type BlazorLike = BlazorActivityHost;

// The auto-pause configuration arrives as flat server [JsonExtensionData] keys on the
// circuit options, following the existing flat-options convention for browser config.
interface WebStartOptionsLike {
  circuit?: Record<string, unknown>;
}

type ServerStartOptionsLike = Record<string, unknown>;

let config: AutoPauseConfig | undefined;
let manager: AutoPauseManager | undefined;

function configure(options: Record<string, unknown> | undefined): void {
  const enabled = options?.['autoPauseEnabled'] as boolean | undefined;
  if (enabled === undefined) {
    return;
  }
  config = {
    enabled,
    hiddenDelayMilliseconds: options?.['autoPauseHiddenDelayMilliseconds'] as number | undefined ?? 120000,
  };
}

function beforeWebStart(options: WebStartOptionsLike): void {
  configure(options.circuit);
}

function beforeServerStart(options: ServerStartOptionsLike): void {
  configure(options);
}

// Called by the framework once Blazor has started; activates auto-pause when AddAutoPause
// enabled it. A second call disposes the previous manager so listeners never accumulate.
function afterWebStarted(blazor: BlazorLike): void {
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

export { beforeWebStart, beforeServerStart, afterWebStarted, afterWebStarted as afterServerStarted };
