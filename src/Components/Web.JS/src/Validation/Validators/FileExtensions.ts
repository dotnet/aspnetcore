// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ValidationContext, ValidationResult, Validator } from '../ValidationTypes';

// Validates that the filename ends with an allowed extension (case-insensitive).
// Extensions param is comma-separated with dot prefix (e.g. ".png,.jpg,.gif").
export const fileExtensionsValidator: Validator = (context: ValidationContext): ValidationResult => {
  const { value, params } = context;
  if (!value) {
    return true;
  }

  if (!params.extensions) {
    return true;
  }

  // Build regex from comma-separated extensions, stripping dots and escaping regex metacharacters.
  const extensions = params.extensions.split(',')
    .map(ext => ext.trim().replace(/^\./, ''))
    .filter(ext => ext.length > 0)
    .map(ext => ext.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'))
    .join('|');

  if (!extensions) {
    return true;
  }

  return new RegExp(`\\.(${extensions})$`, 'i').test(value);
};
