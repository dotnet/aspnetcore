// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { findMessageElements, getFieldElements } from './DomUtils';
import { ValidatableElement } from './ValidationTypes';

interface CssClassNames {
  inputError: string;
  inputValid: string;
  inputModified: string;
  messageError: string;
  messageValid: string;
  summaryError: string;
  summaryValid: string;
}

const defaultCssClassNames: CssClassNames = {
  inputError: 'invalid',
  inputValid: 'valid',
  inputModified: 'modified',
  messageError: 'validation-message',
  messageValid: 'validation-message',
  summaryError: 'validation-summary-errors',
  summaryValid: 'validation-summary-valid',
};

/**
 * Manages visual feedback for validation: CSS classes on inputs, message element
 * content, ARIA attributes for accessibility, and the validation summary.
 */
export class ErrorDisplay {
  private cssClasses: CssClassNames;

  constructor() {
    this.cssClasses = defaultCssClassNames;
  }

  showFieldError(input: ValidatableElement, errorMessage: string): void {
    const messageElements = findMessageElements(input);
    this.updateMessageElements(messageElements, errorMessage);

    const firstMessageElement = messageElements[0];
    if (firstMessageElement && !firstMessageElement.id) {
      firstMessageElement.id = generateMessageId(input);
    }
    const messageId = firstMessageElement?.id;

    for (const target of getFieldElements(input)) {
      addClasses(target, this.cssClasses.inputError);
      removeClasses(target, this.cssClasses.inputValid);
      target.setAttribute('aria-invalid', 'true');
      // Append our message ID to aria-describedby (preserving existing tokens like help text IDs).
      if (messageId) {
        addAriaToken(target, 'aria-describedby', messageId);
      }
    }
  }

  clearFieldError(input: ValidatableElement): void {
    const messageElements = findMessageElements(input);
    this.updateMessageElements(messageElements, '');
    const messageId = messageElements[0]?.id;

    for (const target of getFieldElements(input)) {
      removeClasses(target, this.cssClasses.inputError);
      addClasses(target, this.cssClasses.inputValid);
      target.removeAttribute('aria-invalid');
      // Remove only our message ID, preserving any other developer-provided tokens.
      if (messageId) {
        removeAriaToken(target, 'aria-describedby', messageId);
      }
    }
  }

  // Marks the field as modified (its value has been changed by the user), mirroring the
  // 'modified' class Blazor's interactive validation adds. Combined with the valid/invalid class
  // this drives the template's '.valid.modified' styling. Idempotent.
  markFieldModified(input: ValidatableElement): void {
    for (const target of getFieldElements(input)) {
      addClasses(target, this.cssClasses.inputModified);
    }
  }

  clearFieldToPristine(input: ValidatableElement): void {
    // Pristine differs from the valid state only in that the input gets no valid class either, so
    // the field looks untouched. The message is reset to its empty/valid state, keeping its base
    // class (which for Blazor is the always-present 'validation-message').
    const messageElements = findMessageElements(input);
    this.updateMessageElements(messageElements, '');
    const messageId = messageElements[0]?.id;

    for (const target of getFieldElements(input)) {
      removeClasses(target, this.cssClasses.inputError);
      removeClasses(target, this.cssClasses.inputValid);
      removeClasses(target, this.cssClasses.inputModified);
      target.removeAttribute('aria-invalid');
      if (messageId) {
        removeAriaToken(target, 'aria-describedby', messageId);
      }
    }
  }

  private updateMessageElements(messageElements: HTMLElement[], errorMessage: string): void {
    const classToAdd = errorMessage ? this.cssClasses.messageError : this.cssClasses.messageValid;
    const classToRemove = errorMessage ? this.cssClasses.messageValid : this.cssClasses.messageError;

    for (const messageElement of messageElements) {
      // Remove then add so a class shared by both states (Blazor's single 'validation-message')
      // survives the transition.
      removeClasses(messageElement, classToRemove);
      addClasses(messageElement, classToAdd);

      if (messageElement.getAttribute('data-valmsg-replace') !== 'false') {
        messageElement.textContent = errorMessage;
        // Toggle the hidden attribute (not inline style) so revealing/hiding stays CSP-safe.
        messageElement.hidden = !errorMessage;
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
    // The summary element is the <ul> itself (ValidationSummary renders data-valmsg-summary on it).
    const summaryElement = form.querySelector<HTMLElement>('[data-valmsg-summary]');
    if (!summaryElement) {
      return;
    }

    // Clear existing summary messages.
    while (summaryElement.firstChild) {
      summaryElement.removeChild(summaryElement.firstChild);
    }

    if (!errors || errors.size === 0) {
      // Set summary to valid state if there are no errors and hide it so the empty list stays
      // out of the layout. Toggle the hidden attribute (not inline style) to stay CSP-safe.
      removeClasses(summaryElement, this.cssClasses.summaryError);
      addClasses(summaryElement, this.cssClasses.summaryValid);
      summaryElement.hidden = true;
    } else {
      // Add non-duplicate error messages to the summary. Mirror the server-rendered
      // ValidationSummary markup (<li class="validation-message">) so client- and
      // server-produced summaries are styled identically.
      const uniqueErrorMessages = new Set<string>(errors.values());
      for (const errorMessage of uniqueErrorMessages) {
        const li = document.createElement('li');
        li.className = 'validation-message';
        li.textContent = errorMessage;
        summaryElement.appendChild(li);
      }

      removeClasses(summaryElement, this.cssClasses.summaryValid);
      addClasses(summaryElement, this.cssClasses.summaryError);
      summaryElement.hidden = false;
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
