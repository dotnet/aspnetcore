import { expect, test, describe } from '@jest/globals';

import { parseRules } from '../../src/Validation/DomScanner';
import { ValidationRule } from '../../src/Validation/ValidationEngine';
import { ValidatableElement } from '../../src/Validation/ValidationTypes';

function createElement(tag: string, attributes: Record<string, string>): ValidatableElement {
  const el = document.createElement(tag) as ValidatableElement;
  for (const [name, value] of Object.entries(attributes)) {
    el.setAttribute(name, value);
  }
  return el;
}

function input(attributes: Record<string, string>): ValidatableElement {
  return createElement('input', attributes);
}

describe('parseRules', () => {
  test('returns empty array when element has no attributes', () => {
    const el = input({});
    expect(parseRules(el)).toEqual([]);
  });

  test('returns empty array when element has no data-val-* attributes', () => {
    const el = input({ type: 'text', name: 'age', class: 'form-control' });
    expect(parseRules(el)).toEqual([]);
  });

  test('parses a single rule with message only', () => {
    const el = input({ 'data-val-required': 'This field is required.' });
    const rules = parseRules(el);
    expect(rules).toEqual([
      { ruleName: 'required', errorMessage: 'This field is required.', params: {} },
    ]);
  });

  test('parses a single rule with message and one param', () => {
    const el = input({
      'data-val-maxlength': 'Too long.',
      'data-val-maxlength-max': '100',
    });
    const rules = parseRules(el);
    expect(rules).toEqual([
      { ruleName: 'maxlength', errorMessage: 'Too long.', params: { max: '100' } },
    ]);
  });

  test('parses a single rule with message and multiple params', () => {
    const el = input({
      'data-val-range': 'Value must be between 10 and 50.',
      'data-val-range-min': '10',
      'data-val-range-max': '50',
    });
    const rules = parseRules(el);
    expect(rules).toEqual([
      {
        ruleName: 'range',
        errorMessage: 'Value must be between 10 and 50.',
        params: { min: '10', max: '50' },
      },
    ]);
  });

  test('parses multiple distinct rules on the same element', () => {
    const el = input({
      'data-val-required': 'This field is required.',
      'data-val-range': 'Must be between 1 and 100.',
      'data-val-range-min': '1',
      'data-val-range-max': '100',
    });
    const rules = parseRules(el);
    expect(rules).toHaveLength(2);

    const required = rules.find(r => r.ruleName === 'required');
    const range = rules.find(r => r.ruleName === 'range');

    expect(required).toEqual({ ruleName: 'required', errorMessage: 'This field is required.', params: {} });
    expect(range).toEqual({
      ruleName: 'range',
      errorMessage: 'Must be between 1 and 100.',
      params: { min: '1', max: '100' },
    });
  });

  test('result does not depend on attribute order (params before message)', () => {
    const paramsFirst = input({
      'data-val-range-min': '10',
      'data-val-range-max': '50',
      'data-val-range': 'Out of range.',
    });
    const messageFirst = input({
      'data-val-range': 'Out of range.',
      'data-val-range-min': '10',
      'data-val-range-max': '50',
    });

    const rulesA = parseRules(paramsFirst);
    const rulesB = parseRules(messageFirst);

    expect(rulesA).toEqual(rulesB);
    expect(rulesA).toEqual([
      { ruleName: 'range', errorMessage: 'Out of range.', params: { min: '10', max: '50' } },
    ]);
  });

  test('result does not depend on attribute order with multiple rules', () => {
    const el1 = input({
      'data-val-required': 'Required.',
      'data-val-range': 'Out of range.',
      'data-val-range-min': '1',
    });
    const el2 = input({
      'data-val-range-min': '1',
      'data-val-required': 'Required.',
      'data-val-range': 'Out of range.',
    });

    const rules1 = parseRules(el1);
    const rules2 = parseRules(el2);

    // Both should have the same rules (order of rules in array may differ)
    expect(rules1).toHaveLength(2);
    expect(rules2).toHaveLength(2);

    for (const ruleName of ['required', 'range']) {
      expect(rules1.find(r => r.ruleName === ruleName)).toEqual(
        rules2.find(r => r.ruleName === ruleName)
      );
    }
  });

  test('rule with params but no message gets empty errorMessage', () => {
    const el = input({
      'data-val-range-min': '10',
      'data-val-range-max': '50',
    });
    const rules = parseRules(el);
    expect(rules).toEqual([
      { ruleName: 'range', errorMessage: '', params: { min: '10', max: '50' } },
    ]);
  });

  test('rule with empty error message string', () => {
    const el = input({ 'data-val-required': '' });
    const rules = parseRules(el);
    expect(rules).toEqual([
      { ruleName: 'required', errorMessage: '', params: {} },
    ]);
  });

  test('param with empty value', () => {
    const el = input({
      'data-val-regex': 'Invalid format.',
      'data-val-regex-pattern': '',
    });
    const rules = parseRules(el);
    expect(rules).toEqual([
      { ruleName: 'regex', errorMessage: 'Invalid format.', params: { pattern: '' } },
    ]);
  });

  test('param name containing dashes is preserved in full', () => {
    const el = input({
      'data-val-myrule': 'Error.',
      'data-val-myrule-some-compound': 'value',
    });
    const rules = parseRules(el);
    expect(rules).toEqual([
      { ruleName: 'myrule', errorMessage: 'Error.', params: { 'some-compound': 'value' } },
    ]);
  });

  test('ignores data-val attribute (no trailing dash)', () => {
    const el = input({ 'data-val': 'true' });
    expect(parseRules(el)).toEqual([]);
  });

  test('ignores attribute that is exactly "data-val-" (empty rule name)', () => {
    const el = input({ 'data-val-': 'some value' });
    const rules = parseRules(el);
    expect(rules).toEqual([]);
  });

  test('ignores non-data-val attributes alongside data-val-* attributes', () => {
    const el = input({
      type: 'text',
      name: 'email',
      'data-val-required': 'Email is required.',
      'aria-label': 'Email',
      class: 'form-input',
    });
    const rules = parseRules(el);
    expect(rules).toEqual([
      { ruleName: 'required', errorMessage: 'Email is required.', params: {} },
    ]);
  });

  test('ignores unrelated data-* attributes alongside data-val-* attributes', () => {
    const el = input({
      'data-bind': 'value: name',
      'data-tooltip': 'Enter name',
      'data-val-required': 'Name is required.',
    });
    const rules = parseRules(el);
    expect(rules).toHaveLength(1);
    expect(rules[0].ruleName).toBe('required');
  });
});

