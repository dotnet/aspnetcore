// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { EventManager } from '../EventManager';
import { ElementState, ValidationEngine } from '../ValidationEngine';
import { ValidatableElement } from '../ValidationTypes';

export const ClientValidationElementName = 'blazor-client-validation-data';

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

export function defineBlazorClientValidationDataElement(
  engine: ValidationEngine,
  eventManager: EventManager,
): void {
  if (customElements.get(ClientValidationElementName)) {
    return;
  }

  class BlazorClientValidationDataElement extends HTMLElement {
    static formAssociated = true;

    private internals: ElementInternals;

    private registeredInputs: ValidatableElement[] = [];

    constructor() {
      super();
      this.internals = this.attachInternals();
    }

    connectedCallback(): void {
      const form = this.internals.form;

      if (!form) {
        return;
      }

      const registered = registerValidationData(form, this.textContent || '', engine, eventManager);
      this.registeredInputs.push(...registered);
    }

    disconnectedCallback(): void {
      for (const input of this.registeredInputs) {
        engine.unregisterElement(input);
      }

      this.registeredInputs = [];
    }
  }

  customElements.define(ClientValidationElementName, BlazorClientValidationDataElement);
}

export function registerValidationData(
  form: HTMLFormElement,
  payloadText: string,
  engine: ValidationEngine,
  eventManager: EventManager,
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

  if (!form.hasAttribute('novalidate')) {
    form.setAttribute('novalidate', '');
  }

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
