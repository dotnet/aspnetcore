// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ValidationEngine } from './ValidationEngine';
import { ValidatableElement } from './Types';

// RFC 5322 simplified email regex (same as aspnet-client-validation)
const emailRegex = /^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$/;

// Characters allowed in phone numbers beyond digits and whitespace (matches PhoneAttribute.AdditionalPhoneNumberCharacters)
const phoneAdditionalChars = /^[\d\s\-\.()]*$/;

// Extension abbreviations in order of precedence (matches PhoneAttribute.RemoveExtension)
const phoneExtensionAbbreviations = ['ext.', 'ext', 'x'];

/**
 * Removes a phone extension suffix from the value if present.
 * Matches the logic of PhoneAttribute.RemoveExtension in .NET.
 * Tries "ext.", "ext", "x" in order — each must be followed by optional whitespace then digits.
 */
function removePhoneExtension(phone: string): string {
  const lower = phone.toLowerCase();
  for (const abbr of phoneExtensionAbbreviations) {
    const idx = lower.lastIndexOf(abbr);
    if (idx >= 0) {
      const after = phone.substring(idx + abbr.length).trimStart();
      if (after.length > 0 && /^\d+$/.test(after)) {
        return phone.substring(0, idx);
      }
    }
  }
  return phone;
}

/**
 * Resolves the "other" field element for Compare/equalto validation.
 * The data-val-equalto-other attribute uses MVC's "*.PropertyName" naming convention:
 * the "*" prefix is replaced with the current field's name prefix (everything before the last ".").
 */
function resolveOtherElement(element: ValidatableElement, otherSpec: string): ValidatableElement | null {
  const name = (element as HTMLInputElement).name;
  if (!name) {
    return null;
  }

  let otherName = otherSpec;
  if (otherName.startsWith('*.')) {
    const lastDot = name.lastIndexOf('.');
    const prefix = lastDot >= 0 ? name.substring(0, lastDot) : '';
    otherName = prefix ? prefix + '.' + otherName.substring(2) : otherName.substring(2);
  }

  const form = element.closest('form');
  if (!form) {
    return null;
  }

  const namedItem = form.elements.namedItem(otherName);
  if (!namedItem) {
    return null;
  }

  return namedItem instanceof RadioNodeList
    ? (namedItem[0] as ValidatableElement)
    : (namedItem as ValidatableElement);
}

export function registerBuiltInProviders(engine: ValidationEngine): void {
  engine.addProvider('required', (value, element) => {
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
    return value.trim().length > 0;
  });

  engine.addProvider('length', (value, _element, params) => {
    if (!value) {
      return true;
    }
    const min = parseInt(params['min'], 10);
    const max = parseInt(params['max'], 10);
    if (!isNaN(min) && value.length < min) {
      return false;
    }
    if (!isNaN(max) && value.length > max) {
      return false;
    }
    return true;
  });

  engine.addProvider('minlength', (value, _element, params) => {
    if (!value) {
      return true;
    }
    const min = parseInt(params['min'], 10);
    return isNaN(min) || value.length >= min;
  });

  engine.addProvider('maxlength', (value, _element, params) => {
    if (!value) {
      return true;
    }
    const max = parseInt(params['max'], 10);
    return isNaN(max) || value.length <= max;
  });

  engine.addProvider('range', (value, _element, params) => {
    if (!value) {
      return true;
    }
    const num = parseFloat(value);
    if (isNaN(num)) {
      return false;
    }
    const min = parseFloat(params['min']);
    const max = parseFloat(params['max']);
    if (!isNaN(min) && num < min) {
      return false;
    }
    if (!isNaN(max) && num > max) {
      return false;
    }
    return true;
  });

  // Matches RegularExpressionAttribute.IsValid() — full-string match semantics.
  // Uses exec() and verifies match starts at index 0 and spans the entire value.
  engine.addProvider('regex', (value, _element, params) => {
    if (!value) {
      return true;
    }
    const pattern = params['pattern'];
    if (!pattern) {
      return true;
    }
    const match = new RegExp(pattern).exec(value);
    return match !== null && match.index === 0 && match[0].length === value.length;
  });

  engine.addProvider('email', (value) => {
    if (!value) {
      return true;
    }
    return emailRegex.test(value);
  });

  // Matches UrlAttribute.IsValid() — accepts strings starting with http://, https://, or ftp://
  engine.addProvider('url', (value) => {
    if (!value) {
      return true;
    }
    const v = value.toLowerCase();
    return v.startsWith('http://') || v.startsWith('https://') || v.startsWith('ftp://');
  });

  // Matches PhoneAttribute.IsValid() — strips '+', trims end, removes extension suffix,
  // then validates that remaining chars are digits, whitespace, or -.() and at least one digit exists
  engine.addProvider('phone', (value) => {
    if (!value) {
      return true;
    }
    let v = value.replace(/\+/g, '').trimEnd();
    v = removePhoneExtension(v);
    if (!/\d/.test(v)) {
      return false;
    }
    return phoneAdditionalChars.test(v);
  });

  // Matches CreditCardAttribute.IsValid() — Luhn algorithm.
  // Iterates chars in reverse, skips '-' and ' ', rejects other non-digit chars.
  engine.addProvider('creditcard', (value) => {
    if (!value) {
      return true;
    }

    let checksum = 0;
    let evenDigit = false;

    for (let i = value.length - 1; i >= 0; i--) {
      const ch = value[i];
      if (ch === '-' || ch === ' ') {
        continue;
      }
      if (ch < '0' || ch > '9') {
        return false;
      }

      let digitValue = (ch.charCodeAt(0) - 48) * (evenDigit ? 2 : 1); // '0' = 48
      evenDigit = !evenDigit;

      while (digitValue > 0) {
        checksum += digitValue % 10;
        digitValue = Math.floor(digitValue / 10);
      }
    }

    return checksum % 10 === 0;
  });

  // Matches CompareAttribute — compares field value to another field's value.
  // The "other" param uses MVC's "*.PropertyName" convention.
  engine.addProvider('equalto', (value, element, params) => {
    const other = params['other'];
    if (!other) {
      return true;
    }
    const otherElement = resolveOtherElement(element, other);
    if (!otherElement) {
      return true;
    }
    const otherValue = (otherElement as HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement).value;
    return value === otherValue;
  });

  // Matches FileExtensionsAttribute.IsValid() — validates that file extension is in allowed list.
  // Extensions param is comma-separated (default: "png,jpg,jpeg,gif").
  engine.addProvider('fileextensions', (value, _element, params) => {
    if (!value) {
      return true;
    }
    const extensionsRaw = params['extensions'] || 'png,jpg,jpeg,gif';
    const allowed = extensionsRaw
      .replace(/\s/g, '')
      .replace(/\./g, '')
      .toLowerCase()
      .split(',')
      .map(e => '.' + e);

    const lastDot = value.lastIndexOf('.');
    if (lastDot < 0) {
      return false;
    }
    const ext = value.substring(lastDot).toLowerCase();
    return allowed.indexOf(ext) >= 0;
  });
}
