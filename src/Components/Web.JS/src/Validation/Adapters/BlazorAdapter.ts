// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { registerCoreValidators } from '../CoreValidators';
import { ErrorDisplay } from '../ErrorDisplay';
import { EventManager } from '../EventManager';
import { ElementState, ValidationEngine } from '../ValidationEngine';
import { ValidatableElement, ValidationService, Validator, ValidatorRegistry } from '../ValidationTypes';

const ClientValidationElementName = 'blazor-client-validation-data';

// The attribute on the carrier element that holds the serialized validation rules. The payload
// lives in an attribute (not text content) so the carrier element renders nothing - no CSS or
// hidden attribute is needed to keep it out of the visible page.
const ClientValidationRulesAttributeName = 'data-rules';

interface ClientValidationFormDescriptor {
  fields: ClientValidationFieldDescriptor[];
}

interface ClientValidationFieldDescriptor {
  name: string;
  rules: ClientValidationRule[];
}

interface ClientValidationRule {
  name: string;
  message: string;
  params?: Record<string, string>;
}

/**
 * Creates the client-side validation service when the page contains the known carrier element,
 * or returns `undefined` otherwise.
 * Registers built-in validators, defines the `<blazor-client-validation-data>` custom element that
 * ingests the SSR-rendered rules, and attaches the form event interceptors.
 */
export function createBlazorValidation(): ValidationService | undefined {
  if (!document.querySelector(ClientValidationElementName)) {
    return undefined;
  }

  const registry = new ValidatorRegistry();
  registerCoreValidators(registry);

  const errorDisplay = new ErrorDisplay();
  const engine = new ValidationEngine(registry, errorDisplay);
  const eventManager = new EventManager(engine);

  eventManager.attachFormInterceptors();

  // Upgrade any carrier instances already parsed before the element was defined, which fires their
  // connectedCallback retroactively.
  defineBlazorClientValidationDataElement(engine, eventManager);
  customElements.upgrade(document);

  return {
    addValidator: (name: string, validator: Validator) => registry.set(name, validator),
    validateField: (element: ValidatableElement) => engine.validateElement(element),
    validateForm: (form: HTMLFormElement) => engine.validateForm(form).size === 0,
  };
}

/**
 * Restores `novalidate` on every carrier-owning form after an enhanced navigation update.
 * The morph strips the JS-added `novalidate` when it reuses a form, even when the rules are unchanged.
 * Rule changes and carrier add/remove are handled by the element's own callbacks, so this only
 * restores `novalidate`.
 */
export function ensureNovalidateOnForms(): void {
  document.querySelectorAll(ClientValidationElementName).forEach(element => {
    (element as { ensureNovalidate?: () => void }).ensureNovalidate?.();
  });
}

function defineBlazorClientValidationDataElement(
  engine: ValidationEngine,
  eventManager: EventManager,
): void {
  if (customElements.get(ClientValidationElementName)) {
    return;
  }

  class BlazorClientValidationDataElement extends HTMLElement {
    static formAssociated = true;

    static observedAttributes = [ClientValidationRulesAttributeName];

    private internals: ElementInternals;

    private registeredInputs: ValidatableElement[] = [];

    constructor() {
      super();
      this.internals = this.attachInternals();
    }

    connectedCallback(): void {
      this.applyRules();
    }

    disconnectedCallback(): void {
      this.teardown();
    }

    attributeChangedCallback(name: string, oldValue: string | null, newValue: string | null): void {
      if (name === ClientValidationRulesAttributeName
        && this.isConnected
        && oldValue !== null
        && oldValue !== newValue) {
        this.applyRules();
      }
    }

    // Suppress the browser's native validation UI so the engine's capture-phase submit handling
    // is not pre-empted.
    ensureNovalidate(): void {
      const form = this.internals.form;
      if (form && !form.hasAttribute('novalidate')) {
        form.setAttribute('novalidate', '');
      }
    }

    private applyRules(): void {
      const form = this.internals.form;

      if (!form) {
        return;
      }

      this.teardown();
      const payload = this.getAttribute(ClientValidationRulesAttributeName) || '';
      this.registeredInputs = this.registerValidationData(form, payload);
    }

    private teardown(): void {
      for (const input of this.registeredInputs) {
        engine.unregisterElement(input);
      }

      this.registeredInputs = [];
    }

    /**
     * Parses a `<blazor-client-validation-data>` payload and registers each described field's input
     * with the validation engine, attaching its event listeners.
     * Inputs not found in the form, or already registered, are skipped.
     */
    private registerValidationData(
      form: HTMLFormElement,
      payloadText: string,
    ): ValidatableElement[] {
      let formDescriptor: ClientValidationFormDescriptor | null = null;

      try {
        formDescriptor = JSON.parse(payloadText || '{}');
      } catch (error) {
        console.warn('Failed to parse client validation data:', error);
        return [];
      }

      if (!formDescriptor || !Array.isArray(formDescriptor.fields)) {
        console.warn('Invalid client validation data format.');
        return [];
      }

      this.ensureNovalidate();

      const registeredInputs: ValidatableElement[] = [];

      for (const field of formDescriptor.fields) {
        const input = form.querySelector<ValidatableElement>('[name="' + CSS.escape(field.name) + '"]');

        if (!input) {
          // Skip input registration if the target input element is not found in the form.
          continue;
        }

        if (engine.getElementState(input)) {
          // Avoid double input registration if connectedCallback runs multiple times.
          continue;
        }

        const rules = field.rules.map(rule => ({
          ruleName: rule.name,
          errorMessage: rule.message,
          params: rule.params || {},
        }));

        const state: ElementState = {
          rules: rules,
          form: form,
          triggerEvents: 'default',
          listenerController: new AbortController(),
          hasBeenInvalid: false,
        };

        engine.registerElement(input, form, state);
        eventManager.attachInputListeners(input);
        registeredInputs.push(input);
      }

      return registeredInputs;
    }
  }

  customElements.define(ClientValidationElementName, BlazorClientValidationDataElement);
}

