// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { registerBuiltInValidators } from './BuiltInValidators';
import { DomScanner } from './DomScanner';
import { ErrorDisplay } from './ErrorDisplay';
import { EventManager } from './EventManager';
import { ValidationEngine } from './ValidationEngine';
import { ValidatableElement, Validator, ValidatorRegistry } from './Validator';

export interface BlazorValidationService {
  addValidator(name: string, validator: Validator): void;
  scan(elementOrSelector?: ParentNode | string): void;
  validateField(element: ValidatableElement): boolean;
  validateForm(form: HTMLFormElement): boolean;
}

export function createBlazorValidation(): BlazorValidationService {
  const registry = new ValidatorRegistry();
  registerBuiltInValidators(registry);

  const errorDisplay = new ErrorDisplay();
  const engine = new ValidationEngine(registry, errorDisplay);
  const eventManager = new EventManager(engine);
  const scanner = new DomScanner(engine, eventManager);

  eventManager.attachFormInterceptors();
  scanner.scan(document);

  return {
    addValidator: (name: string, validator: Validator) => registry.set(name, validator),
    scan: (elementOrSelector?: ParentNode | string) => scanner.scan(elementOrSelector),
    validateField: (element: ValidatableElement) => engine.validateElement(element),
    validateForm: (form: HTMLFormElement) => engine.validateForm(form),
  };
}
