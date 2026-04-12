// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { findMessageElements } from './DomUtils';
import { ValidatableElement } from './ValidationTypes';

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

export class ErrorDisplay {
  private cssClasses: CssClassNames;

  constructor(cssClasses?: CssClassNames) {
    this.cssClasses = cssClasses ?? defaultCssClassNames;
  }

  showFieldError(input: ValidatableElement, errorMessage: string): void {
    input.classList.add(this.cssClasses.inputError);
    input.classList.remove(this.cssClasses.inputValid);

    const messageElements = findMessageElements(input);
    this.updateMessageElements(messageElements, errorMessage);

    // Update ARIA attributes.
    input.setAttribute('aria-invalid', 'true');
    const firstMessageElement = messageElements[0];
    if (firstMessageElement) {
      if (!firstMessageElement.id) {
        firstMessageElement.id = `val-msg-${CSS.escape(input.getAttribute('name') || input.id)}`;
      }
      input.setAttribute('aria-describedby', firstMessageElement.id);
    }
  }

  clearFieldError(input: ValidatableElement): void {
    input.classList.remove(this.cssClasses.inputError);
    input.classList.add(this.cssClasses.inputValid);

    const messageElements = findMessageElements(input);
    this.updateMessageElements(messageElements, '');

    // Update ARIA attributes.
    input.removeAttribute('aria-invalid');
    input.removeAttribute('aria-describedby');
  }

  private updateMessageElements(messageElements: HTMLElement[], errorMessage: string): void {
    const classToAdd = errorMessage ? this.cssClasses.messageError : this.cssClasses.messageValid;
    const classToRemove = errorMessage ? this.cssClasses.messageValid : this.cssClasses.messageError;

    for (const messageElement of messageElements) {
      messageElement.classList.add(classToAdd);
      messageElement.classList.remove(classToRemove);

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
      summaryElement.classList.remove(this.cssClasses.summaryError);
      summaryElement.classList.add(this.cssClasses.summaryValid);
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

      summaryElement.classList.remove(this.cssClasses.summaryValid);
      summaryElement.classList.add(this.cssClasses.summaryError);
    }
  }
}
