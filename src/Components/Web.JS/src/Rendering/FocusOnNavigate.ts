// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { domFunctions } from '../DomWrapper';
import { JSEventRegistry } from '../Services/JSEventRegistry';
import { isForSamePath } from '../Services/NavigationUtils';

const customElementName = 'blazor-focus-on-navigate';
let currentFocusOnNavigateElement: FocusOnNavigateElement | null = null;
let locationOnLastNavigation = location.href;

// On the initial page load, we only want to apply focus if there isn't already a focused element.
// https://developer.mozilla.org/docs/Web/API/Document/activeElement#value
let allowChangeFocus = document.activeElement === null || document.activeElement === document.body;

export function enableFocusOnNavigate(jsEventRegistry: JSEventRegistry) {
  customElements.define(customElementName, FocusOnNavigateElement);
  jsEventRegistry.addEventListener('enhancednavigationstart', onEnhancedNavigationStart);
  jsEventRegistry.addEventListener('enhancednavigationend', tryApplyFocus);
  document.addEventListener('focusin', onFocusIn);

  // Focus the element on the initial page load.
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', tryApplyFocus, { once: true });
  } else {
    tryApplyFocus();
  }
}

function onEnhancedNavigationStart() {
  // Only focus on enhanced load when navigating to a new page.
  if (!isForSamePath(locationOnLastNavigation, location.href)) {
    allowChangeFocus = true;
  }

  locationOnLastNavigation = location.href;
}

function onFocusIn() {
  // If the user explicitly focuses a different element before a navigation completes,
  // don't move focus again.
  allowChangeFocus = false;
}

function tryApplyFocus() {
  if (!allowChangeFocus) {
    return;
  }

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
