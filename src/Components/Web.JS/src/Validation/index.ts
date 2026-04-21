// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
