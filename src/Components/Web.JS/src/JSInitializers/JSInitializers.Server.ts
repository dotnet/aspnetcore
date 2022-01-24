// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { CircuitStartOptions } from '../Platform/Circuits/CircuitStartOptions';
import { JSInitializer } from './JSInitializers';

export async function fetchAndInvokeInitializers(options: Partial<CircuitStartOptions>) : Promise<JSInitializer> {
  const jsInitializersResponse = await fetch('_blazor/initializers', {
    method: 'GET',
    credentials: 'include',
    cache: 'no-cache',
  });

  const initializers: string[] = await jsInitializersResponse.json();
  const jsInitializer = new JSInitializer();
  await jsInitializer.importInitializersAsync(initializers, [options]);
  return jsInitializer;
}
