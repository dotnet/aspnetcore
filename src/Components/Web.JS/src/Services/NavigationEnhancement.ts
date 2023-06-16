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

  // TODO: Deal with streaming SSR. The returned result may be left hanging for a while.
  currentEnhancedNavigationAbortController = new AbortController();
  const abortSignal = currentEnhancedNavigationAbortController.signal;
  const responsePromise = fetch(internalDestinationHref, { signal: abortSignal });
  await getResponsePartsWithFraming(responsePromise, abortSignal,
    (response, responseText) => {
      if (response.headers.get('content-type')?.startsWith('text/html')) {
        const parsedHtml = new DOMParser().parseFromString(responseText, 'text/html');
        synchronizeDomContent(document, parsedHtml);
      } else {
        // The response isn't HTML so don't try to parse it that way. If they gave any response text use that,
        // and only generate our own error message if it's definitely an error with no text.
        const isSuccessStatus = response.status >= 200 && response.status < 300;
        document.documentElement.innerHTML = (responseText || isSuccessStatus)
          ? responseText
          : `Error: ${response.status} ${responseText}`;
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

async function getResponsePartsWithFraming(responsePromise: Promise<Response>, abortSignal: AbortSignal, onAllNonStreamingContentReceived: (response: Response, nonStreamingResponseText: string) => void) {
  let response: Response;
  let responseText: string;
  try {
    response = await responsePromise;
    responseText = await response.text();
  } catch (ex) {
    if ((ex as AbortError)?.name === 'AbortError' && abortSignal.aborted) {
      // Not an error; we're just finished
      return;
    } else {
      throw ex;
    }
  }

  onAllNonStreamingContentReceived(response, responseText);
}
