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
  /** Register a synchronous validation provider. Async providers are not supported in Blazor mode. */
  addProvider(name: string, provider: SyncValidationProvider): void;
  /** Re-scan the DOM for validatable elements (called automatically on enhanced navigation). */
  scan(root?: ParentNode): void;
  /** Validate an entire form. */
  validateForm(form: HTMLFormElement): Promise<boolean>;
  /** Validate a single field. */
  validateField(input: ValidatableElement): Promise<boolean>;
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
    scan: (root) => scanner.scan(root || document),
    validateForm: (form) => coordinator.validateForm(form),
    validateField: (input) => coordinator.validateAndUpdate(input),
  };

  // Initial DOM scan
  api.scan(document);

  // Re-scan after Blazor enhanced navigation patches the DOM
  if (typeof Blazor !== 'undefined' && Blazor?.addEventListener) {
    Blazor.addEventListener('enhancedload', () => api.scan(document));
  }

  // Expose public API for extensibility
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  (window as any).__aspnetValidation = api;
}
