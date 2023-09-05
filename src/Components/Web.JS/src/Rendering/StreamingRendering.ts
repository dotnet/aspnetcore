// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { SsrStartOptions } from '../Platform/SsrStartOptions';
import { NavigationEnhancementCallbacks, performEnhancedPageLoad, replaceDocumentWithPlainText } from '../Services/NavigationEnhancement';
import { isWithinBaseUriSpace } from '../Services/NavigationUtils';
import { synchronizeDomContent } from './DomMerging/DomSync';

let enableDomPreservation = true;
let navigationEnhancementCallbacks: NavigationEnhancementCallbacks;

export function attachStreamingRenderingListener(options: SsrStartOptions | undefined, callbacks: NavigationEnhancementCallbacks) {
  navigationEnhancementCallbacks = callbacks;

  if (options?.disableDomPreservation) {
    enableDomPreservation = false;
  }

  // By the time <blazor-ssr-end> is in the DOM, we know all the preceding content within the same <blazor-ssr> is also there,
  // so it's time to process it. We can't simply listen for <blazor-ssr>, because connectedCallback may fire before its content
  // is present, and even listening for a later slotchange event doesn't work because the presence of <script> elements in the
  // content can cause slotchange to fire before the rest of the content is added.
  customElements.define('blazor-ssr-end', BlazorStreamingUpdate);
}

class BlazorStreamingUpdate extends HTMLElement {
  connectedCallback() {
    const blazorSsrElement = this.parentNode!;

    // Synchronously remove this from the DOM to minimize our chance of affecting anything else
    blazorSsrElement.parentNode?.removeChild(blazorSsrElement);

    // When this element receives content, if it's <template blazor-component-id="...">...</template>,
    // insert the template content into the DOM
    blazorSsrElement.childNodes.forEach(node => {
      if (node instanceof HTMLTemplateElement) {
        const componentId = node.getAttribute('blazor-component-id');
        if (componentId) {
          insertStreamingContentIntoDocument(componentId, node.content);
        } else {
          switch (node.getAttribute('type')) {
            case 'redirection':
              // We use 'replace' here because it's closest to the non-progressively-enhanced behavior, and will make the most sense
              // if the async delay was very short, as the user would not perceive having been on the intermediate page.
              const destinationUrl = node.content.textContent!;
              if (isWithinBaseUriSpace(destinationUrl)) {
                history.replaceState(null, '', destinationUrl);
                performEnhancedPageLoad(destinationUrl);
              } else {
                location.replace(destinationUrl);
              }
              break;
            case 'error':
              // This is kind of brutal but matches what happens without progressive enhancement
              replaceDocumentWithPlainText(node.content.textContent || 'Error');
              break;
          }
        }
      }
    });
  }
}

function insertStreamingContentIntoDocument(componentIdAsString: string, docFrag: DocumentFragment): void {
  const markers = findStreamingMarkers(componentIdAsString);
  if (markers) {
    const { startMarker, endMarker } = markers;
    if (enableDomPreservation) {
      synchronizeDomContent({ startExclusive: startMarker, endExclusive: endMarker }, docFrag);
    } else {
      // In this mode we completely delete the old content before inserting the new content
      const destinationRoot = endMarker.parentNode!;
      const existingContent = new Range();
      existingContent.setStart(startMarker, startMarker.textContent!.length);
      existingContent.setEnd(endMarker, 0);
      existingContent.deleteContents();

      while (docFrag.childNodes[0]) {
        destinationRoot.insertBefore(docFrag.childNodes[0], endMarker);
      }
    }

    navigationEnhancementCallbacks.documentUpdated();
  }
}

function findStreamingMarkers(componentIdAsString: string): { startMarker: Comment, endMarker: Comment } | null {
  // Find start marker
  const expectedStartText = `bl:${componentIdAsString}`;
  const iterator = document.createNodeIterator(document, NodeFilter.SHOW_COMMENT);
  let startMarker: Comment | null = null;
  while (startMarker = iterator.nextNode() as Comment | null) {
    if (startMarker.textContent === expectedStartText) {
      break;
    }
  }

  if (!startMarker) {
    return null;
  }

  // Find end marker
  const expectedEndText = `/bl:${componentIdAsString}`;
  let endMarker: Comment | null = null;
  while (endMarker = iterator.nextNode() as Comment | null) {
    if (endMarker.textContent === expectedEndText) {
      break;
    }
  }

  return endMarker ? { startMarker, endMarker } : null;
}
