import { expect, test, describe, beforeAll, afterEach } from '@jest/globals';
import { registerCoreValidators } from '../../src/Validation/CoreValidators';
import { ErrorDisplay } from '../../src/Validation/ErrorDisplay';
import { ElementState, ValidationEngine } from '../../src/Validation/ValidationEngine';
import { ValidatorRegistry } from '../../src/Validation/ValidationTypes';

beforeAll(() => {
  // jsdom does not provide CSS.escape; the engine's DOM helpers use it.
  if (typeof globalThis.CSS === 'undefined') {
    (globalThis as any).CSS = { escape: (v: string) => v.replace(/([^\w-])/g, '\\$1') };
  }
  // jsdom performs no layout, so offsetParent is always null and shouldSkipElement (used by validateForm)
  // would skip every field.
  // Test mock: visible fields report a non-null offsetParent, fields marked hidden report null.
  Object.defineProperty(HTMLElement.prototype, 'offsetParent', {
    configurable: true,
    get(): Element | null {
      return (this as HTMLElement).hidden ? null : ((this as HTMLElement).closest('form') ?? document.body);
    },
  });
});

afterEach(() => {
  document.body.innerHTML = '';
});

function makeEngine(): ValidationEngine {
  const registry = new ValidatorRegistry();
  registerCoreValidators(registry);
  return new ValidationEngine(registry, new ErrorDisplay());
}

// Adds a text input with a single 'required' rule and registers it with the engine.
function addRequiredField(engine: ValidationEngine, form: HTMLFormElement, name: string): HTMLInputElement {
  const input = document.createElement('input');
  input.name = name;
  form.appendChild(input);
  const state: ElementState = {
    rules: [{ ruleName: 'required', errorMessage: `${name} is required.`, params: {} }],
    form,
    triggerEvents: 'default',
    listenerController: new AbortController(),
    hasBeenInvalid: false,
  };
  engine.registerElement(input, form, state);
  return input;
}

describe('ValidationEngine.validateForm', () => {
  test('validates fields, skips hidden fields, focuses the first invalid, and returns the error map', () => {
    const engine = makeEngine();
    const form = document.createElement('form');
    document.body.appendChild(form);

    const first = addRequiredField(engine, form, 'First'); // visible, empty -> invalid
    const hidden = addRequiredField(engine, form, 'Hidden'); // will be hidden -> skipped
    addRequiredField(engine, form, 'Last'); // visible, empty -> invalid

    // Seed a stale error on the field before hiding it, so the skip path is proven to clear it.
    engine.validateElement(hidden);
    expect(hidden.validationMessage).toBe('Hidden is required.');
    hidden.hidden = true;

    const errors = engine.validateForm(form);

    // Only the two visible invalid fields are reported, keyed by field name.
    expect([...errors.keys()]).toEqual(['First', 'Last']);
    expect(errors.get('First')).toBe('First is required.');
    expect(errors.get('Last')).toBe('Last is required.');

    // The skipped hidden field was marked valid: its prior error is cleared.
    expect(hidden.validationMessage).toBe('');
    expect(engine.getElementState(hidden)?.currentError).toBeUndefined();

    // The first invalid field receives focus.
    expect(document.activeElement).toBe(first);
  });
});
