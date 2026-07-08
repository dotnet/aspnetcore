// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { EventManager } from './EventManager';
import { getElementForm, shouldSkipElement } from './DomUtils';
import { ElementState, validatableElementSelector, ValidationEngine, ValidationRule } from './ValidationEngine';
import { ValidatableElement } from './ValidationTypes';

/**
 * Discovers validatable elements in the DOM and registers them with the engine.
 * Handles both initial page scan and re-scans after DOM mutations (e.g., Blazor
 * enhanced navigation). Uses fingerprinting to detect attribute changes without
 * unnecessarily re-registering unchanged elements.
 */
export class DomScanner {
  constructor(
    private engine: ValidationEngine,
    private eventManager: EventManager
  ) { }

  /** Scans the given root (or entire document) for validatable elements. */
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
    const candidates = Array.from(root.querySelectorAll<ValidatableElement>(validatableElementSelector));

    for (const element of candidates) {
      if (shouldSkipElement(element)) {
        continue;
      }

      // Only track one radio button per name group — the validator checks all siblings
      if (element instanceof HTMLInputElement && element.type === 'radio') {
        const form = getElementForm(element);
        if (form && this.isRadioGroupAlreadyTracked(form, element.name)) {
          continue;
        }
      }

      const previousState = this.engine.getElementState(element);
      const fingerprint = computeElementFingerprint(element);

      if (previousState) {
        // Element is already tracked.
        // Check if its attributes have changed.
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
        form: form,
        triggerEvents: element.getAttribute('data-valevent')?.trim() || 'default',
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
      if (!root.contains(form)) {
        continue;
      }

      if (!form.isConnected) {
        this.unregisterElements(formState.trackedElements);
        continue;
      }

      this.unregisterElements(formState.trackedElements, element =>
        !element.isConnected ||
        shouldSkipElement(element) ||
        element.getAttribute('data-val') !== 'true');
    }
  }

  private unregisterElements(
    elements: Set<ValidatableElement>,
    predicate?: (el: ValidatableElement) => boolean,
  ): void {
    const toRemove = predicate ? [...elements].filter(predicate) : [...elements];
    for (const element of toRemove) {
      this.engine.unregisterElement(element);
    }
  }

  private isRadioGroupAlreadyTracked(form: HTMLFormElement, groupName: string): boolean {
    const formState = this.engine.getFormState(form);
    if (!formState) {
      return false;
    }

    for (const tracked of formState.trackedElements) {
      if (tracked instanceof HTMLInputElement
        && tracked.type === 'radio'
        && tracked.name === groupName) {
        return true;
      }
    }

    return false;
  }
}

const ruleAttributePrefix = 'data-val-';
const ruleAttributePrefixLength = ruleAttributePrefix.length;

/**
 * Parses data-val-* attributes on an element into structured validation rules.
 * e.g., data-val-range="Error." + data-val-range-min="10" + data-val-range-max="50"
 * becomes { ruleName: 'range', errorMessage: 'Error.', params: { min: '10', max: '50' } }.
 */
export function parseRules(element: ValidatableElement): ValidationRule[] {
  const ruleMap: Record<string, ValidationRule> = {};

  for (let i = 0; i < element.attributes.length; i++) {
    const attr = element.attributes[i];
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
