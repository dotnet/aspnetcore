import '@dotnet/jsinterop';
import { resetScrollAfterNextBatch } from '../Rendering/Renderer';
import { EventDelegator } from '../Rendering/EventDelegator';

let hasEnabledNavigationInterception = false;
let hasRegisteredNavigationEventListeners = false;

// Will be initialized once someone registers
let notifyLocationChangedCallback: ((uri: string, intercepted: boolean) => Promise<void>) | null = null;

// These are the functions we're making available for invocation from .NET
export const internalFunctions = {
  listenForNavigationEvents,
  enableNavigationInterception,
  navigateTo,
  getBaseURI: () => document.baseURI,
  getLocationHref: () => location.href,
};

function listenForNavigationEvents(callback: (uri: string, intercepted: boolean) => Promise<void>) {
  notifyLocationChangedCallback = callback;

  if (hasRegisteredNavigationEventListeners) {
    return;
  }

  hasRegisteredNavigationEventListeners = true;
  window.addEventListener('popstate', () => notifyLocationChanged(false));
}

function enableNavigationInterception() {
  hasEnabledNavigationInterception = true;
}

export function attachToEventDelegator(eventDelegator: EventDelegator) {
  // We need to participate in EventDelegator's synthetic event bubbling process
  // (so we can respect stopBubbling/preventDefault), so register with that instead
  // of using a native JS event
  eventDelegator.addLinkClickListener((clickEvent, anchorElement) => {
    if (!hasEnabledNavigationInterception) {
      return;
    }

    if (clickEvent.button !== 0 || eventHasSpecialKey(clickEvent)) {
      // Don't stop ctrl/meta-click (etc) from opening links in new tabs/windows
      return;
    }

    // Intercept clicks on all <a> elements where the href is within the <base href> URI space
    // We must explicitly check if it has an 'href' attribute, because if it doesn't, the result might be null or an empty string depending on the browser
    const hrefAttributeName = 'href';
    if (anchorElement.hasAttribute(hrefAttributeName)) {
      const targetAttributeValue = anchorElement.getAttribute('target');
      const opensInSameFrame = !targetAttributeValue || targetAttributeValue === '_self';
      if (!opensInSameFrame) {
        return;
      }

      const href = anchorElement.getAttribute(hrefAttributeName)!;
      const absoluteHref = toAbsoluteUri(href);

      if (isWithinBaseUriSpace(absoluteHref)) {
        clickEvent.preventDefault();
        performInternalNavigation(absoluteHref, true);
      }
    }
  });
}

export function navigateTo(uri: string, forceLoad: boolean) {
  const absoluteUri = toAbsoluteUri(uri);

  if (!forceLoad && isWithinBaseUriSpace(absoluteUri)) {
    // It's an internal URL, so do client-side navigation
    performInternalNavigation(absoluteUri, false);
  } else if (forceLoad && location.href === uri) {
    // Force-loading the same URL you're already on requires special handling to avoid
    // triggering browser-specific behavior issues.
    // For details about what this fixes and why, see https://github.com/aspnet/AspNetCore/pull/10839
    const temporaryUri = uri + '?';
    history.replaceState(null, '', temporaryUri);
    location.replace(uri);
  } else {
    // It's either an external URL, or forceLoad is requested, so do a full page load
    location.href = uri;
  }
}

function performInternalNavigation(absoluteInternalHref: string, interceptedLink: boolean) {
  // Since this was *not* triggered by a back/forward gesture (that goes through a different
  // code path starting with a popstate event), we don't want to preserve the current scroll
  // position, so reset it.
  // To avoid ugly flickering effects, we don't want to change the scroll position until the
  // we render the new page. As a best approximation, wait until the next batch.
  resetScrollAfterNextBatch();

  history.pushState(null, /* ignored title */ '', absoluteInternalHref);
  notifyLocationChanged(interceptedLink);
}

async function notifyLocationChanged(interceptedLink: boolean) {
  if (notifyLocationChangedCallback) {
    await notifyLocationChangedCallback(location.href, interceptedLink);
  }
}

let testAnchor: HTMLAnchorElement;
function toAbsoluteUri(relativeUri: string) {
  testAnchor = testAnchor || document.createElement('a');
  testAnchor.href = relativeUri;
  return testAnchor.href;
}

function findClosestAncestor(element: Element | null, tagName: string) {
  return !element
    ? null
    : element.tagName === tagName
      ? element
      : findClosestAncestor(element.parentElement, tagName);
}

function isWithinBaseUriSpace(href: string) {
  const baseUriWithTrailingSlash = toBaseUriWithTrailingSlash(document.baseURI!); // TODO: Might baseURI really be null?
  return href.startsWith(baseUriWithTrailingSlash);
}

function toBaseUriWithTrailingSlash(baseUri: string) {
  return baseUri.substr(0, baseUri.lastIndexOf('/') + 1);
}

function eventHasSpecialKey(event: MouseEvent) {
  return event.ctrlKey || event.shiftKey || event.altKey || event.metaKey;
}
