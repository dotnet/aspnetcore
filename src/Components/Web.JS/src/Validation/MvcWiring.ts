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

export interface MvcValidationApi {
  /** Register a custom validation provider (sync or async). */
  addProvider(name: string, provider: ValidationProvider): void;
  /**
   * Scan a DOM subtree for new validatable elements.
   * Drop-in replacement for $.validator.unobtrusive.parse().
   * Accepts a CSS selector string, an Element, or a ParentNode.
   * When called with no arguments, scans the entire document.
   */
  parse(selectorOrElement?: string | Element | ParentNode): void;
  /** Validate an entire form. */
  validateForm(form: HTMLFormElement): Promise<boolean>;
  /** Validate a single field. */
  validateField(input: ValidatableElement): Promise<boolean>;
}

export function initializeMvcValidation(cssOverrides?: Partial<CssClassConfig>): void {
  const css: CssClassConfig = { ...defaultCssClasses, ...cssOverrides };
  const engine = new ValidationEngine();
  registerBuiltInProviders(engine);

  // Register remote provider (MVC-only)
  engine.addProvider('remote', remoteProvider);

  const display = new ErrorDisplay(css);
  const coordinator = new ValidationCoordinator(engine, display);
  const eventManager = new EventManager(coordinator);
  const scanner = new DomScanner(coordinator, eventManager);

  eventManager.attachSubmitInterception();

  // Initial scan
  scanner.scan(document);

  // Expose MVC-compatible API
  const api: MvcValidationApi = {
    addProvider: (name, provider) => engine.addProvider(name, provider),
    parse: (selectorOrElement?) => {
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
  };

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  (window as any).__aspnetValidation = api;
}
