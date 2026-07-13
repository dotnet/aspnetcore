// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ValidationContext, ValidationResult, Validator, pass, fail } from '../ValidationTypes';

// Validates that the value equals another field's value (for password confirmation, etc.).
// Resolves "*.PropertyName" to the model-prefixed field name using the current field's name.
// Finds the other field by [name] attribute within the same form.
// Throws if the mandatory `other` parameter is missing.
export const equalToValidator: Validator = (context: ValidationContext): ValidationResult => {
  const { value, element, params } = context;
  if (!params.other) {
    throw new Error('EqualTo validator requires a non-empty "other" parameter.');
  }

  if (!value) {
    return pass();
  }

  const otherFieldName = resolveOtherFieldName(element.name, params.other);
  if (!otherFieldName) {
    return pass();
  }

  const form = element.closest('form');
  if (!form) {
    return pass();
  }

  const otherElement = form.querySelector<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>(`[name="${CSS.escape(otherFieldName)}"]`);

  if (!otherElement) {
    return pass();
  }

  return value === otherElement.value ? pass() : fail();
};

function resolveOtherFieldName(currentName: string, otherParam: string | undefined): string | undefined {
  if (!otherParam) {
    return undefined;
  }

  if (otherParam.startsWith('*.')) {
    // Replace * with the model prefix from the current field's name.
    // E.g. currentName="User.Password", otherParam="*.ConfirmPassword" → "User.ConfirmPassword"
    const lastDot = currentName.lastIndexOf('.');
    const prefix = lastDot >= 0 ? currentName.substring(0, lastDot + 1) : '';
    return prefix + otherParam.substring(2);
  }

  return otherParam;
}
