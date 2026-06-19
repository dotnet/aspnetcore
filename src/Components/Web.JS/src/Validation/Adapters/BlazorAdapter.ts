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

interface ReconcilableValidationElement extends Element {
  reconcile?: () => void;
}

export function defineBlazorClientValidationDataElement(
  engine: ValidationEngine,
  eventManager: EventManager,
): void {
  if (customElements.get(ClientValidationElementName)) {
    return;
  }

  class BlazorClientValidationDataElement extends HTMLElement implements ReconcilableValidationElement {
    static formAssociated = true;

    private internals: ElementInternals;

    private registeredInputs: ValidatableElement[] = [];

    // The payload last applied to the form, used to skip a rebuild when an enhanced-navigation
    // morph reused this carrier without changing its rules (preserving the form's live state).
    private appliedPayload: string | null = null;

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

    // Re-runs registration against the current DOM. Called after an enhanced-navigation update,
    // where the morph reuses this carrier (so connectedCallback does not re-fire), may update its
    // payload in place, and strips the JS-added novalidate from the form.
    reconcile(): void {
      this.applyRules();
    }

    private applyRules(): void {
      const form = this.internals.form;

      if (!form) {
        return;
      }

      // Re-assert novalidate: an enhanced-navigation morph reconciles the form's attributes to the
      // server HTML, which strips the novalidate we add. This is cheap and idempotent.
      if (!form.hasAttribute('novalidate')) {
        form.setAttribute('novalidate', '');
      }

      const payload = this.textContent || '';
      if (this.appliedPayload === payload) {
        // Rules unchanged - leave the existing registration and live error display intact.
        return;
      }

      this.teardown();
      this.registeredInputs = registerValidationData(form, payload, engine, eventManager);
      this.appliedPayload = payload;
    }

    private teardown(): void {
      for (const input of this.registeredInputs) {
        engine.unregisterElement(input);
      }

      this.registeredInputs = [];
      this.appliedPayload = null;
    }
  }

  customElements.define(ClientValidationElementName, BlazorClientValidationDataElement);
}

/**
 * Reconciles every carrier currently in the page after an enhanced-navigation update. The DOM
 * morph reuses existing carriers (so their connectedCallback does not re-fire) and may strip the
 * form's novalidate or update a carrier's payload in place; carriers it removed or added during the
 * morph were already handled by their disconnected/connected callbacks.
 */
export function reconcileValidationElements(): void {
  document.querySelectorAll(ClientValidationElementName).forEach(element => {
    (element as ReconcilableValidationElement).reconcile?.();
  });
}

/**
 * Parses a `<blazor-client-validation-data>` payload and registers each described field's input
 * with the validation engine, attaching its event listeners. Inputs not found in the form, or
 * already registered, are skipped. Also sets `novalidate` on the form so the browser's native
 * validation does not interfere. Returns the inputs registered by this call.
 */
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
