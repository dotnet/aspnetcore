// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { getLogicalParent, LogicalElement } from './Rendering/LogicalElements';

export const PageTitle = {
  getAndRemoveExistingTitle,
};

function getAndRemoveExistingTitle(): string | null {
  // Other <title> elements may exist outside <head> (e.g., inside <svg> elements) but they aren't page titles
  const titleElements = document.head ? document.head.getElementsByTagName('title') : [];

  if (titleElements.length === 0) {
    return null;
  }

  let existingTitle: string | null = null;

  for (let index = titleElements.length - 1; index >= 0; index--) {
    const currentTitleElement = titleElements[index];
    const previousSibling = currentTitleElement.previousSibling;
    const isBlazorTitle = previousSibling instanceof Comment && getLogicalParent(previousSibling as unknown as LogicalElement) !== null;

    if (isBlazorTitle) {
      continue;
    }

    if (existingTitle === null) {
      existingTitle = currentTitleElement.textContent;
    }

    currentTitleElement.parentNode?.removeChild(currentTitleElement);
  }

  return existingTitle;
}
