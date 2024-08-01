// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { domFunctions } from '../DomWrapper';
import { EnhancedNavigationStartEvent, JSEventRegistry } from '../Services/JSEventRegistry';

const customElementName = 'blazor-focus-on-navigate';
const focusOnNavigateRegistrations: FocusOnNavigateRegistration[] = [];

export function enableFocusOnNavigate(jsEventRegistry: JSEventRegistry) {
  customElements.define(customElementName, FocusOnNavigate);
  jsEventRegistry.addEventListener('enhancednavigationstart', onEnhancedNavigationStart);
  jsEventRegistry.addEventListener('enhancednavigationend', onEnhancedNavigationEnd);
  jsEventRegistry.addEventListener('enhancedload', onEnhancedLoad);
  document.addEventListener('focusin', onFocusIn);
  document.addEventListener('focusout', onFocusOut);

  // Focus the element on the initial page load
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', tryApplyFocus);
  } else {
    tryApplyFocus();
  }
}

let shouldFocusOnEnhancedLoad = false;
let isNavigating = false;

function onEnhancedNavigationStart(ev: EnhancedNavigationStartEvent) {
  // Only focus on enhanced load if the enhanced navigation is not a form post.
  shouldFocusOnEnhancedLoad = ev.method !== 'post';
  isNavigating = true;
}

function onEnhancedNavigationEnd() {
  isNavigating = false;
}

function onEnhancedLoad() {
  if (shouldFocusOnEnhancedLoad) {
    tryApplyFocus();
  }
}

function onFocusIn() {
  // As soon as an element get successfully focused, don't attempt to focus again on future
  // enhanced page updates.
  shouldFocusOnEnhancedLoad = false;
}

function onFocusOut(ev: FocusEvent) {
  // It's possible that the element lost focus because it was removed from the page,
  // and now the document doesn't have an active element.
  // There are two variations of this case that we care about:
  // [1] This happens during enhanced navigation. In this case, we'll attempt to re-apply focus to
  //     the element specified by the active <blazor-focus-on-navigate>, because we're still navigating.
  // [2] This happens after an enhanced navigation. One common cause of this is when the focused element
  //     gets replaced in the transition to interactivity. In this case, we'll only attempt to re-apply
  //     focus if the removed element is the same one we manually applied focus to.
  const target = ev.target;
  if (target instanceof Element && (isNavigating || target === lastFocusedElement)) {
    // We want to apply focus after all synchronous changes to the DOM have completed,
    // including the potential removal of this element.
    setTimeout(() => {
      const documentHasNoFocusedElement = document.activeElement === null || document.activeElement === document.body;
      if (documentHasNoFocusedElement && !document.contains(target)) {
        tryApplyFocus();
      }
    }, 0);
  }
}

let lastFocusedElement: Element | null = null;

function tryApplyFocus() {
  lastFocusedElement = null;

  const selector = findActiveSelector();
  if (selector) {
    lastFocusedElement = domFunctions.focusBySelector(selector);
  }
}

function findActiveSelector(): string | null {
  // It's unlikely that there will be more than one <blazor-focus-on-navigate> registered
  // at a time. But if there is, we'll prefer the one most recently added to the DOM,
  // keeping a stack of all previous registrations to fall back on if the current one
  // gets removed.
  let registration: FocusOnNavigateRegistration | undefined;
  while ((registration = focusOnNavigateRegistrations.at(-1)) !== undefined) {
    if (registration.isConnected) {
      return registration.selector;
    }

    focusOnNavigateRegistrations.pop();
  }

  return null;
}

type FocusOnNavigateRegistration = {
  isConnected: boolean;
  selector: string | null;
}

class FocusOnNavigate extends HTMLElement {
  static observedAttributes = ['selector'];

  private readonly _registration: FocusOnNavigateRegistration = {
    isConnected: true,
    selector: null,
  };

  connectedCallback() {
    focusOnNavigateRegistrations.push(this._registration);
  }

  attributeChangedCallback(name: string, oldValue: string, newValue: string) {
    if (name === 'selector') {
      this._registration.selector = newValue;
    }
  }

  disconnectedCallback() {
    this._registration.isConnected = false;
  }
}
