// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export function evaluateHostStartupValues(keys: readonly string[]): Record<string, string> {
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
  return JSON.stringify(evaluateHostStartupValues(JSON.parse(keysJson) as string[]));
}
