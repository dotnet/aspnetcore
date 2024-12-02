// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import '@microsoft/dotnet-js-interop';
import { resetScrollAfterNextBatch } from '../Rendering/Renderer';
import { EventDelegator } from '../Rendering/Events/EventDelegator';
import { attachEnhancedNavigationListener, getInteractiveRouterRendererId, handleClickForNavigationInterception, hasInteractiveRouter, hasProgrammaticEnhancedNavigationHandler, isSamePageWithHash, isWithinBaseUriSpace, performProgrammaticEnhancedNavigation, performScrollToElementOnTheSamePage, scrollToElement, setHasInteractiveRouter, toAbsoluteUri } from './NavigationUtils';
import { WebRendererId } from '../Rendering/WebRendererId';
import { isRendererAttached } from '../Rendering/WebRendererInteropMethods';

let hasRegisteredNavigationEventListeners = false;
let currentHistoryIndex = 0;
let currentLocationChangingCallId = 0;

type NavigationCallbacks = {
  rendererId: WebRendererId;
  hasLocationChangingEventListeners: boolean;
  locationChanged(uri: string, state: string | undefined, intercepted: boolean): Promise<void>;
  locationChanging(callId: number, uri: string, state: string | undefined, intercepted: boolean): Promise<void>;
};

const navigationCallbacks = new Map<WebRendererId, NavigationCallbacks>();

let popStateCallback: ((state: PopStateEvent) => Promise<void> | void) = onBrowserInitiatedPopState;
let resolveCurrentNavigation: ((shouldContinueNavigation: boolean) => void) | null = null;

// These are the functions we're making available for invocation from .NET
export const internalFunctions = {
  listenForNavigationEvents,
  enableNavigationInterception: setHasInteractiveRouter,
  setHasLocationChangingListeners,
  endLocationChanging,
  navigateTo: navigateToFromDotNet,
  refresh,
  getBaseURI: (): string => document.baseURI,
  getLocationHref: (): string => location.href,
  scrollToElement,
};

function listenForNavigationEvents(
  rendererId: WebRendererId,
  locationChangedCallback: (uri: string, state: string | undefined, intercepted: boolean) => Promise<void>,
  locationChangingCallback: (callId: number, uri: string, state: string | undefined, intercepted: boolean) => Promise<void>
): void {
  navigationCallbacks.set(rendererId, {
    rendererId,
    hasLocationChangingEventListeners: false,
    locationChanged: locationChangedCallback,
    locationChanging: locationChangingCallback,
  });

  if (hasRegisteredNavigationEventListeners) {
    return;
  }

  hasRegisteredNavigationEventListeners = true;
  window.addEventListener('popstate', onPopState);
  currentHistoryIndex = history.state?._index ?? 0;

  attachEnhancedNavigationListener((internalDestinationHref, interceptedLink) => {
    notifyLocationChanged(interceptedLink, internalDestinationHref);
  });
}

function setHasLocationChangingListeners(rendererId: WebRendererId, hasListeners: boolean) {
  const callbacks = navigationCallbacks.get(rendererId);
  if (!callbacks) {
    throw new Error(`Renderer with ID '${rendererId}' is not listening for navigation events`);
  }
  callbacks.hasLocationChangingEventListeners = hasListeners;
}


export function attachToEventDelegator(eventDelegator: EventDelegator): void {
  // We need to respond to clicks on <a> elements *after* the EventDelegator has finished
  // running its simulated bubbling process so that we can respect any preventDefault requests.
  // So instead of registering our own native event, register using the EventDelegator.
  eventDelegator.notifyAfterClick(event => {
    if (!hasInteractiveRouter()) {
      return;
    }

    handleClickForNavigationInterception(event, absoluteInternalHref => {
      performInternalNavigation(absoluteInternalHref, /* interceptedLink */ true, /* replace */ false);
    });
  });
}


function refresh(forceReload: boolean): void {
  if (!forceReload && hasProgrammaticEnhancedNavigationHandler()) {
    performProgrammaticEnhancedNavigation(location.href, /* replace */ true);
  } else {
    location.reload();
  }
}

// For back-compat, we need to accept multiple overloads
export function navigateTo(uri: string, options: NavigationOptions): void;
export function navigateTo(uri: string, forceLoad: boolean): void;
export function navigateTo(uri: string, forceLoad: boolean, replace: boolean): void;
export function navigateTo(uri: string, forceLoadOrOptions: NavigationOptions | boolean, replaceIfUsingOldOverload = false): void {
  // Normalize the parameters to the newer overload (i.e., using NavigationOptions)
  const options: NavigationOptions = forceLoadOrOptions instanceof Object
    ? forceLoadOrOptions
    : { forceLoad: forceLoadOrOptions, replaceHistoryEntry: replaceIfUsingOldOverload };

  navigateToCore(uri, options);
}

function navigateToFromDotNet(uri: string, options: NavigationOptions): void {
  // The location changing callback is called from .NET for programmatic navigations originating from .NET.
  // In this case, we shouldn't invoke the callback again from the JS side.
  navigateToCore(uri, options, /* skipLocationChangingCallback */ true);
}

function navigateToCore(uri: string, options: NavigationOptions, skipLocationChangingCallback = false): void {
  const absoluteUri = toAbsoluteUri(uri);

  if (!options.forceLoad && isWithinBaseUriSpace(absoluteUri)) {
    if (shouldUseClientSideRouting()) {
      performInternalNavigation(absoluteUri, false, options.replaceHistoryEntry, options.historyEntryState, skipLocationChangingCallback);
    } else {
      performProgrammaticEnhancedNavigation(absoluteUri, options.replaceHistoryEntry);
    }
  } else {
    // For external navigation, we work in terms of the originally-supplied uri string,
    // not the computed absoluteUri. This is in case there are some special URI formats
    // we're unable to translate into absolute URIs.
    performExternalNavigation(uri, options.replaceHistoryEntry);
  }
}

function performExternalNavigation(uri: string, replace: boolean) {
  if (location.href === uri) {
    // If you're already on this URL, you can't append another copy of it to the history stack,
    // so we can ignore the 'replace' flag. However, reloading the same URL you're already on
    // requires special handling to avoid triggering browser-specific behavior issues.
    // For details about what this fixes and why, see https://github.com/dotnet/aspnetcore/pull/10839
    const temporaryUri = uri + '?';
    history.replaceState(null, '', temporaryUri);
    location.replace(uri);
  } else if (replace) {
    location.replace(uri);
  } else {
    location.href = uri;
  }
}

async function performInternalNavigation(absoluteInternalHref: string, interceptedLink: boolean, replace: boolean, state: string | undefined = undefined, skipLocationChangingCallback = false) {
  ignorePendingNavigation();

  if (isSamePageWithHash(absoluteInternalHref)) {
    saveToBrowserHistory(absoluteInternalHref, replace, state);
    performScrollToElementOnTheSamePage(absoluteInternalHref);
    return;
  }

  const callbacks = getInteractiveRouterNavigationCallbacks();
  if (!skipLocationChangingCallback && callbacks?.hasLocationChangingEventListeners) {
    const shouldContinueNavigation = await notifyLocationChanging(absoluteInternalHref, state, interceptedLink, callbacks);
    if (!shouldContinueNavigation) {
      return;
    }
  }

  // Since this was *not* triggered by a back/forward gesture (that goes through a different
  // code path starting with a popstate event), we don't want to preserve the current scroll
  // position, so reset it.
  // To avoid ugly flickering effects, we don't want to change the scroll position until
  // we render the new page. As a best approximation, wait until the next batch.
  resetScrollAfterNextBatch();

  saveToBrowserHistory(absoluteInternalHref, replace, state);

  await notifyLocationChanged(interceptedLink);
}

function saveToBrowserHistory(absoluteInternalHref: string, replace: boolean, state: string | undefined = undefined): void {
  if (!replace) {
    currentHistoryIndex++;
    history.pushState({
      userState: state,
      _index: currentHistoryIndex,
    }, /* ignored title */ '', absoluteInternalHref);
  } else {
    history.replaceState({
      userState: state,
      _index: currentHistoryIndex,
    }, /* ignored title */ '', absoluteInternalHref);
  }
}

function navigateHistoryWithoutPopStateCallback(delta: number): Promise<void> {
  return new Promise(resolve => {
    const oldPopStateCallback = popStateCallback;

    popStateCallback = () => {
      popStateCallback = oldPopStateCallback;
      resolve();
    };

    history.go(delta);
  });
}

function ignorePendingNavigation() {
  if (resolveCurrentNavigation) {
    resolveCurrentNavigation(false);
    resolveCurrentNavigation = null;
  }
}

function notifyLocationChanging(uri: string, state: string | undefined, intercepted: boolean, callbacks: NavigationCallbacks): Promise<boolean> {
  return new Promise<boolean>(resolve => {
    ignorePendingNavigation();
    currentLocationChangingCallId++;
    resolveCurrentNavigation = resolve;
    callbacks.locationChanging(currentLocationChangingCallId, uri, state, intercepted);
  });
}

function endLocationChanging(callId: number, shouldContinueNavigation: boolean) {
  if (resolveCurrentNavigation && callId === currentLocationChangingCallId) {
    resolveCurrentNavigation(shouldContinueNavigation);
    resolveCurrentNavigation = null;
  }
}

async function onBrowserInitiatedPopState(state: PopStateEvent) {
  ignorePendingNavigation();

  const callbacks = getInteractiveRouterNavigationCallbacks();
  if (callbacks?.hasLocationChangingEventListeners) {
    const index = state.state?._index ?? 0;
    const userState = state.state?.userState;
    const delta = index - currentHistoryIndex;
    const uri = location.href;

    // Temporarily revert the navigation until we confirm if the navigation should continue.
    await navigateHistoryWithoutPopStateCallback(-delta);

    const shouldContinueNavigation = await notifyLocationChanging(uri, userState, false, callbacks);
    if (!shouldContinueNavigation) {
      return;
    }

    await navigateHistoryWithoutPopStateCallback(delta);
  }

  // We don't know if popstate was triggered for a navigation that can be handled by the client-side router,
  // so we treat it as a intercepted link to be safe.
  await notifyLocationChanged(/* interceptedLink */ true);
}

async function notifyLocationChanged(interceptedLink: boolean, internalDestinationHref?: string) {
  const uri = internalDestinationHref ?? location.href;

  await Promise.all(Array.from(navigationCallbacks, async ([rendererId, callbacks]) => {
    if (isRendererAttached(rendererId)) {
      await callbacks.locationChanged(uri, history.state?.userState, interceptedLink);
    }
  }));
}

async function onPopState(state: PopStateEvent) {
  if (popStateCallback && shouldUseClientSideRouting()) {
    await popStateCallback(state);
  }

  currentHistoryIndex = history.state?._index ?? 0;
}

function getInteractiveRouterNavigationCallbacks(): NavigationCallbacks | undefined {
  const interactiveRouterRendererId = getInteractiveRouterRendererId();
  if (interactiveRouterRendererId === undefined) {
    return undefined;
  }

  return navigationCallbacks.get(interactiveRouterRendererId);
}

function shouldUseClientSideRouting() {
  return hasInteractiveRouter() || !hasProgrammaticEnhancedNavigationHandler();
}

// Keep in sync with Components/src/NavigationOptions.cs
export interface NavigationOptions {
  forceLoad: boolean;
  replaceHistoryEntry: boolean;
  historyEntryState?: string;
}
