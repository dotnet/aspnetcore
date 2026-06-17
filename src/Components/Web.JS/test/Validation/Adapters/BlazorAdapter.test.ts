import { expect, test, describe, beforeAll, afterEach } from '@jest/globals';
import {
  registerValidationData,
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

describe('blazor-client-validation-data custom element lifecycle', () => {
  // Smoke test of the form-associated custom element shell (with the attachInternals
  // polyfill): connect registers the form's inputs; disconnect unregisters them, which is
  // the cleanup contract enhanced-navigation / streaming DOM swaps depend on.
  test('registers inputs on connect and unregisters on disconnect', () => {
    const { engine, eventManager } = makeHarness();

    const form = document.createElement('form');
    const input = document.createElement('input');
    input.name = 'Name';
    form.appendChild(input);

    const carrier = document.createElement(ClientValidationElementName);
    carrier.textContent = fieldPayload('Name', [{ name: 'required', message: 'Required.' }]);
    form.appendChild(carrier);
    document.body.appendChild(form);

    // Define after the carrier is in the DOM, mirroring SSR HTML present before JS boots.
    defineBlazorClientValidationDataElement(engine, eventManager);
    customElements.upgrade(document);

    expect(engine.getElementState(input)).toBeDefined();

    carrier.remove();

    expect(engine.getElementState(input)).toBeUndefined();
  });
});
