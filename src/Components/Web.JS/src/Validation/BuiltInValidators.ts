// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ValidationContext, ValidationResult, Validator, ValidatorRegistry } from './Validator';

export function registerBuiltInValidators(registry: ValidatorRegistry): void {
  registry.set('required', requiredValidator);
  registry.set('length', stringLengthValidator);
  registry.set('minlength', stringLengthValidator);
  registry.set('maxlength', stringLengthValidator);
  registry.set('range', rangeValidator);
  registry.set('regex', regexValidator);
  // TODO: email
  // TODO: url
  // TODO: phone
  // TODO: creditcard
  // TODO: equalto
  // TODO: fileextensions
}

const requiredValidator: Validator = (context: ValidationContext): ValidationResult => {
  const { value, element } = context;
  if (element instanceof HTMLInputElement) {
    if (element.type === 'checkbox') {
      return element.checked;
    }

    if (element.type === 'radio') {
      const form = element.closest('form');
      if (form) {
        const radios = form.querySelectorAll<HTMLInputElement>(`input[type="radio"][name="${CSS.escape(element.name)}"]`);
        return Array.from(radios).some(r => r.checked);
      }
    }
  }

  if (!value) {
    return false;
  }

  return value.trim().length > 0;
};

const stringLengthValidator: Validator = (context: ValidationContext): ValidationResult => {
  const { value, params } = context;
  if (!value) {
    return true;
  }

  if (params.min) {
    const min = parseInt(params['min'], 10);
    if (value.length < min) {
      return false;
    }
  }

  if (params.max) {
    const max = parseInt(params['max'], 10);
    if (value.length > max) {
      return false;
    }
  }

  return true;
};

const rangeValidator: Validator = (context: ValidationContext): ValidationResult => {
  const { value, params } = context;
  if (!value) {
    return true;
  }

  const num = Number(value);
  if (isNaN(num)) {
    return false;
  }

  if (params.min !== undefined) {
    if (num < Number(params.min)) {
      return false;
    }
  }

  if (params.max !== undefined) {
    if (num > Number(params.max)) {
      return false;
    }
  }

  return true;
};

const regexValidator: Validator = (context: ValidationContext): ValidationResult => {
  const { value, params } = context;
  if (!value) {
    return true;
  }

  if (!params.pattern) {
    return true;
  }

  // Anchor the pattern for full-match semantics, matching .NET's RegularExpressionAttribute
  // which requires Index == 0 && Length == value.Length. The non-capturing group avoids
  // changing semantics for patterns with alternation (e.g. "a|b").
  const anchored = `^(?:${params.pattern})$`;
  return new RegExp(anchored).test(value);
};
