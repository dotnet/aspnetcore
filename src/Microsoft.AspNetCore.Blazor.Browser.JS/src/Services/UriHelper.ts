import { platform } from '../Environment';
import { MethodHandle, System_String } from '../Platform/Platform';
const registeredFunctionPrefix = 'Microsoft.AspNetCore.Blazor.Browser.Services.BrowserUriHelper';
let notifyLocationChangedMethod: MethodHandle;
let hasRegisteredEventListeners = false;

// These are the functions we're making available for invocation from .NET
export const internalFunctions = {
  enableNavigationInterception,
  navigateTo,
  getBaseURI: () => document.baseURI,
  getLocationHref: () => location.href,
}

function enableNavigationInterception() {
  if (hasRegisteredEventListeners) {
    return;
  }
  hasRegisteredEventListeners = true;

  document.addEventListener('click', event => {
    // Intercept clicks on all <a> elements where the href is within the <base href> URI space
    // We must explicitly check if it has an 'href' attribute, because if it doesn't, the result might be null or an empty string depending on the browser
    const anchorTarget = findClosestAncestor(event.target as Element | null, 'A') as HTMLAnchorElement;
    const hrefAttributeName = 'href';
    if (anchorTarget && anchorTarget.hasAttribute(hrefAttributeName) && event.button === 0) {
      const href = anchorTarget.getAttribute(hrefAttributeName)!;
      const absoluteHref = toAbsoluteUri(href);

      // Don't stop ctrl/meta-click (etc) from opening links in new tabs/windows
      if (isWithinBaseUriSpace(absoluteHref) && !eventHasSpecialKey(event)) {
        event.preventDefault();
        performInternalNavigation(absoluteHref);
      }
    }
  });

  window.addEventListener('popstate', handleInternalNavigation);
}

export function navigateTo(uri: string) {
  const absoluteUri = toAbsoluteUri(uri);
  if (isWithinBaseUriSpace(absoluteUri)) {
    performInternalNavigation(absoluteUri);
  } else {
    location.href = uri;
  }
}

function performInternalNavigation(absoluteInternalHref: string) {
  history.pushState(null, /* ignored title */ '', absoluteInternalHref);
  handleInternalNavigation();
}

async function handleInternalNavigation() {
  await DotNet.invokeMethodAsync(
    'Microsoft.AspNetCore.Blazor.Browser',
    'NotifyLocationChanged',
    location.href
  );
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
      : findClosestAncestor(element.parentElement, tagName)
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
