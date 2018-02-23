import { registerFunction } from '../Interop/RegisteredFunction';
import { platform } from '../Environment';
import { MethodHandle } from '../Platform/Platform';
const registeredFunctionPrefix = 'Microsoft.AspNetCore.Blazor.Browser.Services.BrowserUriHelper';
let notifyLocationChangedMethod: MethodHandle;
let hasRegisteredEventListeners = false;

registerFunction(`${registeredFunctionPrefix}.getLocationHref`,
  () => platform.toDotNetString(location.href));

registerFunction(`${registeredFunctionPrefix}.getBaseURI`,
  () => document.baseURI ? platform.toDotNetString(document.baseURI) : null);

registerFunction(`${registeredFunctionPrefix}.enableNavigationInteception`, () => {
  if (hasRegisteredEventListeners) {
    return;
  }
  hasRegisteredEventListeners = true;

  document.addEventListener('click', event => {
    // Intercept clicks on all <a> elements where the href is within the <base href> URI space
    const anchorTarget = findClosestAncestor(event.target as Element | null, 'A');
    if (anchorTarget) {
      const href = anchorTarget.getAttribute('href');
      if (isWithinBaseUriSpace(toAbsoluteUri(href))) {
        event.preventDefault();
        history.pushState(null, /* ignored title */ '', href);
        handleInternalNavigation();
      }
    }
  });

  window.addEventListener('popstate', handleInternalNavigation);
});

function handleInternalNavigation() {
  if (!notifyLocationChangedMethod) {
    notifyLocationChangedMethod = platform.findMethod(
      'Microsoft.AspNetCore.Blazor.Browser',
      'Microsoft.AspNetCore.Blazor.Browser.Services',
      'BrowserUriHelper',
      'NotifyLocationChanged'
    );
  }

  platform.callMethod(notifyLocationChangedMethod, null, [
    platform.toDotNetString(location.href)
  ]);
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
  const baseUriPrefixWithTrailingSlash = toBaseUriPrefixWithTrailingSlash(document.baseURI!); // TODO: Might baseURI really be null?
  return href.startsWith(baseUriPrefixWithTrailingSlash);
}

function toBaseUriPrefixWithTrailingSlash(baseUri: string) {
  return baseUri.substr(0, baseUri.lastIndexOf('/') + 1);
}
