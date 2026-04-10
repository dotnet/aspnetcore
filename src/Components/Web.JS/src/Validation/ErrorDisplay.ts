// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { getElementForm } from './Utils';
import { ValidatableElement } from './Validator';

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

    // Update message element classes and text content.
    const messageElements = findMessageElements(input);
    for (const messageElement of messageElements) {
      messageElement.classList.add(this.cssClasses.messageError);
      messageElement.classList.remove(this.cssClasses.messageValid);

      // Replace the text context, unless data-valmsg-replace is set to false.
      if (messageElement.getAttribute('data-valmsg-replace') !== 'false') {
        messageElement.textContent = errorMessage;
      }
    }

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

    // Update message element classes and text content.
    const messageElements = findMessageElements(input);
    for (const messageElement of messageElements) {
      messageElement.classList.remove(this.cssClasses.messageError);
      messageElement.classList.add(this.cssClasses.messageValid);

      // Clear the text content, unless data-valmsg-replace is set to false.
      if (messageElement.getAttribute('data-valmsg-replace') !== 'false') {
        messageElement.textContent = '';
      }
    }

    // Update ARIA attributes.
    input.removeAttribute('aria-invalid');
    input.removeAttribute('aria-describedby');
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

function findMessageElements(element: ValidatableElement): HTMLElement[] {
  const name = element.getAttribute('name');
  if (!name) {
    return [];
  }

  const form = getElementForm(element);
  if (!form) {
    return [];
  }

  // TODO: Support message elements outside the form?
  const messageElements = form.querySelectorAll<HTMLElement>(`[data-valmsg-for="${CSS.escape(name)}"]`);
  return Array.from(messageElements);
}
