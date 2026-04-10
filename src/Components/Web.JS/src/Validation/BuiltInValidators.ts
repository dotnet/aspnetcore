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
  registry.set('creditcard', creditcardValidator);
  registry.set('equalto', equaltoValidator);
  registry.set('fileextensions', fileextensionsValidator);
}

// Validates that the field has a non-empty value.
// Checkboxes: must be checked. Radio groups: at least one in the group must be checked.
// Text/select/textarea: value must be non-empty after trimming whitespace.
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

// Validates string length against min and/or max bounds (inclusive).
// Used for 'length' ([StringLength]), 'minlength' ([MinLength]), and 'maxlength' ([MaxLength]) rules.
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

// Validates that a numeric value falls within min/max bounds (inclusive).
// Non-numeric values fail. Uses Number() for parsing.
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

// Validates that the value matches a regular expression pattern (full match).
// The pattern is anchored with ^(?:...)$ to match .NET's exact-match semantics.
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

// Validates email format using the WHATWG HTML5 email pattern (same as jQuery validation).
// Stricter than .NET's EmailAddressAttribute (which only checks for a single '@').
// The client won't let through anything the server would reject.
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

// Validates URL format using the jQuery validation pattern (Diego Perini's regex).
// Requires http/https/ftp protocol or protocol-relative (//). Stricter than .NET's
// UrlAttribute (which only checks for http/https/ftp prefix via StartsWith).
// The client won't let through anything the server would reject.
const urlValidator: Validator = (context: ValidationContext): ValidationResult => {
  const { value } = context;
  if (!value) {
    return true;
  }

  return urlPattern.test(value);
};

// Validates phone numbers matching .NET's PhoneAttribute logic.
// Strips leading '+' and trailing extensions (ext./ext/x + digits).
// Requires at least one digit. Only allows digits, whitespace, and: - . ( )
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

// Validates credit card numbers using the Luhn algorithm (same as .NET CreditCardAttribute).
// Strips dashes and spaces before validation. Requires 13-19 digits (from jQuery validation).
const creditcardValidator: Validator = (context: ValidationContext): ValidationResult => {
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

// Validates that the value equals another field's value (for password confirmation, etc.).
// Resolves "*.PropertyName" to the model-prefixed field name using the current field's name.
// Finds the other field by [name] attribute within the same form.
const equaltoValidator: Validator = (context: ValidationContext): ValidationResult => {
  const { value, element, params } = context;
  if (!value) {
    return true;
  }

  const otherFieldName = resolveOtherFieldName(element.name, params.other);
  if (!otherFieldName) {
    return true;
  }

  const form = element.closest('form');
  if (!form) {
    return true;
  }

  const otherElement = form.querySelector<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>(
    `[name="${CSS.escape(otherFieldName)}"]`
  );

  if (!otherElement) {
    return true;
  }

  return value === otherElement.value;
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

// Validates that the filename ends with an allowed extension (case-insensitive).
// Extensions param is comma-separated with dot prefix (e.g. ".png,.jpg,.gif") as emitted by MVC.
const fileextensionsValidator: Validator = (context: ValidationContext): ValidationResult => {
  const { value, params } = context;
  if (!value) {
    return true;
  }

  if (!params.extensions) {
    return true;
  }

  // Build regex from comma-separated extensions, stripping dots for escaping.
  const extensions = params.extensions.split(',')
    .map(ext => ext.trim().replace(/^\./, ''))
    .filter(ext => ext.length > 0)
    .join('|');

  if (!extensions) {
    return true;
  }

  return new RegExp(`\\.(${extensions})$`, 'i').test(value);
};
