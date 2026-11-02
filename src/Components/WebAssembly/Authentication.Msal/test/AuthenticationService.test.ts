// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { afterEach, beforeEach, describe, expect, jest, test } from '@jest/globals';
import type { PublicClientApplication } from '@azure/msal-browser';

describe('MSAL AuthenticationService initialization', () => {
  const callbackUrl = 'http://localhost/authentication/login-callback';
  const loggingOptions = { debugEnabled: false, traceEnabled: false };
  let AuthenticationService: typeof import('../src/Interop/AuthenticationService').AuthenticationService;
  let clients: ReturnType<typeof createMsalClient>[];
  let pending: Promise<unknown>[];

  beforeEach(async () => {
    // Reload the real wrapper so each test starts with a fresh singleton.
    jest.resetModules();
    clients = [];
    pending = [];
    const msal = jest.requireActual<typeof import('@azure/msal-browser')>('@azure/msal-browser');
    jest.doMock('@azure/msal-browser', () => ({
      ...msal,
      PublicClientApplication: jest.fn(() => {
        const client = createMsalClient(msal);
        clients.push(client);
        return client;
      }),
    }));
    ({ AuthenticationService } = await import('../src/Interop/AuthenticationService'));
  });

  afterEach(async () => {
    for (const client of clients) {
      client.finishInitialization();
    }
    await Promise.allSettled(pending);
  });

  test('nonoverlapping init calls initialize MSAL and handle the redirect only once', async () => {
    const initialization = initialize();
    expect(clients[0].handleRedirectPromise).not.toHaveBeenCalled();

    clients[0].finishInitialization();
    expect(await initialization).toEqual({ status: 'operationCompleted' });
    expect(await initialize()).toEqual({ status: 'operationCompleted' });

    expect(clients).toHaveLength(1);
    expect(clients[0].initialize).toHaveBeenCalledTimes(1);
    expect(clients[0].handleRedirectPromise).toHaveBeenCalledTimes(1);
  });

  test('overlapping init calls complete sign-in without handling a redirect on an uninitialized MSAL client', async () => {
    const first = initialize();
    const second = initialize();

    // Complete the first caller while any competing client is still blocked.
    clients[0].finishInitialization();
    expect(await first).toEqual({ status: 'operationCompleted' });

    for (const client of clients) {
      client.finishInitialization();
    }
    expect(await second).toEqual({ status: 'operationCompleted' });
    expect(clients).toHaveLength(1);
    expect(clients[0].initialize).toHaveBeenCalledTimes(1);
    expect(clients[0].handleRedirectPromise).toHaveBeenCalledTimes(1);
  });

  test('failed initialization rejects overlapping callers and can be retried', async () => {
    const results = Promise.allSettled([initialize(), initialize()]);
    const error = new Error('MSAL initialization failed');

    expect(clients).toHaveLength(1);
    clients[0].failInitialization(error);
    expect(await results).toEqual([{ status: 'rejected', reason: error }, { status: 'rejected', reason: error }]);
    expect(clients[0].handleRedirectPromise).not.toHaveBeenCalled();

    const retry = initialize();
    expect(clients).toHaveLength(2);
    clients[1].finishInitialization();
    expect(await retry).toEqual({ status: 'operationCompleted' });
    expect(clients[1].initialize).toHaveBeenCalledTimes(1);
    expect(clients[1].handleRedirectPromise).toHaveBeenCalledTimes(1);
  });

  function initialize() {
    const result = AuthenticationService.init({
      auth: {
        clientId: 'test-client',
        authority: 'https://login.microsoftonline.com/test-tenant',
        redirectUri: callbackUrl,
        knownAuthorities: [],
      },
      defaultAccessTokenScopes: [],
      additionalScopesToConsent: [],
      loginMode: 'redirect',
    }, loggingOptions)
      .then(() => AuthenticationService.completeSignIn(callbackUrl));
    pending.push(result);
    return result;
  }
});

function createMsalClient(msal: typeof import('@azure/msal-browser')) {
  let finishInitialization!: () => void;
  let failInitialization!: (error: Error) => void;
  const initialization = new Promise<void>((resolve, reject) => {
    finishInitialization = resolve;
    failInitialization = reject;
  });
  let initialized = false;

  return {
    finishInitialization,
    failInitialization,
    initialize: jest.fn<PublicClientApplication['initialize']>(() => initialization.then(() => {
      initialized = true;
    })),
    handleRedirectPromise: jest.fn<PublicClientApplication['handleRedirectPromise']>(() => {
      if (!initialized) {
        return Promise.reject(new msal.BrowserAuthError(msal.BrowserAuthErrorCodes.uninitializedPublicClientApplication));
      }

      return Promise.resolve(null);
    }),
  };
}
