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
  registry.set('email', emailValidator);
  registry.set('url', urlValidator);
  registry.set('phone', phoneValidator);
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

// WHATWG email pattern, same as jQuery validation.
// Source: https://html.spec.whatwg.org/multipage/input.html#valid-e-mail-address
const emailPattern = /^[a-zA-Z0-9.!#$%&'*+\/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$/;

const emailValidator: Validator = (context: ValidationContext): ValidationResult => {
  const { value } = context;
  if (!value) {
    return true;
  }

  return emailPattern.test(value);
};

// URL pattern from jQuery validation, based on Diego Perini's regex.
// Allows http, https, ftp, and protocol-relative URLs.
// Source: https://gist.github.com/dperini/729294
const urlPattern = /^(?:(?:(?:https?|ftp):)?\/\/)(?:(?:[^\]\[?\/<~#`!@$^&*()+=}|:";',>{ ]|%[0-9A-Fa-f]{2})+(?::(?:[^\]\[?\/<~#`!@$^&*()+=}|:";',>{ ]|%[0-9A-Fa-f]{2})*)?@)?(?:(?!(?:10|127)(?:\.\d{1,3}){3})(?!(?:169\.254|192\.168)(?:\.\d{1,3}){2})(?!172\.(?:1[6-9]|2\d|3[0-1])(?:\.\d{1,3}){2})(?:[1-9]\d?|1\d\d|2[01]\d|22[0-3])(?:\.(?:1?\d{1,2}|2[0-4]\d|25[0-5])){2}(?:\.(?:[1-9]\d?|1\d\d|2[0-4]\d|25[0-4]))|(?:(?:[a-z0-9\u00a1-\uffff][a-z0-9\u00a1-\uffff_-]{0,62})?[a-z0-9\u00a1-\uffff]\.)+(?:[a-z\u00a1-\uffff]{2,}\.?))(?::\d{2,5})?(?:[/?#]\S*)?$/i;

const urlValidator: Validator = (context: ValidationContext): ValidationResult => {
  const { value } = context;
  if (!value) {
    return true;
  }

  return urlPattern.test(value);
};

const phoneValidator: Validator = (context: ValidationContext): ValidationResult => {
  const { value } = context;
  if (!value) {
    return true;
  }

  // Strip leading '+' (international prefix)
  let phone = value.startsWith('+') ? value.substring(1) : value;

  // Strip trailing extension: "ext." / "ext" / "x" followed by digits
  phone = phone.replace(/\s*(ext\.?|x)\s*\d+$/i, '').trimEnd();

  // Must contain at least one digit
  if (!/\d/.test(phone)) {
    return false;
  }

  // Only allow digits, whitespace, and: - . ( )
  return /^[\d\s\-.()\u00a0]+$/.test(phone);
};
