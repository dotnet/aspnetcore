// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ValidationDirective, ValidatableElement } from './Types';

const dataValPrefix = 'data-val-';

/**
 * Parse data-val-* attributes on an element into structured validation directives.
 *
 * Two-pass algorithm (adopted from aspnet-client-validation):
 *   Pass 1: Collect all data-val-* attributes into a flat map
 *   Pass 2: Group by rule name — keys without hyphens are rules, with hyphens are params
 */
export function parseDirectives(element: ValidatableElement): ValidationDirective[] {
  const attrs: Record<string, string> = {};
  for (let i = 0; i < element.attributes.length; i++) {
    const attr = element.attributes[i];
    if (attr.name.startsWith(dataValPrefix)) {
      const key = attr.name.substring(dataValPrefix.length);
      if (key) {
        attrs[key] = attr.value;
      }
    }
  }

  const directives: ValidationDirective[] = [];
  const keys = Object.keys(attrs);

  for (const key of keys) {
    if (key.includes('-')) {
      continue;
    }

    const rule = key;
    const message = attrs[rule];
    const params: Record<string, string> = {};

    const prefix = rule + '-';
    for (const paramKey of keys) {
      if (paramKey.startsWith(prefix)) {
        const paramName = paramKey.substring(prefix.length);
        params[paramName] = attrs[paramKey];
      }
    }

    directives.push({ rule, message, params });
  }

  return directives;
}
