// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ErrorDisplay } from './ErrorDisplay';
import { findMessageElements, getElementForm, shouldSkipElement } from './DomUtils';
import { AsyncValidationTracker, ValidatableElement, ValidationContext, ValidationResult, ValidatorRegistry } from './ValidationTypes';

export type ValidationRule = {
  ruleName: string;
  errorMessage: string;
  params: Record<string, string>;
}

export interface ElementState {
  rules: ValidationRule[];
  triggerEvents: string; // Space-separated list of event types, 'default' and 'submit' are special values.
  fingerprint: string;
  listenerController: AbortController;
  currentError?: string;
  hasBeenInvalid: boolean;
}

export interface FormState {
  trackedElements: Set<ValidatableElement>;
  hasBeenSubmitted: boolean;
}

export const validatableElementSelector = 'input[data-val="true"], select[data-val="true"], textarea[data-val="true"]';

export class ValidationEngine {
  private trackedForms: Map<HTMLFormElement, FormState> = new Map();

  private trackedElements: WeakMap<ValidatableElement, ElementState> = new WeakMap();

  constructor(
    private validatorRegistry: ValidatorRegistry,
    private errorDisplay: ErrorDisplay,
    private asyncTracker: AsyncValidationTracker,
  ) { }

  /** Called when all pending async validations resolve. Set by wiring code. */
  onPendingComplete: (() => void) | null = null;

  registerElement(element: ValidatableElement, form: HTMLFormElement, state: ElementState): void {
    this.trackedElements.set(element, state);
    const formState = this.trackedForms.get(form) ?? this.registerForm(form);
    formState.trackedElements.add(element);
    initializeMessageElementsAria(element);
  }

  unregisterElement(element: ValidatableElement): void {
    const state = this.trackedElements.get(element);
    if (state) {
      // Abort active listeners for this element.
      state.listenerController.abort();
      element.setCustomValidity('');
      this.errorDisplay.clearFieldError(element);
      this.trackedElements.delete(element);
    }

    const form = getElementForm(element);
    if (form) {
      const formState = this.trackedForms.get(form);
      if (formState) {
        formState.trackedElements.delete(element);
        if (formState.trackedElements.size === 0) {
          this.trackedForms.delete(form);
        }
      }
    }
  }

  resetForm(form: HTMLFormElement): void {
    const formState = this.trackedForms.get(form);
    if (!formState) {
      return;
    }

    for (const element of formState.trackedElements) {
      const state = this.trackedElements.get(element);
      if (state) {
        this.markValid(element, state);
        state.hasBeenInvalid = false;
      }
    }

    formState.hasBeenSubmitted = false;
    this.asyncTracker.clear();
    this.errorDisplay.updateSummary(form);
  }

  hasAsyncPending(): boolean {
    return this.asyncTracker.hasPending();
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

  validateForm(form: HTMLFormElement): boolean {
    const formState = this.trackedForms.get(form);
    if (!formState) {
      // The form is not being tracked, so consider it valid.
      return true;
    }

    const summaryErrors = new Map<string, string>();
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

      const isValid = this.validateElement(input, { immediate: true });
      if (!isValid) {
        const name = input.name || input.id || '';
        summaryErrors.set(name, input.validationMessage);
        firstInvalidElement ??= input;
      }
    }

    if (firstInvalidElement) {
      firstInvalidElement.focus();
    }

    this.errorDisplay.updateSummary(form, summaryErrors);
    return summaryErrors.size === 0;
  }

  validateElement(element: ValidatableElement, options?: { immediate?: boolean }): boolean {
    const state = this.getElementState(element);

    if (!state) {
      // No validation rules for this element, so consider it valid.
      return true;
    }

    const errorMessage = this.validateElementInternal(element, state, options?.immediate);

    if (errorMessage) {
      this.markInvalid(element, state, errorMessage);
      return false;
    } else {
      this.markValid(element, state);
      return true;
    }
  }

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

  private validateElementInternal(element: ValidatableElement, state: ElementState, immediate?: boolean): string {
    const value = getElementValue(element);
    const context: ValidationContext = { value, element, params: {}, immediate };

    // Clear any previous pending state for this element
    this.asyncTracker.clear(element);

    // Phase 1: Non-deferred validators — run first; if any fail, skip deferred entirely.
    for (const rule of state.rules) {
      if (this.validatorRegistry.isDeferred(rule.ruleName)) {
        continue;
      }

      const validator = this.validatorRegistry.get(rule.ruleName);
      if (!validator) {
        continue;
      }

      context.params = rule.params;
      const result = validator(context);
      const errorMessage = resolveErrorMessage(result as ValidationResult, rule);

      if (errorMessage) {
        return errorMessage;
      }
    }

    // Phase 2: Deferred validators — only run after all non-deferred validators pass.
    for (const rule of state.rules) {
      if (!this.validatorRegistry.isDeferred(rule.ruleName)) {
        continue;
      }

      const validator = this.validatorRegistry.get(rule.ruleName);
      if (!validator) {
        continue;
      }

      context.params = rule.params;
      context.signal = this.asyncTracker.createSignal(element, rule.ruleName);
      const result = validator(context);

      if (isPromiseLike(result)) {
        this.asyncTracker.track(element, rule.ruleName, result, () => {
          this.validateElement(element);
          const form = getElementForm(element);
          if (form) {
            this.updateValidationSummary(form);
          }

          if (!this.hasAsyncPending()) {
            this.onPendingComplete?.();
          }
        });
      } else {
        // Sync return from a deferred validator (e.g., cache hit).
        const errorMessage = resolveErrorMessage(result, rule);
        if (errorMessage) {
          return errorMessage;
        }
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
  if (result === false) {
    return rule.errorMessage;
  }

  if (typeof result === 'string') {
    return result;
  }

  return '';
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
function isPromiseLike(value: unknown): value is Promise<ValidationResult> {
  return value != null && typeof (value as any).then === 'function';
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
