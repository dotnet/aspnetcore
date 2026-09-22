// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ValidationContext, ValidationResult, Validator, pass, fail } from '../ValidationTypes';

// Validates credit card numbers using the Luhn algorithm, matching .NET's CreditCardAttribute
// exactly: iterate the value right-to-left, skip '-' and ' ' (ASCII space only), fail on any
// other non-ASCII-digit character, and accept when the Luhn checksum is a multiple of 10.
export const creditCardValidator: Validator = (context: ValidationContext): ValidationResult => {
  const { value } = context;
  if (!value) {
    return pass();
  }

  let checksum = 0;
  let evenDigit = false;

  for (let i = value.length - 1; i >= 0; i--) {
    const char = value[i];

    if (char < '0' || char > '9') {
      if (char === '-' || char === ' ') {
        continue;
      }
      return fail();
    }

    let digitValue = (char.charCodeAt(0) - 48) * (evenDigit ? 2 : 1);
    evenDigit = !evenDigit;

    while (digitValue > 0) {
      checksum += digitValue % 10;
      digitValue = Math.floor(digitValue / 10);
    }
  }

  return (checksum % 10) === 0 ? pass() : fail();
};
