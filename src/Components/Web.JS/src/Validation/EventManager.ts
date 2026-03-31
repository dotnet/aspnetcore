// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ValidatableElement } from './Types';
import { ValidationCoordinator } from './ValidationCoordinator';

export class EventManager {
  private submitHandler: ((e: SubmitEvent) => void) | null = null;
  private resetHandler: ((e: Event) => void) | null = null;
  private resubmitting = false;
  private submittedForms = new WeakSet<HTMLFormElement>();

  constructor(private coordinator: ValidationCoordinator) {}

  /**
   * Returns true if the given form has been submitted at least once.
   * Used by input handlers to decide whether to validate on typing.
   */
  isFormSubmitted(form: HTMLFormElement): boolean {
    return this.submittedForms.has(form);
  }

  /**
   * Attach a document-level submit handler in the CAPTURE phase.
   * This runs before Blazor's enhanced navigation handler (which uses bubble phase),
   * allowing us to preventDefault() before enhanced nav sends the fetch request.
   *
   * Uses a sync-first strategy:
   * 1. Try validateFormSync() — if all providers return synchronously, we can
   *    decide within the original event handler whether to prevent.
   * 2. If any provider returns a Promise (async), fall back to preventDefault()
   *    + async validation + requestSubmit() on success.
   *
   * The sync path is critical for Blazor SSR: Blazor's EventDelegator intercepts
   * submit events in capture phase and prevents re-submitted synthetic events from
   * reaching the .NET handler. By validating synchronously when possible, we avoid
   * the need to re-submit entirely.
   */
  attachSubmitInterception(): void {
    this.submitHandler = (event: SubmitEvent) => {
      // Let re-submitted events pass through (async path only)
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

      // Mark this form as submitted — enables eager input validation
      this.submittedForms.add(form);

      // Try synchronous validation first
      const syncResult = this.coordinator.validateFormSync(form);

      if (syncResult === true) {
        // All providers returned synchronously and form is valid — let the event through
        return;
      }

      if (syncResult === false) {
        // All providers returned synchronously and form is invalid — block
        event.preventDefault();
        event.stopPropagation();
        return;
      }

      // syncResult === 'async': at least one provider returned a Promise.
      // Must prevent, validate async, and re-submit on success.
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

  /**
   * Attach a document-level reset handler in the CAPTURE phase.
   * Clears all validation state and submitted tracking for the form.
   * Uses setTimeout(0) because the reset event fires before the browser
   * resets field values — we need to clear state after values are reset.
   */
  attachResetInterception(): void {
    this.resetHandler = (event: Event) => {
      const form = event.target;
      if (!(form instanceof HTMLFormElement)) {
        return;
      }

      if (!form.querySelector('[data-val="true"]')) {
        return;
      }

      this.submittedForms.delete(form);
      setTimeout(() => this.coordinator.clearForm(form), 0);
    };

    document.addEventListener('reset', this.resetHandler, true);
  }

  detachSubmitInterception(): void {
    if (this.submitHandler) {
      document.removeEventListener('submit', this.submitHandler, true);
      this.submitHandler = null;
    }
  }

  detachResetInterception(): void {
    if (this.resetHandler) {
      document.removeEventListener('reset', this.resetHandler, true);
      this.resetHandler = null;
    }
  }

  /**
   * Attach input/change event listeners to an element with smart validation timing
   * that matches MVC's jQuery validation defaults.
   *
   * Timing behavior:
   * - 'input' events: validate only if the form has been submitted or the field
   *   is currently invalid. This prevents "red while typing" on pristine forms
   *   while providing immediate feedback after first submit or first error.
   * - 'change' events: always validate (fires on blur/commit).
   *
   * The data-val-event attribute can override which events trigger validation:
   * - "change"       — validate only on blur (no typing validation)
   * - "none"         — no real-time validation (submit-only)
   * - "input change" — explicit default for text inputs
   * - Any space-separated list of DOM event names
   */
  attachInputListeners(element: ValidatableElement): void {
    const state = this.coordinator.getState(element);
    if (!state) {
      return;
    }

    const customEvent = element.getAttribute('data-val-event');

    // "none" means submit-only — no real-time validation listeners
    if (customEvent === 'none') {
      return;
    }

    const inputHandler = () => {
      const form = element.closest('form');
      // Validate on input if: form was submitted OR field is currently invalid
      // This matches jQuery validation's onkeyup default behavior:
      // element.name in this.submitted || element.name in this.invalid
      const shouldValidate = state.currentError ||
        (form instanceof HTMLFormElement && this.isFormSubmitted(form));
      if (shouldValidate) {
        this.coordinator.validateAndUpdate(element).then(() => {
          if (form instanceof HTMLFormElement) {
            this.coordinator.updateFormSummary(form);
          }
        });
      }
    };

    const changeHandler = () => {
      this.coordinator.validateAndUpdate(element).then(() => {
        const form = element.closest('form');
        if (form instanceof HTMLFormElement) {
          this.coordinator.updateFormSummary(form);
        }
      });
    };

    // Determine which events to listen to
    const events = customEvent
      ? customEvent.split(/\s+/)
      : (element instanceof HTMLSelectElement
        ? ['change']
        : ['input', 'change']);

    for (const eventName of events) {
      // 'input' uses the gated handler; all other events use the change (always-validate) handler
      const handler = eventName === 'input' ? inputHandler : changeHandler;
      element.addEventListener(eventName, handler);
      state.listeners.push({ event: eventName, handler });
    }
  }
}
