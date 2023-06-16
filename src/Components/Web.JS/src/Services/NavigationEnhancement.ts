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
  // While this function is async, any exceptions will be treated as unhandled, because
  // the caller is not in an async context and disregards the returned Promise
  // TODO: Ensure that if we get back an HTTP failure result, we still display the
  //       returned content. If there isn't any, display the status code.
  // TODO: If a second navigation occurs while the first is in flight, discard the first.
  // TODO: Deal with streaming SSR. The returned result may be left hanging for a while.
  const response = await fetch(internalDestinationHref);
  const responseHtml = await response.text();
  const parsedHtml = new DOMParser().parseFromString(responseHtml, 'text/html');
  synchronizeDomContent(document, parsedHtml);
}
