// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ValidationContext, ValidationResult, Validator, pass, fail } from '../ValidationTypes';

// Validates that the value matches a regular expression pattern (full match).
// The pattern is anchored with ^(?:...)$ to match .NET's exact-match semantics.
// Throws if the mandatory `pattern` parameter is missing.
export const regexValidator: Validator = (context: ValidationContext): ValidationResult => {
  const { value, params } = context;
  if (!params.pattern) {
    throw new Error('regex validator requires a non-empty "pattern" parameter.');
  }

  if (!value) {
    return pass();
  }

  // Anchor the pattern for full-match semantics, matching .NET's RegularExpressionAttribute
  // which requires Index == 0 && Length == value.Length. The non-capturing group avoids
  // changing semantics for patterns with alternation (e.g. "a|b").
  const anchored = `^(?:${params.pattern})$`;

  try {
    return new RegExp(anchored).test(value) ? pass() : fail();
  } catch {
    return pass();
  }
};
