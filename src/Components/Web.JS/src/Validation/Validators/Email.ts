// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ValidationContext, ValidationResult, Validator, pass, fail } from '../ValidationTypes';

// Validates email format with the same semantics as .NET's EmailAddressAttribute:
// no CR/LF, and exactly one '@' that is neither the first nor the last character.
// Matching the server-side rule guarantees the client never rejects a value the
// server would accept (and vice versa).
// Source: https://github.com/dotnet/runtime/blob/main/src/libraries/System.ComponentModel.Annotations/src/System/ComponentModel/DataAnnotations/EmailAddressAttribute.cs
export const emailValidator: Validator = (context: ValidationContext): ValidationResult => {
  const { value } = context;
  if (!value) {
    return pass();
  }

  if (value.includes('\r') || value.includes('\n')) {
    return fail();
  }

  const firstAt = value.indexOf('@');
  const lastAt = value.lastIndexOf('@');
  return (firstAt > 0 && firstAt === lastAt && firstAt !== value.length - 1) ? pass() : fail();
};
