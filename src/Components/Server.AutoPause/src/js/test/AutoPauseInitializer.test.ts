// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { beforeWebStart, afterWebStarted } from '../autopause.lib.module';
import { BlazorActivityHost } from '../AutoPauseManager';

type ActivityHandler = (ev: { busy: boolean }) => void;

interface TrackingBlazor extends BlazorActivityHost, Record<string, unknown> {
  added: ActivityHandler[];
  removed: ActivityHandler[];
}

function createBlazor(): TrackingBlazor {
  const added: ActivityHandler[] = [];
  const removed: ActivityHandler[] = [];
  return {
    added,
    removed,
    pauseCircuit: async () => true,
    resumeCircuit: async () => true,
    addEventListener: (_type, handler) => { added.push(handler); },
    removeEventListener: (_type, handler) => { removed.push(handler); },
  } as TrackingBlazor;
}

function enable(blazor: TrackingBlazor): void {
  beforeWebStart({ circuit: { autoPauseEnabled: true, autoPauseHiddenDelayMilliseconds: 100 } });
  afterWebStarted(blazor);
}

describe('autopause initializer', () => {
  it('afterWebStarted starts the manager when enabled', () => {
    const blazor = createBlazor();
    enable(blazor);
    expect(blazor.added).toHaveLength(1);
  });

  it('afterWebStarted does not start when config is disabled', () => {
    const blazor = createBlazor();
    beforeWebStart({ circuit: { autoPauseEnabled: false, autoPauseHiddenDelayMilliseconds: 100 } });
    afterWebStarted(blazor);
    expect(blazor.added).toHaveLength(0);
  });

  it('afterWebStarted does not start when config is absent', () => {
    const blazor = createBlazor();
    beforeWebStart({ circuit: {} });
    afterWebStarted(blazor);
    expect(blazor.added).toHaveLength(0);
  });

  it('does not expose any package-specific global on blazor', () => {
    const blazor = createBlazor();
    enable(blazor);
    // The single public API is the framework's Blazor.pause.waitFor; the package adds nothing.
    expect(blazor.autoPause).toBeUndefined();
  });

  it('a second afterWebStarted disposes the previous manager so listeners do not accumulate', () => {
    const blazor = createBlazor();
    enable(blazor);
    expect(blazor.added).toHaveLength(1);
    expect(blazor.removed).toHaveLength(0);

    // A fresh afterWebStarted must tear down the prior instance, detaching its
    // circuitactivitychanged listener before attaching the new one.
    afterWebStarted(blazor);
    expect(blazor.removed).toEqual([blazor.added[0]]);
    expect(blazor.added).toHaveLength(2);
  });

  it('a disabled second afterWebStarted disposes the previous manager', () => {
    const blazor = createBlazor();
    enable(blazor);
    expect(blazor.added).toHaveLength(1);

    // Restart with auto-pause now disabled: the prior manager must still be torn down so
    // its listeners cannot pause/resume the new circuit unexpectedly.
    beforeWebStart({ circuit: { autoPauseEnabled: false } });
    afterWebStarted(blazor);
    expect(blazor.removed).toEqual([blazor.added[0]]);
  });
});
