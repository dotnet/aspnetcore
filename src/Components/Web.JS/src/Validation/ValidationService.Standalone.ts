// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { registerCoreValidators } from './CoreValidators';
import { DomScanner } from './DomScanner';
import { ErrorDisplay } from './ErrorDisplay';
import { EventManager } from './EventManager';
import { DefaultAsyncValidationTracker } from './AsyncValidationTracker';
import { ValidationEngine } from './ValidationEngine';
import { ValidatableElement, ValidationService, Validator, ValidatorOptions, ValidatorRegistry } from './ValidationTypes';
import { numberValidator } from './Validators/Number';
import { remoteValidator } from './Validators/Remote';

export function initializeStandaloneValidation(): void {
  const registry = new ValidatorRegistry();
  registerCoreValidators(registry);

  // MVC-specific validators (not included in the Blazor bundle)
  registry.set('number', numberValidator);
  registry.set('remote', remoteValidator, { async: true });

  const errorDisplay = new ErrorDisplay();
  const asyncTracker = new DefaultAsyncValidationTracker();
  const engine = new ValidationEngine(registry, errorDisplay, asyncTracker);
  const eventManager = new EventManager(engine);
  const scanner = new DomScanner(engine, eventManager);

  // Wire deferred submission retry: when all async validators resolve, retry any blocked submit.
  engine.onPendingComplete = () => eventManager.retryDeferredSubmission();
  eventManager.attachFormInterceptors();
  scanner.scan(document);

  const service: ValidationService = {
    addValidator: (name: string, validator: Validator, options?: ValidatorOptions) => registry.set(name, validator, options),
    scan: (elementOrSelector?: ParentNode | string) => scanner.scan(elementOrSelector),
    validateField: (element: ValidatableElement) => engine.validateElement(element),
    validateForm: (form: HTMLFormElement) => engine.validateForm(form),
  };

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  (window as any).__aspnetValidation = service;
}
