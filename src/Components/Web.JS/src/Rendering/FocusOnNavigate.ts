// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { domFunctions } from '../DomWrapper';
import { EnhancedNavigationStartEvent, JSEventRegistry } from '../Services/JSEventRegistry';

const customElementName = 'blazor-focus-on-navigate';
const focusOnNavigateRegistrations: FocusOnNavigateRegistration[] = [];

let allowFocusOnEnhancedLoad = false;

export function enableFocusOnNavigate(jsEventRegistry: JSEventRegistry) {
  customElements.define(customElementName, FocusOnNavigate);
  jsEventRegistry.addEventListener('enhancednavigationstart', onEnhancedNavigationStart);
  jsEventRegistry.addEventListener('enhancedload', onEnhancedLoad);
  document.addEventListener('focusin', onFocusIn);
  document.addEventListener('focusout', onFocusOut);

  // Focus the element on the initial page load
  if (document.readyState === 'loading') {
    allowFocusOnEnhancedLoad = true;
    document.addEventListener('DOMContentLoaded', afterInitialPageLoad);
  } else {
    afterInitialPageLoad();
  }
}

function afterInitialPageLoad() {
  tryApplyFocus(/* forceMoveFocus */ false);
}

function onEnhancedNavigationStart(ev: EnhancedNavigationStartEvent) {
  // Only focus on enhanced load if the enhanced navigation is not a form post.
  allowFocusOnEnhancedLoad = ev.method !== 'post';
}

function onEnhancedLoad() {
  if (allowFocusOnEnhancedLoad) {
    tryApplyFocus(/* forceMoveFocus */ true);
  }
}

function onFocusIn() {
  // As soon as an element get successfully focused, don't attempt to focus again on future
  // enhanced page updates.
  allowFocusOnEnhancedLoad = false;
}

let lastFocusedElement: Element | null = null;

function onFocusOut(ev: FocusEvent) {
  // It's possible that the element lost focus because it was removed from the page,
  // and now the document doesn't have an active element.
  // This could have happened either because an enhanced page update removed the element or
  // because the focused element was replaced during the transition to interactivity
  // (see https://github.com/dotnet/aspnetcore/issues/42561).
  // In either case, we'll attempt to reapply focus only if:
  // [1] that element was the one we last focused programmatically, and
  // [2] it's about to get removed from the DOM.
  const target = ev.target;
  if (target instanceof Element && target === lastFocusedElement) {
    // We want to apply focus after all synchronous changes to the DOM have completed,
    // including the potential removal of this element.
    setTimeout(() => {
      if (!document.contains(target)) {
        tryApplyFocus(/* forceMoveFocus */ false);
      }
    }, 0);
  }
}

function tryApplyFocus(forceMoveFocus: boolean) {
  // Don't apply focus if there's already a focused element and 'forceMoveFocus' is false.
  // See also: https://developer.mozilla.org/docs/Web/API/Document/activeElement#value
  if (!forceMoveFocus && document.activeElement !== null && document.activeElement !== document.body) {
    return;
  }

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
