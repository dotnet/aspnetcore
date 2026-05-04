// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { findMessageElements } from './DomUtils';
import { ValidatableElement } from './ValidationTypes';

/**
 * CSS class names applied to inputs, message elements, and the validation summary.
 * Override via ValidationOptions.cssClasses to integrate with CSS frameworks
 * (e.g., Bootstrap's 'is-invalid'/'is-valid', or Tailwind utility classes).
 */
export interface CssClassNames {
  inputError: string;
  inputValid: string;
  messageError: string;
  messageValid: string;
  summaryError: string;
  summaryValid: string;
}

export const defaultCssClassNames: CssClassNames = {
  inputError: 'input-validation-error',
  inputValid: 'input-validation-valid',
  messageError: 'field-validation-error',
  messageValid: 'field-validation-valid',
  summaryError: 'validation-summary-errors',
  summaryValid: 'validation-summary-valid',
};

/**
 * Manages visual feedback for validation: CSS classes on inputs, message element
 * content, ARIA attributes for accessibility, and the validation summary.
 */
export class ErrorDisplay {
  private cssClasses: CssClassNames;

  constructor(cssClasses?: Partial<CssClassNames>) {
    this.cssClasses = { ...defaultCssClassNames, ...cssClasses };
  }

  showFieldError(input: ValidatableElement, errorMessage: string): void {
    addClasses(input, this.cssClasses.inputError);
    removeClasses(input, this.cssClasses.inputValid);

    const messageElements = findMessageElements(input);
    this.updateMessageElements(messageElements, errorMessage);

    // Update ARIA attributes.
    input.setAttribute('aria-invalid', 'true');
    const firstMessageElement = messageElements[0];
    if (firstMessageElement) {
      if (!firstMessageElement.id) {
        firstMessageElement.id = generateMessageId(input);
      }
      // Append our message ID to aria-describedby (preserving existing tokens like help text IDs)
      addAriaToken(input, 'aria-describedby', firstMessageElement.id);
    }
  }

  clearFieldError(input: ValidatableElement): void {
    removeClasses(input, this.cssClasses.inputError);
    addClasses(input, this.cssClasses.inputValid);

    const messageElements = findMessageElements(input);
    this.updateMessageElements(messageElements, '');

    // Update ARIA attributes.
    input.removeAttribute('aria-invalid');
    // Remove only our message ID from aria-describedby when we know it,
    // preserving any other developer-provided tokens.
    const msgId = messageElements[0]?.id;
    if (msgId) {
      removeAriaToken(input, 'aria-describedby', msgId);
    }
  }

  clearFieldToPristine(input: ValidatableElement): void {
    removeClasses(input, this.cssClasses.inputError);
    removeClasses(input, this.cssClasses.inputValid);

    const messageElements = findMessageElements(input);
    for (const messageElement of messageElements) {
      removeClasses(messageElement, this.cssClasses.messageError);
      removeClasses(messageElement, this.cssClasses.messageValid);
      if (messageElement.getAttribute('data-valmsg-replace') !== 'false') {
        messageElement.textContent = '';
      }
    }

    input.removeAttribute('aria-invalid');
    const msgId = messageElements[0]?.id;
    if (msgId) {
      removeAriaToken(input, 'aria-describedby', msgId);
    }
  }

  private updateMessageElements(messageElements: HTMLElement[], errorMessage: string): void {
    const classToAdd = errorMessage ? this.cssClasses.messageError : this.cssClasses.messageValid;
    const classToRemove = errorMessage ? this.cssClasses.messageValid : this.cssClasses.messageError;

    for (const messageElement of messageElements) {
      addClasses(messageElement, classToAdd);
      removeClasses(messageElement, classToRemove);

      if (messageElement.getAttribute('data-valmsg-replace') !== 'false') {
        messageElement.textContent = errorMessage;
      }

      this.removeServerRenderedSiblings(messageElement);
    }
  }

  private removeServerRenderedSiblings(messageElement: HTMLElement): void {
    // Remove server-rendered sibling validation messages (Blazor SSR renders
    // one <div class="validation-message"> per error, only the first has data-valmsg-for).
    let sibling = messageElement.nextElementSibling;
    while (sibling) {
      const next = sibling.nextElementSibling;
      if (sibling.classList.contains('validation-message') && !sibling.hasAttribute('data-valmsg-for')) {
        sibling.remove();
      } else {
        break; // Stop at the first unrelated element
      }
      sibling = next;
    }
  }

  updateSummary(form: HTMLFormElement, errors?: Map<string, string>): void {
    // TODO: Support multiple summary elements?
    // TODO: Support summary elements outside the form?
    const summaryElement = form.querySelector<HTMLElement>('[data-valmsg-summary]');
    if (!summaryElement) {
      return;
    }

    let ul = summaryElement.querySelector('ul');

    // Clear existing summary messages.
    while (ul?.firstChild) {
      ul.removeChild(ul.firstChild);
    }

    if (!errors || errors.size === 0) {
      // Set summary to valid state if there are no errors.
      removeClasses(summaryElement, this.cssClasses.summaryError);
      addClasses(summaryElement, this.cssClasses.summaryValid);
    } else {
      if (!ul) {
        ul = document.createElement('ul');
        summaryElement.appendChild(ul);
      }

      // Add non-duplicate error messages to the summary.
      const uniqueErrorMessages = new Set<string>(errors.values());
      for (const errorMessage of uniqueErrorMessages) {
        const li = document.createElement('li');
        li.textContent = errorMessage;
        ul.appendChild(li);
      }

      removeClasses(summaryElement, this.cssClasses.summaryValid);
      addClasses(summaryElement, this.cssClasses.summaryError);
    }
  }
}

function addClasses(element: Element, classes: string): void {
  for (const cls of classes.split(' ')) {
    if (cls) {
      element.classList.add(cls);
    }
  }
}

function removeClasses(element: Element, classes: string): void {
  for (const cls of classes.split(' ')) {
    if (cls) {
      element.classList.remove(cls);
    }
  }
}

let messageIdCounter = 0;

/** Generates a unique, safe ID for a validation message element. */
function generateMessageId(input: ValidatableElement): string {
  const name = (input.getAttribute('name') || input.id || 'field').replace(/[^a-zA-Z0-9_-]/g, '-');
  return `val-msg-${name}-${++messageIdCounter}`;
}

/** Appends a token to a space-separated attribute value (e.g., aria-describedby), avoiding duplicates. */
function addAriaToken(element: Element, attribute: string, token: string): void {
  const existing = element.getAttribute(attribute) || '';
  const tokens = existing.split(/\s+/).filter(t => t && t !== token);
  tokens.push(token);
  element.setAttribute(attribute, tokens.join(' '));
}

/** Removes a token from a space-separated attribute value. Removes the attribute entirely if no tokens remain. */
function removeAriaToken(element: Element, attribute: string, token: string): void {
  const existing = element.getAttribute(attribute) || '';
  const tokens = existing.split(/\s+/).filter(t => t && t !== token);
  if (tokens.length > 0) {
    element.setAttribute(attribute, tokens.join(' '));
  } else {
    element.removeAttribute(attribute);
  }
}
