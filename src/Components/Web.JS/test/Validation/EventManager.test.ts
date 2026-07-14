import { expect, test, describe, beforeAll, beforeEach, afterEach, jest } from '@jest/globals';
import { registerCoreValidators } from '../../src/Validation/CoreValidators';
import { ErrorDisplay } from '../../src/Validation/ErrorDisplay';
import { EventManager } from '../../src/Validation/EventManager';
import { ElementState, ValidationEngine } from '../../src/Validation/ValidationEngine';
import { ValidatorRegistry } from '../../src/Validation/ValidationTypes';

beforeAll(() => {
  // jsdom does not provide CSS.escape; the radio fan-out and DOM helpers use it.
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

function makeHarness() {
  const registry = new ValidatorRegistry();
  registerCoreValidators(registry);
  const errorDisplay = new ErrorDisplay();
  const engine = new ValidationEngine(registry, errorDisplay);
  const eventManager = new EventManager(engine);
  return { engine, eventManager };
}

// Adds a text input with a single 'required' rule and registers it with the engine.
function addRequiredField(
  engine: ValidationEngine,
  form: HTMLFormElement,
  name: string,
  value = '',
  triggerEvents = 'default',
): HTMLInputElement {
  const input = document.createElement('input');
  input.name = name;
  input.value = value;
  form.appendChild(input);
  const state: ElementState = {
    rules: [{ ruleName: 'required', errorMessage: `${name} is required.`, params: {} }],
    form,
    triggerEvents,
    listenerController: new AbortController(),
    hasBeenInvalid: false,
  };
  engine.registerElement(input, form, state);
  return input;
}

describe('EventManager radio fan-out (eager recovery)', () => {
  // Only the first radio of a group is registered with the engine, but listeners must be
  // attached to every radio in the group. Selecting a non-first radio must therefore clear
  // the error without a submit. Without the fan-out the non-first radio has no listener and
  // the error would persist.
  test('selecting a non-first radio in the group clears the error', () => {
    const { engine, eventManager } = makeHarness();

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
    eventManager.attachInputListeners(first);

    // Force an initial failure: no radio in the group is selected.
    expect(engine.validateElement(first)).toBe(false);
    expect(engine.getElementState(first)?.currentError).toBe('Pick a color.');

    // Select a non-first radio and dispatch 'change' on it.
    third.checked = true;
    third.dispatchEvent(new Event('change', { bubbles: true }));

    expect(engine.getElementState(first)?.currentError).toBeUndefined();
  });
});

describe('EventManager form submission interception', () => {
  let engine: ValidationEngine;
  let eventManager: EventManager;

  beforeEach(() => {
    ({ engine, eventManager } = makeHarness());
    eventManager.attachFormInterceptors();
  });

  afterEach(() => {
    eventManager.detachFormInterceptors();
  });

  // Dispatches a cancelable submit on the form (optionally from a submitter) and captures whether
  // the interceptor blocked it and what 'validationcomplete' reported.
  function dispatchSubmit(form: HTMLFormElement, submitter?: HTMLElement) {
    const event = new Event('submit', { bubbles: true, cancelable: true });
    if (submitter) {
      Object.defineProperty(event, 'submitter', { value: submitter });
    }
    const preventDefault = jest.spyOn(event, 'preventDefault');
    const stopPropagation = jest.spyOn(event, 'stopPropagation');
    const completions: Array<{ valid: boolean; errors: Map<string, string> }> = [];
    form.addEventListener('validationcomplete', e => completions.push((e as CustomEvent).detail));
    form.dispatchEvent(event);
    return { preventDefault, stopPropagation, completions };
  }

  test('invalid tracked form: blocks the submit and reports validationcomplete with the errors', () => {
    const form = document.createElement('form');
    document.body.appendChild(form);
    addRequiredField(engine, form, 'Name'); // empty -> invalid

    const { preventDefault, stopPropagation, completions } = dispatchSubmit(form);

    expect(preventDefault).toHaveBeenCalled();
    expect(stopPropagation).toHaveBeenCalled();
    expect(completions).toHaveLength(1);
    expect(completions[0].valid).toBe(false);
    expect(completions[0].errors.get('Name')).toBe('Name is required.');
  });

  test('valid tracked form: allows the submit and reports validationcomplete as valid', () => {
    const form = document.createElement('form');
    document.body.appendChild(form);
    addRequiredField(engine, form, 'Name', 'Ada'); // non-empty -> valid

    const { preventDefault, completions } = dispatchSubmit(form);

    expect(preventDefault).not.toHaveBeenCalled();
    expect(completions).toHaveLength(1);
    expect(completions[0].valid).toBe(true);
    expect(completions[0].errors.size).toBe(0);
  });

  test('untracked form: not intercepted', () => {
    const form = document.createElement('form');
    document.body.appendChild(form); // no fields registered -> no form state

    const { preventDefault, completions } = dispatchSubmit(form);

    expect(preventDefault).not.toHaveBeenCalled();
    expect(completions).toHaveLength(0);
  });

  test('formnovalidate submitter: not intercepted even when the form is invalid', () => {
    const form = document.createElement('form');
    document.body.appendChild(form);
    addRequiredField(engine, form, 'Name'); // empty -> would be invalid

    const button = document.createElement('button');
    button.setAttribute('formnovalidate', '');
    form.appendChild(button);

    const { preventDefault, completions } = dispatchSubmit(form, button);

    expect(preventDefault).not.toHaveBeenCalled();
    expect(completions).toHaveLength(0);
  });
});

describe('EventManager validation triggers (lazy validation, eager recovery)', () => {
  // Default gating: 'change' always validates, but 'input' only validates once the field has been
  // invalid or the form has been submitted. Validity is observed via validationMessage, which the
  // engine sets through setCustomValidity.
  test('before any error or submit: input does not validate, change does', () => {
    const { engine, eventManager } = makeHarness();
    const form = document.createElement('form');
    document.body.appendChild(form);
    const input = addRequiredField(engine, form, 'Name'); // empty -> would be invalid
    eventManager.attachInputListeners(input);

    input.dispatchEvent(new Event('input'));
    expect(input.validationMessage).toBe(''); // gated out, not validated

    input.dispatchEvent(new Event('change'));
    expect(input.validationMessage).toBe('Name is required.');
  });

  test('after the field has been invalid: input validates (eager recovery)', () => {
    const { engine, eventManager } = makeHarness();
    const form = document.createElement('form');
    document.body.appendChild(form);
    const input = addRequiredField(engine, form, 'Name');
    eventManager.attachInputListeners(input);

    // First error via change flips hasBeenInvalid.
    input.dispatchEvent(new Event('change'));
    expect(input.validationMessage).toBe('Name is required.');

    // Now input validates: fixing the value and firing input clears the error.
    input.value = 'Ada';
    input.dispatchEvent(new Event('input'));
    expect(input.validationMessage).toBe('');
  });

  test('after the form has been submitted: input validates', () => {
    const { engine, eventManager } = makeHarness();
    const form = document.createElement('form');
    document.body.appendChild(form);
    const input = addRequiredField(engine, form, 'Name');
    eventManager.attachInputListeners(input);

    // Simulate a prior submit attempt without routing through validateForm.
    engine.getFormState(form)!.hasBeenSubmitted = true;

    input.dispatchEvent(new Event('input'));
    expect(input.validationMessage).toBe('Name is required.');
  });
});

describe('EventManager modified state', () => {
  // Blazor's default styling shows the "valid" (green) outline only via '.valid.modified'. Editing
  // a field to a valid value and committing it (change) must add both 'valid' and 'modified' so the
  // static and interactive render modes look the same.
  test('marks the field modified with the valid class when edited to a valid value', () => {
    const { engine, eventManager } = makeHarness();
    const form = document.createElement('form');
    document.body.appendChild(form);
    const input = addRequiredField(engine, form, 'Name');
    eventManager.attachInputListeners(input);

    input.value = 'Ada';
    input.dispatchEvent(new Event('change'));

    expect(input.classList.contains('modified')).toBe(true);
    expect(input.classList.contains('valid')).toBe(true);
    expect(input.classList.contains('invalid')).toBe(false);
  });

  // An invalid edit is marked modified but keeps only the 'invalid' class (no 'valid'), so it never
  // shows the green outline while invalid.
  test('marks modified with the invalid class (not valid) when edited to an invalid value', () => {
    const { engine, eventManager } = makeHarness();
    const form = document.createElement('form');
    document.body.appendChild(form);
    const input = addRequiredField(engine, form, 'Name');
    eventManager.attachInputListeners(input);

    input.dispatchEvent(new Event('change')); // still empty -> invalid

    expect(input.classList.contains('modified')).toBe(true);
    expect(input.classList.contains('invalid')).toBe(true);
    expect(input.classList.contains('valid')).toBe(false);
  });

  // Resetting the form returns fields to pristine, dropping the modified (and valid/invalid) classes.
  test('a form reset clears the modified class', () => {
    const { engine, eventManager } = makeHarness();
    const form = document.createElement('form');
    document.body.appendChild(form);
    const input = addRequiredField(engine, form, 'Name');
    eventManager.attachInputListeners(input);

    input.value = 'Ada';
    input.dispatchEvent(new Event('change'));
    expect(input.classList.contains('modified')).toBe(true);

    engine.resetForm(form);

    expect(input.classList.contains('modified')).toBe(false);
    expect(input.classList.contains('valid')).toBe(false);
    expect(input.classList.contains('invalid')).toBe(false);
  });
});
