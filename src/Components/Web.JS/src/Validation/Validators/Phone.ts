// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ValidationContext, ValidationResult, Validator, pass, fail } from '../ValidationTypes';

// Validates phone numbers with the same semantics as .NET's PhoneAttribute:
// removes every '+', trims trailing whitespace, strips a trailing extension
// ("ext."/"ext"/"x" followed by digits), requires at least one digit, and then allows
// only digits, whitespace, and the characters '-', '.', '(', ')'.
const phoneTrailingWhitespace = /\p{White_Space}+$/u;
const phoneExtension = /\p{White_Space}*(?:ext\.?|x)\p{White_Space}*\p{Nd}+$/iu;
const phoneHasDigit = /\p{Nd}/u;
const phoneAllowedChars = /^[\p{Nd}\p{White_Space}().-]+$/u;

export const phoneValidator: Validator = (context: ValidationContext): ValidationResult => {
  const { value } = context;
  if (!value) {
    return pass();
  }

  // Remove every '+', trim trailing whitespace, then strip a trailing extension.
  const phone = value
    .replace(/\+/gu, '')
    .replace(phoneTrailingWhitespace, '')
    .replace(phoneExtension, '');

  // Must contain at least one digit.
  if (!phoneHasDigit.test(phone)) {
    return fail();
  }

  return phoneAllowedChars.test(phone) ? pass() : fail();
};
