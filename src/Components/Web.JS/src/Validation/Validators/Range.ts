// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ValidationContext, ValidationResult, Validator, pass, fail } from '../ValidationTypes';

// Validates that a numeric value falls within min/max bounds (inclusive).
// Non-numeric values fail. Uses Number() for parsing.
// Throws if neither bound is defined (rule has no effect).
export const rangeValidator: Validator = (context: ValidationContext): ValidationResult => {
  const { value, params } = context;
  if (params.min === undefined && params.max === undefined) {
    throw new Error('Range validator requires at least one of "min" or "max" parameters.');
  }

  if (!value) {
    return pass();
  }

  const num = Number(value);
  if (isNaN(num)) {
    return fail();
  }

  if (params.min !== undefined) {
    if (num < Number(params.min)) {
      return fail();
    }
  }

  if (params.max !== undefined) {
    if (num > Number(params.max)) {
      return fail();
    }
  }

  return pass();
};
