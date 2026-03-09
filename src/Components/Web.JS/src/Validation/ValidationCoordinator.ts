// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ValidatableElement, ElementState } from './Types';
import { ValidationEngine } from './ValidationEngine';
import { ErrorDisplay } from './ErrorDisplay';

const validatableSelector = 'input[data-val="true"], select[data-val="true"], textarea[data-val="true"]';

export class ValidationCoordinator {
  private elementState: WeakMap<ValidatableElement, ElementState> = new WeakMap();

  constructor(
    private engine: ValidationEngine,
    private display: ErrorDisplay
  ) {}

  registerElement(element: ValidatableElement, state: ElementState): void {
    this.elementState.set(element, state);
  }

  unregisterElement(element: ValidatableElement): void {
    const state = this.elementState.get(element);
    if (state) {
      for (const { event, handler } of state.listeners) {
        element.removeEventListener(event, handler);
      }
      element.setCustomValidity('');
      this.elementState.delete(element);
    }
  }

  hasState(element: ValidatableElement): boolean {
    return this.elementState.has(element);
  }

  getState(element: ValidatableElement): ElementState | undefined {
    return this.elementState.get(element);
  }

  /**
     * Validate a single element against all its directives.
     * Returns the first error message, or empty string if valid.
     */
  validateElement(element: ValidatableElement): string {
    const state = this.elementState.get(element);
    if (!state) {
      return '';
    }

    const value = this.getElementValue(element);

    for (const directive of state.directives) {
      const provider = this.engine.getProvider(directive.rule);
      if (!provider) {
        continue;
      }

      const result = provider(value, element, directive.params);
      if (result === true) {
        continue;
      }

      // Provider returned false (use default message) or a string (custom message)
      return typeof result === 'string' ? result : directive.message;
    }

    return '';
  }

  /**
     * Validate and update the visual state for an element.
     */
  validateAndUpdate(element: ValidatableElement): boolean {
    const error = this.validateElement(element);
    const state = this.elementState.get(element);
    if (!state) {
      return true;
    }

    if (error) {
      this.markInvalid(element, state, error);
      return false;
    } else {
      this.markValid(element, state);
      return true;
    }
  }

  /**
     * Validate all tracked inputs within a form.
     */
  validateForm(form: HTMLFormElement): boolean {
    const inputs = form.querySelectorAll<ValidatableElement>(validatableSelector);
    let allValid = true;
    let firstInvalid: ValidatableElement | null = null;

    for (const input of Array.from(inputs)) {
      if (!this.validateAndUpdate(input)) {
        allValid = false;
        if (!firstInvalid) {
          firstInvalid = input;
        }
      }
    }

    if (firstInvalid) {
      firstInvalid.focus();
    }

    this.updateFormSummary(form);

    return allValid;
  }

  /**
     * Collect current errors and update the validation summary for a form.
     */
  updateFormSummary(form: HTMLFormElement): void {
    const errors = new Map<string, string>();
    const inputs = form.querySelectorAll<ValidatableElement>(validatableSelector);

    for (const input of Array.from(inputs)) {
      const state = this.elementState.get(input);
      if (state?.currentError) {
        const name = input.getAttribute('name') || input.id || '';
        errors.set(name, state.currentError);
      }
    }

    this.display.updateSummary(form, errors);
  }

  private markInvalid(element: ValidatableElement, state: ElementState, message: string): void {
    element.setCustomValidity(message);
    state.hasBeenInvalid = true;
    state.currentError = message;
    this.display.showFieldError(element, state.messageElements, message);
  }

  private markValid(element: ValidatableElement, state: ElementState): void {
    element.setCustomValidity('');
    state.currentError = '';
    this.display.clearFieldError(element, state.messageElements);
  }

  private getElementValue(element: ValidatableElement): string {
    if (element instanceof HTMLInputElement) {
      if (element.type === 'checkbox') {
        return element.checked ? element.value : '';
      }
      if (element.type === 'radio') {
        return element.checked ? element.value : '';
      }
    }
    return element.value;
  }
}
