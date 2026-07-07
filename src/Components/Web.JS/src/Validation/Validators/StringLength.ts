// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ValidationContext, ValidationResult, Validator, pass, fail } from '../ValidationTypes';

// Validates string length against min and/or max bounds (inclusive).
// Used for 'length' ([StringLength]), 'minlength' ([MinLength]), and 'maxlength' ([MaxLength]) rules.
// Throws if neither bound is defined (rule has no effect).
export const stringLengthValidator: Validator = (context: ValidationContext): ValidationResult => {
  const { value, params } = context;
  if (!params.min && !params.max) {
    throw new Error('length/minlength/maxlength validator requires at least one of "min" or "max" parameters.');
  }

  if (!value) {
    return pass();
  }

  if (params.min) {
    const min = parseInt(params['min'], 10);
    if (value.length < min) {
      return fail();
    }
  }

  if (params.max) {
    const max = parseInt(params['max'], 10);
    if (value.length > max) {
      return fail();
    }
  }

  return pass();
};
