// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { SsrStartOptions } from '../Platform/SsrStartOptions';
import { NavigationEnhancementCallbacks, hasNeverStartedAnyEnhancedPageLoad, performEnhancedPageLoad, replaceDocumentWithPlainText } from '../Services/NavigationEnhancement';
import { isWithinBaseUriSpace, toAbsoluteUri } from '../Services/NavigationUtils';
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
          // For enhanced nav page loads, we automatically cancel the response stream if another enhanced nav supersedes it. But there's
          // no way to cancel the original page load. So, to avoid continuing to process <blazor-ssr> blocks from the original page load
          // if an enhanced nav supersedes it, we must explicitly check whether this content is from the original page load, and if so,
          // ignore it if any enhanced nav has started yet. Fixes https://github.com/dotnet/aspnetcore/issues/50733
          const isFromEnhancedNav = node.getAttribute('enhanced-nav') === 'true';
          if (isFromEnhancedNav || hasNeverStartedAnyEnhancedPageLoad()) {
            insertStreamingContentIntoDocument(componentId, node.content);
          }
        } else {
          switch (node.getAttribute('type')) {
            case 'redirection':
              // We use 'replace' here because it's closest to the non-progressively-enhanced behavior, and will make the most sense
              // if the async delay was very short, as the user would not perceive having been on the intermediate page.
              const destinationUrl = toAbsoluteUri(node.content.textContent!);
              const isFormPost = node.getAttribute('from') === 'form-post';
              const isEnhancedNav = node.getAttribute('enhanced') === 'true';
              if (isEnhancedNav && isWithinBaseUriSpace(destinationUrl)) {
                // At this point the destinationUrl might be an opaque URL so we don't know whether it's internal/external or
                // whether it's even going to the same URL we're currently on. So we don't know how to update the history.
                // Defer that until the redirection is resolved by performEnhancedPageLoad.
                const treatAsRedirectionFromMethod = isFormPost ? 'post' : 'get';
                const fetchOptions = undefined;
                performEnhancedPageLoad(destinationUrl, /* interceptedLink */ false, fetchOptions, treatAsRedirectionFromMethod);
              } else {
                if (isFormPost) {
                  // The URL is not yet updated. Push a whole new entry so that 'back' goes back to the pre-redirection location.
                  // WARNING: The following check to avoid duplicating history entries won't work if the redirection is to an opaque URL.
                  // We could change the server-side logic to return URLs in plaintext if they match the current request URL already,
                  // but it's arguably easier to understand that history non-duplication only works for enhanced nav, which is also the
                  // case for non-streaming responses.
                  if (destinationUrl !== location.href) {
                    location.assign(destinationUrl);
                  }
                } else {
                  // The URL was already updated on the original link click. Replace so that 'back' goes to the pre-redirection location.
                  location.replace(destinationUrl);
                }
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
