// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { domFunctions } from '../DomWrapper';
import { JSEventRegistry } from '../Services/JSEventRegistry';
import { isForSamePath } from '../Services/NavigationUtils';
import { depthFirstNodeTreeTraversal, LogicalElement } from './LogicalElements';

const customElementName = 'blazor-focus-on-navigate';
let currentFocusOnNavigateElement: FocusOnNavigateElement | null = null;
let locationOnLastNavigation = location.href;
let allowApplyFocusAfterEnhancedNavigation = false;
let focusedNavigationElement: NavigationFocus | null = null;

interface NavigationFocus {
  element: Element;
  selector: string;
  owner: FocusOnNavigateElement;
}

export interface PendingNavigationFocus extends NavigationFocus {
  root: LogicalElement;
}

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
    focusedNavigationElement = null;
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
  focusedNavigationElement = null;
  const owner = currentFocusOnNavigateElement;
  const selector = owner?.getAttribute('selector');
  if (owner && selector) {
    const element = document.querySelector(selector);
    domFunctions.focusBySelector(selector);
    if (element && document.activeElement === element && currentFocusOnNavigateElement === owner && owner.getAttribute('selector') === selector) {
      focusedNavigationElement = { element, selector, owner };
    }
  }
}

export function captureNavigationFocus(root: LogicalElement): PendingNavigationFocus | null {
  const focus = focusedNavigationElement;
  if (!focus) {
    return null;
  }

  if (document.activeElement !== focus.element) {
    return null;
  }

  if (!findElementInLogicalRoot(root, element => element === focus.element)) {
    return null;
  }

  focusedNavigationElement = null;
  return { ...focus, root };
}

export function restoreNavigationFocus(focus: PendingNavigationFocus): void {
  if (!canRestoreFocus(focus)) {
    return;
  }

  const replacement = findElementInLogicalRoot(focus.root, element => element.matches(focus.selector));
  if (!replacement || !replacement.isConnected) {
    return;
  }

  if (!('focus' in replacement) || typeof replacement.focus !== 'function'
    || !('tabIndex' in replacement) || typeof replacement.tabIndex !== 'number') {
    throw new Error('Unable to focus an invalid element.');
  }

  if (!replacement.hasAttribute('tabindex')) {
    replacement.tabIndex = -1;
  }

  replacement.focus({ preventScroll: true });
}

function canRestoreFocus(focus: PendingNavigationFocus): boolean {
  return !focus.element.isConnected
    && (document.activeElement === null || document.activeElement === document.body)
    && currentFocusOnNavigateElement === focus.owner
    && focus.owner.isConnected
    && focus.owner.getAttribute('selector') === focus.selector;
}

function findElementInLogicalRoot(root: LogicalElement, predicate: (element: Element) => boolean): Element | null {
  for (const child of depthFirstNodeTreeTraversal(root)) {
    if (child !== root && child instanceof Element && predicate(child)) {
      return child;
    }
  }

  return null;
}

class FocusOnNavigateElement extends HTMLElement {
  connectedCallback() {
    // eslint-disable-next-line @typescript-eslint/no-this-alias
    currentFocusOnNavigateElement = this;
  }

  disconnectedCallback() {
    if (currentFocusOnNavigateElement === this) {
      currentFocusOnNavigateElement = null;
      focusedNavigationElement = null;
    }
  }
}
