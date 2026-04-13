// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { getElementForm } from './DomUtils';
import { ValidationEngine } from './ValidationEngine';
import { ValidatableElement } from './ValidationTypes';

export class EventManager {
  private formInterceptorController: AbortController | null = null;

  private deferredSubmission: { form: HTMLFormElement; submitter: Element | null } | null = null;

  constructor(private engine: ValidationEngine) { }

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

    // Explicit data-val-event override: listen to the specified event(s), no gating.
    if (state.triggerEvents !== 'default') {
      for (const eventType of state.triggerEvents.split(' ')) {
        element.addEventListener(eventType, validate, { signal });
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
    const result = this.engine.validateForm(form);

    if (!result) {
      event.preventDefault();
      event.stopPropagation();
      dispatchValidationComplete(form, false);
      return;
    }

    // Block submission if async validation is still in flight.
    if (this.engine.hasAsyncPending()) {
      event.preventDefault();
      event.stopPropagation();
      this.deferredSubmission = { form, submitter: event.submitter };
      return;
    }

    dispatchValidationComplete(form, result);
  }

  /** Called when all async validators resolve. Retries deferred submission. */
  retryDeferredSubmission(): void {
    if (!this.deferredSubmission) {
      return;
    }

    if (this.engine.hasAsyncPending()) {
      return; // still pending
    }

    const { form, submitter } = this.deferredSubmission;
    this.deferredSubmission = null;

    const result = this.engine.validateForm(form);
    dispatchValidationComplete(form, result);

    if (result) {
      // requestSubmit fires a real SubmitEvent → compatible with enhanced nav
      form.requestSubmit(submitter as HTMLElement | null);
    }
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

    // Clear deferred submission on reset.
    if (this.deferredSubmission?.form === form) {
      this.deferredSubmission = null;
    }

    // Use setTimeout because the reset event fires before the browser resets field values.
    // We clear validation state after values are reset so the state is consistent.
    setTimeout(() => this.engine.resetForm(form), 0);
  }
}

function dispatchValidationComplete(form: HTMLFormElement, valid: boolean): void {
  form.dispatchEvent(new CustomEvent('validationcomplete', {
    bubbles: true,
    detail: { valid },
  }));
}
