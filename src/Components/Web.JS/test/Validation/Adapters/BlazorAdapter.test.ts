import { expect, test, describe, beforeAll, afterEach } from '@jest/globals';
import {
  registerValidationData,
  reconcileValidationElements,
  defineBlazorClientValidationDataElement,
  ClientValidationElementName,
} from '../../../src/Validation/Adapters/BlazorAdapter';
import { registerCoreValidators } from '../../../src/Validation/CoreValidators';
import { ErrorDisplay } from '../../../src/Validation/ErrorDisplay';
import { EventManager } from '../../../src/Validation/EventManager';
import { ValidationEngine } from '../../../src/Validation/ValidationEngine';
import { ValidatorRegistry } from '../../../src/Validation/ValidationTypes';

beforeAll(() => {
  // jsdom does not provide CSS.escape. The adapter and DOM helpers use it.
  if (typeof globalThis.CSS === 'undefined') {
    (globalThis as any).CSS = { escape: (v: string) => v.replace(/([^\w-])/g, '\\$1') };
  }
  // jsdom does not implement attachInternals/ElementInternals. Provide a minimal
  // getter-based polyfill that mirrors the only behavior the adapter relies on:
  // live ancestry-based form association (ElementInternals.form is live, not snapshotted).
  if (typeof (HTMLElement.prototype as any).attachInternals !== 'function') {
    (HTMLElement.prototype as any).attachInternals = function () {
      const el = this as HTMLElement;
      return {
        get form() {
          return el.closest('form');
        },
      };
    };
  }
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

function makeFormWithInput(name: string): HTMLFormElement {
  const form = document.createElement('form');
  const input = document.createElement('input');
  input.name = name;
  form.appendChild(input);
  document.body.appendChild(form);
  return form;
}

function fieldPayload(name: string, rules: unknown[]): string {
  return JSON.stringify({ fields: [{ name, rules }] });
}

describe('registerValidationData', () => {
  // The wire contract between the .NET serializer and the JS engine: each rule's
  // `name`/`message`/`params` must land in engine state as `ruleName`/`errorMessage`/`params`.
  test('maps payload rules to engine state and sets novalidate on the form', () => {
    const { engine, eventManager } = makeHarness();
    const form = makeFormWithInput('Name');
    const input = form.querySelector('input')!;

    const payload = fieldPayload('Name', [
      { name: 'required', message: 'Name is required.' },
      { name: 'length', message: 'Too long.', params: { max: '10' } },
    ]);

    const registered = registerValidationData(form, payload, engine, eventManager);

    expect(registered).toEqual([input]);
    expect(form.hasAttribute('novalidate')).toBe(true);
    expect(engine.getElementState(input)?.rules).toEqual([
      { ruleName: 'required', errorMessage: 'Name is required.', params: {} },
      { ruleName: 'length', errorMessage: 'Too long.', params: { max: '10' } },
    ]);
  });

  // Registration is scoped to the owning form: a carrier for one form must not register
  // an identically named input belonging to a different form.
  test('registers only the matching form\'s input when forms share a field name', () => {
    const { engine, eventManager } = makeHarness();
    const form1 = makeFormWithInput('Email');
    const form2 = makeFormWithInput('Email');
    const input1 = form1.querySelector('input')!;
    const input2 = form2.querySelector('input')!;

    const registered = registerValidationData(
      form1,
      fieldPayload('Email', [{ name: 'required', message: 'x' }]),
      engine,
      eventManager,
    );

    expect(registered).toEqual([input1]);
    expect(engine.getElementState(input1)).toBeDefined();
    expect(engine.getElementState(input2)).toBeUndefined();
  });

  // Partial-render robustness: a field with no matching input is skipped, and remaining
  // fields still register.
  test('skips fields whose input is missing and registers the rest', () => {
    const { engine, eventManager } = makeHarness();
    const form = makeFormWithInput('Name');
    const input = form.querySelector('input')!;

    const payload = JSON.stringify({
      fields: [
        { name: 'Ghost', rules: [{ name: 'required', message: 'x' }] },
        { name: 'Name', rules: [{ name: 'required', message: 'Required.' }] },
      ],
    });

    const registered = registerValidationData(form, payload, engine, eventManager);

    expect(registered).toEqual([input]);
    expect(engine.getElementState(input)).toBeDefined();
  });

  // Lazy-init plus customElements.upgrade can invoke registration more than once; the
  // second pass must not re-register or replace existing state.
  test('does not re-register an input when called twice', () => {
    const { engine, eventManager } = makeHarness();
    const form = makeFormWithInput('Name');
    const input = form.querySelector('input')!;
    const payload = fieldPayload('Name', [{ name: 'required', message: 'Required.' }]);

    const first = registerValidationData(form, payload, engine, eventManager);
    const firstState = engine.getElementState(input);
    const second = registerValidationData(form, payload, engine, eventManager);

    expect(first).toEqual([input]);
    expect(second).toEqual([]);
    expect(engine.getElementState(input)).toBe(firstState);
  });
});

// The form-associated custom element owns each carrier's lifecycle: connect registers the form's
// inputs, disconnect unregisters them, and reconcile (driven by refreshValidationService on
// enhanced navigation) re-applies rules when a reused carrier's payload changed and re-asserts the
// novalidate a DOM morph strips. customElements.define is global and irreversible, so the element
// is defined once - bound to this shared engine - and every test in this block runs against it,
// relying on the afterEach DOM clear to fire disconnectedCallback and reset engine state.
describe('blazor-client-validation-data custom element', () => {
  let engine: ValidationEngine;

  beforeAll(() => {
    const harness = makeHarness();
    engine = harness.engine;
    defineBlazorClientValidationDataElement(engine, harness.eventManager);
  });

  function makeCarrierForm(inputName: string, payload: string): { form: HTMLFormElement; input: HTMLInputElement; carrier: HTMLElement } {
    const form = document.createElement('form');
    const input = document.createElement('input');
    input.name = inputName;
    form.appendChild(input);

    const carrier = document.createElement(ClientValidationElementName);
    carrier.setAttribute('data-rules', payload);
    form.appendChild(carrier);

    document.body.appendChild(form);
    return { form, input, carrier };
  }

  // Connect registers the form's inputs; disconnect unregisters them, which is the cleanup
  // contract enhanced-navigation DOM swaps depend on (a removed carrier tears down its form).
  test('registers inputs on connect and unregisters on disconnect', () => {
    const { input, carrier } = makeCarrierForm(
      'Name',
      fieldPayload('Name', [{ name: 'required', message: 'Required.' }]),
    );

    expect(engine.getElementState(input)).toBeDefined();

    carrier.remove();

    expect(engine.getElementState(input)).toBeUndefined();
  });

  // A reused carrier whose payload changed (form A -> form B after a morph) must drop the old
  // form's rules and re-register with the new ones, and re-apply the novalidate the morph stripped.
  test('reconcile rebuilds and re-asserts novalidate when the carrier payload changes', () => {
    const { form, input, carrier } = makeCarrierForm(
      'Alpha',
      fieldPayload('Alpha', [{ name: 'required', message: 'Alpha required.' }]),
    );

    expect(engine.getElementState(input)?.rules[0].errorMessage).toBe('Alpha required.');

    // Simulate the morph: same input element reused but renamed, novalidate stripped, payload changed.
    input.name = 'Beta';
    form.removeAttribute('novalidate');
    carrier.setAttribute('data-rules', fieldPayload('Beta', [{ name: 'required', message: 'Beta required.' }]));

    reconcileValidationElements();

    expect(form.hasAttribute('novalidate')).toBe(true);
    expect(engine.getElementState(input)?.rules).toEqual([
      { ruleName: 'required', errorMessage: 'Beta required.', params: {} },
    ]);
  });

  // An unchanged carrier payload must be a no-op so a preserved form's live state is not cleared,
  // while novalidate is still re-asserted (the morph strips it even when rules are unchanged).
  test('reconcile leaves the form untouched when the payload is unchanged but re-asserts novalidate', () => {
    const { form, input } = makeCarrierForm(
      'Gamma',
      fieldPayload('Gamma', [{ name: 'required', message: 'Required.' }]),
    );
    const stateBefore = engine.getElementState(input);

    form.removeAttribute('novalidate');
    reconcileValidationElements();

    expect(engine.getElementState(input)).toBe(stateBefore);
    expect(form.hasAttribute('novalidate')).toBe(true);
  });
});
