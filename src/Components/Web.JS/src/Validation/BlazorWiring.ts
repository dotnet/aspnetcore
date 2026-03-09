// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { CssClassConfig, defaultCssClasses, ValidationProvider, ValidatableElement } from './Types';
import { ValidationEngine } from './ValidationEngine';
import { registerBuiltInProviders } from './BuiltInProviders';
import { ErrorDisplay } from './ErrorDisplay';
import { ValidationCoordinator } from './ValidationCoordinator';
import { EventManager } from './EventManager';
import { DomScanner } from './DomScanner';

declare const Blazor: { addEventListener?: (name: string, callback: () => void) => void } | undefined;

export interface ValidationServiceApi {
  addProvider(name: string, provider: ValidationProvider): void;
  scan(root?: ParentNode): void;
  validateForm(form: HTMLFormElement): boolean;
  validateField(input: ValidatableElement): boolean;
}

export function createValidationService(cssOverrides?: Partial<CssClassConfig>): ValidationServiceApi {
  const css: CssClassConfig = { ...defaultCssClasses, ...cssOverrides };
  const engine = new ValidationEngine();
  registerBuiltInProviders(engine);

  const display = new ErrorDisplay(css);
  const coordinator = new ValidationCoordinator(engine, display);
  const eventManager = new EventManager(coordinator);
  const scanner = new DomScanner(coordinator, eventManager);

  eventManager.attachSubmitInterception();

  return {
    addProvider: (name, provider) => engine.addProvider(name, provider),
    scan: (root) => scanner.scan(root || document),
    validateForm: (form) => coordinator.validateForm(form),
    validateField: (input) => coordinator.validateAndUpdate(input),
  };
}

export function initializeBlazorValidation(cssOverrides?: Partial<CssClassConfig>): void {
  const service = createValidationService(cssOverrides);

  // Initial DOM scan
  service.scan(document);

  // Re-scan after Blazor enhanced navigation patches the DOM
  if (typeof Blazor !== 'undefined' && Blazor?.addEventListener) {
    Blazor.addEventListener('enhancedload', () => service.scan(document));
  }

  // Expose public API for extensibility
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  (window as any).__aspnetValidation = service;
}
