// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ValidationContext, ValidationResult, Validator, pass, fail } from '../ValidationTypes';

// Validates URL format with the same semantics as .NET's UrlAttribute (string overload):
// the value must start with "http://", "https://", or "ftp://" (case-insensitive).
// Matching the server-side rule guarantees the client never rejects a value the
// server would accept (and vice versa). This intentionally accepts hostnames
// like "http://localhost" and even values with embedded whitespace - the server
// would too.
// Source: https://github.com/dotnet/runtime/blob/main/src/libraries/System.ComponentModel.Annotations/src/System/ComponentModel/DataAnnotations/UrlAttribute.cs
const urlPrefix = /^(?:https?|ftp):\/\//i;

export const urlValidator: Validator = (context: ValidationContext): ValidationResult => {
  const { value } = context;
  if (!value) {
    return pass();
  }

  return urlPrefix.test(value) ? pass() : fail();
};
