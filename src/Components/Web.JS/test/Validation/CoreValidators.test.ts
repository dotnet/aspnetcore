import { expect, test, describe, beforeAll } from '@jest/globals';
import { registerCoreValidators } from '../../src/Validation/CoreValidators';
import { ValidatorRegistry, ValidationContext, ValidationResult, Validator } from '../../src/Validation/ValidationTypes';

// jsdom does not provide CSS.escape; polyfill for radio group tests
beforeAll(() => {
  if (typeof globalThis.CSS === 'undefined') {
    (globalThis as any).CSS = { escape: (v: string) => v.replace(/([^\w-])/g, '\\$1') };
  }
});

// Returns a function that yields the boolean success of the named validator.
// Existing tests assert against this boolean; tests that need to inspect the
// full ValidationResult shape (e.g., custom message) call `getValidatorRaw`.
function getValidator(name: string): (ctx: ValidationContext) => boolean {
  const v = getValidatorRaw(name);
  return (ctx: ValidationContext) => v(ctx).success;
}

function getValidatorRaw(name: string): Validator {
  const registry = new ValidatorRegistry();
  registerCoreValidators(registry);
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

describe('registerCoreValidators', () => {
  test('registers required and length validators', () => {
    const registry = new ValidatorRegistry();
    registerCoreValidators(registry);
    expect(registry.get('required')).toBeDefined();
    expect(registry.get('length')).toBeDefined();
  });
});

// ValidationResult is a structured object so custom validators can supply a
// per-call error message that overrides the rule's default. The built-in
// validators just produce { success: true } / { success: false } and rely on
// the rule's own message.
describe('ValidationResult shape', () => {
  test('built-in validators return a ValidationResult object', () => {
    const required = getValidatorRaw('required');
    const result = required({ value: '', element: document.createElement('input') as any, params: {} });
    expect(result).toEqual({ success: false });
  });

  test('built-in validators return success: true on pass', () => {
    const required = getValidatorRaw('required');
    const result = required({ value: 'x', element: document.createElement('input') as any, params: {} });
    expect(result).toEqual({ success: true });
  });

  test('custom validators can supply a per-call message via the result', () => {
    const registry = new ValidatorRegistry();
    registerCoreValidators(registry);
    registry.set('custom', () => ({ success: false, message: 'override' }));
    const result = registry.get('custom')!({ value: '', element: document.createElement('input') as any, params: {} });
    expect(result).toEqual({ success: false, message: 'override' });
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

  test('throws when no constraints are specified', () => {
    expect(() => length(makeContext({ value: 'anything', params: {} }))).toThrow(/min.*max/);
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

    test('throws when both bounds are absent', () => {
      expect(() => range(makeContext({ value: '999999', params: {} }))).toThrow(/min.*max/);
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

  test('throws when pattern param is missing', () => {
    expect(() => regex(makeContext({ value: 'anything', params: {} }))).toThrow(/pattern/);
  });
});

// Validates email format with the same semantics as .NET's EmailAddressAttribute.
// Empty values pass (emptiness is [Required]'s concern).
describe('emailValidator', () => {
  const email = getValidator('email');

  describe('empty values are not validated', () => {
    test('accepts empty string', () => {
      expect(email(makeContext({ value: '' }))).toBe(true);
    });

    test('accepts null', () => {
      expect(email(makeContext({ value: null }))).toBe(true);
    });

    test('accepts undefined', () => {
      expect(email(makeContext({ value: undefined }))).toBe(true);
    });
  });

  describe('valid emails', () => {
    test('accepts simple email', () => {
      expect(email(makeContext({ value: 'user@example.com' }))).toBe(true);
    });

    test('accepts email with subdomain', () => {
      expect(email(makeContext({ value: 'user@mail.example.com' }))).toBe(true);
    });

    test('accepts email with dots in local part', () => {
      expect(email(makeContext({ value: 'first.last@example.com' }))).toBe(true);
    });

    test('accepts email with plus tag', () => {
      expect(email(makeContext({ value: 'user+tag@example.com' }))).toBe(true);
    });

    test('accepts email with digits', () => {
      expect(email(makeContext({ value: 'user123@example456.com' }))).toBe(true);
    });

    test('accepts email with hyphens in domain', () => {
      expect(email(makeContext({ value: 'user@my-domain.com' }))).toBe(true);
    });

    test('accepts single-char local part', () => {
      expect(email(makeContext({ value: 'a@example.com' }))).toBe(true);
    });

    // Matches .NET's EmailAddressAttribute, which only requires a single '@'
    // not at the start or end. Whitespace inside the value is allowed.
    test('accepts value with internal whitespace (matches .NET)', () => {
      expect(email(makeContext({ value: 'user @example.com' }))).toBe(true);
    });

    // The server accepts addresses without a TLD; the client must too.
    test('accepts value without TLD (matches .NET)', () => {
      expect(email(makeContext({ value: 'a@b' }))).toBe(true);
    });
  });

  describe('invalid emails', () => {
    test('rejects missing @', () => {
      expect(email(makeContext({ value: 'userexample.com' }))).toBe(false);
    });

    test('rejects missing local part', () => {
      expect(email(makeContext({ value: '@example.com' }))).toBe(false);
    });

    test('rejects missing domain', () => {
      expect(email(makeContext({ value: 'user@' }))).toBe(false);
    });

    test('rejects double @', () => {
      expect(email(makeContext({ value: 'user@@example.com' }))).toBe(false);
    });

    test('rejects two separated @', () => {
      expect(email(makeContext({ value: 'a@b@c' }))).toBe(false);
    });

    test('rejects plain text', () => {
      expect(email(makeContext({ value: 'not-an-email' }))).toBe(false);
    });

    test('rejects value containing CR', () => {
      expect(email(makeContext({ value: 'user\r@example.com' }))).toBe(false);
    });

    test('rejects value containing LF', () => {
      expect(email(makeContext({ value: 'user@example.com\n' }))).toBe(false);
    });
  });
});

// Validates URL format with the same semantics as .NET's UrlAttribute.
// Empty values pass (emptiness is [Required]'s concern).
describe('urlValidator', () => {
  const url = getValidator('url');

  describe('empty values are not validated', () => {
    test('accepts empty string', () => {
      expect(url(makeContext({ value: '' }))).toBe(true);
    });

    test('accepts null', () => {
      expect(url(makeContext({ value: null }))).toBe(true);
    });

    test('accepts undefined', () => {
      expect(url(makeContext({ value: undefined }))).toBe(true);
    });
  });

  describe('valid URLs', () => {
    test('accepts http URL', () => {
      expect(url(makeContext({ value: 'http://example.com' }))).toBe(true);
    });

    test('accepts https URL', () => {
      expect(url(makeContext({ value: 'https://example.com' }))).toBe(true);
    });

    test('accepts ftp URL', () => {
      expect(url(makeContext({ value: 'ftp://example.com' }))).toBe(true);
    });

    test('accepts URL with path', () => {
      expect(url(makeContext({ value: 'https://example.com/path/to/page' }))).toBe(true);
    });

    test('accepts URL with query string', () => {
      expect(url(makeContext({ value: 'https://example.com?q=1&b=2' }))).toBe(true);
    });

    test('accepts URL with port', () => {
      expect(url(makeContext({ value: 'https://example.com:8080' }))).toBe(true);
    });

    test('accepts URL with subdomain', () => {
      expect(url(makeContext({ value: 'https://www.example.com' }))).toBe(true);
    });

    // .NET UrlAttribute uses case-insensitive prefix matching.
    test('accepts URL with uppercase scheme (matches .NET)', () => {
      expect(url(makeContext({ value: 'HTTPS://example.com' }))).toBe(true);
    });

    test('accepts URL with mixed-case scheme (matches .NET)', () => {
      expect(url(makeContext({ value: 'Http://example.com' }))).toBe(true);
    });

    // .NET only checks the scheme prefix, so localhost/private hosts pass.
    test('accepts http://localhost (matches .NET)', () => {
      expect(url(makeContext({ value: 'http://localhost' }))).toBe(true);
    });

    // .NET accepts whatever follows the scheme, including embedded whitespace.
    test('accepts URL with embedded whitespace (matches .NET)', () => {
      expect(url(makeContext({ value: 'https://exam ple.com' }))).toBe(true);
    });
  });

  describe('invalid URLs', () => {
    test('rejects plain text', () => {
      expect(url(makeContext({ value: 'not-a-url' }))).toBe(false);
    });

    test('rejects missing protocol', () => {
      expect(url(makeContext({ value: 'example.com' }))).toBe(false);
    });

    test('rejects single word', () => {
      expect(url(makeContext({ value: 'hello' }))).toBe(false);
    });

    // .NET UrlAttribute requires http/https/ftp - protocol-relative URLs are not accepted.
    test('rejects protocol-relative URL (matches .NET)', () => {
      expect(url(makeContext({ value: '//example.com' }))).toBe(false);
    });

    test('rejects unsupported scheme', () => {
      expect(url(makeContext({ value: 'gopher://example.com' }))).toBe(false);
    });

    test('rejects mailto scheme', () => {
      expect(url(makeContext({ value: 'mailto:user@example.com' }))).toBe(false);
    });

    test('rejects scheme without ://', () => {
      expect(url(makeContext({ value: 'http:example.com' }))).toBe(false);
    });

    test('rejects leading whitespace before scheme', () => {
      expect(url(makeContext({ value: ' http://example.com' }))).toBe(false);
    });
  });
});

// Matches .NET PhoneAttribute behavior:
// - Null/empty passes
// - Strips leading '+' and trailing extensions (ext./ext/x + digits)
// - Must contain at least one digit
// - Only digits, whitespace, and -.() allowed
describe('phoneValidator', () => {
  const phone = getValidator('phone');

  describe('empty values are not validated', () => {
    test('accepts empty string', () => {
      expect(phone(makeContext({ value: '' }))).toBe(true);
    });

    test('accepts null', () => {
      expect(phone(makeContext({ value: null }))).toBe(true);
    });

    test('accepts undefined', () => {
      expect(phone(makeContext({ value: undefined }))).toBe(true);
    });
  });

  describe('valid phone numbers', () => {
    test('accepts digits only', () => {
      expect(phone(makeContext({ value: '5551234567' }))).toBe(true);
    });

    test('accepts formatted US number', () => {
      expect(phone(makeContext({ value: '(555) 123-4567' }))).toBe(true);
    });

    test('accepts dashes', () => {
      expect(phone(makeContext({ value: '555-123-4567' }))).toBe(true);
    });

    test('accepts dots', () => {
      expect(phone(makeContext({ value: '555.123.4567' }))).toBe(true);
    });

    test('accepts spaces', () => {
      expect(phone(makeContext({ value: '555 123 4567' }))).toBe(true);
    });

    test('accepts leading plus', () => {
      expect(phone(makeContext({ value: '+1-555-123-4567' }))).toBe(true);
    });

    test('accepts international format', () => {
      expect(phone(makeContext({ value: '+44 20 7946 0958' }))).toBe(true);
    });

    test('accepts with ext. extension', () => {
      expect(phone(makeContext({ value: '555-1234 ext. 5678' }))).toBe(true);
    });

    test('accepts with ext extension', () => {
      expect(phone(makeContext({ value: '555-1234 ext5678' }))).toBe(true);
    });

    test('accepts with x extension', () => {
      expect(phone(makeContext({ value: '555-1234 x5678' }))).toBe(true);
    });
  });

  describe('invalid phone numbers', () => {
    test('rejects alphabetic string', () => {
      expect(phone(makeContext({ value: 'abcdef' }))).toBe(false);
    });

    test('rejects letters mixed with digits', () => {
      expect(phone(makeContext({ value: '555-abc-1234' }))).toBe(false);
    });

    test('rejects no digits at all', () => {
      expect(phone(makeContext({ value: '(--)' }))).toBe(false);
    });

    test('rejects special characters', () => {
      expect(phone(makeContext({ value: '555@1234' }))).toBe(false);
    });
  });
});

// Matches .NET CreditCardAttribute behavior (Luhn algorithm).
// Empty values pass. Strips dashes and spaces before validation.
// Length check: 13-19 digits.
describe('creditcardValidator', () => {
  const creditcard = getValidator('creditcard');

  describe('empty values are not validated', () => {
    test('accepts empty string', () => {
      expect(creditcard(makeContext({ value: '' }))).toBe(true);
    });

    test('accepts null', () => {
      expect(creditcard(makeContext({ value: null }))).toBe(true);
    });

    test('accepts undefined', () => {
      expect(creditcard(makeContext({ value: undefined }))).toBe(true);
    });
  });

  describe('valid card numbers', () => {
    test('accepts valid Visa number', () => {
      expect(creditcard(makeContext({ value: '4111111111111111' }))).toBe(true);
    });

    test('accepts valid Mastercard number', () => {
      expect(creditcard(makeContext({ value: '5500000000000004' }))).toBe(true);
    });

    test('accepts with dashes', () => {
      expect(creditcard(makeContext({ value: '4111-1111-1111-1111' }))).toBe(true);
    });

    test('accepts with spaces', () => {
      expect(creditcard(makeContext({ value: '4111 1111 1111 1111' }))).toBe(true);
    });
  });

  describe('invalid card numbers', () => {
    test('rejects Luhn-invalid number', () => {
      expect(creditcard(makeContext({ value: '1234567890123456' }))).toBe(false);
    });

    test('rejects too short (12 digits)', () => {
      expect(creditcard(makeContext({ value: '411111111111' }))).toBe(false);
    });

    test('rejects too long (20 digits)', () => {
      expect(creditcard(makeContext({ value: '41111111111111111111' }))).toBe(false);
    });

    test('rejects letters', () => {
      expect(creditcard(makeContext({ value: 'abcd1234abcd1234' }))).toBe(false);
    });

    test('rejects special characters', () => {
      expect(creditcard(makeContext({ value: '4111@1111#1111$1111' }))).toBe(false);
    });
  });
});

// Matches .NET CompareAttribute behavior:
// - Empty values pass
// - Strict string equality with another field's value
// - Resolves "*.PropertyName" to model-prefixed field name
describe('equaltoValidator', () => {
  const equalto = getValidator('equalto');

  function makeFormContext(
    fieldName: string,
    fieldValue: string,
    otherName: string,
    otherValue: string,
    params: Record<string, string>,
  ): ValidationContext {
    const form = document.createElement('form');
    const field = document.createElement('input');
    field.name = fieldName;
    field.value = fieldValue;
    form.appendChild(field);

    const other = document.createElement('input');
    other.name = otherName;
    other.value = otherValue;
    form.appendChild(other);

    return { value: fieldValue, element: field, params };
  }

  describe('empty values are not validated', () => {
    test('accepts empty string', () => {
      expect(equalto(makeContext({ value: '', params: { other: '*.Confirm' } }))).toBe(true);
    });

    test('accepts null', () => {
      expect(equalto(makeContext({ value: null, params: { other: '*.Confirm' } }))).toBe(true);
    });
  });

  describe('matching values', () => {
    test('accepts when values match exactly', () => {
      const ctx = makeFormContext('Password', 'secret', 'Confirm', 'secret', { other: '*.Confirm' });
      expect(equalto(ctx)).toBe(true);
    });

    test('accepts when both fields are empty', () => {
      const ctx = makeFormContext('Password', '', 'Confirm', '', { other: '*.Confirm' });
      // value is empty so validator returns true before comparison
      expect(equalto(ctx)).toBe(true);
    });
  });

  describe('non-matching values', () => {
    test('rejects when values differ', () => {
      const ctx = makeFormContext('Password', 'secret', 'Confirm', 'different', { other: '*.Confirm' });
      expect(equalto(ctx)).toBe(false);
    });

    test('rejects when other field is empty but current is not', () => {
      const ctx = makeFormContext('Password', 'secret', 'Confirm', '', { other: '*.Confirm' });
      expect(equalto(ctx)).toBe(false);
    });
  });

  describe('other field resolution', () => {
    test('resolves *.PropertyName with no prefix (simple model)', () => {
      const ctx = makeFormContext('Password', 'abc', 'ConfirmPassword', 'abc', { other: '*.ConfirmPassword' });
      expect(equalto(ctx)).toBe(true);
    });

    test('resolves *.PropertyName with dotted prefix (nested model)', () => {
      const ctx = makeFormContext('User.Password', 'abc', 'User.ConfirmPassword', 'abc', { other: '*.ConfirmPassword' });
      expect(equalto(ctx)).toBe(true);
    });

    test('passes when other field does not exist in form', () => {
      const form = document.createElement('form');
      const field = document.createElement('input');
      field.name = 'Password';
      field.value = 'secret';
      form.appendChild(field);

      const ctx: ValidationContext = { value: 'secret', element: field, params: { other: '*.NonExistent' } };
      expect(equalto(ctx)).toBe(true);
    });

    test('throws when other param is missing', () => {
      const ctx = makeFormContext('Password', 'secret', 'Confirm', 'different', {});
      expect(() => equalto(ctx)).toThrow(/other/);
    });
  });
});

// Matches .NET FileExtensionsAttribute behavior:
// - Null/empty passes
// - Case-insensitive extension matching
// - Extensions param format: ".png,.jpg,.gif" (dot-prefixed, comma-separated)
describe('fileextensionsValidator', () => {
  const fileext = getValidator('fileextensions');
  const imgExtensions = { extensions: '.png,.jpg,.jpeg,.gif' };

  describe('empty values are not validated', () => {
    test('accepts empty string', () => {
      expect(fileext(makeContext({ value: '', params: imgExtensions }))).toBe(true);
    });

    test('accepts null', () => {
      expect(fileext(makeContext({ value: null, params: imgExtensions }))).toBe(true);
    });

    test('accepts undefined', () => {
      expect(fileext(makeContext({ value: undefined, params: imgExtensions }))).toBe(true);
    });
  });

  describe('valid file extensions', () => {
    test('accepts matching extension', () => {
      expect(fileext(makeContext({ value: 'photo.png', params: imgExtensions }))).toBe(true);
    });

    test('accepts case-insensitive match', () => {
      expect(fileext(makeContext({ value: 'photo.PNG', params: imgExtensions }))).toBe(true);
    });

    test('accepts another allowed extension', () => {
      expect(fileext(makeContext({ value: 'photo.jpg', params: imgExtensions }))).toBe(true);
    });

    test('accepts file with multiple dots in name', () => {
      expect(fileext(makeContext({ value: 'my.vacation.photo.jpeg', params: imgExtensions }))).toBe(true);
    });
  });

  describe('invalid file extensions', () => {
    test('rejects disallowed extension', () => {
      expect(fileext(makeContext({ value: 'script.exe', params: imgExtensions }))).toBe(false);
    });

    test('rejects file with no extension', () => {
      expect(fileext(makeContext({ value: 'readme', params: imgExtensions }))).toBe(false);
    });
  });

  test('throws when extensions param is missing', () => {
    expect(() => fileext(makeContext({ value: 'anything.xyz', params: {} }))).toThrow(/extensions/);
  });
});


