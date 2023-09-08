// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export function discoverCircuitComponentValidation(node: Node): string | null {
  const iterator = document.createNodeIterator(node, NodeFilter.SHOW_COMMENT);
  const expectedStartText = 'bl-validate:';
  while (iterator.nextNode()) {
    const currentNode = iterator.referenceNode;
    if (currentNode.textContent?.startsWith(expectedStartText)) {
      currentNode.parentNode!.removeChild(currentNode);
      return currentNode.textContent.substring(expectedStartText.length);
    }
  }

  return null;
}
