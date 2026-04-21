// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export type ValidatableElement = HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement;

export type ValidationContext = {
  value: string | null | undefined;
  element: ValidatableElement;
  params: Record<string, string>;
  /** True when validation is triggered by form submission. Async validators should skip debounce. */
  immediate?: boolean;
  /** AbortSignal provided by the async tracker. Aborted when a new validation supersedes this one. */
  signal?: AbortSignal;
}

export type ValidationResult = boolean | string;

export type Validator = (context: ValidationContext) => ValidationResult | Promise<ValidationResult>;

export interface ValidatorOptions {
  /** When true, this validator runs only after all non-deferred validators pass.
   *  Use for validators that make network requests or have other side effects. */
  deferred?: boolean;
}

export interface ValidationService {
  addValidator(name: string, validator: Validator, options?: ValidatorOptions): void;
  scan(elementOrSelector?: ParentNode | string): void;
  validateField(element: ValidatableElement): boolean;
  validateForm(form: HTMLFormElement): boolean;
}

export interface AsyncValidationTracker {
  /**
   * Create an AbortSignal for an element+validator pair.
   * If a signal already exists for the same pair, the previous one is aborted.
   * Validators should pass this signal to fetch() and listen for abort to cancel debounce.
   */
  createSignal(element: ValidatableElement, validatorName: string): AbortSignal;

  /**
   * Track an async validation promise for an element+validator pair.
   * If a promise is already tracked for the same pair, the old one becomes stale
   * (its onResolved callback is silently dropped via version checking).
   */
  track(
    element: ValidatableElement,
    validatorName: string,
    promise: Promise<ValidationResult>,
    onResolved: () => void,
  ): void;

  /** Returns true if any element has pending async validation. */
  hasPending(): boolean;

  /**
   * Clear pending state and abort all in-flight signals.
   * @param element  If provided, clear only for that element. Otherwise clear all.
   */
  clear(element?: ValidatableElement): void;
}

interface ValidatorEntry {
  fn: Validator;
  deferred: boolean;
}

export class ValidatorRegistry {
  private validators: Map<string, ValidatorEntry> = new Map();

  set(name: string, validator: Validator, options?: ValidatorOptions): void {
    this.validators.set(name, { fn: validator, deferred: options?.deferred ?? false });
  }

  get(name: string): Validator | undefined {
    return this.validators.get(name)?.fn;
  }

  isDeferred(name: string): boolean {
    return this.validators.get(name)?.deferred ?? false;
  }
}
