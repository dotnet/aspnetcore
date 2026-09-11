// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { expect, test, describe } from '@jest/globals';
import {
  tryApplySpecialProperty,
  applyAnyDeferredValue,
} from '../src/Rendering/DomSpecialPropertyUtil';

const WIRE_TRUE: '' = '' as const;
const WIRE_FALSE: null = null;

describe('DomSpecialPropertyUtil - dynamic type input', () => {
  describe('wire format: bool attribute values arrive as "" (true) or null (false)', () => {
    test('checkbox becomes checked when a "value" frame carries "" (true) and type is set to checkbox', () => {
      const input = document.createElement('input');

      tryApplySpecialProperty(input, 'type', 'checkbox');
      tryApplySpecialProperty(input, 'value', WIRE_TRUE);

      expect(input.type).toBe('checkbox');
      expect(input.checked).toBe(true);
    });

    test('checkbox stays unchecked when a "value" frame carries null (false)', () => {
      const input = document.createElement('input');

      tryApplySpecialProperty(input, 'type', 'checkbox');
      tryApplySpecialProperty(input, 'value', WIRE_FALSE);

      expect(input.type).toBe('checkbox');
      expect(input.checked).toBe(false);
    });

    test('checkbox becomes checked when a "checked" frame carries "" (true) - the static-type path', () => {
      const input = document.createElement('input');

      tryApplySpecialProperty(input, 'type', 'checkbox');
      tryApplySpecialProperty(input, 'checked', WIRE_TRUE);

      expect(input.checked).toBe(true);
    });

    test('checkbox becomes unchecked when a "checked" frame carries null (false)', () => {
      const input = document.createElement('input');

      tryApplySpecialProperty(input, 'type', 'checkbox');
      tryApplySpecialProperty(input, 'checked', WIRE_FALSE);

      expect(input.checked).toBe(false);
    });
  });

  describe('value applied BEFORE type (deferred via _blazorDeferredValue)', () => {
    test('a "value=true" frame applied before "type=checkbox" still produces a checked checkbox', () => {
      const input = document.createElement('input');

      tryApplySpecialProperty(input, 'value', WIRE_TRUE);
      tryApplySpecialProperty(input, 'type', 'checkbox');

      expect(input.type).toBe('checkbox');
      expect(input.checked).toBe(true);
    });

    test('a "value=false" frame applied before "type=checkbox" still produces an unchecked checkbox', () => {
      const input = document.createElement('input');

      tryApplySpecialProperty(input, 'value', WIRE_FALSE);
      tryApplySpecialProperty(input, 'type', 'checkbox');

      expect(input.type).toBe('checkbox');
      expect(input.checked).toBe(false);
    });

    test('a non-bool value on a text input is not corrupted when the type changes after the value', () => {
      const input = document.createElement('input');

      tryApplySpecialProperty(input, 'value', 'hello');
      tryApplySpecialProperty(input, 'type', 'text');

      expect(input.type).toBe('text');
      expect(input.value).toBe('hello');
    });

    test('wire-bool true value stashed while input was text-type is replayed as checked after type changes to checkbox', () => {
      const input = document.createElement('input');

      tryApplySpecialProperty(input, 'type', 'text');
      tryApplySpecialProperty(input, 'value', WIRE_TRUE);
      tryApplySpecialProperty(input, 'type', 'checkbox');

      expect(input.type).toBe('checkbox');
      expect(input.checked).toBe(true);
    });

    test('wire-bool false value stashed while input was text-type is replayed as unchecked after type changes to checkbox', () => {
      const input = document.createElement('input');

      tryApplySpecialProperty(input, 'type', 'text');
      tryApplySpecialProperty(input, 'value', WIRE_FALSE);
      tryApplySpecialProperty(input, 'type', 'checkbox');

      expect(input.type).toBe('checkbox');
      expect(input.checked).toBe(false);
    });
  });

  describe('textarea value is preserved', () => {
    test('applying value "Hello" to a textarea leaves the textarea with value "Hello"', () => {
      const textarea = document.createElement('textarea');

      tryApplySpecialProperty(textarea, 'value', 'Hello');
      applyAnyDeferredValue(textarea);

      expect(textarea.value).toBe('Hello');
    });

    test('applying null to a textarea clears the value', () => {
      const textarea = document.createElement('textarea') as HTMLTextAreaElement;
      textarea.value = 'prior';

      tryApplySpecialProperty(textarea, 'value', null);
      applyAnyDeferredValue(textarea);

      expect(textarea.value).toBe('');
    });
  });

  describe('InputCheckbox: form-post value does not affect checked state', () => {
    test('value="True" on a checkbox sets .value but does not force .checked to false', () => {
      const input = document.createElement('input');
      input.setAttribute('type', 'checkbox');
      // Simulate <InputCheckbox>: checked is applied first, then the form-post value
      tryApplySpecialProperty(input, 'checked', '');
      tryApplySpecialProperty(input, 'value', 'True');
      applyAnyDeferredValue(input);

      expect(input.checked).toBe(true);
      expect(input.value).toBe('True');
    });

    test('value="True" applied before type=checkbox still sets .value and leaves .checked driven by the checked frame', () => {
      const input = document.createElement('input');

      tryApplySpecialProperty(input, 'value', 'True');
      tryApplySpecialProperty(input, 'type', 'checkbox');
      tryApplySpecialProperty(input, 'checked', '');

      expect(input.checked).toBe(true);
      expect(input.value).toBe('True');
    });
  });

  describe('tryApplySpecialProperty returns false for non-special attributes', () => {
    test('returns false for a div', () => {
      const div = document.createElement('div');
      const result = tryApplySpecialProperty(div, 'type', 'checkbox');
      expect(result).toBe(false);
    });
  });

  describe('applyAnyDeferredValue replays the stashed value on a checkbox', () => {
    test('a checkbox with a stashed wire-true ("") becomes checked', () => {
      const input = document.createElement('input');
      (input as any)._blazorDeferredValue = WIRE_TRUE;
      input.setAttribute('type', 'checkbox');

      applyAnyDeferredValue(input);

      expect(input.checked).toBe(true);
    });

    test('a checkbox with a stashed wire-false (null) becomes unchecked', () => {
      const input = document.createElement('input');
      (input as any)._blazorDeferredValue = WIRE_FALSE;
      input.setAttribute('type', 'checkbox');

      applyAnyDeferredValue(input);

      expect(input.checked).toBe(false);
    });
  });
});