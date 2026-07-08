// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ErrorDisplay } from './ErrorDisplay';
import { findMessageElements, shouldSkipElement } from './DomUtils';
import { ValidatableElement, ValidationContext, ValidationResult, ValidatorRegistry } from './ValidationTypes';

/** A parsed validation rule from data-val-* attributes on an element. */
export type ValidationRule = {
  ruleName: string;
  errorMessage: string;
  params: Record<string, string>;
};

/** Per-element validation state tracked by the engine. */
export interface ElementState {
  rules: ValidationRule[];
  form: HTMLFormElement; // Owning form, stored at registration to avoid DOM traversal on disconnect
  triggerEvents: string; // 'default' | 'submit' | space-separated event types
  fingerprint: string; // Hash of data-val* attributes for change detection during re-scan
  listenerController: AbortController;
  currentError?: string;
  hasBeenInvalid: boolean; // Enables eager recovery (input-level validation after first error)
}

/** Per-form validation state tracked by the engine. */
export interface FormState {
  trackedElements: Set<ValidatableElement>;
  hasBeenSubmitted: boolean; // Enables input-level validation after first submit attempt
}

/** CSS selector for elements that opt into client-side validation via data-val="true". */
export const validatableElementSelector = 'input[data-val="true"], select[data-val="true"], textarea[data-val="true"]';

/**
 * Central validation coordinator. Manages per-form and per-element state, runs validator
 * functions against field values, and delegates UI updates to ErrorDisplay.
 *
 * Invariants:
 * - Each validatable element is tracked in exactly one form's trackedElements set.
 * - An element's rules are immutable after registration; attribute changes trigger
 *   unregister + re-register via DomScanner's fingerprint comparison.
 * - setCustomValidity() is called on every validated element to drive the browser's
 *   Constraint Validation API (:valid/:invalid pseudo-classes).
 */
export class ValidationEngine {
  private trackedForms: Map<HTMLFormElement, FormState> = new Map();

  private trackedElements: WeakMap<ValidatableElement, ElementState> = new WeakMap();

  constructor(
    private validatorRegistry: ValidatorRegistry,
    private errorDisplay: ErrorDisplay,
  ) { }

  /** Registers a validatable element with its parsed rules and associates it with a form. */
  registerElement(element: ValidatableElement, form: HTMLFormElement, state: ElementState): void {
    this.trackedElements.set(element, state);
    const formState = this.trackedForms.get(form) ?? this.registerForm(form);
    formState.trackedElements.add(element);
    initializeMessageElementsAria(element);
  }

  /** Unregisters an element: aborts its listeners, clears errors, and removes from form tracking. */
  unregisterElement(element: ValidatableElement): void {
    const state = this.trackedElements.get(element);
    if (state) {
      // Abort active listeners for this element.
      state.listenerController.abort();
      element.setCustomValidity('');
      this.errorDisplay.clearFieldError(element);

      // Remove from form tracking using stored form ref (works even if element is disconnected from DOM)
      const formState = this.trackedForms.get(state.form);
      if (formState) {
        formState.trackedElements.delete(element);
        if (formState.trackedElements.size === 0) {
          this.trackedForms.delete(state.form);
        }
      }

      this.trackedElements.delete(element);
    }
  }

  /** Clears all validation state for a form, resets fields to neutral, and clears the summary. */
  resetForm(form: HTMLFormElement): void {
    const formState = this.trackedForms.get(form);
    if (!formState) {
      return;
    }

    for (const element of formState.trackedElements) {
      const state = this.trackedElements.get(element);
      if (state) {
        this.markPristine(element, state);
      }
    }

    formState.hasBeenSubmitted = false;
    this.errorDisplay.updateSummary(form);
  }

  getElementState(element: ValidatableElement): ElementState | undefined {
    return this.trackedElements.get(element);
  }

  getFormState(form: HTMLFormElement): FormState | undefined {
    return this.trackedForms.get(form);
  }

  getTrackedForms(): Map<HTMLFormElement, FormState> {
    return this.trackedForms;
  }

  /**
   * Validates all tracked elements in the form, updates error display and summary,
   * and focuses the first invalid field. Returns a map of field name to error message
   * (empty map when valid). The public API wraps this as a boolean.
   */
  validateForm(form: HTMLFormElement): Map<string, string> {
    const formState = this.trackedForms.get(form);
    if (!formState) {
      // The form is not being tracked, so consider it valid.
      return new Map();
    }

    const errors = new Map<string, string>();
    let firstInvalidElement: ValidatableElement | null = null;

    for (const input of formState.trackedElements) {
      if (shouldSkipElement(input)) {
        // Skip hidden fields but mark them as valid to clear previous errors
        const state = this.getElementState(input);
        if (state) {
          this.markValid(input, state);
        }
        continue;
      }

      const isValid = this.validateElement(input);
      if (!isValid) {
        const name = input.name || input.id || '';
        errors.set(name, input.validationMessage);
        firstInvalidElement ??= input;
      }
    }

    if (firstInvalidElement) {
      firstInvalidElement.focus();
    }

    this.errorDisplay.updateSummary(form, errors);
    return errors;
  }

  /** Validates a single element against all its rules, updates error display, returns validity. */
  validateElement(element: ValidatableElement): boolean {
    const state = this.getElementState(element);

    if (!state) {
      // No validation rules for this element, so consider it valid.
      return true;
    }

    const errorMessage = this.validateElementInternal(element, state);

    if (errorMessage) {
      this.markInvalid(element, state, errorMessage);
      return false;
    } else {
      this.markValid(element, state);
      return true;
    }
  }

  /** Rebuilds and updates the validation summary element for the form based on current errors. */
  updateValidationSummary(form: HTMLFormElement): void {
    const formState = this.trackedForms.get(form);
    if (!formState) {
      return;
    }

    const errors = new Map<string, string>();

    for (const element of formState.trackedElements) {
      const state = this.trackedElements.get(element);
      if (state?.currentError) {
        const name = element.getAttribute('name') || element.id || '';
        errors.set(name, state.currentError);
      }
    }

    this.errorDisplay.updateSummary(form, errors);
  }

  private validateElementInternal(element: ValidatableElement, state: ElementState): string {
    const value = getElementValue(element);
    const context: ValidationContext = { value, element, params: {} };

    for (const rule of state.rules) {
      const validator = this.validatorRegistry.get(rule.ruleName);
      if (!validator) {
        continue;
      }

      context.params = rule.params;
      const result = validator(context);
      const errorMessage = resolveErrorMessage(result, rule);

      if (errorMessage) {
        return errorMessage;
      }
    }

    return '';
  }

  private registerForm(form: HTMLFormElement): FormState {
    const formState: FormState = { trackedElements: new Set(), hasBeenSubmitted: false };
    this.trackedForms.set(form, formState);
    this.errorDisplay.updateSummary(form);
    return formState;
  }


  private markInvalid(element: ValidatableElement, state: ElementState, errorMessage: string): void {
    state.currentError = errorMessage;
    state.hasBeenInvalid = true;
    element.setCustomValidity(errorMessage);
    this.errorDisplay.showFieldError(element, errorMessage);
  }

  private markValid(element: ValidatableElement, state: ElementState): void {
    state.currentError = undefined;
    element.setCustomValidity('');
    this.errorDisplay.clearFieldError(element);
  }

  private markPristine(element: ValidatableElement, state: ElementState): void {
    state.currentError = undefined;
    state.hasBeenInvalid = false;
    element.setCustomValidity('');
    this.errorDisplay.clearFieldToPristine(element);
  }
}

function getElementValue(element: ValidatableElement): string {
  if (element instanceof HTMLInputElement) {
    if (element.type === 'checkbox' || element.type === 'radio') {
      return element.checked ? element.value : '';
    }
  }

  return element.value;
}

function resolveErrorMessage(result: ValidationResult, rule: ValidationRule): string {
  if (result.success) {
    return '';
  }

  return result.message ?? rule.errorMessage;
}

function initializeMessageElementsAria(element: ValidatableElement): void {
  for (const el of findMessageElements(element)) {
    if (!el.hasAttribute('role')) {
      el.setAttribute('role', 'alert');
    }
    if (!el.hasAttribute('aria-live')) {
      el.setAttribute('aria-live', 'assertive');
    }
  }
}
