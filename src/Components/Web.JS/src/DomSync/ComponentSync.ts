import { synchronizeDOMContent } from './DomSync';

export function synchronizeComponentContent(componentId: number, html: string) {
  const startMarker = tryFindPassiveComponentStartMarker(componentId);
  if (!startMarker) {
    // While streaming rendering is in flight, the user might have navigated to a different page via progressive
    // navigation. We'll still get the streaming update but it may no longer be applicable.
    return;
  }

  const endMarker = findPassiveComponentEndMarker(startMarker);
  synchronizeDOMContent({ parent: startMarker.parentNode!, subset: { startMarker, endMarker } }, html);
}

function tryFindPassiveComponentStartMarker(componentId: number): Comment | null {
  const expectedText = `c${componentId}`;
  const iterator = document.createNodeIterator(document, NodeFilter.SHOW_COMMENT);
  let node: Node | null;
  while (node = iterator.nextNode()) {
    if (node.textContent === expectedText) {
      return node as Comment;
    }
  }

  return null;
}

const passiveComponentStartMarkerPattern = /^c\d+$/;

function findPassiveComponentEndMarker(startMarker: Comment): Comment {
  let depth = 1;
  let currentNode: Node | null = startMarker;
  while (currentNode) {
    currentNode = currentNode.nextSibling;
    if (currentNode?.nodeType === 8) { // Comment
      if (currentNode.textContent === '/c') {
        depth--;
        if (depth === 0) {
          return currentNode as Comment;
        }
      } else if (passiveComponentStartMarkerPattern.test(currentNode.textContent!)) {
        depth++;
      }
    }
  }

  throw new Error(`Cannot find closing marker corresponding to ${startMarker.textContent}`);
}
