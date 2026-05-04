// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/** An HTML form element that can be validated. */
export type ValidatableElement = HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement;

/** Context passed to a validator function with the current field value, element, and rule parameters. */
export type ValidationContext = {
  value: string | null | undefined;
  element: ValidatableElement;
  params: Record<string, string>;
};

/**
 * The result of a validator function.
 * - `true` means valid.
 * - `false` means invalid (the rule's default error message is used).
 * - A `string` means invalid with a custom error message.
 */
export type ValidationResult = boolean | string;

/** A function that validates a field value and returns a result. */
export type Validator = (context: ValidationContext) => ValidationResult;

/** Configuration options for the client-side form validation service. */
export interface ValidationOptions {
  /** Override default CSS class names for validation states.
   *  Supports space-separated class names (e.g., 'border-red-500 ring-1'). */
  cssClasses?: Partial<import('./ErrorDisplay').CssClassNames>;
}

/**
 * Public API for client-side form validation. Exposed as `Blazor.formValidation`
 * (when embedded in blazor.web.js) or `window.__aspnetValidation` (standalone bundle).
 */
export interface ValidationService {
  /** Registers a custom validator function for a given rule name (e.g., 'zipcode'). */
  addValidator(name: string, validator: Validator): void;
  /** Scans the DOM (or a subtree) for validatable elements and registers them. */
  scanRules(elementOrSelector?: ParentNode | string): void;
  /** Validates a single field element and updates its error display. */
  validateField(element: ValidatableElement): boolean;
  /** Validates all tracked fields in the form. Returns true if all fields are valid. */
  validateForm(form: HTMLFormElement): boolean;
}

interface ValidatorEntry {
  fn: Validator;
}

/**
 * Maps rule names (e.g., 'required', 'range') to validator functions.
 * Used internally for built-in validators and externally via `addValidator`.
 */
export class ValidatorRegistry {
  private validators: Map<string, ValidatorEntry> = new Map();

  set(name: string, validator: Validator): void {
    this.validators.set(name, { fn: validator });
  }

  get(name: string): Validator | undefined {
    return this.validators.get(name)?.fn;
  }
}
