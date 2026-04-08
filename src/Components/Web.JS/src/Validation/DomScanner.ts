// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { EventManager } from './EventManager';
import { isHiddenElement } from './Utils';
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
    // Hidden elements are skipped. They will be picked up if the become visible and the DOM is re-scanned.
    // TODO: Filter disabled elements?
    const candidates = root.querySelectorAll<ValidatableElement>(validatableElementSelector);
    const validatableElements = Array.from(candidates).filter(e => !isHiddenElement(e));

    console.log(`Found ${validatableElements.length} validatable elements.`);

    for (const element of validatableElements) {
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
      if (!rules) {
        continue;
      }

      const form = element.closest('form');
      if (!form) {
        continue;
      }

      const messageElements = findMessageElements(element, form);

      const state: ElementState = {
        rules: rules,
        triggerEvents: 'change', // TODO: Support different trigger events
        messageElements: messageElements,
        listeners: [],
        fingerprint: fingerprint,
      };

      this.engine.registerElement(element, state);
      this.eventManager.attachInputListeners(element);

      // Suppress native browser validation UI, we handle it ourselves
      if (!form.hasAttribute('novalidate')) {
        form.setAttribute('novalidate', '');
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

function findMessageElements(input: ValidatableElement, form: HTMLFormElement): HTMLElement[] {
  const name = input.getAttribute('name');
  if (!name) {
    return [];
  }

  // TODO: Support message elements outside the form.
  const messageElements = form.querySelectorAll<HTMLElement>(`[data-valmsg-for="${CSS.escape(name)}"]`);
  return Array.from(messageElements);
}

function computeElementFingerprint(element: ValidatableElement): string {
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
