// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

const identifierPattern = /^[A-Za-z_$][A-Za-z0-9_$]*$/;
const dangerousSegments = new Set([
  '__proto__',
  'prototype',
  'constructor',
]);

export function evaluateHostStartupValues(keysOrJson: readonly string[] | string): Record<string, string> {
  const keys = parseKeys(keysOrJson);
  const uniqueKeys = new Set<string>();

  for (const key of keys) {
    validateKey(key);
    if (uniqueKeys.has(key)) {
      throw new Error(`The browser startup value key '${key}' was provided more than once.`);
    }

    uniqueKeys.add(key);
  }

  const values: Record<string, string> = Object.create(null);
  for (const key of keys) {
    let value: unknown = globalThis;
    for (const segment of key.split('.')) {
      if (value === null || value === undefined) {
        throw new Error(`The browser startup value '${key}' could not be resolved.`);
      }

      value = (value as Record<string, unknown>)[segment];
    }

    if (typeof value !== 'string') {
      throw new Error(`The browser startup value '${key}' must resolve to a string.`);
    }

    values[key] = value;
  }

  return values;
}

export function evaluateHostStartupValuesJson(keysJson: string): string {
  return JSON.stringify(evaluateHostStartupValues(keysJson));
}

function parseKeys(keysOrJson: readonly string[] | string): readonly string[] {
  let keys: unknown;
  try {
    keys = typeof keysOrJson === 'string' ? JSON.parse(keysOrJson) : keysOrJson;
  } catch {
    throw new Error('Browser startup value keys must be an array of strings.');
  }

  if (!Array.isArray(keys) || keys.some(key => typeof key !== 'string')) {
    throw new Error('Browser startup value keys must be an array of strings.');
  }

  return keys;
}

function validateKey(key: string): void {
  const segments = key.split('.');
  if (segments.some(segment => !identifierPattern.test(segment) || dangerousSegments.has(segment))) {
    throw new Error(`The browser startup value key '${key}' is not a valid property path.`);
  }
}
