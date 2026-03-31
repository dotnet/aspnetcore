// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { initializeBlazorValidation } from './BlazorWiring';
import { initializeStandaloneValidation } from './StandaloneWiring';

function initialize(): void {
  // Auto-detect: if Blazor runtime is present, use Blazor mode (hooks into enhanced navigation).
  // Otherwise, use standalone mode (for MVC, Razor Pages, or Blazor SSR without blazor.web.js).
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const blazor = (window as any).Blazor;
  if (blazor && typeof blazor.addEventListener === 'function') {
    initializeBlazorValidation();
  } else {
    initializeStandaloneValidation();
  }
}

if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', initialize);
} else {
  initialize();
}
