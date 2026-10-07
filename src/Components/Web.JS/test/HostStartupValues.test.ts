// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { afterEach, describe, expect, test } from '@jest/globals';
import { evaluateHostStartupValues, evaluateHostStartupValuesJson } from '../src/Services/HostStartupValues';

const globals = globalThis as unknown as Record<string, unknown>;

describe('HostStartupValues', () => {
  afterEach(() => {
    delete globals.testStartupValues;
  });

  test('evaluates property paths from an array or serialized array', () => {
    globals.testStartupValues = { nested: { value: 'expected' } };

    expect(evaluateHostStartupValues(['testStartupValues.nested.value']))
      .toEqual({ 'testStartupValues.nested.value': 'expected' });
    expect(JSON.parse(evaluateHostStartupValuesJson('["testStartupValues.nested.value"]')))
      .toEqual({ 'testStartupValues.nested.value': 'expected' });
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

});
