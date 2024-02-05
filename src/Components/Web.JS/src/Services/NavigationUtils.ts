// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { WebRendererId } from '../Rendering/WebRendererId';

let interactiveRouterRendererId: WebRendererId | undefined = undefined;
let programmaticEnhancedNavigationHandler: typeof performProgrammaticEnhancedNavigation | undefined;
let enhancedNavigationListener: typeof notifyEnhancedNavigationListners | undefined;

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
      event.preventDefault();
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

export function isSamePageWithHash(absoluteHref: string): boolean {
  const url = new URL(absoluteHref);
  return url.hash !== '' && location.origin === url.origin && location.pathname === url.pathname && location.search === url.search;
}

export function performScrollToElementOnTheSamePage(absoluteHref : string): void {
  const hashIndex = absoluteHref.indexOf('#');
  if (hashIndex === absoluteHref.length - 1) {
    return;
  }

  const identifier = absoluteHref.substring(hashIndex + 1);
  scrollToElement(identifier);
}

export function scrollToElement(identifier: string): void {
  document.getElementById(identifier)?.scrollIntoView();
}

export function attachEnhancedNavigationListener(listener: typeof enhancedNavigationListener) {
  enhancedNavigationListener = listener;
}

export function notifyEnhancedNavigationListners(internalDestinationHref: string, interceptedLink: boolean) {
  enhancedNavigationListener?.(internalDestinationHref, interceptedLink);
}

export function hasProgrammaticEnhancedNavigationHandler(): boolean {
  return programmaticEnhancedNavigationHandler !== undefined;
}

export function attachProgrammaticEnhancedNavigationHandler(handler: typeof programmaticEnhancedNavigationHandler) {
  programmaticEnhancedNavigationHandler = handler;
}

export function performProgrammaticEnhancedNavigation(absoluteInternalHref: string, replace: boolean): void {
  if (!programmaticEnhancedNavigationHandler) {
    throw new Error('No enhanced programmatic navigation handler has been attached');
  }

  programmaticEnhancedNavigationHandler(absoluteInternalHref, replace);
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

function canProcessAnchor(anchorTarget: HTMLAnchorElement | SVGAElement) {
  const targetAttributeValue = anchorTarget.getAttribute('target');
  const opensInSameFrame = !targetAttributeValue || targetAttributeValue === '_self';
  return opensInSameFrame && anchorTarget.hasAttribute('href') && !anchorTarget.hasAttribute('download');
}

function findAnchorTarget(event: MouseEvent): HTMLAnchorElement | SVGAElement | null {
  const path = event.composedPath && event.composedPath();
  if (path) {
    // This logic works with events that target elements within a shadow root,
    // as long as the shadow mode is 'open'. For closed shadows, we can't possibly
    // know what internal element was clicked.
    for (let i = 0; i < path.length; i++) {
      const candidate = path[i];
      if (candidate instanceof HTMLAnchorElement || candidate instanceof SVGAElement) {
        return candidate;
      }
    }
  }
  return null;
}

export function hasInteractiveRouter(): boolean {
  return interactiveRouterRendererId !== undefined;
}

export function getInteractiveRouterRendererId() : WebRendererId | undefined {
  return interactiveRouterRendererId;
}

export function setHasInteractiveRouter(rendererId: WebRendererId) {
  if (interactiveRouterRendererId !== undefined && interactiveRouterRendererId !== rendererId) {
    throw new Error('Only one interactive runtime may enable navigation interception at a time.');
  }

  interactiveRouterRendererId = rendererId;
}
