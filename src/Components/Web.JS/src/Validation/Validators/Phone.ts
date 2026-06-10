// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ValidationContext, ValidationResult, Validator, pass, fail } from '../ValidationTypes';

// Validates phone numbers matching .NET's PhoneAttribute logic.
// Strips leading '+' and trailing extensions (ext./ext/x + digits).
// Requires at least one digit. Only allows digits, whitespace, and: - . ( )
export const phoneValidator: Validator = (context: ValidationContext): ValidationResult => {
  const { value } = context;
  if (!value) {
    return pass();
  }

  // Strip leading '+' (international prefix)
  let phone = value.startsWith('+') ? value.substring(1) : value;

  // Strip trailing extension: "ext." / "ext" / "x" followed by digits
  phone = phone.replace(/\s*(ext\.?|x)\s*\d+$/i, '').trimEnd();

  // Must contain at least one digit
  if (!/\d/.test(phone)) {
    return fail();
  }

  // Only allow digits, whitespace, and: - . ( )
  return /^[\d\s\-.()\u00a0]+$/.test(phone) ? pass() : fail();
};
