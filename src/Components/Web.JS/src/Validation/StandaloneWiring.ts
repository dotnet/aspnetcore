// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { CssClassConfig, defaultCssClasses, ValidationProvider, ValidatableElement } from './Types';
import { ValidationEngine } from './ValidationEngine';
import { registerBuiltInProviders } from './BuiltInProviders';
import { remoteProvider } from './RemoteProvider';
import { ErrorDisplay } from './ErrorDisplay';
import { ValidationCoordinator } from './ValidationCoordinator';
import { EventManager } from './EventManager';
import { DomScanner } from './DomScanner';

export interface StandaloneValidationApi {
  /** Register a custom validation provider (sync or async). Replaces any existing provider with the same name. */
  addProvider(name: string, provider: ValidationProvider): void;
  /**
   * Scan a DOM subtree for new validatable elements.
   * Drop-in replacement for $.validator.unobtrusive.parse().
   * Accepts a CSS selector string, an Element, or a ParentNode.
   * When called with no arguments, scans the entire document.
   */
  scan(selectorOrElement?: string | Element | ParentNode): void;
  /** Validate an entire form. */
  validateForm(form: HTMLFormElement): Promise<boolean>;
  /** Validate a single field. */
  validateField(input: ValidatableElement): Promise<boolean>;
  /** Programmatically set a validation error on a field. */
  setError(input: ValidatableElement, message: string): void;
  /** Programmatically clear a validation error from a field. */
  clearError(input: ValidatableElement): void;
}

export function initializeStandaloneValidation(cssOverrides?: Partial<CssClassConfig>): void {
  const css: CssClassConfig = { ...defaultCssClasses, ...cssOverrides };
  const engine = new ValidationEngine();
  registerBuiltInProviders(engine);

  // Register remote provider (supported in standalone mode, not in Blazor mode)
  engine.addProvider('remote', remoteProvider);

  const display = new ErrorDisplay(css);
  const coordinator = new ValidationCoordinator(engine, display);
  const eventManager = new EventManager(coordinator);
  const scanner = new DomScanner(coordinator, eventManager);

  eventManager.attachSubmitInterception();
  eventManager.attachResetInterception();

  // Initial scan
  scanner.scan(document);

  const api: StandaloneValidationApi = {
    addProvider: (name, provider) => engine.addProvider(name, provider),
    scan: (selectorOrElement?) => {
      if (!selectorOrElement) {
        scanner.scan(document);
      } else if (typeof selectorOrElement === 'string') {
        const root = document.querySelector(selectorOrElement);
        if (root) {
          scanner.scan(root);
        }
      } else {
        scanner.scan(selectorOrElement as ParentNode);
      }
    },
    validateForm: (form) => coordinator.validateForm(form),
    validateField: (input) => coordinator.validateAndUpdate(input),
    setError: (input, message) => coordinator.setError(input, message),
    clearError: (input) => coordinator.clearError(input),
  };

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  (window as any).__aspnetValidation = api;
}
