import { expect, test, describe, beforeAll, afterEach } from '@jest/globals';
import { createBlazorValidation } from '../../../src/Validation/Adapters/BlazorAdapter';
import { ValidationService } from '../../../src/Validation/ValidationTypes';

// The wire element name. Kept in sync with the .NET renderer and the (private) adapter constant.
const ClientValidationElementName = 'blazor-client-validation-data';

// A single service is used for all the tests in this file: customElements.define is global and irreversible,
// so the carrier element is defined once (by the first createBlazorValidation) and every test runs against
// the resulting service.
let service: ValidationService;

beforeAll(() => {
  // jsdom does not provide CSS.escape - the adapter uses it to look up inputs by name.
  if (typeof globalThis.CSS === 'undefined') {
    (globalThis as any).CSS = { escape: (v: string) => v.replace(/([^\w-])/g, '\\$1') };
  }
  // jsdom does not implement attachInternals/ElementInternals. Provide a minimal getter-based
  // polyfill mirroring the only behavior the adapter relies on: live ancestry-based form association.
  if (typeof (HTMLElement.prototype as any).attachInternals !== 'function') {
    (HTMLElement.prototype as any).attachInternals = function () {
      const el = this as HTMLElement;
      return { get form() { return el.closest('form'); } };
    };
  }

  // createBlazorValidation needs a carrier in the DOM to proceed; seed one, then clear.
  document.body.appendChild(document.createElement(ClientValidationElementName));
  service = createBlazorValidation()!;
  document.body.innerHTML = '';
});

afterEach(() => {
  document.body.innerHTML = '';
});

function fieldPayload(name: string, rules: unknown[]): string {
  return JSON.stringify({ fields: [{ name, rules }] });
}

function makeTestForm(inputName: string, payload: string): { form: HTMLFormElement; input: HTMLInputElement; carrier: HTMLElement } {
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

describe('carrier payload ingestion', () => {
  // The happy path: a rendered carrier contains the validation rules and params,
  // sets novalidate so the browser's native validation does not pre-empt the engine, and
  // surfaces the rule's message. A length rule exercises the param round-trip in one pass.
  test('registers fields with rule params and sets novalidate on connect', () => {
    const { form, input } = makeTestForm('Name', fieldPayload('Name', [{ name: 'length', message: 'Too long.', params: { max: '3' } }]));

    expect(form.hasAttribute('novalidate')).toBe(true);

    input.value = 'abcd';
    expect(service.validateField(input)).toBe(false);
    expect(input.validationMessage).toBe('Too long.');

    input.value = 'ab';
    expect(service.validateField(input)).toBe(true);
  });

  // Registration is scoped to the carrier's own form: an identically named input in another form
  // (with no carrier) is not registered.
  test('registers only the carrier form\'s inputs', () => {
    const { input: input1 } = makeTestForm('Email', fieldPayload('Email', [{ name: 'required', message: 'x' }]));

    const form2 = document.createElement('form');
    const input2 = document.createElement('input');
    input2.name = 'Email';
    form2.appendChild(input2);
    document.body.appendChild(form2);

    expect(service.validateField(input1)).toBe(false);
    expect(service.validateField(input2)).toBe(true);
  });

  // A field whose input is not present is skipped; the remaining fields still register.
  test('skips payload fields with no matching input and registers the rest', () => {
    const payload = JSON.stringify({
      fields: [
        { name: 'Ghost', rules: [{ name: 'required', message: 'x' }] },
        { name: 'Name', rules: [{ name: 'required', message: 'Required.' }] },
      ],
    });
    const { input } = makeTestForm('Name', payload);

    expect(service.validateField(input)).toBe(false);
  });
});

describe('enhanced-navigation reconciliation', () => {
  // Removing the carrier (as a morph does when navigating to a form-less page) unregisters its
  // inputs, so the field validates as having no rules.
  test('unregisters inputs when the carrier is removed', () => {
    const { input, carrier } = makeTestForm('Name', fieldPayload('Name', [{ name: 'required', message: 'Required.' }]));

    expect(service.validateField(input)).toBe(false);

    carrier.remove();

    expect(service.validateField(input)).toBe(true);
  });

  // An enhanced navigation morph that reuses the carrier and changes data-rules in place drives a rebuild via
  // attributeChangedCallback: the old rules are dropped, the new ones registered, novalidate restored.
  test('rebuilds rules and re-asserts novalidate when the carrier payload changes', () => {
    const { form, input, carrier } = makeTestForm('Alpha', fieldPayload('Alpha', [{ name: 'required', message: 'Alpha required.' }]));

    expect(service.validateField(input)).toBe(false);
    expect(input.validationMessage).toBe('Alpha required.');

    // Simulate the morph: same input reused but renamed, novalidate stripped, payload changed.
    input.name = 'Beta';
    form.removeAttribute('novalidate');
    carrier.setAttribute('data-rules', fieldPayload('Beta', [{ name: 'required', message: 'Beta required.' }]));

    // The rebuild restored novalidate and cleared the stale Alpha registration.
    expect(form.hasAttribute('novalidate')).toBe(true);
    expect(input.validationMessage).toBe('');

    expect(service.validateField(input)).toBe(false);
    expect(input.validationMessage).toBe('Beta required.');
  });
});

describe('message element display toggle', () => {
  // The server renders the empty message placeholder with the hidden attribute. When a field becomes
  // invalid the engine reveals it (clears hidden) and fills the text. When it becomes valid the
  // engine hides it again, so an empty message box never affects layout.
  test('reveals the placeholder on error and hides it again when valid', () => {
    const { form, input } = makeTestForm('Name', fieldPayload('Name', [{ name: 'required', message: 'Required.' }]));

    const message = document.createElement('div');
    message.className = 'validation-message';
    message.setAttribute('data-valmsg-for', 'Name');
    message.setAttribute('data-valmsg-replace', 'true');
    message.hidden = true;
    form.appendChild(message);

    // Invalid: the placeholder is revealed and shows the message.
    expect(service.validateField(input)).toBe(false);
    expect(message.hidden).toBe(false);
    expect(message.textContent).toBe('Required.');

    // Valid: the placeholder is hidden again and emptied.
    input.value = 'Ada';
    expect(service.validateField(input)).toBe(true);
    expect(message.hidden).toBe(true);
    expect(message.textContent).toBe('');
  });
});
