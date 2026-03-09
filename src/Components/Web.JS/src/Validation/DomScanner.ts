// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ValidatableElement } from './Types';
import { parseDirectives } from './DirectiveParser';
import { findMessageElements } from './ErrorDisplay';
import { ValidationCoordinator } from './ValidationCoordinator';
import { EventManager } from './EventManager';

export class DomScanner {
  constructor(
    private coordinator: ValidationCoordinator,
    private eventManager: EventManager
  ) {}

  /**
     * Scan a DOM subtree for validatable elements and wire them up.
     * Idempotent — already-tracked elements are skipped.
     */
  scan(root: ParentNode): void {
    const inputs = root.querySelectorAll<ValidatableElement>('input[data-val="true"], select[data-val="true"], textarea[data-val="true"]');

    for (const input of Array.from(inputs)) {
      if (this.coordinator.hasState(input)) {
        continue;
      }

      const directives = parseDirectives(input);
      if (directives.length === 0) {
        continue;
      }

      const form = input.closest('form');
      if (!form) {
        continue;
      }

      const messageElements = findMessageElements(input, form);

      this.coordinator.registerElement(input, {
        directives,
        hasBeenInvalid: false,
        currentError: '',
        messageElements,
        listeners: [],
      });

      this.eventManager.attachInputListeners(input);

      // Suppress native browser validation UI — we handle it ourselves
      if (!form.hasAttribute('novalidate')) {
        form.setAttribute('novalidate', '');
      }
    }
  }
}
