// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ErrorDisplay } from './ErrorDisplay';
import { isHiddenElement } from './Utils';
import { ValidatableElement, ValidationContext, ValidationResult, ValidatorRegistry } from './Validator';

export type ValidationRule = {
  ruleName: string;
  errorMessage: string;
  params: Record<string, string>;
}

export interface ElementState {
  rules: ValidationRule[];
  messageElements: HTMLElement[];
}

export const validatableElementSelector = 'input[data-val="true"], select[data-val="true"], textarea[data-val="true"]';

export class ValidationEngine {
  private elementState: WeakMap<ValidatableElement, ElementState> = new WeakMap();

  constructor(
    private validatorRegistry: ValidatorRegistry,
    private errorDisplay: ErrorDisplay,
  ) { }

  validateForm(form: HTMLFormElement): boolean {
    const summaryErrors = new Map<string, string>();
    const inputs = form.querySelectorAll<ValidatableElement>(validatableElementSelector);
    let firstInvalidElement: ValidatableElement | null = null;

    for (const input of inputs) {
      if (isHiddenElement(input)) {
        // Skip hidden fields but mark them as valid to clear previous errors
        const state = this.elementState.get(input);
        if (state) {
          this.markValid(input, state);
        }
        continue;
      }

      const isValid = this.validateElement(input);
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

  validateElement(element: ValidatableElement): boolean {
    const state = this.elementState.get(element);

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

  private validateElementInternal(element: ValidatableElement, state: ElementState): string {
    const value = getElementValue(element);

    for (const rule of state.rules) {
      const validator = this.validatorRegistry.get(rule.ruleName);
      if (!validator) {
        // No validator found for this rule, so skip it.
        continue;
      }

      const context: ValidationContext = {
        value: value,
        element: element,
        params: rule.params,
      };

      const result = validator(context);
      const errorMessage = resolveErrorMessage(result, rule);

      if (errorMessage) {
        // Return the first error message found.
        return errorMessage;
      }
    }

    return '';
  }

  private markInvalid(element: ValidatableElement, state: ElementState, errorMessage: string): void {
    element.setCustomValidity(errorMessage);
    this.errorDisplay.showFieldError(element, state.messageElements, errorMessage);
  }

  private markValid(element: ValidatableElement, state: ElementState): void {
    element.setCustomValidity('');
    this.errorDisplay.clearFieldError(element, state.messageElements);
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
