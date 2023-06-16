/**
 * Checks if a click event corresponds to an <a> tag referencing a URL within the base href, and that interception
 * isn't bypassed (e.g., by a 'download' attribute or the user holding a meta key while clicking).
 * @param event The event that occurred
 * @param callbackIfIntercepted A callback that will be invoked if the event corresponds to a click on an <a> that can be intercepted.
 */
export function handleClickForNavigationInterception(event: MouseEvent, callbackIfIntercepted: (absoluteInternalHref: string) => void): void {
  if (event.button !== 0 || eventHasSpecialKey(event)) {
    // Don't stop ctrl/meta-click (etc) from opening links in new tabs/windows
    return;
  }

  if (event.defaultPrevented) {
    return;
  }

  // Intercept clicks on all <a> elements where the href is within the <base href> URI space
  // We must explicitly check if it has an 'href' attribute, because if it doesn't, the result might be null or an empty string depending on the browser
  const anchorTarget = findAnchorTarget(event);

  if (anchorTarget && canProcessAnchor(anchorTarget)) {
    const anchorHref = anchorTarget.getAttribute('href')!;

    const absoluteHref = toAbsoluteUri(anchorHref);

    if (isWithinBaseUriSpace(absoluteHref)) {
      callbackIfIntercepted(absoluteHref);
    }
  }
}

export function isWithinBaseUriSpace(href: string) {
  const baseUriWithoutTrailingSlash = toBaseUriWithoutTrailingSlash(document.baseURI!);
  const nextChar = href.charAt(baseUriWithoutTrailingSlash.length);

  return href.startsWith(baseUriWithoutTrailingSlash)
  && (nextChar === '' || nextChar === '/' || nextChar === '?' || nextChar === '#');
}

function toBaseUriWithoutTrailingSlash(baseUri: string) {
  return baseUri.substring(0, baseUri.lastIndexOf('/'));
}

let testAnchor: HTMLAnchorElement;
export function toAbsoluteUri(relativeUri: string): string {
  testAnchor = testAnchor || document.createElement('a');
  testAnchor.href = relativeUri;
  return testAnchor.href;
}

function eventHasSpecialKey(event: MouseEvent) {
  return event.ctrlKey || event.shiftKey || event.altKey || event.metaKey;
}

function canProcessAnchor(anchorTarget: HTMLAnchorElement) {
  const targetAttributeValue = anchorTarget.getAttribute('target');
  const opensInSameFrame = !targetAttributeValue || targetAttributeValue === '_self';
  return opensInSameFrame && anchorTarget.hasAttribute('href') && !anchorTarget.hasAttribute('download');
}

function findAnchorTarget(event: MouseEvent): HTMLAnchorElement | null {
  // _blazorDisableComposedPath is a temporary escape hatch in case any problems are discovered
  // in this logic. It can be removed in a later release, and should not be considered supported API.
  const path = !window['_blazorDisableComposedPath'] && event.composedPath && event.composedPath();
  if (path) {
    // This logic works with events that target elements within a shadow root,
    // as long as the shadow mode is 'open'. For closed shadows, we can't possibly
    // know what internal element was clicked.
    for (let i = 0; i < path.length; i++) {
      const candidate = path[i];
      if (candidate instanceof Element && candidate.tagName === 'A') {
        return candidate as HTMLAnchorElement;
      }
    }
    return null;
  } else {
    // Since we're adding use of composedPath in a patch, retain compatibility with any
    // legacy browsers that don't support it by falling back on the older logic, even
    // though it won't work properly with ShadowDOM. This can be removed in the next
    // major release.
    return findClosestAnchorAncestorLegacy(event.target as Element | null, 'A');
  }
}

function findClosestAnchorAncestorLegacy(element: Element | null, tagName: string) {
  return !element
  ? null
  : element.tagName === tagName
  ? element
  : findClosestAnchorAncestorLegacy(element.parentElement, tagName);
}
