// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { registerCoreValidators } from './CoreValidators';
import { DomScanner } from './DomScanner';
import { ErrorDisplay } from './ErrorDisplay';
import { EventManager } from './EventManager';
import { ValidationEngine } from './ValidationEngine';
import { ValidatableElement, ValidationService, Validator, ValidatorRegistry } from './ValidationTypes';

export function initializeBlazorValidation(): ValidationService {
  const registry = new ValidatorRegistry();
  registerCoreValidators(registry);

  const errorDisplay = new ErrorDisplay();
  const engine = new ValidationEngine(registry, errorDisplay, undefined);
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
