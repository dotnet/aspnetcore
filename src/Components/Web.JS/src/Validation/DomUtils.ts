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

  // Use getComputedStyle for robust visibility detection covering CSS classes, stylesheets,
  // and inherited styles. Combined with offsetParent to detect inherited display:none.
  const style = getComputedStyle(element);
  if (element.offsetParent === null && style.position !== 'fixed' && style.position !== 'sticky') {
    return true;
  }
  if (style.visibility === 'hidden') {
    return true;
  }

  return false;
}
