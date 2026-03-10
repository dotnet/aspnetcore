// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export type ValidatableElement = HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement;

/**
 * A validation provider function.
 * @returns true if valid, false to use the directive's default error message, or a string for a custom error message.
 */
export type ValidationProvider = (
  value: string,
  element: ValidatableElement,
  params: Record<string, string>
) => boolean | string;

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
