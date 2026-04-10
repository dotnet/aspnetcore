import { expect, test, describe, beforeAll } from '@jest/globals';
import { registerBuiltInValidators } from '../../src/Validation/BuiltInValidators';
import { ValidatorRegistry, ValidationContext, Validator } from '../../src/Validation/Validator';

// jsdom does not provide CSS.escape; polyfill for radio group tests
beforeAll(() => {
  if (typeof globalThis.CSS === 'undefined') {
    (globalThis as any).CSS = { escape: (v: string) => v.replace(/([^\w-])/g, '\\$1') };
  }
});

function getValidator(name: string): Validator {
  const registry = new ValidatorRegistry();
  registerBuiltInValidators(registry);
  const v = registry.get(name);
  if (!v) throw new Error(`Validator '${name}' not found`);
  return v;
}

function makeContext(overrides: Partial<ValidationContext> & { element?: HTMLElement } = {}): ValidationContext {
  return {
    value: overrides.value ?? '',
    element: (overrides.element ?? document.createElement('input')) as any,
    params: overrides.params ?? {},
  };
}

function makeCheckbox(checked: boolean): HTMLInputElement {
  const el = document.createElement('input');
  el.type = 'checkbox';
  el.checked = checked;
  return el;
}

function makeRadioGroup(name: string, ...radios: { checked: boolean; name?: string }[]): HTMLInputElement[] {
  const form = document.createElement('form');
  return radios.map(r => {
    const el = document.createElement('input');
    el.type = 'radio';
    el.name = r.name ?? name;
    el.checked = r.checked;
    form.appendChild(el);
    return el;
  });
}

describe('registerBuiltInValidators', () => {
  test('registers required and length validators', () => {
    const registry = new ValidatorRegistry();
    registerBuiltInValidators(registry);
    expect(registry.get('required')).toBeDefined();
    expect(registry.get('length')).toBeDefined();
  });
});

// Matches .NET [Required] behavior:
// - Null/empty/whitespace-only values are invalid (AllowEmptyStrings = false by default)
// - Checkboxes must be checked (like [Required] on a bool property)
// - Radio groups must have a selection
describe('requiredValidator', () => {
  const required = getValidator('required');

  describe('text input', () => {
    test('rejects null', () => {
      expect(required(makeContext({ value: null }))).toBe(false);
    });

    test('rejects undefined', () => {
      expect(required(makeContext({ value: undefined }))).toBe(false);
    });

    test('rejects empty string', () => {
      expect(required(makeContext({ value: '' }))).toBe(false);
    });

    test('rejects whitespace-only string', () => {
      expect(required(makeContext({ value: '   ' }))).toBe(false);
    });

    test('rejects string containing only tabs and newlines', () => {
      expect(required(makeContext({ value: '\t\n ' }))).toBe(false);
    });

    test('accepts non-empty value', () => {
      expect(required(makeContext({ value: 'hello' }))).toBe(true);
    });

    test('accepts value with non-whitespace content surrounded by spaces', () => {
      expect(required(makeContext({ value: '  a  ' }))).toBe(true);
    });
  });

  describe('checkbox', () => {
    test('accepts when checked', () => {
      expect(required(makeContext({ element: makeCheckbox(true), value: 'on' }))).toBe(true);
    });

    test('rejects when unchecked', () => {
      expect(required(makeContext({ element: makeCheckbox(false), value: '' }))).toBe(false);
    });

    test('only the checked state matters, not the value (checked with empty value)', () => {
      expect(required(makeContext({ element: makeCheckbox(true), value: '' }))).toBe(true);
    });

    test('only the checked state matters, not the value (unchecked with non-empty value)', () => {
      expect(required(makeContext({ element: makeCheckbox(false), value: 'yes' }))).toBe(false);
    });
  });

  describe('radio group', () => {
    test('accepts when any radio in the group is selected', () => {
      const radios = makeRadioGroup('color', { checked: false }, { checked: true });
      expect(required(makeContext({ element: radios[0], value: '' }))).toBe(true);
    });

    test('rejects when no radio in the group is selected', () => {
      const radios = makeRadioGroup('color', { checked: false }, { checked: false });
      expect(required(makeContext({ element: radios[0], value: '' }))).toBe(false);
    });

    test('only considers radios with the same name attribute', () => {
      const radios = makeRadioGroup('color', { checked: false }, { checked: true, name: 'size' });
      expect(required(makeContext({ element: radios[0], value: '' }))).toBe(false);
    });

    test('handles name attributes containing special characters', () => {
      const radios = makeRadioGroup('field.name[0]', { checked: false }, { checked: true });
      expect(required(makeContext({ element: radios[0], value: '' }))).toBe(true);
    });

    test('rejects standalone radio not inside a form with no value', () => {
      const radio = document.createElement('input');
      radio.type = 'radio';
      radio.name = 'color';
      expect(required(makeContext({ element: radio, value: '' }))).toBe(false);
    });

    test('accepts standalone radio not inside a form with a value', () => {
      const radio = document.createElement('input');
      radio.type = 'radio';
      radio.name = 'color';
      expect(required(makeContext({ element: radio, value: 'red' }))).toBe(true);
    });
  });

  describe('select', () => {
    test('accepts when a value is selected', () => {
      const select = document.createElement('select');
      expect(required(makeContext({ element: select, value: 'option1' }))).toBe(true);
    });

    test('rejects when no value is selected', () => {
      const select = document.createElement('select');
      expect(required(makeContext({ element: select, value: '' }))).toBe(false);
    });
  });

  describe('textarea', () => {
    test('accepts non-empty content', () => {
      const textarea = document.createElement('textarea');
      expect(required(makeContext({ element: textarea, value: 'text' }))).toBe(true);
    });

    test('rejects whitespace-only content', () => {
      const textarea = document.createElement('textarea');
      expect(required(makeContext({ element: textarea, value: '   ' }))).toBe(false);
    });
  });
});

// Matches .NET [Length] / [StringLength] / [MinLength] / [MaxLength] behavior:
// - Null/empty values pass (emptiness is [Required]'s concern, not [Length]'s)
// - Boundaries are inclusive (min <= length <= max)
describe('lengthValidator', () => {
  const length = getValidator('length');

  describe('empty values are not validated', () => {
    test('accepts null', () => {
      expect(length(makeContext({ value: null, params: { min: '3' } }))).toBe(true);
    });

    test('accepts undefined', () => {
      expect(length(makeContext({ value: undefined, params: { min: '3' } }))).toBe(true);
    });

    test('accepts empty string', () => {
      expect(length(makeContext({ value: '', params: { min: '3' } }))).toBe(true);
    });
  });

  describe('minimum length', () => {
    test('rejects value shorter than minimum', () => {
      expect(length(makeContext({ value: 'ab', params: { min: '3' } }))).toBe(false);
    });

    test('accepts value exactly at minimum', () => {
      expect(length(makeContext({ value: 'abc', params: { min: '3' } }))).toBe(true);
    });

    test('accepts value longer than minimum', () => {
      expect(length(makeContext({ value: 'abcd', params: { min: '3' } }))).toBe(true);
    });
  });

  describe('maximum length', () => {
    test('rejects value longer than maximum', () => {
      expect(length(makeContext({ value: 'abcdef', params: { max: '5' } }))).toBe(false);
    });

    test('accepts value exactly at maximum', () => {
      expect(length(makeContext({ value: 'abcde', params: { max: '5' } }))).toBe(true);
    });

    test('accepts value shorter than maximum', () => {
      expect(length(makeContext({ value: 'abc', params: { max: '5' } }))).toBe(true);
    });
  });

  describe('minimum and maximum combined', () => {
    test('rejects value below minimum', () => {
      expect(length(makeContext({ value: 'ab', params: { min: '3', max: '5' } }))).toBe(false);
    });

    test('rejects value above maximum', () => {
      expect(length(makeContext({ value: 'abcdef', params: { min: '3', max: '5' } }))).toBe(false);
    });

    test('accepts value within range', () => {
      expect(length(makeContext({ value: 'abcd', params: { min: '3', max: '5' } }))).toBe(true);
    });

    test('accepts value at minimum boundary', () => {
      expect(length(makeContext({ value: 'abc', params: { min: '3', max: '5' } }))).toBe(true);
    });

    test('accepts value at maximum boundary', () => {
      expect(length(makeContext({ value: 'abcde', params: { min: '3', max: '5' } }))).toBe(true);
    });
  });

  describe('exact length (min equals max)', () => {
    test('accepts value of exact length', () => {
      expect(length(makeContext({ value: 'abc', params: { min: '3', max: '3' } }))).toBe(true);
    });

    test('rejects shorter value', () => {
      expect(length(makeContext({ value: 'ab', params: { min: '3', max: '3' } }))).toBe(false);
    });

    test('rejects longer value', () => {
      expect(length(makeContext({ value: 'abcd', params: { min: '3', max: '3' } }))).toBe(false);
    });
  });

  test('accepts any value when no constraints are specified', () => {
    expect(length(makeContext({ value: 'anything', params: {} }))).toBe(true);
  });
});

// Matches .NET [Range] behavior:
// - Null/empty values pass (emptiness is [Required]'s concern)
// - Boundaries are inclusive (min <= value <= max)
// - Uses numeric comparison via Number()
// - Non-numeric values are rejected
describe('rangeValidator', () => {
  const range = getValidator('range');

  describe('empty values are not validated', () => {
    test('accepts null', () => {
      expect(range(makeContext({ value: null, params: { min: '1', max: '100' } }))).toBe(true);
    });

    test('accepts undefined', () => {
      expect(range(makeContext({ value: undefined, params: { min: '1', max: '100' } }))).toBe(true);
    });

    test('accepts empty string', () => {
      expect(range(makeContext({ value: '', params: { min: '1', max: '100' } }))).toBe(true);
    });
  });

  describe('integer range', () => {
    test('rejects value below minimum', () => {
      expect(range(makeContext({ value: '0', params: { min: '1', max: '100' } }))).toBe(false);
    });

    test('accepts value exactly at minimum', () => {
      expect(range(makeContext({ value: '1', params: { min: '1', max: '100' } }))).toBe(true);
    });

    test('accepts value in the middle', () => {
      expect(range(makeContext({ value: '50', params: { min: '1', max: '100' } }))).toBe(true);
    });

    test('accepts value exactly at maximum', () => {
      expect(range(makeContext({ value: '100', params: { min: '1', max: '100' } }))).toBe(true);
    });

    test('rejects value above maximum', () => {
      expect(range(makeContext({ value: '101', params: { min: '1', max: '100' } }))).toBe(false);
    });
  });

  describe('decimal range', () => {
    test('accepts decimal value within range', () => {
      expect(range(makeContext({ value: '50.0', params: { min: '0.5', max: '99.5' } }))).toBe(true);
    });

    test('accepts decimal at minimum boundary', () => {
      expect(range(makeContext({ value: '0.5', params: { min: '0.5', max: '99.5' } }))).toBe(true);
    });

    test('rejects decimal below minimum', () => {
      expect(range(makeContext({ value: '0.4', params: { min: '0.5', max: '99.5' } }))).toBe(false);
    });

    test('accepts decimal at maximum boundary', () => {
      expect(range(makeContext({ value: '99.5', params: { min: '0.5', max: '99.5' } }))).toBe(true);
    });

    test('rejects decimal above maximum', () => {
      expect(range(makeContext({ value: '99.6', params: { min: '0.5', max: '99.5' } }))).toBe(false);
    });
  });

  describe('negative range', () => {
    test('accepts value in negative range', () => {
      expect(range(makeContext({ value: '-50', params: { min: '-100', max: '-1' } }))).toBe(true);
    });

    test('accepts boundary of negative range', () => {
      expect(range(makeContext({ value: '-100', params: { min: '-100', max: '-1' } }))).toBe(true);
    });

    test('rejects value outside negative range', () => {
      expect(range(makeContext({ value: '0', params: { min: '-100', max: '-1' } }))).toBe(false);
    });
  });

  describe('non-numeric input', () => {
    test('rejects alphabetic string', () => {
      expect(range(makeContext({ value: 'abc', params: { min: '1', max: '100' } }))).toBe(false);
    });

    test('rejects mixed alphanumeric', () => {
      expect(range(makeContext({ value: '123abc', params: { min: '1', max: '100' } }))).toBe(false);
    });
  });

  describe('partial params', () => {
    test('validates only minimum when max is absent', () => {
      expect(range(makeContext({ value: '50', params: { min: '10' } }))).toBe(true);
      expect(range(makeContext({ value: '5', params: { min: '10' } }))).toBe(false);
    });

    test('validates only maximum when min is absent', () => {
      expect(range(makeContext({ value: '50', params: { max: '100' } }))).toBe(true);
      expect(range(makeContext({ value: '150', params: { max: '100' } }))).toBe(false);
    });

    test('accepts any numeric value when both are absent', () => {
      expect(range(makeContext({ value: '999999', params: {} }))).toBe(true);
    });
  });
});

// Matches .NET [RegularExpression] behavior:
// - Null/empty values pass (emptiness is [Required]'s concern)
// - Full match required (pattern is anchored with ^(?:...)$)
// - Case-sensitive by default
describe('regexValidator', () => {
  const regex = getValidator('regex');

  describe('empty values are not validated', () => {
    test('accepts null', () => {
      expect(regex(makeContext({ value: null, params: { pattern: '[A-Z]+' } }))).toBe(true);
    });

    test('accepts undefined', () => {
      expect(regex(makeContext({ value: undefined, params: { pattern: '[A-Z]+' } }))).toBe(true);
    });

    test('accepts empty string', () => {
      expect(regex(makeContext({ value: '', params: { pattern: '[A-Z]+' } }))).toBe(true);
    });
  });

  describe('matching', () => {
    test('accepts value matching letter pattern', () => {
      expect(regex(makeContext({ value: 'Hello', params: { pattern: '[A-Za-z]+' } }))).toBe(true);
    });

    test('accepts value matching digit pattern', () => {
      expect(regex(makeContext({ value: '555-1234', params: { pattern: '\\d{3}-\\d{4}' } }))).toBe(true);
    });

    test('rejects value not matching pattern', () => {
      expect(regex(makeContext({ value: '12345', params: { pattern: '[A-Za-z]+' } }))).toBe(false);
    });
  });

  describe('full match (not partial)', () => {
    test('rejects partial match at start', () => {
      expect(regex(makeContext({ value: 'abc123', params: { pattern: '[A-Za-z]+' } }))).toBe(false);
    });

    test('rejects partial match in middle', () => {
      expect(regex(makeContext({ value: '123abc456', params: { pattern: '[A-Za-z]+' } }))).toBe(false);
    });

    test('rejects partial match at end', () => {
      expect(regex(makeContext({ value: '123abc', params: { pattern: '[A-Za-z]+' } }))).toBe(false);
    });
  });

  describe('already-anchored patterns', () => {
    test('works correctly when pattern has anchors', () => {
      expect(regex(makeContext({ value: 'abc', params: { pattern: '^[a-z]+$' } }))).toBe(true);
    });

    test('rejects non-match with anchored pattern', () => {
      expect(regex(makeContext({ value: 'ABC', params: { pattern: '^[a-z]+$' } }))).toBe(false);
    });
  });

  describe('alternation', () => {
    test('accepts first alternative', () => {
      expect(regex(makeContext({ value: 'cat', params: { pattern: 'cat|dog' } }))).toBe(true);
    });

    test('accepts second alternative', () => {
      expect(regex(makeContext({ value: 'dog', params: { pattern: 'cat|dog' } }))).toBe(true);
    });

    test('rejects non-matching alternative', () => {
      expect(regex(makeContext({ value: 'fish', params: { pattern: 'cat|dog' } }))).toBe(false);
    });
  });

  describe('case sensitivity', () => {
    test('rejects wrong case', () => {
      expect(regex(makeContext({ value: 'ABC', params: { pattern: '[a-z]+' } }))).toBe(false);
    });
  });

  test('accepts any value when pattern param is missing', () => {
    expect(regex(makeContext({ value: 'anything', params: {} }))).toBe(true);
  });
});
