// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export type ValidatableElement = HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement;

export type ValidationContext = {
  value: string | null | undefined;
  element: ValidatableElement;
  params: Record<string, string>;
}

export type ValidationResult = boolean | string;

export type Validator = (context: ValidationContext) => ValidationResult;

export interface ValidationService {
  addValidator(name: string, validator: Validator): void;
  scan(elementOrSelector?: ParentNode | string): void;
  validateField(element: ValidatableElement): boolean;
  validateForm(form: HTMLFormElement): boolean;
}

export class ValidatorRegistry {
  private validators: Map<string, Validator> = new Map();

  set(name: string, validator: Validator): void {
    this.validators.set(name, validator);
  }

  get(name: string): Validator | undefined {
    return this.validators.get(name);
  }
}
