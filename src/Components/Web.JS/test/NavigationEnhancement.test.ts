// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
import { afterEach, describe, expect, jest, test } from '@jest/globals';
import { attachProgressivelyEnhancedNavigationListener, detachProgressivelyEnhancedNavigationListener, isPageLoading, performEnhancedPageLoad } from '../src/Services/NavigationEnhancement';
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
    const locationDescriptor = Object.getOwnPropertyDescriptor(window, 'location')!;
    const locationReplaceMock = jest.fn();
    Object.defineProperty(window, 'location', {
      configurable: true,
      value: { ...window.location, replace: locationReplaceMock },
    });
    try {
      await performEnhancedPageLoad(destination, true);
      expect(replaceStateSpy).toHaveBeenCalledWith(null, '', `${destination}?`);
      expect(locationReplaceMock).toHaveBeenCalledWith(destination);
      expect(consoleWarnSpy).toHaveBeenCalledWith(`Enhanced navigation failed for destination ${destination}. Falling back to full page load.`);
    } finally {
      Object.defineProperty(window, 'location', locationDescriptor);
    }
  });
  test('falls back to a full page load when an uppercase GET fetch fails', async () => {
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
    const locationDescriptor = Object.getOwnPropertyDescriptor(window, 'location')!;
    const locationReplaceMock = jest.fn();
    Object.defineProperty(window, 'location', {
      configurable: true,
      value: { ...window.location, replace: locationReplaceMock },
    });
    try {
      await performEnhancedPageLoad(destination, true, { method: 'GET' });
      expect(replaceStateSpy).toHaveBeenCalledWith(null, '', `${destination}?`);
      expect(locationReplaceMock).toHaveBeenCalledWith(destination);
      expect(consoleWarnSpy).toHaveBeenCalledWith(`Enhanced navigation failed for destination ${destination}. Falling back to full page load.`);
    } finally {
      Object.defineProperty(window, 'location', locationDescriptor);
    }
  });
  test('treats a mixed-case enhanced form method as GET', async () => {
    attachProgressivelyEnhancedNavigationListener({
      enhancedNavigationStarted: jest.fn(),
      beforeDomUpdate: jest.fn(),
      documentUpdated: jest.fn(),
      enhancedNavigationCompleted: jest.fn(),
    });
    document.body.innerHTML = `
      <form action="/destination" method="post" data-enhance>
        <button type="submit" formmethod="GeT">Submit</button>
      </form>`;
    const form = document.querySelector('form')!;
    const submitter = document.querySelector('button')!;
    const fetchError = new TypeError('Failed to fetch');
    let rejectFetch!: (reason: unknown) => void;
    const fetchPromise = new Promise<Response>((_resolve, reject) => rejectFetch = reject);
    const fetchMock = jest.fn<typeof fetch>().mockReturnValue(fetchPromise);
    Object.defineProperty(globalThis, 'fetch', { configurable: true, value: fetchMock });
    const replaceStateSpy = jest.spyOn(history, 'replaceState');
    const consoleWarnSpy = jest.spyOn(console, 'warn').mockImplementation(() => undefined);
    const locationDescriptor = Object.getOwnPropertyDescriptor(window, 'location')!;
    const locationReplaceMock = jest.fn();
    Object.defineProperty(window, 'location', {
      configurable: true,
      value: { ...window.location, replace: locationReplaceMock },
    });
    try {
      const submitEvent = new Event('submit', { bubbles: true, cancelable: true });
      Object.defineProperty(submitEvent, 'submitter', { value: submitter });
      form.dispatchEvent(submitEvent);
      rejectFetch(fetchError);
      await fetchPromise.catch(() => undefined);
      await Promise.resolve();

      const requestedDestination = fetchMock.mock.calls[0][0] as string;
      expect(submitEvent.defaultPrevented).toBe(true);
      expect(fetchMock).toHaveBeenCalledWith(requestedDestination, expect.objectContaining({ method: 'GeT' }));
      expect(replaceStateSpy).toHaveBeenLastCalledWith(null, '', `${requestedDestination}?`);
      expect(locationReplaceMock).toHaveBeenCalledWith(requestedDestination);
      expect(consoleWarnSpy).toHaveBeenCalledWith(`Enhanced navigation failed for destination ${requestedDestination}. Falling back to full page load.`);
    } finally {
      Object.defineProperty(window, 'location', locationDescriptor);
    }
  });
  test('falls back to a full page load when reading a GET response body fails', async () => {
    attachProgressivelyEnhancedNavigationListener({
      enhancedNavigationStarted: jest.fn(),
      beforeDomUpdate: jest.fn(),
      documentUpdated: jest.fn(),
      enhancedNavigationCompleted: jest.fn(),
    });
    const destination = new URL('/destination', document.baseURI).toString();
    const responseBodyError = new TypeError('Failed to read response body');
    const response = {
      body: {},
      headers: new Headers(),
      text: () => Promise.reject(responseBodyError),
    } as Response;
    const fetchMock = jest.fn<typeof fetch>().mockResolvedValue(response);
    Object.defineProperty(globalThis, 'fetch', { configurable: true, value: fetchMock });
    const replaceStateSpy = jest.spyOn(history, 'replaceState');
    const consoleWarnSpy = jest.spyOn(console, 'warn').mockImplementation(() => undefined);
    const locationDescriptor = Object.getOwnPropertyDescriptor(window, 'location')!;
    const locationReplaceMock = jest.fn();
    Object.defineProperty(window, 'location', {
      configurable: true,
      value: { ...window.location, replace: locationReplaceMock },
    });
    try {
      await performEnhancedPageLoad(destination, true);
      expect(replaceStateSpy).toHaveBeenCalledWith(null, '', `${destination}?`);
      expect(locationReplaceMock).toHaveBeenCalledWith(destination);
      expect(consoleWarnSpy).toHaveBeenCalledWith(`Enhanced navigation failed for destination ${destination}. Falling back to full page load.`);
    } finally {
      Object.defineProperty(window, 'location', locationDescriptor);
    }
  });
  test('does not fall back to a full page load when a GET fetch is aborted', async () => {
    attachProgressivelyEnhancedNavigationListener({
      enhancedNavigationStarted: jest.fn(),
      beforeDomUpdate: jest.fn(),
      documentUpdated: jest.fn(),
      enhancedNavigationCompleted: jest.fn(),
    });
    const destination = new URL('/destination', document.baseURI).toString();
    const supersedingDestination = new URL('/superseding-destination', document.baseURI).toString();
    const supersedingFetchError = new TypeError('Failed to fetch');
    let fetchCallCount = 0;
    const fetchMock = jest.fn<typeof fetch>().mockImplementation((_input, init): Promise<Response> => {
      fetchCallCount++;
      if (fetchCallCount === 1) {
        return new Promise<Response>((_resolve, reject) => {
          init?.signal?.addEventListener('abort', () => reject(new DOMException('The operation was aborted.', 'AbortError')));
        });
      }
      return Promise.reject(supersedingFetchError);
    });
    Object.defineProperty(globalThis, 'fetch', { configurable: true, value: fetchMock });
    const replaceStateSpy = jest.spyOn(history, 'replaceState');
    const consoleWarnSpy = jest.spyOn(console, 'warn').mockImplementation(() => undefined);
    const abortedNavigation = performEnhancedPageLoad(destination, true);
    const supersedingNavigation = performEnhancedPageLoad(supersedingDestination, false, { method: 'post' });
    await expect(supersedingNavigation).rejects.toBe(supersedingFetchError);
    await abortedNavigation;
    expect(replaceStateSpy).not.toHaveBeenCalled();
    expect(consoleWarnSpy).not.toHaveBeenCalled();
  });
  test('does not fall back to a full page load when reading a GET response body is aborted', async () => {
    attachProgressivelyEnhancedNavigationListener({
      enhancedNavigationStarted: jest.fn(),
      beforeDomUpdate: jest.fn(),
      documentUpdated: jest.fn(),
      enhancedNavigationCompleted: jest.fn(),
    });
    const destination = new URL('/destination', document.baseURI).toString();
    const supersedingDestination = new URL('/superseding-destination', document.baseURI).toString();
    const supersedingFetchError = new TypeError('Failed to fetch');
    let notifyBodyReadStarted!: () => void;
    const bodyReadStarted = new Promise<void>(resolve => notifyBodyReadStarted = resolve);
    let fetchCallCount = 0;
    const fetchMock = jest.fn<typeof fetch>().mockImplementation((_input, init): Promise<Response> => {
      fetchCallCount++;
      if (fetchCallCount === 1) {
        return Promise.resolve({
          body: {},
          headers: new Headers(),
          text: () => new Promise<string>((_resolve, reject) => {
            notifyBodyReadStarted();
            init?.signal?.addEventListener('abort', () => reject(new DOMException('The operation was aborted.', 'AbortError')));
          }),
        } as Response);
      }
      return Promise.reject(supersedingFetchError);
    });
    Object.defineProperty(globalThis, 'fetch', { configurable: true, value: fetchMock });
    const replaceStateSpy = jest.spyOn(history, 'replaceState');
    const consoleWarnSpy = jest.spyOn(console, 'warn').mockImplementation(() => undefined);
    const abortedNavigation = performEnhancedPageLoad(destination, true);
    await bodyReadStarted;
    const supersedingNavigation = performEnhancedPageLoad(supersedingDestination, false, { method: 'post' });
    await expect(supersedingNavigation).rejects.toBe(supersedingFetchError);
    await abortedNavigation;
    expect(replaceStateSpy).not.toHaveBeenCalled();
    expect(consoleWarnSpy).not.toHaveBeenCalled();
  });
  test('propagates a fetch failure for a POST request without falling back to a full page load', async () => {
    attachProgressivelyEnhancedNavigationListener({
      enhancedNavigationStarted: jest.fn(),
      beforeDomUpdate: jest.fn(),
      documentUpdated: jest.fn(),
      enhancedNavigationCompleted: jest.fn(),
    });
    const destination = new URL('/destination', document.baseURI).toString();
    const fetchError = new TypeError('Failed to fetch');
    const fetchMock = jest.fn<typeof fetch>().mockRejectedValue(fetchError);
    Object.defineProperty(globalThis, 'fetch', { configurable: true, value: fetchMock });
    const replaceStateSpy = jest.spyOn(history, 'replaceState');
    const consoleWarnSpy = jest.spyOn(console, 'warn').mockImplementation(() => undefined);
    await expect(performEnhancedPageLoad(destination, false, { method: 'post' })).rejects.toBe(fetchError);
    expect(replaceStateSpy).not.toHaveBeenCalled();
    expect(consoleWarnSpy).not.toHaveBeenCalled();
  });
  test('propagates a streaming response body failure for a POST request and completes the navigation', async () => {
    const enhancedNavigationCompleted = jest.fn();
    attachProgressivelyEnhancedNavigationListener({
      enhancedNavigationStarted: jest.fn(),
      beforeDomUpdate: jest.fn(),
      documentUpdated: jest.fn(),
      enhancedNavigationCompleted,
    });
    const destination = new URL('/destination', document.baseURI).toString();
    const responseBodyError = new TypeError('Failed to read response body');
    const responseBody = {
      pipeThrough: jest.fn().mockReturnThis(),
      pipeTo: jest.fn<() => Promise<void>>().mockRejectedValue(responseBodyError),
    };
    const response = {
      body: responseBody as unknown as ReadableStream<Uint8Array>,
      headers: new Headers({ 'ssr-framing': 'boundary' }),
    } as Response;
    const fetchMock = jest.fn<typeof fetch>().mockResolvedValue(response);
    Object.defineProperty(globalThis, 'fetch', { configurable: true, value: fetchMock });
    const textDecoderStreamDescriptor = Object.getOwnPropertyDescriptor(globalThis, 'TextDecoderStream');
    const transformStreamDescriptor = Object.getOwnPropertyDescriptor(globalThis, 'TransformStream');
    const writableStreamDescriptor = Object.getOwnPropertyDescriptor(globalThis, 'WritableStream');
    Object.defineProperty(globalThis, 'TextDecoderStream', { configurable: true, value: jest.fn() });
    Object.defineProperty(globalThis, 'TransformStream', { configurable: true, value: jest.fn() });
    Object.defineProperty(globalThis, 'WritableStream', { configurable: true, value: jest.fn() });
    const replaceStateSpy = jest.spyOn(history, 'replaceState');
    const consoleWarnSpy = jest.spyOn(console, 'warn').mockImplementation(() => undefined);
    try {
      await expect(performEnhancedPageLoad(destination, false, { method: 'post' })).rejects.toBe(responseBodyError);
      expect(replaceStateSpy).not.toHaveBeenCalled();
      expect(consoleWarnSpy).not.toHaveBeenCalled();
      expect(enhancedNavigationCompleted).toHaveBeenCalledTimes(1);
      expect(isPageLoading()).toBe(false);
    } finally {
      if (textDecoderStreamDescriptor) {
        Object.defineProperty(globalThis, 'TextDecoderStream', textDecoderStreamDescriptor);
      } else {
        delete (globalThis as unknown as { TextDecoderStream?: typeof TextDecoderStream }).TextDecoderStream;
      }
      if (transformStreamDescriptor) {
        Object.defineProperty(globalThis, 'TransformStream', transformStreamDescriptor);
      } else {
        delete (globalThis as unknown as { TransformStream?: typeof TransformStream }).TransformStream;
      }
      if (writableStreamDescriptor) {
        Object.defineProperty(globalThis, 'WritableStream', writableStreamDescriptor);
      } else {
        delete (globalThis as unknown as { WritableStream?: typeof WritableStream }).WritableStream;
      }
    }
  });
  test('does not complete a newer navigation when a stale POST response body fails', async () => {
    const enhancedNavigationCompleted = jest.fn();
    attachProgressivelyEnhancedNavigationListener({
      enhancedNavigationStarted: jest.fn(),
      beforeDomUpdate: jest.fn(),
      documentUpdated: jest.fn(),
      enhancedNavigationCompleted,
    });
    const staleDestination = new URL('/stale-destination', document.baseURI).toString();
    const currentDestination = new URL('/current-destination', document.baseURI).toString();
    const staleResponseBodyError = new TypeError('Failed to read stale response body');
    let notifyBodyReadStarted!: () => void;
    const bodyReadStarted = new Promise<void>(resolve => notifyBodyReadStarted = resolve);
    let rejectStaleResponseBody!: (reason: unknown) => void;
    const staleResponse = {
      body: {},
      headers: new Headers(),
      text: () => new Promise<string>((_resolve, reject) => {
        rejectStaleResponseBody = reject;
        notifyBodyReadStarted();
      }),
    } as Response;
    const currentResponse = {
      body: {},
      headers: new Headers({
        'blazor-enhanced-nav': 'allow',
        'content-type': 'text/html',
      }),
      redirected: false,
      status: 200,
      text: () => Promise.resolve('<!DOCTYPE html><html><head></head><body><main>Current navigation</main></body></html>'),
      type: 'basic',
      url: currentDestination,
    } as Response;
    const fetchMock = jest.fn<typeof fetch>()
      .mockResolvedValueOnce(staleResponse)
      .mockResolvedValueOnce(currentResponse);
    Object.defineProperty(globalThis, 'fetch', { configurable: true, value: fetchMock });

    const staleNavigation = performEnhancedPageLoad(staleDestination, false, { method: 'post' });
    await bodyReadStarted;
    const currentNavigation = performEnhancedPageLoad(currentDestination, false);
    await currentNavigation;
    expect(enhancedNavigationCompleted).toHaveBeenCalledTimes(1);
    expect(isPageLoading()).toBe(false);

    rejectStaleResponseBody(staleResponseBodyError);
    await expect(staleNavigation).rejects.toBe(staleResponseBodyError);
    expect(enhancedNavigationCompleted).toHaveBeenCalledTimes(1);
    expect(isPageLoading()).toBe(false);
  });

  test('displays the error notification when an enhanced form POST fetch fails', async () => {
    const enhancedNavigationStarted = jest.fn();
    const enhancedNavigationCompleted = jest.fn();
    attachProgressivelyEnhancedNavigationListener({
      enhancedNavigationStarted,
      beforeDomUpdate: jest.fn(),
      documentUpdated: jest.fn(),
      enhancedNavigationCompleted,
    });
    document.body.innerHTML = `
      <form action="/destination" method="post" data-enhance>
        <input name="value" value="preserved value">
        <button type="submit">Submit</button>
      </form>
      <div id="blazor-error-ui" style="display: none"></div>`;
    const form = document.querySelector('form')!;
    const fetchError = new TypeError('Failed to fetch');
    const fetchPromise = Promise.reject<Response>(fetchError);
    const fetchMock = jest.fn<typeof fetch>().mockReturnValue(fetchPromise);
    Object.defineProperty(globalThis, 'fetch', { configurable: true, value: fetchMock });
    const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation(() => undefined);
    const replaceStateSpy = jest.spyOn(history, 'replaceState');

    const submitEvent = new Event('submit', { bubbles: true, cancelable: true });
    form.dispatchEvent(submitEvent);
    await fetchPromise.catch(() => undefined);
    await Promise.resolve();

    expect(submitEvent.defaultPrevented).toBe(true);
    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(consoleErrorSpy).toHaveBeenCalledWith(fetchError);
    expect(document.querySelector<HTMLElement>('#blazor-error-ui')!.style.display).toBe('block');
    expect(document.querySelector<HTMLInputElement>('input')!.value).toBe('preserved value');
    expect(replaceStateSpy).not.toHaveBeenCalled();
    expect(enhancedNavigationStarted).toHaveBeenCalledTimes(1);
    expect(enhancedNavigationCompleted).toHaveBeenCalledTimes(1);
  });

  test('processes a successful HTML response', async () => {
    const beforeDomUpdate = jest.fn();
    const documentUpdated = jest.fn();
    const enhancedNavigationCompleted = jest.fn();
    attachProgressivelyEnhancedNavigationListener({
      enhancedNavigationStarted: jest.fn(),
      beforeDomUpdate,
      documentUpdated,
      enhancedNavigationCompleted,
    });
    const destination = new URL('/destination', document.baseURI).toString();
    const response = {
      body: {},
      headers: new Headers({
        'blazor-enhanced-nav': 'allow',
        'content-type': 'text/html',
      }),
      redirected: false,
      status: 200,
      text: () => Promise.resolve('<!DOCTYPE html><html><head></head><body><main id="content">Loaded</main></body></html>'),
      type: 'basic',
      url: destination,
    } as Response;
    const fetchMock = jest.fn<typeof fetch>().mockResolvedValue(response);
    Object.defineProperty(globalThis, 'fetch', { configurable: true, value: fetchMock });

    await performEnhancedPageLoad(destination, false);

    expect(document.querySelector('#content')?.textContent).toBe('Loaded');
    expect(beforeDomUpdate).toHaveBeenCalledTimes(1);
    expect(documentUpdated).toHaveBeenCalledTimes(1);
    expect(enhancedNavigationCompleted).toHaveBeenCalledTimes(1);
  });
});
