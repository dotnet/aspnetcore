// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { CssClassConfig, defaultCssClasses, SyncValidationProvider, ValidatableElement } from './Types';
import { ValidationEngine } from './ValidationEngine';
import { registerBuiltInProviders } from './BuiltInProviders';
import { ErrorDisplay } from './ErrorDisplay';
import { ValidationCoordinator } from './ValidationCoordinator';
import { EventManager } from './EventManager';
import { DomScanner } from './DomScanner';

declare const Blazor: { addEventListener?: (name: string, callback: () => void) => void } | undefined;

export interface BlazorValidationApi {
  /** Register a synchronous validation provider. Replaces any existing provider with the same name. */
  addProvider(name: string, provider: SyncValidationProvider): void;
  /** Scan a DOM subtree for new validatable elements (called automatically on enhanced navigation). */
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

export function initializeBlazorValidation(cssOverrides?: Partial<CssClassConfig>): void {
  const css: CssClassConfig = { ...defaultCssClasses, ...cssOverrides };
  const engine = new ValidationEngine();
  registerBuiltInProviders(engine);

  const display = new ErrorDisplay(css);
  const coordinator = new ValidationCoordinator(engine, display);
  const eventManager = new EventManager(coordinator);
  const scanner = new DomScanner(coordinator, eventManager);

  eventManager.attachSubmitInterception();
  eventManager.attachResetInterception();

  const api: BlazorValidationApi = {
    addProvider: (name, provider) => {
      // Wrap the provider with a runtime guard against async returns.
      // The SyncValidationProvider type prevents Promises at compile time,
      // but JS consumers could still return one — catch it at runtime.
      engine.addProvider(name, (value, element, params) => {
        const result: unknown = provider(value, element, params);
        if (result instanceof Promise) {
          throw new Error(
            `Validation provider '${name}' returned a Promise. ` +
            `Async providers are not supported in Blazor mode. ` +
            `Return a boolean or string synchronously.`
          );
        }
        return result as boolean | string;
      });
    },
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

  // Initial DOM scan
  api.scan();

  // Re-scan after Blazor enhanced navigation patches the DOM
  if (typeof Blazor !== 'undefined' && Blazor?.addEventListener) {
    Blazor.addEventListener('enhancedload', () => api.scan());
  }

  // Expose public API on the Blazor global object and window for extensibility
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  (window as any).Blazor && ((window as any).Blazor.validation = api);
  (window as any).__aspnetValidation = api;
}
