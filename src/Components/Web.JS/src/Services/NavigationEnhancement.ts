import { AbortError } from '@microsoft/signalr';
import { synchronizeDomContent } from '../Rendering/DomMerging/DomSync';
import { handleClickForNavigationInterception } from './NavigationUtils';

/*
We want this to work independently from all the existing logic for NavigationManager and EventDelegator, since
this feature should be available in minimal .js bundles that don't include support for any interactive mode.
So, this file should not import those modules.

However, when NavigationManager/EventDelegator are being used, we do have to defer to them since SPA-style
interactive navigation needs to take precedence. The approach is:
- When blazor.web.js starts up, it will enable progressively-enhanced (PE) nav
- If an interactive <Router/> is added, it will call enableNavigationInterception and that will disable PE nav's event listener.
- When NavigationManager.ts sees a navigation occur, it goes through a complex flow (respecting @preventDefault,
  triggering OnLocationChanging, evaluating the URL against the .NET route table, etc). Normally this will conclude
  by picking a component page and rendering it without notifying the PE nav logic at all.
  - But if no component page is matched, it then explicitly calls back into the PE nav logic to fall back on the logic
  that would have happened if there was no interactive <Router/>. As such, PE nav isn't *really* disabled; it just only
  runs as a fallback if <Router/> nav doesn't match the URL.
  - If an interactive <Router/> is removed, we don't currently handle that. No notification goes back from .NET
  to JS about that. But if the scenario becomes important, we could add some disableNavigationInterception and resume PE nav.
*/

let currentEnhancedNavigationAbortController: AbortController | null;

export function attachProgressivelyEnhancedNavigationListener() {
  document.body.addEventListener('click', onBodyClicked);
  window.addEventListener('popstate', onPopState);
}

function onBodyClicked(event: MouseEvent) {
  handleClickForNavigationInterception(event, absoluteInternalHref => {
    history.pushState(null, /* ignored title */ '', absoluteInternalHref);
    performEnhancedPageLoad(absoluteInternalHref);
  });
}

function onPopState(state: PopStateEvent) {
  performEnhancedPageLoad(location.href);
}

async function performEnhancedPageLoad(internalDestinationHref: string) {
  // First, stop any preceding enhanced page load
  currentEnhancedNavigationAbortController?.abort();

  // Now request the new page via fetch, and a special header that tells the server we want it to inject
  // framing boundaries to distinguish the initial document and each subsequent streaming SSR update.
  currentEnhancedNavigationAbortController = new AbortController();
  const abortSignal = currentEnhancedNavigationAbortController.signal;
  const responsePromise = fetch(internalDestinationHref, {
    signal: abortSignal,
    headers: { 'blazor-enhanced-nav': 'on' },
  });
  await getResponsePartsWithFraming(responsePromise, abortSignal,
    (response, initialContent) => {
      if (response.headers.get('content-type')?.startsWith('text/html')) {
        const parsedHtml = new DOMParser().parseFromString(initialContent, 'text/html');
        synchronizeDomContent(document, parsedHtml);
      } else {
        // The response isn't HTML so don't try to parse it that way. If they gave any response text use that,
        // and only generate our own error message if it's definitely an error with no text.
        const isSuccessStatus = response.status >= 200 && response.status < 300;
        document.documentElement.innerHTML = (initialContent || isSuccessStatus)
          ? initialContent
          : `Error: ${response.status} ${initialContent}`;
      }
    },
    (streamingElementMarkup) => {
      const fragment = document.createRange().createContextualFragment(streamingElementMarkup);
      while (fragment.firstChild) {
        document.body.appendChild(fragment.firstChild);
      }
    });

  // Finally, if there's a hash in the URL, recreate the behavior of scrolling to the corresponding element by ID
  const hashPosition = internalDestinationHref.indexOf('#');
  if (hashPosition >= 0) {
    const hash = internalDestinationHref.substring(hashPosition + 1);
    const targetElem = document.getElementById(hash);
    targetElem?.scrollIntoView();
  }
}

async function getResponsePartsWithFraming(responsePromise: Promise<Response>, abortSignal: AbortSignal, onInitialDocument: (response: Response, initialDocumentText: string) => void, onStreamingElement: (streamingElementMarkup) => void) {
  let response: Response;

  try {
    response = await responsePromise;
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
    if ((ex as AbortError)?.name === 'AbortError' && abortSignal.aborted) {
      // Not an error. This happens if a different navigation started before this one completed.
      return;
    } else {
      throw ex;
    }
  }
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
      if (buffer) {
        controller.enqueue(buffer);
      }
    }
  });
}
