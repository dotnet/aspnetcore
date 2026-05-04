// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ValidationContext, ValidationResult, Validator } from '../ValidationTypes';

// WHATWG email pattern.
// Source: https://html.spec.whatwg.org/multipage/input.html#valid-e-mail-address
const emailPattern = /^[a-zA-Z0-9.!#$%&'*+\/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$/;

// Validates email format using the WHATWG HTML5 email pattern.
// Stricter than .NET's EmailAddressAttribute (which only checks for a single '@').
// The client won't let through anything the server would reject.
export const emailValidator: Validator = (context: ValidationContext): ValidationResult => {
  const { value } = context;
  if (!value) {
    return true;
  }

  return emailPattern.test(value);
};
