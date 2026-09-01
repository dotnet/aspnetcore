// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { afterEach, describe, expect, test } from '@jest/globals';
import { evaluateHostStartupValues, evaluateHostStartupValuesJson } from '../src/Services/HostStartupValues';

const globals = globalThis as unknown as Record<string, unknown>;

describe('HostStartupValues', () => {
  afterEach(() => {
    delete globals.testStartupValues;
    delete globals.duplicateStartupValue;
  });

  test('evaluates property paths from an array or JSON array', () => {
    globals.testStartupValues = { nested: { value: 'expected' } };

    expect(evaluateHostStartupValues(['testStartupValues.nested.value']))
      .toEqual({ 'testStartupValues.nested.value': 'expected' });
    expect(JSON.parse(evaluateHostStartupValuesJson('["testStartupValues.nested.value"]')))
      .toEqual({ 'testStartupValues.nested.value': 'expected' });
  });

  test('rejects duplicate keys before evaluating them', () => {
    let evaluations = 0;
    Object.defineProperty(globalThis, 'duplicateStartupValue', {
      configurable: true,
      get: () => {
        evaluations++;
        return 'value';
      },
    });

    expect(() => evaluateHostStartupValues(['duplicateStartupValue', 'duplicateStartupValue']))
      .toThrow("The browser startup value key 'duplicateStartupValue' was provided more than once.");
    expect(evaluations).toBe(0);
  });

  test.each([
    '',
    'location.href()',
    'location["href"]',
    'location..href',
    '__proto__.value',
    'value.prototype.name',
    'value.constructor.name',
  ])('rejects invalid property path %s', key => {
    expect(() => evaluateHostStartupValues([key]))
      .toThrow(`The browser startup value key '${key}' is not a valid property path.`);
  });

  test.each([
    [null, 'testStartupValues.nullValue'],
    [undefined, 'testStartupValues.undefinedValue'],
    [42, 'testStartupValues.numberValue'],
    [() => 'value', 'testStartupValues.functionValue'],
  ])('rejects non-string leaf %p', (value, key) => {
    globals.testStartupValues = {
      [key.substring(key.indexOf('.') + 1)]: value,
    };

    expect(() => evaluateHostStartupValues([key]))
      .toThrow(`The browser startup value '${key}' must resolve to a string.`);
  });

  test('rejects an unresolved intermediate path', () => {
    globals.testStartupValues = null;

    expect(() => evaluateHostStartupValues(['testStartupValues.value']))
      .toThrow("The browser startup value 'testStartupValues.value' could not be resolved.");
  });

  test('rejects a JSON value that is not an array of strings', () => {
    expect(() => evaluateHostStartupValues('{"key":"value"}'))
      .toThrow('Browser startup value keys must be an array of strings.');
    expect(() => evaluateHostStartupValues('["key",42]'))
      .toThrow('Browser startup value keys must be an array of strings.');
  });
});
