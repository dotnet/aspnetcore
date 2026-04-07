// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { registerBuiltInValidators } from './BuiltInValidators';
import { DomScanner } from './DomScanner';
import { ValidationEngine } from './ValidationEngine';
import { ValidatableElement, Validator, ValidatorRegistry } from './Validator';

export interface StandaloneValidationService {
  addValidator(name: string, validator: Validator): void;
  scan(): void;
  validateField(element: ValidatableElement): Promise<boolean>;
  validateForm(form: HTMLFormElement): Promise<boolean>;
}

export function initializeStandaloneValidation(): void {
  const registry = new ValidatorRegistry();
  registerBuiltInValidators(registry);

  const engine = new ValidationEngine(registry);
  const scanner = new DomScanner(engine);

  const api: StandaloneValidationService = {
    addValidator: (name: string, validator: Validator) => registry.set(name, validator),
    scan: () => {
      // TODO: Add support for scanning subtrees.
      scanner.scan(document);
    },
    validateField: (element: ValidatableElement) => engine.validateField(element),
    validateForm: (form: HTMLFormElement) => engine.validateForm(form),
  };

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  (window as any).__aspnetValidation = api;
}
