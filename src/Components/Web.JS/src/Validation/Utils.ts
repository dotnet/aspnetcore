// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export function getElementForm(element: HTMLElement): HTMLFormElement | null {
  return element.closest('form');
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
