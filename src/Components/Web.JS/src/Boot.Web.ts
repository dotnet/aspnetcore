// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Currently this only deals with inserting streaming content into the DOM.
// Later this will be expanded to include:
//  - Progressive enhancement of navigation and form posting
//  - Preserving existing DOM elements in all the above
//  - The capabilities of Boot.Server.ts and Boot.WebAssembly.ts to handle insertion
//    of interactive components

import { DotNet } from '@microsoft/dotnet-js-interop';
import { setCircuitOptions } from './Boot.Server.Common';
import { setWebAssemblyOptions } from './Boot.WebAssembly.Common';
import { shouldAutoStart } from './BootCommon';
import { Blazor } from './GlobalExports';
import { WebStartOptions } from './Platform/WebStartOptions';
import { attachStreamingRenderingListener } from './Rendering/StreamingRendering';
import { NavigationEnhancementCallbacks, attachProgressivelyEnhancedNavigationListener } from './Services/NavigationEnhancement';
import { WebRootComponentManager } from './Services/WebRootComponentManager';
import { hasProgrammaticEnhancedNavigationHandler, performProgrammaticEnhancedNavigation } from './Services/NavigationUtils';
import { attachComponentDescriptorHandler, registerAllComponentDescriptors } from './Rendering/DomMerging/DomSync';
import { JSEventRegistry } from './Services/JSEventRegistry';
import { fetchAndInvokeInitializers } from './JSInitializers/JSInitializers.Web';
import { ConsoleLogger } from './Platform/Logging/Loggers';
import { LogLevel } from './Platform/Logging/Logger';
import { resolveOptions } from './Platform/Circuits/CircuitStartOptions';
import { JSInitializer } from './JSInitializers/JSInitializers';

let started = false;
let rootComponentManager: WebRootComponentManager;

function boot(options?: Partial<WebStartOptions>) : Promise<void> {
  if (started) {
    throw new Error('Blazor has already started.');
  }

  started = true;
  options = options || {};
  options.logLevel ??= LogLevel.Error;

  // Defined here to avoid inadvertently imported enhanced navigation
  // related APIs in WebAssembly or Blazor Server contexts.
  Blazor._internal.hotReloadApplied = () => {
    if (hasProgrammaticEnhancedNavigationHandler()) {
      performProgrammaticEnhancedNavigation(location.href, true);
    }
  };

  rootComponentManager = new WebRootComponentManager(options?.ssr?.circuitInactivityTimeoutMs ?? 2000);
  const jsEventRegistry = JSEventRegistry.create(Blazor);

  const navigationEnhancementCallbacks: NavigationEnhancementCallbacks = {
    documentUpdated: () => {
      rootComponentManager.onDocumentUpdated();
      jsEventRegistry.dispatchEvent('enhancedload', {});
    },
    enhancedNavigationCompleted() {
      rootComponentManager.onEnhancedNavigationCompleted();
    },
  };

  attachComponentDescriptorHandler(rootComponentManager);
  attachStreamingRenderingListener(options?.ssr, navigationEnhancementCallbacks);

  if (!options?.ssr?.disableDomPreservation) {
    attachProgressivelyEnhancedNavigationListener(navigationEnhancementCallbacks);
  }

  // Wait until the initial page response completes before activating interactive components.
  // If stream rendering is used, this helps to ensure that only the final set of interactive
  // components produced by the stream render actually get activated for interactivity.
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', onInitialDomContentLoaded.bind(null, options));
  } else {
    onInitialDomContentLoaded(options);
  }

  return Promise.resolve();
}

function onInitialDomContentLoaded(options: Partial<WebStartOptions>) {

  // Retrieve and start invoking the initializers.
  // Blazor server options get defaults that are configured before we invoke the initializers
  // so we do the same here.
  const initialCircuitOptions = resolveOptions(options?.circuit || {});
  options.circuit = initialCircuitOptions;
  const logger = new ConsoleLogger(initialCircuitOptions.logLevel);
  const initializersPromise = fetchAndInvokeInitializers(options, logger);
  setCircuitOptions(resolveConfiguredOptions(initializersPromise, initialCircuitOptions));
  setWebAssemblyOptions(resolveConfiguredOptions(initializersPromise, options?.webAssembly || {}));

  registerAllComponentDescriptors(document);
  rootComponentManager.onDocumentUpdated();

  callAfterStartedCallbacks(initializersPromise);
}

async function resolveConfiguredOptions<TOptions>(initializers: Promise<JSInitializer>, options: TOptions): Promise<TOptions> {
  await initializers;
  return options;
}

async function callAfterStartedCallbacks(initializersPromise: Promise<JSInitializer>): Promise<void> {
  const initializers = await initializersPromise;
  await initializers.invokeAfterStartedCallbacks(Blazor);
}

Blazor.start = boot;
window['DotNet'] = DotNet;

if (shouldAutoStart()) {
  boot();
}
