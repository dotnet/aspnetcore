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
     * If an element is already tracked but its data-val-* attributes have changed
     * (e.g., after enhanced navigation DOM patching), it is re-registered with
     * fresh directives and message elements.
     */
  scan(root: ParentNode): void {
    const inputs = root.querySelectorAll<ValidatableElement>('input[data-val="true"], select[data-val="true"], textarea[data-val="true"]');
    const initializedForms = new Set<HTMLFormElement>();

    for (const input of Array.from(inputs)) {
      const existingState = this.coordinator.getState(input);
      if (existingState) {
        // Element already tracked — check if its attributes have changed
        // (DomSync can keep a DOM node but update its attributes)
        const currentFingerprint = this.getDirectiveFingerprint(input);
        if (existingState.directiveFingerprint === currentFingerprint) {
          continue;
        }
        // Attributes changed — unregister old state before re-registering
        this.coordinator.unregisterElement(input);
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
        directiveFingerprint: this.getDirectiveFingerprint(input),
      });

      this.eventManager.attachInputListeners(input);

      // Suppress native browser validation UI — we handle it ourselves
      if (!form.hasAttribute('novalidate')) {
        form.setAttribute('novalidate', '');
      }

      initializedForms.add(form);
    }

    // Initialize validation summaries to the "valid" state so they don't
    // show an empty error container before the first validation.
    for (const form of initializedForms) {
      this.coordinator.updateFormSummary(form);
    }
  }

  /**
     * Compute a fingerprint of all data-val-* attributes on an element.
     * Used to detect when DomSync has kept a DOM node but changed its validation attributes.
     */
  private getDirectiveFingerprint(element: ValidatableElement): string {
    const parts: string[] = [];
    for (let i = 0; i < element.attributes.length; i++) {
      const attr = element.attributes[i];
      if (attr.name.startsWith('data-val')) {
        parts.push(`${attr.name}=${attr.value}`);
      }
    }
    parts.sort();
    return parts.join('|');
  }
}
