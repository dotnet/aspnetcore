// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { expect, test } from '@jest/globals';
import { beginHeadStylesheetTransition, deferHeadStylesheetRemoval, endHeadStylesheetTransition } from '../src/Rendering/HeadStylesheetTransition';

test('nested batches preserve outer deferred stylesheet removals', () => {
  const outer = document.createElement('link');
  outer.rel = 'stylesheet';
  outer.id = 'outer';
  const inner = document.createElement('link');
  inner.rel = 'stylesheet';
  document.head.append(outer, inner);

  try {
    beginHeadStylesheetTransition();
    expect(deferHeadStylesheetRemoval(outer)).toBe(true);
    beginHeadStylesheetTransition();
    expect(deferHeadStylesheetRemoval(inner)).toBe(true);
    endHeadStylesheetTransition();
    expect(inner.isConnected).toBe(false);
    expect(outer.isConnected).toBe(true);
    endHeadStylesheetTransition();
    expect(outer.isConnected).toBe(false);
  } finally {
    outer.remove();
    inner.remove();
  }
});
