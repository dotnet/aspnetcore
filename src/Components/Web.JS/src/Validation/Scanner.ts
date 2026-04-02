// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { validatableElementSelector, ValidationEngine } from './ValidationEngine';
import { ValidatableElement } from './Validator';

export type ValidationDirective = {
  ruleName: string;
  errorMessage: string;
  params: Record<string, string>;
}

export class Scanner {
  constructor(private engine: ValidationEngine) {}

  scan(root: ParentNode): void {
    // TODO: Filter disabled elements?
    const candidates = root.querySelectorAll<ValidatableElement>(validatableElementSelector);
    const validatableElements = Array.from(candidates).filter(e => !isHiddenElement(e));

    console.log(`Found ${validatableElements.length} validatable elements.`);

    for (const element of validatableElements) {
      // TODO: Check existing state

      const directives = parseDirectives(element);
      if (!directives) {
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

function parseDirectives(element: ValidatableElement): ValidationDirective[] {
  const attributes: Record<string, string> = {};

  const directives: ValidationDirective[] = [];

  return directives;
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
