// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { expect, test, describe, beforeEach, afterEach } from '@jest/globals';
import { attachToEventDelegator } from '../src/Services/NavigationManager';
import { isInteractiveRouterConnected, setHasInteractiveRouter, setInteractiveRouterConnectionChecker } from '../src/Services/NavigationUtils';
import { EventDelegator } from '../src/Rendering/Events/EventDelegator';
import { WebRendererId } from '../src/Rendering/WebRendererId';

// Captures the click handler that NavigationManager registers, so tests can invoke it directly
// instead of standing up a real EventDelegator.
function captureAfterClickHandler(): (event: MouseEvent) => void {
  let handler: ((event: MouseEvent) => void) | undefined;
  const eventDelegator = {
    notifyAfterClick: (callback: (event: MouseEvent) => void) => {
      handler = callback;
    },
  };

  attachToEventDelegator(eventDelegator as unknown as EventDelegator);

  if (!handler) {
    throw new Error('attachToEventDelegator did not register an after-click handler');
  }

  return handler;
}

// A left-click on an internal <a>, shaped like the events handleClickForNavigationInterception reads.
function createInternalLinkClick(href: string): MouseEvent {
  const anchor = document.createElement('a');
  anchor.setAttribute('href', href);
  document.body.appendChild(anchor);

  let defaultPrevented = false;

  return {
    button: 0,
    ctrlKey: false,
    shiftKey: false,
    altKey: false,
    metaKey: false,
    get defaultPrevented() {
      return defaultPrevented;
    },
    preventDefault() {
      defaultPrevented = true;
    },
    composedPath: () => [anchor],
  } as unknown as MouseEvent;
}

describe('Issue #9964 - follow links when the interactive router is disconnected', () => {
  beforeEach(() => {
    setHasInteractiveRouter(WebRendererId.Server);
  });

  afterEach(() => {
    setInteractiveRouterConnectionChecker(undefined);
    document.body.innerHTML = '';
  });

  test('reports connected when no checker is registered', () => {
    expect(isInteractiveRouterConnected()).toBe(true);
  });

  test('reports whatever the registered checker returns', () => {
    let connected = true;
    setInteractiveRouterConnectionChecker(() => connected);

    expect(isInteractiveRouterConnected()).toBe(true);

    connected = false;
    expect(isInteractiveRouterConnected()).toBe(false);
  });

  test('intercepts link clicks while connected', () => {
    setInteractiveRouterConnectionChecker(() => true);
    const notifyAfterClick = captureAfterClickHandler();

    const event = createInternalLinkClick('/some-page');
    notifyAfterClick(event);

    expect(event.defaultPrevented).toBe(true);
  });

  test('leaves link clicks to the browser while disconnected', () => {
    setInteractiveRouterConnectionChecker(() => false);
    const notifyAfterClick = captureAfterClickHandler();

    const event = createInternalLinkClick('/some-page');
    notifyAfterClick(event);

    // Not calling preventDefault is what lets the browser perform a full page load, which follows
    // the link and gives the app a chance to establish a new connection.
    expect(event.defaultPrevented).toBe(false);
  });
});
