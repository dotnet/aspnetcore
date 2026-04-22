// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ValidationContext, ValidationResult, Validator } from '../ValidationTypes';

// Validates that a numeric value falls within min/max bounds (inclusive).
// Non-numeric values fail. Uses Number() for parsing.
export const rangeValidator: Validator = (context: ValidationContext): ValidationResult => {
  const { value, params } = context;
  if (!value) {
    return true;
  }

  const num = Number(value);
  if (isNaN(num)) {
    return false;
  }

  if (params.min !== undefined) {
    if (num < Number(params.min)) {
      return false;
    }
  }

  if (params.max !== undefined) {
    if (num > Number(params.max)) {
      return false;
    }
  }

  return true;
};
