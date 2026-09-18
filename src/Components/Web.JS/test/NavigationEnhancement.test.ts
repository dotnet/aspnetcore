// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { afterEach, describe, expect, jest, test } from '@jest/globals';
import { attachProgressivelyEnhancedNavigationListener, detachProgressivelyEnhancedNavigationListener, performEnhancedPageLoad } from '../src/Services/NavigationEnhancement';

describe('NavigationEnhancement', () => {
  afterEach(() => {
    detachProgressivelyEnhancedNavigationListener();
    delete (globalThis as unknown as { fetch?: typeof fetch }).fetch;
    jest.restoreAllMocks();
  });

  test('falls back to a full page load when a GET fetch fails', async () => {
    attachProgressivelyEnhancedNavigationListener({
      enhancedNavigationStarted: jest.fn(),
      beforeDomUpdate: jest.fn(),
      documentUpdated: jest.fn(),
      enhancedNavigationCompleted: jest.fn(),
    });

    const destination = new URL('/destination', document.baseURI).toString();
    const fetchMock = jest.fn<typeof fetch>().mockRejectedValue(new TypeError('Failed to fetch'));
    Object.defineProperty(globalThis, 'fetch', { configurable: true, value: fetchMock });
    const replaceStateSpy = jest.spyOn(history, 'replaceState');
    const consoleWarnSpy = jest.spyOn(console, 'warn').mockImplementation(() => undefined);
    jest.spyOn(console, 'error').mockImplementation(() => undefined);

    await performEnhancedPageLoad(destination, true);

    expect(replaceStateSpy).toHaveBeenCalledWith(null, '', `${destination}?`);
    expect(consoleWarnSpy).toHaveBeenCalledWith(`Enhanced navigation failed for destination ${destination}. Falling back to full page load.`);
  });
});