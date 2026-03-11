// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ValidationProvider, ValidationProviderResult, ValidatableElement } from './Types';

interface RemoteCache {
  data: string;
  result: ValidationProviderResult;
}

const remoteCache = new WeakMap<ValidatableElement, RemoteCache>();

/**
 * Resolves a "*.PropertyName" field specifier to a full field name,
 * using the current element's name prefix.
 */
function resolveFieldName(currentName: string, spec: string): string {
  if (spec.startsWith('*.')) {
    const lastDot = currentName.lastIndexOf('.');
    const prefix = lastDot >= 0 ? currentName.substring(0, lastDot + 1) : '';
    return prefix + spec.substring(2);
  }
  return spec;
}

/**
 * Remote validation provider. Makes HTTP requests to a server endpoint
 * to validate a field value. Uses per-element caching to avoid redundant
 * requests when the same data is re-validated (e.g., on form submit after blur).
 *
 * Expected data-val-remote-* attributes:
 *   - data-val-remote="{error message}"
 *   - data-val-remote-url="{validation endpoint}"
 *   - data-val-remote-type="Get" or "Post" (default: GET)
 *   - data-val-remote-additionalfields="*.Field1,*.Field2"
 *
 * Server response protocol (matches MVC/jquery-validation):
 *   - true or "true" → valid
 *   - false or "false" → invalid (use default error message)
 *   - any other string → invalid with custom error message
 */
export const remoteProvider: ValidationProvider = (
  value: string,
  element: ValidatableElement,
  params: Record<string, string>
): ValidationProviderResult | Promise<ValidationProviderResult> => {
  if (!value) {
    return true;
  }

  const url = params['url'];
  if (!url) {
    return true;
  }

  const method = (params['type'] || 'GET').toUpperCase();

  // Build request data
  const data = new URLSearchParams();
  data.set(element.name, value);

  // Collect additional fields from the form
  const additionalFields = (params['additionalfields'] || '').split(',').filter(Boolean);
  const form = element.closest('form');

  for (const spec of additionalFields) {
    const fieldName = resolveFieldName(element.name, spec);
    if (fieldName === element.name) {
      continue;
    }
    const otherElement = form?.elements.namedItem(fieldName);
    if (otherElement && 'value' in otherElement) {
      data.set(fieldName, (otherElement as unknown as HTMLInputElement).value);
    }
  }

  // Check cache — return synchronously if cached
  const cacheKey = data.toString();
  const cached = remoteCache.get(element);
  if (cached && cached.data === cacheKey) {
    return cached.result;
  }

  // Make HTTP request
  const requestUrl = method === 'GET' ? `${url}?${cacheKey}` : url;

  return fetch(requestUrl, {
    method,
    headers: method === 'POST'
      ? { 'Content-Type': 'application/x-www-form-urlencoded' }
      : {},
    body: method === 'POST' ? data.toString() : undefined,
  })
    .then(response => response.json())
    .then((serverResult: unknown): ValidationProviderResult => {
      let result: ValidationProviderResult;
      if (serverResult === true || serverResult === 'true') {
        result = true;
      } else if (typeof serverResult === 'string') {
        result = serverResult;
      } else {
        result = false;
      }

      // Cache the result
      remoteCache.set(element, { data: cacheKey, result });
      return result;
    })
    .catch(() => true); // Network error: don't block the user
};
