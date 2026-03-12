// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export type ValidatableElement = HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement;

/**
 * The result of a synchronous validation check.
 * - true: valid
 * - false: invalid (use the directive's default error message)
 * - string: invalid with a custom error message
 */
export type ValidationProviderResult = boolean | string;

/**
 * A synchronous validation provider function.
 * Used by Blazor, where async validation is not supported.
 */
export type SyncValidationProvider = (
  value: string,
  element: ValidatableElement,
  params: Record<string, string>
) => ValidationProviderResult;

/**
 * A validation provider function. May return a result synchronously
 * or a Promise for async validation (e.g., remote server checks).
 * Used by MVC, which supports async providers via the requestSubmit fallback.
 */
export type ValidationProvider = (
  value: string,
  element: ValidatableElement,
  params: Record<string, string>
) => ValidationProviderResult | Promise<ValidationProviderResult>;

export interface ValidationDirective {
  rule: string;
  message: string;
  params: Record<string, string>;
}

export interface ElementState {
  directives: ValidationDirective[];
  hasBeenInvalid: boolean;
  currentError: string;
  messageElements: Element[];
  listeners: { event: string; handler: EventListener }[];
  directiveFingerprint: string;
}

export interface CssClassConfig {
  inputError: string;
  inputValid: string;
  messageError: string;
  messageValid: string;
  summaryError: string;
  summaryValid: string;
}

export const defaultCssClasses: CssClassConfig = {
  inputError: 'input-validation-error',
  inputValid: 'input-validation-valid',
  messageError: 'field-validation-error',
  messageValid: 'field-validation-valid',
  summaryError: 'validation-summary-errors',
  summaryValid: 'validation-summary-valid',
};
