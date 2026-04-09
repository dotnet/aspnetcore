// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { EventManager } from './EventManager';
import { getElementForm, shouldSkipElement } from './Utils';
import { ElementState, validatableElementSelector, ValidationEngine, ValidationRule } from './ValidationEngine';
import { ValidatableElement } from './Validator';

export class DomScanner {
  constructor(
    private engine: ValidationEngine,
    private eventManager: EventManager
  ) { }

  scan(elementOrSelector?: ParentNode | string): void {
    if (!elementOrSelector) {
      this.scanSubtree(document);
    } else {
      const root = typeof elementOrSelector === 'string'
        ? document.querySelector(elementOrSelector)
        : elementOrSelector;
      if (root) {
        this.scanSubtree(root);
      }
    }
  }

  private scanSubtree(root: ParentNode): void {
    // Phase 1: Reconcile - clean up elements that are no longer valid candidates
    this.reconcile(root);

    // Phase 2: Discover — find and register new/changed elements
    const candidates = root.querySelectorAll<ValidatableElement>(validatableElementSelector);

    // TODOL: Remove debug log
    console.log(`Found ${candidates.length} validatable elements.`);

    for (const element of candidates) {
      if (shouldSkipElement(element)) {
        continue;
      }

      const previousState = this.engine.getElementState(element);
      const fingerprint = computeElementFingerprint(element);

      if (previousState) {
        // Element is already tracked.
        // Check if its attributes has changed.
        if (fingerprint === previousState.fingerprint) {
          continue;
        }

        // Unregister the element so we can re-register with the current state.
        this.engine.unregisterElement(element);
      }

      const rules = parseRules(element);
      if (rules.length === 0) {
        continue;
      }

      const form = getElementForm(element);
      if (!form) {
        continue;
      }

      const state: ElementState = {
        rules: rules,
        triggerEvents: element.getAttribute('data-val-event') ?? 'change',
        listenerController: new AbortController(),
        fingerprint: fingerprint,
        hasBeenInvalid: false,
      };

      this.engine.registerElement(element, form, state);
      this.eventManager.attachInputListeners(element);

      // Suppress native browser validation UI, we handle it ourselves
      if (!form.hasAttribute('novalidate')) {
        form.setAttribute('novalidate', '');
      }
    }
  }

  private reconcile(root: ParentNode): void {
    for (const [form, formState] of this.engine.getTrackedForms()) {
      // Only reconcile forms within the scan root
      if (!root.contains(form)) {
        continue;
      }

      // If a form itself is removed from DOM, unregister all its elements.
      if (!form.isConnected) {
        const elementsToUnregister: ValidatableElement[] = [];
        for (const element of formState.trackedElements) {
          elementsToUnregister.push(element);
        }
        for (const element of elementsToUnregister) {
          this.engine.unregisterElement(element);
        }
        continue;
      }

      // If an element is removed from DOM or becomes not validatable, unregister it.
      const elementsToUnregister: ValidatableElement[] = [];
      for (const element of formState.trackedElements) {
        if (!element.isConnected ||
          shouldSkipElement(element) ||
          element.getAttribute('data-val') !== 'true') {
          elementsToUnregister.push(element);
        }
      }
      for (const element of elementsToUnregister) {
        this.engine.unregisterElement(element);
      }
    }
  }
}

const ruleAttributePrefix = 'data-val-';
const ruleAttributePrefixLength = ruleAttributePrefix.length;

export function parseRules(element: ValidatableElement): ValidationRule[] {
  const ruleMap: Record<string, ValidationRule> = {};

  for (const attr of element.attributes) {
    if (!attr.name.startsWith(ruleAttributePrefix)) {
      continue;
    }

    const ruleAndParamName = attr.name.substring(ruleAttributePrefixLength);
    const dashIndex = ruleAndParamName.indexOf('-');
    const ruleName = dashIndex === -1 ? ruleAndParamName : ruleAndParamName.substring(0, dashIndex);
    const paramName = dashIndex === -1 ? undefined : ruleAndParamName.substring(dashIndex + 1);

    if (!ruleName) {
      continue;
    }

    const rule = ruleMap[ruleName] ?? { ruleName, errorMessage: '', params: {} };

    if (!paramName) {
      // Attribute shape: data-val-range="Value must be between 10 and 50."
      rule.errorMessage = attr.value;
    } else {
      // Attribute shape: data-val-range-min="10"
      rule.params[paramName] = attr.value;
    }

    ruleMap[ruleName] = rule;
  }

  return Object.values(ruleMap);
}

function computeElementFingerprint(element: ValidatableElement): string {
  const parts: string[] = [];

  const name = element.getAttribute('name');
  if (name) {
    parts.push(`name=${name}`);
  }

  for (let i = 0; i < element.attributes.length; i++) {
    const attr = element.attributes[i];
    if (attr.name.startsWith('data-val')) {
      parts.push(`${attr.name}=${attr.value}`);
    }
  }

  parts.sort();
  return parts.join('|');
}
