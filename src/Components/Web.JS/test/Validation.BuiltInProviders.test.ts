import { expect, test, describe } from '@jest/globals';
import { ValidationEngine } from '../src/Validation/ValidationEngine';
import { registerBuiltInProviders } from '../src/Validation/BuiltInProviders';
import { ValidatableElement } from '../src/Validation/Types';

// Create a shared engine with all built-in providers registered
const engine = new ValidationEngine();
registerBuiltInProviders(engine);

// Helper to create a minimal mock input element for testing
function mockInput(attrs?: Record<string, string>): ValidatableElement {
  const el = document.createElement('input') as HTMLInputElement;
  if (attrs) {
    for (const [k, v] of Object.entries(attrs)) {
      el.setAttribute(k, v);
    }
  }
  return el;
}

const noParams: Record<string, string> = {};

describe('url provider', () => {
  const validate = engine.getProvider('url')!;
  const el = mockInput();

  test('empty value is valid', () => {
    expect(validate('', el, noParams)).toBe(true);
  });

  test('http:// URL is valid', () => {
    expect(validate('http://example.com', el, noParams)).toBe(true);
  });

  test('https:// URL is valid', () => {
    expect(validate('https://example.com', el, noParams)).toBe(true);
  });

  test('ftp:// URL is valid', () => {
    expect(validate('ftp://files.example.com', el, noParams)).toBe(true);
  });

  test('bare http:// is valid (matches UrlAttribute)', () => {
    expect(validate('http://', el, noParams)).toBe(true);
  });

  test('case insensitive scheme', () => {
    expect(validate('HTTP://EXAMPLE.COM', el, noParams)).toBe(true);
    expect(validate('Https://Example.com', el, noParams)).toBe(true);
  });

  test('missing scheme is invalid', () => {
    expect(validate('example.com', el, noParams)).toBe(false);
  });

  test('mailto: scheme is invalid', () => {
    expect(validate('mailto:user@example.com', el, noParams)).toBe(false);
  });

  test('file:// scheme is invalid', () => {
    expect(validate('file:///path', el, noParams)).toBe(false);
  });

  test('just text is invalid', () => {
    expect(validate('not a url', el, noParams)).toBe(false);
  });
});

describe('phone provider', () => {
  const validate = engine.getProvider('phone')!;
  const el = mockInput();

  test('empty value is valid', () => {
    expect(validate('', el, noParams)).toBe(true);
  });

  // Basic valid numbers
  test('digits only', () => {
    expect(validate('1234567890', el, noParams)).toBe(true);
  });

  test('formatted US number', () => {
    expect(validate('(425) 555-0123', el, noParams)).toBe(true);
  });

  test('number with leading +', () => {
    expect(validate('+1 (425) 555-0123', el, noParams)).toBe(true);
  });

  test('international format with dots', () => {
    expect(validate('+44.20.7946.0958', el, noParams)).toBe(true);
  });

  test('number with dashes', () => {
    expect(validate('425-555-0123', el, noParams)).toBe(true);
  });

  test('number with spaces', () => {
    expect(validate('425 555 0123', el, noParams)).toBe(true);
  });

  // Extension handling (matches PhoneAttribute.RemoveExtension)
  test('number with ext. extension', () => {
    expect(validate('425-555-0123 ext. 1234', el, noParams)).toBe(true);
  });

  test('number with ext extension', () => {
    expect(validate('425-555-0123 ext 5678', el, noParams)).toBe(true);
  });

  test('number with x extension', () => {
    expect(validate('425-555-0123 x99', el, noParams)).toBe(true);
  });

  test('number with EXT. (case insensitive)', () => {
    expect(validate('425-555-0123 EXT. 42', el, noParams)).toBe(true);
  });

  test('extension must have digits after it', () => {
    // "ext." with no digits following is NOT treated as extension
    // "425ext." → chars e,x,t not allowed → invalid
    expect(validate('425ext.', el, noParams)).toBe(false);
  });

  // Trailing whitespace (matches .NET .TrimEnd())
  test('trailing whitespace is trimmed', () => {
    expect(validate('425-555-0123   ', el, noParams)).toBe(true);
  });

  // Invalid numbers
  test('no digits at all', () => {
    expect(validate('---', el, noParams)).toBe(false);
  });

  test('letters are invalid', () => {
    expect(validate('425-CALL-NOW', el, noParams)).toBe(false);
  });

  test('special characters are invalid', () => {
    expect(validate('425#555@0123', el, noParams)).toBe(false);
  });

  test('whitespace-only after processing is invalid', () => {
    expect(validate('   ', el, noParams)).toBe(false);
  });

  test('multiple + signs are stripped', () => {
    expect(validate('++1-425-555-0123', el, noParams)).toBe(true);
  });

  test('+ in middle is stripped', () => {
    expect(validate('+1+425', el, noParams)).toBe(true);
  });
});

describe('regex provider (full-string match)', () => {
  const validate = engine.getProvider('regex')!;
  const el = mockInput();

  test('empty value is valid', () => {
    expect(validate('', el, { pattern: '\\d+' })).toBe(true);
  });

  test('no pattern param is valid', () => {
    expect(validate('anything', el, {})).toBe(true);
  });

  test('exact match passes', () => {
    expect(validate('12345', el, { pattern: '\\d+' })).toBe(true);
  });

  test('partial match fails (pattern matches substring but not entire string)', () => {
    expect(validate('abc123def', el, { pattern: '\\d+' })).toBe(false);
  });

  test('leading mismatch fails', () => {
    expect(validate('abc123', el, { pattern: '\\d+' })).toBe(false);
  });

  test('trailing mismatch fails', () => {
    expect(validate('123abc', el, { pattern: '\\d+' })).toBe(false);
  });

  test('pattern with anchors works', () => {
    expect(validate('hello', el, { pattern: '^hello$' })).toBe(true);
    expect(validate('hello world', el, { pattern: '^hello$' })).toBe(false);
  });

  test('complex pattern full-match', () => {
    expect(validate('abc-123', el, { pattern: '[a-z]+-\\d+' })).toBe(true);
    expect(validate('ABC-123', el, { pattern: '[a-z]+-\\d+' })).toBe(false);
  });

  test('dot-star matches everything', () => {
    expect(validate('literally anything', el, { pattern: '.*' })).toBe(true);
  });

  test('alternation full-match', () => {
    expect(validate('cat', el, { pattern: 'cat|dog' })).toBe(true);
    expect(validate('dog', el, { pattern: 'cat|dog' })).toBe(true);
    expect(validate('catdog', el, { pattern: 'cat|dog' })).toBe(false);
  });
});

describe('creditcard provider (Luhn algorithm)', () => {
  const validate = engine.getProvider('creditcard')!;
  const el = mockInput();

  test('empty value is valid', () => {
    expect(validate('', el, noParams)).toBe(true);
  });

  // Known valid credit card test numbers (Luhn-valid)
  test('valid Visa test number', () => {
    expect(validate('4111111111111111', el, noParams)).toBe(true);
  });

  test('valid MasterCard test number', () => {
    expect(validate('5500000000000004', el, noParams)).toBe(true);
  });

  test('valid Amex test number', () => {
    expect(validate('378282246310005', el, noParams)).toBe(true);
  });

  test('valid number with dashes', () => {
    expect(validate('4111-1111-1111-1111', el, noParams)).toBe(true);
  });

  test('valid number with spaces', () => {
    expect(validate('4111 1111 1111 1111', el, noParams)).toBe(true);
  });

  test('valid number with mixed separators', () => {
    expect(validate('4111-1111 1111-1111', el, noParams)).toBe(true);
  });

  // Invalid numbers
  test('invalid Luhn checksum', () => {
    expect(validate('4111111111111112', el, noParams)).toBe(false);
  });

  test('all zeros is invalid (checksum is 0 but no digits processed correctly)', () => {
    // All zeros: checksum = 0, (0 % 10 === 0) → actually passes Luhn
    // .NET CreditCardAttribute also returns true for all zeros
    expect(validate('0000000000000000', el, noParams)).toBe(true);
  });

  test('letters are invalid', () => {
    expect(validate('4111abcd11111111', el, noParams)).toBe(false);
  });

  test('special characters are invalid', () => {
    expect(validate('4111#1111#1111', el, noParams)).toBe(false);
  });

  test('single digit passes Luhn if digit is 0', () => {
    // Single '0': checksum = 0, 0 % 10 === 0 → valid (matches .NET)
    expect(validate('0', el, noParams)).toBe(true);
  });

  test('single non-zero digit', () => {
    // Single '1': checksum = 1, 1 % 10 !== 0 → invalid
    expect(validate('1', el, noParams)).toBe(false);
  });
});

describe('equalto provider (Compare)', () => {
  const validate = engine.getProvider('equalto')!;

  test('no other param is valid', () => {
    const el = mockInput();
    expect(validate('anything', el, {})).toBe(true);
  });

  test('matching values are valid', () => {
    const form = document.createElement('form');
    const password = document.createElement('input');
    password.setAttribute('name', 'Model.Password');
    password.value = 'secret123';
    form.appendChild(password);

    const confirm = document.createElement('input');
    confirm.setAttribute('name', 'Model.ConfirmPassword');
    form.appendChild(confirm);

    expect(validate('secret123', confirm, { other: '*.Password' })).toBe(true);
  });

  test('different values are invalid', () => {
    const form = document.createElement('form');
    const password = document.createElement('input');
    password.setAttribute('name', 'Model.Password');
    password.value = 'secret123';
    form.appendChild(password);

    const confirm = document.createElement('input');
    confirm.setAttribute('name', 'Model.ConfirmPassword');
    form.appendChild(confirm);

    expect(validate('different', confirm, { other: '*.Password' })).toBe(false);
  });

  test('resolves without prefix when field has no dot', () => {
    const form = document.createElement('form');
    const password = document.createElement('input');
    password.setAttribute('name', 'Password');
    password.value = 'mypass';
    form.appendChild(password);

    const confirm = document.createElement('input');
    confirm.setAttribute('name', 'ConfirmPassword');
    form.appendChild(confirm);

    expect(validate('mypass', confirm, { other: '*.Password' })).toBe(true);
  });

  test('other element not found is valid', () => {
    const form = document.createElement('form');
    const confirm = document.createElement('input');
    confirm.setAttribute('name', 'Model.ConfirmPassword');
    form.appendChild(confirm);

    expect(validate('anything', confirm, { other: '*.NonExistent' })).toBe(true);
  });

  test('element not in form is valid', () => {
    const el = mockInput({ name: 'ConfirmPassword' });
    expect(validate('anything', el, { other: '*.Password' })).toBe(true);
  });
});

describe('fileextensions provider', () => {
  const validate = engine.getProvider('fileextensions')!;
  const el = mockInput();

  test('empty value is valid', () => {
    expect(validate('', el, noParams)).toBe(true);
  });

  // Default extensions: png, jpg, jpeg, gif
  test('png is valid with defaults', () => {
    expect(validate('photo.png', el, {})).toBe(true);
  });

  test('jpg is valid with defaults', () => {
    expect(validate('photo.jpg', el, {})).toBe(true);
  });

  test('jpeg is valid with defaults', () => {
    expect(validate('photo.jpeg', el, {})).toBe(true);
  });

  test('gif is valid with defaults', () => {
    expect(validate('animation.gif', el, {})).toBe(true);
  });

  test('case insensitive extension', () => {
    expect(validate('photo.PNG', el, {})).toBe(true);
    expect(validate('photo.Jpg', el, {})).toBe(true);
  });

  test('pdf is invalid with defaults', () => {
    expect(validate('document.pdf', el, {})).toBe(false);
  });

  test('no extension is invalid', () => {
    expect(validate('noextension', el, {})).toBe(false);
  });

  test('custom extensions', () => {
    const params = { extensions: 'pdf,doc,docx' };
    expect(validate('document.pdf', el, params)).toBe(true);
    expect(validate('document.doc', el, params)).toBe(true);
    expect(validate('document.docx', el, params)).toBe(true);
    expect(validate('photo.png', el, params)).toBe(false);
  });

  test('extensions with dots and spaces are normalized', () => {
    const params = { extensions: '.pdf, .doc, .docx' };
    expect(validate('document.pdf', el, params)).toBe(true);
    expect(validate('document.doc', el, params)).toBe(true);
  });

  test('path with multiple dots uses last extension', () => {
    expect(validate('archive.backup.png', el, {})).toBe(true);
    expect(validate('archive.png.exe', el, {})).toBe(false);
  });
});
