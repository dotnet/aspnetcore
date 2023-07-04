// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { MonoConfig } from 'dotnet';
import { WebAssemblyStartOptions } from '../Platform/WebAssemblyStartOptions';
import { BlazorInitializer, JSInitializer } from './JSInitializers';

export async function invokeOnBeforeStart(moduleConfig: MonoConfig, options: Partial<WebAssemblyStartOptions>): Promise<JSInitializer> {
  const initializerArguments = [options, moduleConfig.resources?.extensions ?? {}];
  const jsInitializer = new JSInitializer();

  const beforeStartPromises: Promise<void>[] = [];
  if (moduleConfig.libraryInitializers) {
    for (let i = 0; i < moduleConfig.libraryInitializers.length; i++) {
      const initializer = moduleConfig.libraryInitializers[i];
      const blazorInitializer = initializer as Partial<BlazorInitializer>;
      if (blazorInitializer === undefined) {
        continue;
      }

      const { beforeStart: beforeStart, afterStarted: afterStarted } = blazorInitializer;
      if (afterStarted) {
        jsInitializer.addAfterStartedCallback(afterStarted);
      }

      if (beforeStart) {
        beforeStartPromises.push(beforeStart(...initializerArguments));
      }
    }
  }

  await Promise.all(beforeStartPromises);
  return jsInitializer;
}
