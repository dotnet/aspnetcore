// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ValidatableElement, CssClassConfig } from './Types';

/**
 * Find validation message elements for a given input.
 * Matches by input name attribute against data-valmsg-for attribute value.
 */
export function findMessageElements(input: ValidatableElement, form: HTMLFormElement): Element[] {
  const name = input.getAttribute('name');
  if (!name) {
    return [];
  }

  return Array.from(form.querySelectorAll(`[data-valmsg-for="${CSS.escape(name)}"]`));
}

export class ErrorDisplay {
  constructor(private css: CssClassConfig) {}

  showFieldError(input: ValidatableElement, messageElements: Element[], message: string): void {
    input.classList.add(this.css.inputError);
    input.classList.remove(this.css.inputValid);

    for (const el of messageElements) {
      el.textContent = message;
      el.classList.add(this.css.messageError);
      el.classList.remove(this.css.messageValid);
    }

    // ARIA extension point:
    // input.setAttribute('aria-invalid', 'true');
    // if (messageElements.length > 0 && messageElements[0].id) {
    //     input.setAttribute('aria-describedby', messageElements[0].id);
    // }
  }

  clearFieldError(input: ValidatableElement, messageElements: Element[]): void {
    input.classList.remove(this.css.inputError);
    input.classList.add(this.css.inputValid);

    for (const el of messageElements) {
      el.textContent = '';
      el.classList.remove(this.css.messageError);
      el.classList.add(this.css.messageValid);
    }

    // ARIA extension point:
    // input.removeAttribute('aria-invalid');
  }

  updateSummary(form: HTMLFormElement, errors: Map<string, string>): void {
    const summary = form.querySelector('[data-valmsg-summary="true"]');
    if (!summary) {
      return;
    }

    let ul = summary.querySelector('ul');
    if (!ul) {
      ul = document.createElement('ul');
      summary.appendChild(ul);
    }

    // Clear existing content safely (no innerHTML)
    while (ul.firstChild) {
      ul.removeChild(ul.firstChild);
    }

    if (errors.size > 0) {
      const uniqueMessages = new Set(errors.values());
      for (const message of uniqueMessages) {
        const li = document.createElement('li');
        li.textContent = message;
        ul.appendChild(li);
      }
      summary.classList.add(this.css.summaryError);
      summary.classList.remove(this.css.summaryValid);
    } else {
      summary.classList.remove(this.css.summaryError);
      summary.classList.add(this.css.summaryValid);
    }
  }
}
