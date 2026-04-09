// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ValidationEngine } from './ValidationEngine';
import { ValidatableElement } from './Validator';

type SubmitHandler = (event: SubmitEvent) => void;

// TODO: Implement "lazy validation, eager recovery" pattern

export class EventManager {
  private submitHandler: SubmitHandler | null = null;

  constructor(private engine: ValidationEngine) { }

  attachSubmitInterception(): void {
    this.submitHandler = this.handleSubmit.bind(this);
    document.addEventListener('submit', this.submitHandler, true);
  }

  detachSubmitInterception(): void {
    if (this.submitHandler) {
      document.removeEventListener('submit', this.submitHandler, true);
      this.submitHandler = null;
    }
  }

  attachInputListeners(element: ValidatableElement): void {
    const state = this.engine.getElementState(element);
    if (!state) {
      return;
    }

    if (state.triggerEvents === 'submit') {
      // No listeners for fields that are only validated on submit.
      return;
    }

    // TODO: Add gating logic for input event?
    const handler = () => {
      this.engine.validateElement(element);
      const form = element.closest('form');
      if (form instanceof HTMLFormElement) {
        this.engine.updateValidationSummary(form);
      }
    };

    for (const eventType of state.triggerEvents.split(' ')) {
      element.addEventListener(eventType, handler, { signal: state.listenerController.signal });
    }
  }

  private handleSubmit(event: SubmitEvent): void {
    const form = event.target;

    if (!(form instanceof HTMLFormElement)) {
      return;
    }

    // Only intercept tracked forms.
    if (!this.engine.getFormState(form)) {
      return;
    }

    // Respect formnovalidate on the submit button (HTML spec).
    if (event.submitter?.hasAttribute('formnovalidate')) {
      return;
    }

    // TODO: Mark form as submitted to enable eager input validation

    const result = this.engine.validateForm(form);

    if (!result) {
      event.preventDefault();
      event.stopPropagation();
    }

    dispatchValidationComplete(form, result);
  }
}

function dispatchValidationComplete(form: HTMLFormElement, valid: boolean): void {
  form.dispatchEvent(new CustomEvent('validationcomplete', {
    bubbles: true,
    detail: { valid },
  }));
}
