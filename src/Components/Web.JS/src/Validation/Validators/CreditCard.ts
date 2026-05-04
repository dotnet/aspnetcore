// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ValidationContext, ValidationResult, Validator } from '../ValidationTypes';

// Validates credit card numbers using the Luhn algorithm (same as .NET CreditCardAttribute).
// Strips dashes and spaces before validation. Requires 13-19 digits.
export const creditCardValidator: Validator = (context: ValidationContext): ValidationResult => {
  const { value } = context;
  if (!value) {
    return true;
  }

  // Strip dashes and spaces
  const stripped = value.replace(/[\s-]/g, '');

  // Only digits allowed after stripping
  if (!/^\d+$/.test(stripped)) {
    return false;
  }

  // Valid card numbers are 13-19 digits
  if (stripped.length < 13 || stripped.length > 19) {
    return false;
  }

  // Luhn algorithm
  let checksum = 0;
  let doubleDigit = false;
  for (let i = stripped.length - 1; i >= 0; i--) {
    let digitValue = (stripped.charCodeAt(i) - 48) * (doubleDigit ? 2 : 1);
    doubleDigit = !doubleDigit;
    while (digitValue > 0) {
      checksum += digitValue % 10;
      digitValue = Math.floor(digitValue / 10);
    }
  }

  return (checksum % 10) === 0;
};
