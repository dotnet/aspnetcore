// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ValidationContext, ValidationResult, Validator } from '../ValidationTypes';

// Validates that the value is a parseable number. MVC emits data-val-number for
// float, double, and decimal properties. Blazor uses type="number" for native enforcement instead.
export const numberValidator: Validator = (context: ValidationContext): ValidationResult => {
  const { value } = context;
  if (!value) {
    return true;
  }

  return !isNaN(Number(value));
};
