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
  test('validates fields, skips hidden and disabled fields, focuses the first invalid, and returns the error map', () => {
    const engine = makeEngine();
    const form = document.createElement('form');
    document.body.appendChild(form);

    const first = addRequiredField(engine, form, 'First'); // visible, empty -> invalid
    const hidden = addRequiredField(engine, form, 'Hidden'); // will be hidden -> skipped
    const disabled = addRequiredField(engine, form, 'Disabled'); // will be disabled -> skipped
    addRequiredField(engine, form, 'Last'); // visible, empty -> invalid

    // Seed stale errors on the skipped fields, so the skip path is proven to clear them.
    engine.validateElement(hidden);
    engine.validateElement(disabled);
    expect(hidden.validationMessage).toBe('Hidden is required.');
    expect(disabled.validationMessage).toBe('Disabled is required.');
    hidden.hidden = true;
    disabled.disabled = true;

    const errors = engine.validateForm(form);

    // Only the two visible invalid fields are reported, keyed by field name.
    expect([...errors.keys()]).toEqual(['First', 'Last']);
    expect(errors.get('First')).toBe('First is required.');
    expect(errors.get('Last')).toBe('Last is required.');

    // The skipped hidden and disabled fields were marked valid: their prior errors are cleared.
    expect(hidden.validationMessage).toBe('');
    expect(engine.getElementState(hidden)?.currentError).toBeUndefined();
    expect(disabled.validationMessage).toBe('');
    expect(engine.getElementState(disabled)?.currentError).toBeUndefined();

    // The first invalid field receives focus.
    expect(document.activeElement).toBe(first);
  });
});

describe('ValidationEngine validation summary', () => {
  // Builds a form whose <ul data-valmsg-summary> is the summary carrier (matching the static-SSR
  // markup ValidationSummary renders), plus a single required field registered with the engine.
  function makeSummaryHarness(): { engine: ValidationEngine; form: HTMLFormElement; summary: HTMLUListElement; input: HTMLInputElement } {
    const engine = makeEngine();
    const form = document.createElement('form');

    const summary = document.createElement('ul');
    summary.setAttribute('data-valmsg-summary', 'true');
    summary.className = 'validation-errors validation-summary-valid';
    summary.hidden = true;
    form.appendChild(summary);

    document.body.appendChild(form);
    const input = addRequiredField(engine, form, 'Name');
    return { engine, form, summary, input };
  }

  test('populates the <ul> carrier with <li> messages and reveals it when there are errors', () => {
    const { engine, form, summary, input } = makeSummaryHarness();

    engine.validateElement(input); // empty -> invalid
    engine.updateValidationSummary(form);

    const items = summary.querySelectorAll('li.validation-message');
    expect(items).toHaveLength(1);
    expect(items[0].textContent).toBe('Name is required.');
    expect(summary.classList.contains('validation-summary-errors')).toBe(true);
    expect(summary.classList.contains('validation-summary-valid')).toBe(false);
    expect(summary.hidden).toBe(false);
  });

  test('clears the messages and hides the <ul> carrier when there are no errors', () => {
    const { engine, form, summary, input } = makeSummaryHarness();

    engine.validateElement(input);
    engine.updateValidationSummary(form);
    expect(summary.querySelectorAll('li')).toHaveLength(1);

    // Provide a value so the field becomes valid, then rebuild the summary.
    input.value = 'Ada';
    engine.validateElement(input);
    engine.updateValidationSummary(form);

    expect(summary.querySelectorAll('li')).toHaveLength(0);
    expect(summary.classList.contains('validation-summary-valid')).toBe(true);
    expect(summary.classList.contains('validation-summary-errors')).toBe(false);
    expect(summary.hidden).toBe(true);
  });
});

describe('ValidationEngine field ARIA state', () => {
  // Adds a message element linked to the field so aria-describedby can point at it.
  function addMessageElement(form: HTMLFormElement, fieldName: string): HTMLElement {
    const message = document.createElement('div');
    message.setAttribute('data-valmsg-for', fieldName);
    form.appendChild(message);
    return message;
  }

  test('sets aria-invalid and aria-describedby on error and removes them when valid, preserving foreign tokens', () => {
    const engine = makeEngine();
    const form = document.createElement('form');
    document.body.appendChild(form);

    const input = addRequiredField(engine, form, 'Name');
    // A developer-provided token that must survive the validation lifecycle.
    input.setAttribute('aria-describedby', 'help-text');
    const message = addMessageElement(form, 'Name');

    // Invalid: aria-invalid is set and our message id is appended to aria-describedby.
    expect(engine.validateElement(input)).toBe(false);
    expect(input.getAttribute('aria-invalid')).toBe('true');
    expect(message.id).not.toBe('');
    expect(input.getAttribute('aria-describedby')).toBe(`help-text ${message.id}`);

    // Valid: aria-invalid is removed and only our token is stripped, keeping the developer's.
    input.value = 'Ada';
    expect(engine.validateElement(input)).toBe(true);
    expect(input.hasAttribute('aria-invalid')).toBe(false);
    expect(input.getAttribute('aria-describedby')).toBe('help-text');
  });
});

describe('ValidationEngine radio group state fan-out', () => {
  // Interactive Blazor styles every radio in a group consistently. The engine tracks only the first
  // radio, so its state (CSS classes + aria) must fan out to every radio in the group.
  test('applies invalid/valid classes and aria-invalid to every radio in the group', () => {
    const engine = makeEngine();
    const form = document.createElement('form');
    const radios = ['Red', 'Green', 'Blue'].map(value => {
      const radio = document.createElement('input');
      radio.type = 'radio';
      radio.name = 'Color';
      radio.value = value;
      form.appendChild(radio);
      return radio;
    });
    document.body.appendChild(form);

    const [first, , third] = radios;
    const state: ElementState = {
      rules: [{ ruleName: 'required', errorMessage: 'Pick a color.', params: {} }],
      form,
      triggerEvents: 'default',
      listenerController: new AbortController(),
      hasBeenInvalid: false,
    };
    engine.registerElement(first, form, state);

    // Invalid (nothing selected): every radio gets the invalid class and aria-invalid.
    expect(engine.validateElement(first)).toBe(false);
    for (const radio of radios) {
      expect(radio.classList.contains('invalid')).toBe(true);
      expect(radio.classList.contains('valid')).toBe(false);
      expect(radio.getAttribute('aria-invalid')).toBe('true');
    }

    // Selecting any radio makes the group valid: every radio flips to the valid class, aria cleared.
    third.checked = true;
    expect(engine.validateElement(first)).toBe(true);
    for (const radio of radios) {
      expect(radio.classList.contains('valid')).toBe(true);
      expect(radio.classList.contains('invalid')).toBe(false);
      expect(radio.hasAttribute('aria-invalid')).toBe(false);
    }
  });
});
