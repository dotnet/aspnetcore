// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Standalone bundle entry point. Creates the validation service on DOMContentLoaded
// and exposes it as window.__aspnetValidation for custom validator registration.

import { createValidationService } from './ValidationService';

function initialize(): void {
  const service = createValidationService();

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  (window as any).__aspnetValidation = service;
}

if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', initialize);
} else {
  initialize();
}
