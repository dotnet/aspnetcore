// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { domFunctions } from '../DomWrapper';
import { JSEventRegistry } from '../Services/JSEventRegistry';
import { isForSamePath } from '../Services/NavigationUtils';

const customElementName = 'blazor-focus-on-navigate';
let currentFocusOnNavigateElement: FocusOnNavigateElement | null = null;
let locationOnLastNavigation = location.href;
let allowApplyFocusAfterEnhancedNavigation = false;

export function enableFocusOnNavigate(jsEventRegistry: JSEventRegistry) {
  customElements.define(customElementName, FocusOnNavigateElement);
  jsEventRegistry.addEventListener('enhancednavigationstart', onEnhancedNavigationStart);
  jsEventRegistry.addEventListener('enhancednavigationend', onEnhancedNavigationEnd);
  document.addEventListener('focusin', onFocusIn);

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', onInitialPageLoad, { once: true });
  } else {
    onInitialPageLoad();
  }
}

function onInitialPageLoad() {
  // On the initial page load, we only want to apply focus if there isn't already
  // a focused element.
  // See also: https://developer.mozilla.org/docs/Web/API/Document/activeElement#value
  if (document.activeElement !== null && document.activeElement !== document.body) {
    return;
  }

  // If an element on the page is requesting autofocus, but hasn't yet been focused,
  // we'll respect that.
  if (document.querySelector('[autofocus]')) {
    return;
  }

  tryApplyFocus();
}

function onEnhancedNavigationStart() {
  // Only move focus when navigating to a new page.
  if (!isForSamePath(locationOnLastNavigation, location.href)) {
    allowApplyFocusAfterEnhancedNavigation = true;
  }

  locationOnLastNavigation = location.href;
}

function onEnhancedNavigationEnd() {
  if (allowApplyFocusAfterEnhancedNavigation) {
    tryApplyFocus();
  }
}

function onFocusIn() {
  // If the user explicitly focuses a different element before a navigation completes,
  // don't move focus again.
  allowApplyFocusAfterEnhancedNavigation = false;
}

function tryApplyFocus() {
  const selector = currentFocusOnNavigateElement?.getAttribute('selector');
  if (selector) {
    domFunctions.focusBySelector(selector);
  }
}

class FocusOnNavigateElement extends HTMLElement {
  connectedCallback() {
    // eslint-disable-next-line @typescript-eslint/no-this-alias
    currentFocusOnNavigateElement = this;
  }

  disconnectedCallback() {
    if (currentFocusOnNavigateElement === this) {
      currentFocusOnNavigateElement = null;
    }
  }
}
