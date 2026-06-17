import { expect, test, describe, beforeAll, afterEach } from '@jest/globals';
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
