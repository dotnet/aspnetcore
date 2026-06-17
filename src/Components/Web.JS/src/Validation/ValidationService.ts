// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { defineBlazorClientValidationDataElement } from './Adapters/BlazorAdapter';
import { registerCoreValidators } from './CoreValidators';
import { ErrorDisplay } from './ErrorDisplay';
import { EventManager } from './EventManager';
import { ValidationEngine } from './ValidationEngine';
import { ValidationOptions, ValidatableElement, ValidationService, Validator, ValidatorRegistry } from './ValidationTypes';

/**
 * Creates and initializes a client-side form validation service. Registers built-in
 * validators, defines the form-associated `<blazor-client-validation-data>` custom element
 * that ingests the SSR-rendered validation rules, and attaches form event interceptors.
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

  eventManager.attachFormInterceptors();

  // Register validation rules from the Blazor rendered form associated custom element:
  // define the custom element and upgrade any instances already parsed before this ran,
  // which fires their connectedCallback retroactively.
  defineBlazorClientValidationDataElement(engine, eventManager);
  customElements.upgrade(document);

  return {
    addValidator: (name: string, validator: Validator) => registry.set(name, validator),
    validateField: (element: ValidatableElement) => engine.validateElement(element),
    validateForm: (form: HTMLFormElement) => engine.validateForm(form).size === 0,
  };
}
