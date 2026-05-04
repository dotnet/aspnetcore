// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { registerCoreValidators } from './CoreValidators';
import { DomScanner } from './DomScanner';
import { ErrorDisplay } from './ErrorDisplay';
import { EventManager } from './EventManager';
import { ValidationEngine } from './ValidationEngine';
import { ValidationOptions, ValidatableElement, ValidationService, Validator, ValidatorRegistry } from './ValidationTypes';

/**
 * Creates and initializes a client-side form validation service. Registers built-in
 * validators, scans the DOM for validatable elements, and attaches form event interceptors.
 *
 * @param options - Optional configuration (e.g., custom CSS class names).
 * @returns A ValidationService instance for programmatic access.
 */
export function createValidationService(options?: ValidationOptions): ValidationService {
  const registry = new ValidatorRegistry();
  registerCoreValidators(registry);

  const errorDisplay = new ErrorDisplay(options?.cssClasses);
  const engine = new ValidationEngine(registry, errorDisplay);
  const eventManager = new EventManager(engine);
  const scanner = new DomScanner(engine, eventManager);

  eventManager.attachFormInterceptors();
  scanner.scan(document);

  return {
    addValidator: (name: string, validator: Validator) => registry.set(name, validator),
    scanRules: (elementOrSelector?: ParentNode | string) => scanner.scan(elementOrSelector),
    validateField: (element: ValidatableElement) => engine.validateElement(element),
    validateForm: (form: HTMLFormElement) => engine.validateForm(form).size === 0,
  };
}
