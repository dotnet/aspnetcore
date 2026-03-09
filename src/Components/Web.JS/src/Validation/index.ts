// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { initializeBlazorValidation } from './BlazorWiring';

// Auto-initialize when the DOM is ready
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', () => initializeBlazorValidation());
} else {
  initializeBlazorValidation();
}
