import { registerFunction } from '../Interop/RegisteredFunction';
import { platform } from '../Environment';
import { MethodHandle, System_String } from '../Platform/Platform';
const registeredFunctionPrefix = 'Microsoft.AspNetCore.Blazor.Browser.Services.BrowserUriHelper';
let notifyLocationChangedMethod: MethodHandle;
let hasRegisteredEventListeners = false;

registerFunction(`${registeredFunctionPrefix}.getLocationHref`,
  () => platform.toDotNetString(location.href));

registerFunction(`${registeredFunctionPrefix}.getBaseURI`,
  () => document.baseURI ? platform.toDotNetString(document.baseURI) : null);

registerFunction(`${registeredFunctionPrefix}.enableNavigationInterception`, () => {
  if (hasRegisteredEventListeners) {
    return;
  }
  hasRegisteredEventListeners = true;

  document.addEventListener('click', event => {
    // Intercept clicks on all <a> elements where the href is within the <base href> URI space
    const anchorTarget = findClosestAncestor(event.target as Element | null, 'A');
    if (anchorTarget) {
      const href = anchorTarget.getAttribute('href');
      const absoluteHref = toAbsoluteUri(href);
      //if the user wants to user some specific browser/OS feature, we dont handle it and let the browser/OS
      const anyChangeBehaviorKeyHold = event.ctrlKey || event.shiftKey || event.altKey || event.metaKey;
      if (isWithinBaseUriSpace(absoluteHref) && !anyChangeBehaviorKeyHold) 
      {
        event.preventDefault();
        performInternalNavigation(absoluteHref);
      }
    }
  });

  window.addEventListener('popstate', handleInternalNavigation);
});

registerFunction(`${registeredFunctionPrefix}.navigateTo`, (uriDotNetString: System_String) => {
  navigateTo(platform.toJavaScriptString(uriDotNetString));
});

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
  const baseUriWithTrailingSlash = toBaseUriWithTrailingSlash(document.baseURI!); // TODO: Might baseURI really be null?
  return href.startsWith(baseUriWithTrailingSlash);
}

function toBaseUriWithTrailingSlash(baseUri: string) {
  return baseUri.substr(0, baseUri.lastIndexOf('/') + 1);
}
