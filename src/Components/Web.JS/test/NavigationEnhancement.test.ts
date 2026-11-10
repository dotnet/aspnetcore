// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { afterEach, beforeEach, describe, expect, jest, test } from '@jest/globals';
import { EventDelegator } from '../src/Rendering/Events/EventDelegator';
import { eventNameAliasRegisteredCallbacks } from '../src/Rendering/Events/EventTypes';
import { dispatchEvent } from '../src/Rendering/WebRendererInteropMethods';
import { attachProgressivelyEnhancedNavigationListener, detachProgressivelyEnhancedNavigationListener } from '../src/Services/NavigationEnhancement';

jest.mock('../src/Rendering/Renderer', () => ({
  scheduleScrollReset: jest.fn(),
  ScrollResetSchedule: { AfterDocumentUpdate: 1 },
}));
jest.mock('../src/Rendering/WebRendererInteropMethods', () => ({
  dispatchEvent: jest.fn(),
  isRendererAttached: jest.fn(() => true),
}));

describe('Enhanced navigation click-listener coordination', () => {
  let add: jest.SpiedFunction<typeof document.addEventListener>;
  let remove: jest.SpiedFunction<typeof document.removeEventListener>;
  let pushState: jest.SpiedFunction<typeof history.pushState>;
  let anchor: HTMLAnchorElement;

  beforeEach(() => {
    jest.useFakeTimers();
    history.replaceState(null, '', '/');
    anchor = document.createElement('a');
    anchor.href = '#target';
    document.body.append(anchor);
    add = jest.spyOn(document, 'addEventListener');
    remove = jest.spyOn(document, 'removeEventListener');
    pushState = jest.spyOn(history, 'pushState');
    attachProgressivelyEnhancedNavigationListener({
      enhancedNavigationStarted: jest.fn(),
      beforeDomUpdate: jest.fn(),
      documentUpdated: jest.fn(),
      enhancedNavigationCompleted: jest.fn(),
    });
  });

  afterEach(() => {
    detachProgressivelyEnhancedNavigationListener();
    for (const [
      type,
      listener,
      options,
    ] of add.mock.calls) {
      document.removeEventListener(type, listener, options);
    }
    document.body.replaceChildren();
    eventNameAliasRegisteredCallbacks.length = 0;
    jest.runOnlyPendingTimers();
    jest.useRealTimers();
    jest.restoreAllMocks();
    jest.mocked(dispatchEvent).mockReset();
  });

  test('applies preventDefault before enhanced navigation without replacing the existing click delegate', () => {
    const delegator = new EventDelegator(1);
    delegator.setListener(anchor, 'click', 1, 0);
    remove.mockClear();
    delegator.setPreventDefault(anchor, 'click', true);
    expect(remove).not.toHaveBeenCalled();

    click(anchor);
    expect(pushState).not.toHaveBeenCalled();

    delegator.setPreventDefault(anchor, 'click', false);
    click(anchor);
    expect(pushState).toHaveBeenCalledTimes(1);
    expect(location.hash).toBe('#target');
  });

  test('defers reordering when a managed click registers another renderer and dispatches a nested prevented click', () => {
    const navigationListener = add.mock.calls[0][1];
    const prevented = document.createElement('a');
    prevented.href = '#prevented';
    document.body.append(prevented);
    const delegator = new EventDelegator(1);
    delegator.setListener(anchor, 'click', 1, 0);
    delegator.setPreventDefault(prevented, 'click', true);
    remove.mockClear();

    jest.mocked(dispatchEvent).mockImplementation(() => {
      new EventDelegator(2).notifyAfterClick(jest.fn());
      expect(remove).not.toHaveBeenCalled();
      click(prevented);
      expect(remove).not.toHaveBeenCalled();
      expect(pushState).not.toHaveBeenCalled();
      return Promise.resolve();
    });

    click(anchor);
    expect(dispatchEvent).toHaveBeenCalledTimes(1);
    expect(remove).toHaveBeenCalledWith('click', navigationListener);
    expect(pushState).toHaveBeenCalledTimes(1);
    expect(location.hash).toBe('#target');
  });
});

function click(element: Element): void {
  element.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true }));
}
