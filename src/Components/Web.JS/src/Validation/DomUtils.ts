// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export function getElementForm(element: HTMLElement): HTMLFormElement | null {
  return element.closest('form');
}

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

  let current: HTMLElement | null = element;
  while (current) {
    if (current.style.display === 'none') {
      return true;
    }
    current = current.parentElement;
  }

  return false;
}
