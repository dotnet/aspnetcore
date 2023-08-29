// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { synchronizeDomContent } from '../Rendering/DomMerging/DomSync';
import { attachProgrammaticEnhancedNavigationHandler, handleClickForNavigationInterception, hasInteractiveRouter } from './NavigationUtils';

/*
In effect, we have two separate client-side navigation mechanisms:

[1] Interactive client-side routing. This is the traditional Blazor Server/WebAssembly navigation mechanism for SPAs.
    It is enabled whenever you have a <Router/> rendering as interactive. This intercepts all navigation within the
    base href URI space and tries to display a corresponding [Route] component or the NotFoundContent.
[2] Progressively-enhanced navigation. This is a new mechanism in .NET 8 and is only relevant for multi-page apps.
    It is enabled when you load blazor.web.js and don't have an interactive <Router/>. This intercepts navigation within
    the base href URI space and tries to load it via a `fetch` request and DOM syncing.

Only one of these can be enabled at a time, otherwise both would be trying to intercept click/popstate and act on them.
In fact even if we made the event handlers able to coexist, the two together would still not produce useful behaviors because
[1] implies you have a <Router/>, and that will try to supply UI content for all pages or NotFoundContent if the URL doesn't
match a [Route] component, so there would be nothing left for [2] to handle.

So, whenever [1] is enabled, we automatically disable [2].

However, a single site can use both [1] and [2] on different URLs.
 - You can navigate from [1] to [2] by setting up the interactive <Router/> not to know about any [Route] components in your MPA,
   and so it will fall back on a full-page load to get from the SPA URLs to the MPA URLs.
 - You can navigate from [2] to [1] in that it just works by default. A <Router/> can be added dynamically and will then take
   over and disable [2].

Note that we don't reference NavigationManager.ts from NavigationEnhancement.ts or vice-versa. This is to ensure we could produce
different bundles that only contain minimal content.
*/

let currentEnhancedNavigationAbortController: AbortController | null;
let navigationEnhancementCallbacks: NavigationEnhancementCallbacks;
let performingEnhancedPageLoad: boolean;

export interface NavigationEnhancementCallbacks {
  documentUpdated: () => void;
}

export function isPerformingEnhancedPageLoad() {
  return performingEnhancedPageLoad;
}

export function attachProgressivelyEnhancedNavigationListener(callbacks: NavigationEnhancementCallbacks) {
  navigationEnhancementCallbacks = callbacks;
  document.addEventListener('click', onDocumentClick);
  document.addEventListener('submit', onDocumentSubmit);
  window.addEventListener('popstate', onPopState);

  attachProgrammaticEnhancedNavigationHandler(performProgrammaticEnhancedNavigation);
}

export function detachProgressivelyEnhancedNavigationListener() {
  document.removeEventListener('click', onDocumentClick);
  document.removeEventListener('submit', onDocumentSubmit);
  window.removeEventListener('popstate', onPopState);
}

function performProgrammaticEnhancedNavigation(absoluteInternalHref: string, replace: boolean) {
  if (replace) {
    history.replaceState(null, /* ignored title */ '', absoluteInternalHref);
  } else {
    history.pushState(null, /* ignored title */ '', absoluteInternalHref);
  }

  performEnhancedPageLoad(absoluteInternalHref);
}

function onDocumentClick(event: MouseEvent) {
  if (hasInteractiveRouter()) {
    return;
  }

  handleClickForNavigationInterception(event, absoluteInternalHref => {
    history.pushState(null, /* ignored title */ '', absoluteInternalHref);
    performEnhancedPageLoad(absoluteInternalHref);
  });
}

function onPopState(state: PopStateEvent) {
  if (hasInteractiveRouter()) {
    return;
  }

  performEnhancedPageLoad(location.href);
}

function onDocumentSubmit(event: SubmitEvent) {
  if (hasInteractiveRouter() || event.defaultPrevented) {
    return;
  }

  // We need to be careful not to interfere with existing interactive forms. As it happens, EventDelegator always
  // uses a capturing event handler for 'submit', so it will necessarily run before this handler, and so we won't
  // even get here if there's an interactive submit (because it will have set defaultPrevented which we check above).
  // However if we ever change that, we would need to change this code to integrate properly with EventDelegator
  // to make sure this handler only ever runs after interactive handlers.
  const formElem = event.target;
  if (formElem instanceof HTMLFormElement) {
    event.preventDefault();

    const url = new URL(formElem.action);
    const fetchOptions: RequestInit = { method: formElem.method };
    const formData = new FormData(formElem);

    // Replicate the normal behavior of appending the submitter name/value to the form data
    const submitter = event.submitter as HTMLButtonElement;
    if (submitter && submitter.name) {
      formData.append(submitter.name, submitter.value);
    }

    if (fetchOptions.method === 'get') { // method is always returned as lowercase
      url.search = new URLSearchParams(formData as any).toString();
    } else {
      fetchOptions.body = formData;
    }

    performEnhancedPageLoad(url.toString(), fetchOptions);
  }
}

export async function performEnhancedPageLoad(internalDestinationHref: string, fetchOptions?: RequestInit) {
  performingEnhancedPageLoad = true;

  // First, stop any preceding enhanced page load
  currentEnhancedNavigationAbortController?.abort();

  // Now request the new page via fetch, and a special header that tells the server we want it to inject
  // framing boundaries to distinguish the initial document and each subsequent streaming SSR update.
  currentEnhancedNavigationAbortController = new AbortController();
  const abortSignal = currentEnhancedNavigationAbortController.signal;
  const responsePromise = fetch(internalDestinationHref, Object.assign({
    signal: abortSignal,
    headers: { 'blazor-enhanced-nav': 'on' },
  }, fetchOptions));
  await getResponsePartsWithFraming(responsePromise, abortSignal,
    (response, initialContent) => {
      if (response.redirected) {
        // We already followed a redirection, so update the current URL to match the redirected destination, just like for normal navigation redirections
        history.replaceState(null, '', response.url);
        internalDestinationHref = response.url;
      }

      const responseContentType = response.headers.get('content-type');
      if (responseContentType?.startsWith('text/html') && initialContent) {
        // For HTML responses, regardless of the status code, display it
        const parsedHtml = new DOMParser().parseFromString(initialContent, 'text/html');
        synchronizeDomContent(document, parsedHtml);
        navigationEnhancementCallbacks.documentUpdated();
      } else if (responseContentType?.startsWith('text/') && initialContent) {
        // For any other text-based content, we'll just display it, because that's what
        // would happen if this was a non-enhanced request.
        replaceDocumentWithPlainText(initialContent);
      } else if ((response.status < 200 || response.status >= 300) && !initialContent) {
        // For any non-success response that has no content at all, make up our own error UI
        replaceDocumentWithPlainText(`Error: ${response.status} ${response.statusText}`);
      } else {
        // For any other response, it's not HTML and we don't know what to do. It might be plain text,
        // or an image, or something else.
        if (!fetchOptions?.method || fetchOptions.method === 'get') {
          // If it's a get request, we'll trust that it's idempotent and cheap enough to request
          // a second time, so we can fall back on a full reload.
          // The ? trick here is the same workaround as described in #10839, and without it, the user
          // would not be able to use the back button afterwards.
          history.replaceState(null, '', internalDestinationHref + '?');
          location.replace(internalDestinationHref);
        } else {
          // For non-get requests, we can't safely re-request, so just treat it as an error
          replaceDocumentWithPlainText(`Error: ${fetchOptions.method} request to ${internalDestinationHref} returned non-HTML content of type ${responseContentType || 'unspecified'}.`);
        }
      }
    },
    (streamingElementMarkup) => {
      const fragment = document.createRange().createContextualFragment(streamingElementMarkup);
      while (fragment.firstChild) {
        document.body.appendChild(fragment.firstChild);
      }
    });

  if (!abortSignal.aborted) {
    // The whole response including any streaming SSR is now finished, and it was not aborted (no other navigation
    // has since started). So finally, recreate the native "scroll to hash" behavior.
    const hashPosition = internalDestinationHref.indexOf('#');
    if (hashPosition >= 0) {
      const hash = internalDestinationHref.substring(hashPosition + 1);
      const targetElem = document.getElementById(hash);
      targetElem?.scrollIntoView();
    }

    performingEnhancedPageLoad = false;
    navigationEnhancementCallbacks.documentUpdated();
  }
}

async function getResponsePartsWithFraming(responsePromise: Promise<Response>, abortSignal: AbortSignal, onInitialDocument: (response: Response, initialDocumentText: string) => void, onStreamingElement: (streamingElementMarkup) => void) {
  let response: Response;

  try {
    response = await responsePromise;

    const externalRedirectionUrl = response.headers.get('blazor-enhanced-nav-redirect-location');
    if (externalRedirectionUrl) {
      location.replace(externalRedirectionUrl);
      return;
    }

    if (!response.body) { // Not sure how this can happen, but the TypeScript annotations suggest it can
      onInitialDocument(response, '');
      return;
    }

    const frameBoundary = response.headers.get('ssr-framing');
    if (!frameBoundary) {
      // Shouldn't happen, but perhaps some proxy stripped the headers. In that case we just won't respect streaming and will
      // wait for the whole response.
      const allResponseText = await response.text();
      onInitialDocument(response, allResponseText);
      return;
    }

    // This is going to be a framed response, so split it into chunks based on our framing boundaries
    let isFirstFramedChunk = true;
    await response.body
      .pipeThrough(new TextDecoderStream())
      .pipeThrough(splitStream(`<!--${frameBoundary}-->`))
      .pipeTo(new WritableStream({
        write(chunk) {
          // Inside here, we know the chunks correspond precisely to frames within our message framing mechanism.
          // The first one is always the initial document that we will merge into the existing DOM. All subsequent ones
          // are blocks of <blazor-ssr>...</blazor-ssr> markup whose insertion would trigger a streaming SSR DOM update.
          if (isFirstFramedChunk) {
            isFirstFramedChunk = false;
            onInitialDocument(response, chunk);
          } else {
            onStreamingElement(chunk);
          }
        }
      }));
  } catch (ex) {
    if ((ex as Error).name === 'AbortError' && abortSignal.aborted) {
      // Not an error. This happens if a different navigation started before this one completed.
      return;
    } else {
      throw ex;
    }
  }
}

export function replaceDocumentWithPlainText(text: string) {
  document.documentElement.textContent = text;
  const docStyle = document.documentElement.style;
  docStyle.fontFamily = 'consolas, monospace';
  docStyle.whiteSpace = 'pre-wrap';
  docStyle.padding = '1rem';
}

function splitStream(frameBoundaryMarker: string) {
  let buffer = '';

  return new TransformStream({
    transform(chunk, controller) {
      buffer += chunk;

      // Only call 'split' if we can see at least one marker, and only look for it within the new content (allowing for it to split over chunks)
      if (buffer.indexOf(frameBoundaryMarker, buffer.length - chunk.length - frameBoundaryMarker.length) >= 0) {
        const frames = buffer.split(frameBoundaryMarker);
        frames.slice(0, -1).forEach(part => controller.enqueue(part));
        buffer = frames[frames.length - 1];
      }
    },
    flush(controller) {
      controller.enqueue(buffer);
    }
  });
}
