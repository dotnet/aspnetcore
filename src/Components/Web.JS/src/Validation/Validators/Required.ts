// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ValidationContext, ValidationResult, Validator, pass, fail } from '../ValidationTypes';

// Validates that the field has a non-empty value.
// Checkboxes: must be checked. Radio groups: at least one in the group must be checked.
// Text/select/textarea: value must be non-empty after trimming whitespace.
export const requiredValidator: Validator = (context: ValidationContext): ValidationResult => {
  const { value, element } = context;
  if (element instanceof HTMLInputElement) {
    if (element.type === 'checkbox') {
      return element.checked ? pass() : fail();
    }

    if (element.type === 'radio') {
      const form = element.closest('form');
      if (form) {
        const radios = form.querySelectorAll<HTMLInputElement>(`input[type="radio"][name="${CSS.escape(element.name)}"]`);
        return Array.from(radios).some(r => r.checked) ? pass() : fail();
      }
    }
  }

  if (!value) {
    return fail();
  }

  return value.trim().length > 0 ? pass() : fail();
};
