// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/** Returns the closest ancestor <form> element, or null if the element is not in a form. */
export function getElementForm(element: HTMLElement): HTMLFormElement | null {
  return element.closest('form');
}

/**
 * Finds all message elements linked to the given input via data-valmsg-for attribute.
 * Message elements must be within the same form as the input.
 */
export function findMessageElements(element: HTMLElement): HTMLElement[] {
  const name = element.getAttribute('name');
  if (!name) {
    return [];
  }

  const form = getElementForm(element);
  if (!form) {
    return [];
  }

  return Array.from(form.querySelectorAll<HTMLElement>(`[data-valmsg-for="${CSS.escape(name)}"]`));
}

/**
 * Returns true if the element should be skipped during validation
 * (disabled, hidden, type="hidden", or not visible in the layout).
 */
export function shouldSkipElement(element: HTMLElement): boolean {
  if ((element as HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement).disabled) {
    return true;
  }

  if (element.hidden) {
    return true;
  }

  if (element instanceof HTMLInputElement && element.type === 'hidden') {
    return true;
  }

  // offsetParent is null for elements that are not rendered (display:none on self or ancestor,
  // or not connected to the document). This covers CSS-class-based hiding (e.g., Bootstrap's
  // .d-none) not just inline styles. Note: offsetParent is also null for position:fixed elements,
  // but those are visible — we check for that case explicitly.
  if (element.offsetParent === null && getComputedStyle(element).position !== 'fixed') {
    return true;
  }

  return false;
}
