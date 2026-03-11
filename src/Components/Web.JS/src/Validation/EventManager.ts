// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ValidatableElement } from './Types';
import { ValidationCoordinator } from './ValidationCoordinator';

export class EventManager {
  private submitHandler: ((e: SubmitEvent) => void) | null = null;
  private resubmitting = false;

  constructor(private coordinator: ValidationCoordinator) {}

  /**
   * Attach a document-level submit handler in the CAPTURE phase.
   * This runs before Blazor's enhanced navigation handler (which uses bubble phase),
   * allowing us to preventDefault() before enhanced nav sends the fetch request.
   *
   * Validation is always async (providers may return Promises). The handler
   * prevents the original submit, runs validation, and re-submits via
   * requestSubmit() on success.
   */
  attachSubmitInterception(): void {
    this.submitHandler = (event: SubmitEvent) => {
      // Let re-submitted events pass through to reach Blazor's enhanced nav handler
      if (this.resubmitting) {
        return;
      }

      const form = event.target;
      if (!(form instanceof HTMLFormElement)) {
        return;
      }

      // Only intercept forms that have validation-tracked inputs
      if (!form.querySelector('[data-val="true"]')) {
        return;
      }

      // Respect formnovalidate on the submit button (HTML spec)
      if (event.submitter?.hasAttribute('formnovalidate')) {
        return;
      }

      // Always prevent — validation is async, we re-submit on success
      event.preventDefault();
      event.stopPropagation();

      const submitter = event.submitter;
      this.coordinator.validateForm(form).then(isValid => {
        if (isValid) {
          this.resubmitting = true;
          form.requestSubmit(submitter as HTMLElement | undefined);
          this.resubmitting = false;
        }
      });
    };

    document.addEventListener('submit', this.submitHandler, true);
  }

  detachSubmitInterception(): void {
    if (this.submitHandler) {
      document.removeEventListener('submit', this.submitHandler, true);
      this.submitHandler = null;
    }
  }

  /**
   * Attach input/change event listeners to an element with smart validation timing.
   *
   * - 'input' events only CLEAR existing errors (prevents "red while typing")
   * - 'change' events can SET new errors (fires on blur/commit)
   */
  attachInputListeners(element: ValidatableElement): void {
    const state = this.coordinator.getState(element);
    if (!state) {
      return;
    }

    const inputHandler = () => {
      // Only validate on input if field has been shown invalid before
      if (state.hasBeenInvalid && state.currentError) {
        this.coordinator.validateAndUpdate(element).then(() => {
          const form = element.closest('form');
          if (form) {
            this.coordinator.updateFormSummary(form);
          }
        });
      }
    };

    const changeHandler = () => {
      this.coordinator.validateAndUpdate(element).then(() => {
        const form = element.closest('form');
        if (form) {
          this.coordinator.updateFormSummary(form);
        }
      });
    };

    if (element instanceof HTMLSelectElement) {
      element.addEventListener('change', changeHandler);
      state.listeners.push({ event: 'change', handler: changeHandler });
    } else {
      element.addEventListener('input', inputHandler);
      element.addEventListener('change', changeHandler);
      state.listeners.push(
        { event: 'input', handler: inputHandler },
        { event: 'change', handler: changeHandler }
      );
    }
  }
}
