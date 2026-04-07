// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export function isHiddenElement(element: HTMLElement): boolean {
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
