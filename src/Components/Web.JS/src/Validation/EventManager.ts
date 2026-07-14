// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { getElementForm } from './DomUtils';
import { ValidationEngine } from './ValidationEngine';
import { ValidatableElement } from './ValidationTypes';

/**
 * Manages DOM event listeners for validation triggers and form submission interception.
 *
 * Form-level listeners use capture phase on `document` to fire before other handlers
 * (including Blazor enhanced navigation). When validation fails, stopPropagation()
 * prevents the submit from reaching bubble-phase handlers.
 */
export class EventManager {
  private formInterceptorController: AbortController | null = null;

  constructor(private engine: ValidationEngine) { }

  /**
   * Attaches per-field validation listeners based on the element's trigger configuration.
   * Default: 'lazy validation, eager recovery' — validate on change always, on input
   * only after the form has been submitted or the field has had an error.
   */
  attachInputListeners(element: ValidatableElement): void {
    const state = this.engine.getElementState(element);
    if (!state) {
      return;
    }

    if (state.triggerEvents === 'submit') {
      // No listeners for fields that are only validated on submit.
      return;
    }

    const form = getElementForm(element);
    if (!form) {
      return;
    }

    const formState = this.engine.getFormState(form);
    if (!formState) {
      return;
    }

    const signal = state.listenerController.signal;

    const validate = () => {
      this.engine.validateElement(element);
      this.engine.updateValidationSummary(form);
    };

    // Explicit data-valevent override: listen to the specified event(s), no gating.
    if (state.triggerEvents !== 'default') {
      for (const eventType of state.triggerEvents.split(/\s+/)) {
        if (eventType) {
          element.addEventListener(eventType, validate, { signal });
        }
      }
      return;
    }

    // Default: lazy validation, eager recovery.
    // Always validate on 'change' (fires on blur-commit for text, immediately for checkbox/select).
    // After the form has been submitted, or after the field has been invalid, validate also on 'input'.
    const validateGated = () => {
      if (formState.hasBeenSubmitted || state.hasBeenInvalid) {
        validate();
      }
    };

    element.addEventListener('change', validate, { signal });
    element.addEventListener('input', validateGated, { signal });
  }

  /** Attaches document-level submit and reset interceptors (capture phase). */
  attachFormInterceptors(): void {
    this.formInterceptorController = new AbortController();
    const signal = this.formInterceptorController.signal;

    document.addEventListener('submit', this.handleSubmit.bind(this), { signal, capture: true });
    document.addEventListener('reset', this.handleReset.bind(this), { signal, capture: true });
  }

  detachFormInterceptors(): void {
    this.formInterceptorController?.abort();
    this.formInterceptorController = null;
  }

  private handleSubmit(event: SubmitEvent): void {
    const form = event.target;

    if (!(form instanceof HTMLFormElement)) {
      return;
    }

    // Respect formnovalidate on the submit button (HTML spec).
    if (event.submitter?.hasAttribute('formnovalidate')) {
      return;
    }

    // Only intercept tracked forms.
    const formState = this.engine.getFormState(form);
    if (!formState) {
      return;
    }

    formState.hasBeenSubmitted = true;
    const errors = this.engine.validateForm(form);

    if (errors.size > 0) {
      event.preventDefault();
      event.stopPropagation();
      dispatchValidationComplete(form, false, errors);
      return;
    }

    dispatchValidationComplete(form, true, errors);
  }

  private handleReset(event: Event): void {
    const form = event.target;
    if (!(form instanceof HTMLFormElement)) {
      return;
    }

    // Only intercept tracked forms.
    if (!this.engine.getFormState(form)) {
      return;
    }

    // Use setTimeout because the reset event fires before the browser resets field values.
    // We clear validation state after values are reset so the state is consistent.
    setTimeout(() => this.engine.resetForm(form), 0);
  }
}

/**
 * Dispatches a 'validationcomplete' custom event on the form. Bubbles to document.
 * detail.valid: whether the form is valid. detail.errors: Map of field name to error message.
 */
function dispatchValidationComplete(form: HTMLFormElement, valid: boolean, errors: Map<string, string>): void {
  form.dispatchEvent(new CustomEvent('validationcomplete', {
    bubbles: true,
    detail: { valid, errors },
  }));
}
