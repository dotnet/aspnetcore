// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ValidationEngine } from './ValidationEngine';

// RFC 5322 simplified email regex (same as aspnet-client-validation)
const emailRegex = /^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$/;

const urlRegex = /^(https?|ftp):\/\/[^\s/$.?#].[^\s]*$/i;

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

  engine.addProvider('regex', (value, _element, params) => {
    if (!value) {
      return true;
    }
    const pattern = params['pattern'];
    if (!pattern) {
      return true;
    }
    return new RegExp(pattern).test(value);
  });

  engine.addProvider('email', (value) => {
    if (!value) {
      return true;
    }
    return emailRegex.test(value);
  });

  engine.addProvider('url', (value) => {
    if (!value) {
      return true;
    }
    return urlRegex.test(value);
  });
}
