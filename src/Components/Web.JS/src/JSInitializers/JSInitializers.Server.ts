// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { CircuitStartOptions } from '../Platform/Circuits/CircuitStartOptions';
import { WebRendererId } from '../Rendering/WebRendererId';
import { JSInitializer } from './JSInitializers';

export async function fetchAndInvokeInitializers(options: Partial<CircuitStartOptions>) : Promise<JSInitializer> {
  if (options.initializers) {
    // Initializers were already resolved, so we don't have to fetch them, we just invoke the beforeStart ones
    // and return the list of afterStarted ones so they get processed in the same way as in traditional Blazor Server.
    await Promise.all(options.initializers.beforeStart.map(i => i(options)));
    return new JSInitializer(/* singleRuntime: */ false, undefined, options.initializers.afterStarted, WebRendererId.Server);
  }

  const jsInitializersResponse = await fetch('_blazor/initializers', {
    method: 'GET',
    credentials: 'include',
    cache: 'no-cache',
  });

  const initializers: string[] = await jsInitializersResponse.json();
  const jsInitializer = new JSInitializer(/* singleRuntime: */ true, undefined, undefined, WebRendererId.Server);
  await jsInitializer.importInitializersAsync(initializers, [options]);
  return jsInitializer;
}
