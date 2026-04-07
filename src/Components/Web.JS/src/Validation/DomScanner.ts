// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { validatableElementSelector, ValidationEngine } from './ValidationEngine';
import { ValidatableElement } from './Validator';

export type ValidationRule = {
  ruleName: string;
  errorMessage: string;
  params: Record<string, string>;
}

export class DomScanner {
  constructor(private engine: ValidationEngine) { }

  scan(root: ParentNode): void {
    // TODO: Filter disabled elements?
    const candidates = root.querySelectorAll<ValidatableElement>(validatableElementSelector);
    const validatableElements = Array.from(candidates).filter(e => !isHiddenElement(e));

    console.log(`Found ${validatableElements.length} validatable elements.`);

    for (const element of validatableElements) {
      // TODO: Check existing state

      const rules = parseRules(element);
      if (!rules) {
        continue;
      }

      const form = element.closest('form');
      if (!form) {
        continue;
      }

      const messageElements = findMessageElements(element, form);

      // TODO: Register element to ValidationEngine

      // TODO: Attach event listeners via EventManager

      // Suppress native browser validation UI — we handle it ourselves
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

function isHiddenElement(element: HTMLElement): boolean {
  // TODO: Add allowHiddenFields option?
  if (element.hidden) {
    return true;
  }

  if (element instanceof HTMLInputElement && element.type === 'hidden') {
    return true;
  }

  // TODO: More robust check? Consider `input.offsetWidth || input.offsetHeight || input.getClientRects().length`
  let current: HTMLElement | null = element;
  while (current) {
    if (current.style.display === 'none') {
      return true;
    }
    current = current.parentElement;
  }

  return false;
}
