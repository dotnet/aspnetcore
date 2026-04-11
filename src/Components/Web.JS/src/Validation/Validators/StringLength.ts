// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ValidationContext, ValidationResult, Validator } from '../ValidationTypes';

// Validates string length against min and/or max bounds (inclusive).
// Used for 'length' ([StringLength]), 'minlength' ([MinLength]), and 'maxlength' ([MaxLength]) rules.
export const stringLengthValidator: Validator = (context: ValidationContext): ValidationResult => {
  const { value, params } = context;
  if (!value) {
    return true;
  }

  if (params.min) {
    const min = parseInt(params['min'], 10);
    if (value.length < min) {
      return false;
    }
  }

  if (params.max) {
    const max = parseInt(params['max'], 10);
    if (value.length > max) {
      return false;
    }
  }

  return true;
};
