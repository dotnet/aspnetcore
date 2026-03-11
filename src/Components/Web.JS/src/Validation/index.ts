// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { initializeBlazorValidation } from './BlazorWiring';
import { initializeMvcValidation } from './MvcWiring';

function initialize(): void {
  // Auto-detect: if Blazor's enhanced navigation is available, use Blazor mode
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const blazor = (window as any).Blazor;
  if (blazor && typeof blazor.addEventListener === 'function') {
    initializeBlazorValidation();
  } else {
    initializeMvcValidation();
  }
}

if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', initialize);
} else {
  initialize();
}
