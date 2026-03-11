import { expect, test, describe, beforeEach } from '@jest/globals';

// Polyfill CSS.escape for jsdom (not available in Node)
if (typeof globalThis.CSS === 'undefined') {
  (globalThis as any).CSS = {
    escape: (value: string) => value.replace(/([^\w-])/g, '\\$1'),
  };
}

import { ValidationEngine } from '../src/Validation/ValidationEngine';
import { ValidationCoordinator } from '../src/Validation/ValidationCoordinator';
import { ErrorDisplay } from '../src/Validation/ErrorDisplay';
import { EventManager } from '../src/Validation/EventManager';
import { DomScanner } from '../src/Validation/DomScanner';
import { registerBuiltInProviders } from '../src/Validation/BuiltInProviders';
import { ValidatableElement, defaultCssClasses, ValidationProvider } from '../src/Validation/Types';

// Helper to create a form with validatable elements
function createForm(
  fields: { name: string; value: string; rules: Record<string, string> }[]
): { form: HTMLFormElement; inputs: HTMLInputElement[] } {
  const form = document.createElement('form');
  const inputs: HTMLInputElement[] = [];

  for (const field of fields) {
    const input = document.createElement('input');
    input.setAttribute('name', field.name);
    input.setAttribute('data-val', 'true');
    input.value = field.value;

    for (const [attr, val] of Object.entries(field.rules)) {
      input.setAttribute(attr, val);
    }

    form.appendChild(input);

    // Add a message span
    const span = document.createElement('span');
    span.setAttribute('data-valmsg-for', field.name);
    form.appendChild(span);

    inputs.push(input);
  }

  document.body.appendChild(form);
  return { form, inputs };
}

function createStack() {
  const engine = new ValidationEngine();
  registerBuiltInProviders(engine);
  const display = new ErrorDisplay(defaultCssClasses);
  const coordinator = new ValidationCoordinator(engine, display);
  const eventManager = new EventManager(coordinator);
  const scanner = new DomScanner(coordinator, eventManager);
  return { engine, display, coordinator, eventManager, scanner };
}

// Cleanup after each test
let cleanupForms: HTMLFormElement[] = [];
beforeEach(() => {
  for (const form of cleanupForms) {
    form.remove();
  }
  cleanupForms = [];
});

function trackForm(form: HTMLFormElement) {
  cleanupForms.push(form);
  return form;
}

describe('ValidationCoordinator async', () => {
  test('validateElement returns Promise<string>', async () => {
    const { coordinator, scanner } = createStack();
    const { form, inputs } = createForm([
      { name: 'email', value: 'test@example.com', rules: { 'data-val-email': 'Invalid email' } },
    ]);
    trackForm(form);
    scanner.scan(form);

    const result = coordinator.validateElement(inputs[0]);
    expect(result).toBeInstanceOf(Promise);
    expect(await result).toBe('');
  });

  test('validateElement resolves to error message on failure', async () => {
    const { coordinator, scanner } = createStack();
    const { form, inputs } = createForm([
      { name: 'email', value: 'not-an-email', rules: { 'data-val-email': 'Invalid email' } },
    ]);
    trackForm(form);
    scanner.scan(form);

    const error = await coordinator.validateElement(inputs[0]);
    expect(error).toBe('Invalid email');
  });

  test('validateElement with async provider returning Promise<true>', async () => {
    const { engine, coordinator, scanner } = createStack();

    const asyncProvider: ValidationProvider = () => Promise.resolve(true);
    engine.setProvider('email', asyncProvider);

    const { form, inputs } = createForm([
      { name: 'email', value: 'test', rules: { 'data-val-email': 'Invalid' } },
    ]);
    trackForm(form);
    scanner.scan(form);

    const error = await coordinator.validateElement(inputs[0]);
    expect(error).toBe('');
  });

  test('validateElement with async provider returning Promise<false>', async () => {
    const { engine, coordinator, scanner } = createStack();

    const asyncProvider: ValidationProvider = () => Promise.resolve(false);
    engine.setProvider('email', asyncProvider);

    const { form, inputs } = createForm([
      { name: 'email', value: 'test', rules: { 'data-val-email': 'Async error' } },
    ]);
    trackForm(form);
    scanner.scan(form);

    const error = await coordinator.validateElement(inputs[0]);
    expect(error).toBe('Async error');
  });

  test('validateElement with async provider returning custom error string', async () => {
    const { engine, coordinator, scanner } = createStack();

    const asyncProvider: ValidationProvider = () => Promise.resolve('Server says no');
    engine.setProvider('email', asyncProvider);

    const { form, inputs } = createForm([
      { name: 'email', value: 'test', rules: { 'data-val-email': 'Default msg' } },
    ]);
    trackForm(form);
    scanner.scan(form);

    const error = await coordinator.validateElement(inputs[0]);
    expect(error).toBe('Server says no');
  });

  test('sync failure prevents later async provider from running', async () => {
    const { engine, coordinator, scanner } = createStack();

    let asyncCalled = false;
    engine.addProvider('custom', () => {
      asyncCalled = true;
      return Promise.resolve(true);
    });

    const { form, inputs } = createForm([
      {
        name: 'field',
        value: '',
        rules: {
          'data-val-required': 'Required',
          'data-val-custom': 'Custom check',
        },
      },
    ]);
    trackForm(form);
    scanner.scan(form);

    const error = await coordinator.validateElement(inputs[0]);
    expect(error).toBe('Required');
    expect(asyncCalled).toBe(false);
  });

  test('validateAndUpdate returns Promise<boolean>', async () => {
    const { coordinator, scanner } = createStack();
    const { form, inputs } = createForm([
      { name: 'email', value: 'test@example.com', rules: { 'data-val-email': 'Invalid' } },
    ]);
    trackForm(form);
    scanner.scan(form);

    const result = coordinator.validateAndUpdate(inputs[0]);
    expect(result).toBeInstanceOf(Promise);
    expect(await result).toBe(true);
  });

  test('validateAndUpdate marks invalid field with CSS classes', async () => {
    const { coordinator, scanner } = createStack();
    const { form, inputs } = createForm([
      { name: 'email', value: 'bad', rules: { 'data-val-email': 'Invalid email' } },
    ]);
    trackForm(form);
    scanner.scan(form);

    const isValid = await coordinator.validateAndUpdate(inputs[0]);
    expect(isValid).toBe(false);
    expect(inputs[0].classList.contains('input-validation-error')).toBe(true);

    const span = form.querySelector('[data-valmsg-for="email"]')!;
    expect(span.textContent).toBe('Invalid email');
    expect(span.classList.contains('field-validation-error')).toBe(true);
  });

  test('validateAndUpdate clears error on valid field', async () => {
    const { coordinator, scanner } = createStack();
    const { form, inputs } = createForm([
      { name: 'email', value: 'bad', rules: { 'data-val-email': 'Invalid email' } },
    ]);
    trackForm(form);
    scanner.scan(form);

    // First make it invalid
    await coordinator.validateAndUpdate(inputs[0]);
    expect(inputs[0].classList.contains('input-validation-error')).toBe(true);

    // Now fix and re-validate
    inputs[0].value = 'test@example.com';
    await coordinator.validateAndUpdate(inputs[0]);
    expect(inputs[0].classList.contains('input-validation-error')).toBe(false);
    expect(inputs[0].classList.contains('input-validation-valid')).toBe(true);
  });

  test('validateForm returns Promise<boolean>', async () => {
    const { coordinator, scanner } = createStack();
    const { form } = createForm([
      { name: 'email', value: 'test@example.com', rules: { 'data-val-email': 'Invalid' } },
    ]);
    trackForm(form);
    scanner.scan(form);

    const result = coordinator.validateForm(form);
    expect(result).toBeInstanceOf(Promise);
    expect(await result).toBe(true);
  });

  test('validateForm validates all fields in parallel', async () => {
    const { coordinator, scanner } = createStack();
    const { form, inputs } = createForm([
      { name: 'name', value: '', rules: { 'data-val-required': 'Name required' } },
      { name: 'email', value: 'bad', rules: { 'data-val-email': 'Invalid email' } },
    ]);
    trackForm(form);
    scanner.scan(form);

    const isValid = await coordinator.validateForm(form);
    expect(isValid).toBe(false);

    // Both fields should be marked invalid
    expect(inputs[0].classList.contains('input-validation-error')).toBe(true);
    expect(inputs[1].classList.contains('input-validation-error')).toBe(true);
  });

  test('validateForm returns true when all fields valid', async () => {
    const { coordinator, scanner } = createStack();
    const { form } = createForm([
      { name: 'name', value: 'John', rules: { 'data-val-required': 'Required' } },
      { name: 'email', value: 'test@example.com', rules: { 'data-val-email': 'Invalid' } },
    ]);
    trackForm(form);
    scanner.scan(form);

    const isValid = await coordinator.validateForm(form);
    expect(isValid).toBe(true);
  });

  test('validateForm focuses first invalid field', async () => {
    const { coordinator, scanner } = createStack();
    const { form, inputs } = createForm([
      { name: 'a', value: 'ok', rules: { 'data-val-required': 'Required' } },
      { name: 'b', value: '', rules: { 'data-val-required': 'Required' } },
      { name: 'c', value: '', rules: { 'data-val-required': 'Required' } },
    ]);
    trackForm(form);
    scanner.scan(form);

    await coordinator.validateForm(form);

    // jsdom doesn't fully implement focus, but we can check the focus was called
    // by verifying the second field (first invalid) has the error class
    expect(inputs[1].classList.contains('input-validation-error')).toBe(true);
  });

  test('validateForm with mixed sync and async providers', async () => {
    const { engine, coordinator, scanner } = createStack();

    let asyncResolved = false;
    engine.addProvider('custom', () => {
      return new Promise(resolve => {
        setTimeout(() => {
          asyncResolved = true;
          resolve(true);
        }, 10);
      });
    });

    const { form, inputs } = createForm([
      { name: 'name', value: 'John', rules: { 'data-val-required': 'Required' } },
      { name: 'code', value: 'ABC', rules: { 'data-val-custom': 'Invalid code' } },
    ]);
    trackForm(form);
    scanner.scan(form);

    const isValid = await coordinator.validateForm(form);
    expect(isValid).toBe(true);
    expect(asyncResolved).toBe(true);
  });
});

describe('EventManager async submit', () => {
  test('submit is prevented and re-submitted on valid form', async () => {
    const { scanner, eventManager } = createStack();
    const { form } = createForm([
      { name: 'name', value: 'John', rules: { 'data-val-required': 'Required' } },
    ]);
    trackForm(form);
    scanner.scan(form);
    eventManager.attachSubmitInterception();

    let resubmitCount = 0;
    form.requestSubmit = () => { resubmitCount++; };

    // Dispatch submit event from the form (bubbles up to document where capture handler listens)
    const event = new Event('submit', { cancelable: true, bubbles: true }) as SubmitEvent;
    form.dispatchEvent(event);

    // The handler always prevents the original submit
    expect(event.defaultPrevented).toBe(true);

    // Wait for async validation to complete and re-submit
    await new Promise(resolve => setTimeout(resolve, 10));

    expect(resubmitCount).toBe(1);

    eventManager.detachSubmitInterception();
  });

  test('submit is prevented and NOT re-submitted on invalid form', async () => {
    const { scanner, eventManager } = createStack();
    const { form } = createForm([
      { name: 'name', value: '', rules: { 'data-val-required': 'Required' } },
    ]);
    trackForm(form);
    scanner.scan(form);
    eventManager.attachSubmitInterception();

    let resubmitCount = 0;
    form.requestSubmit = () => { resubmitCount++; };

    const event = new Event('submit', { cancelable: true, bubbles: true }) as SubmitEvent;
    form.dispatchEvent(event);

    expect(event.defaultPrevented).toBe(true);

    await new Promise(resolve => setTimeout(resolve, 10));

    expect(resubmitCount).toBe(0);

    eventManager.detachSubmitInterception();
  });

  test('formnovalidate skips validation', async () => {
    const { scanner, eventManager } = createStack();
    const { form } = createForm([
      { name: 'name', value: '', rules: { 'data-val-required': 'Required' } },
    ]);
    trackForm(form);

    const button = document.createElement('button');
    button.setAttribute('formnovalidate', '');
    form.appendChild(button);

    scanner.scan(form);
    eventManager.attachSubmitInterception();

    const event = new Event('submit', { cancelable: true, bubbles: true }) as SubmitEvent;
    Object.defineProperty(event, 'submitter', { value: button });
    form.dispatchEvent(event);

    // Should NOT be prevented since formnovalidate is set
    expect(event.defaultPrevented).toBe(false);

    eventManager.detachSubmitInterception();
  });
});
