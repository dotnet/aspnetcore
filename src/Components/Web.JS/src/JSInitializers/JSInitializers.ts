// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Blazor, IBlazor } from '../GlobalExports';
import { AfterBlazorServerStartedCallback, BeforeBlazorServerStartedCallback, CircuitStartOptions, ServerInitializers } from '../Platform/Circuits/CircuitStartOptions';
import { LogLevel, Logger } from '../Platform/Logging/Logger';
import { AfterBlazorWebAssemblyStartedCallback, BeforeBlazorWebAssemblyStartedCallback, WebAssemblyInitializers, WebAssemblyStartOptions } from '../Platform/WebAssemblyStartOptions';
import { WebStartOptions } from '../Platform/WebStartOptions';
import { WebRendererId } from '../Rendering/WebRendererId';
import { getRendererAttachedPromise } from '../Rendering/WebRendererInteropMethods';

type BeforeBlazorStartedCallback = (...args: unknown[]) => Promise<void>;
export type AfterBlazorStartedCallback = (blazor: typeof Blazor) => Promise<void>;
type BeforeBlazorWebStartedCallback = (options: WebStartOptions) => Promise<void>;
type AfterBlazorWebStartedCallback = (blazor: IBlazor) => Promise<void>;
export type BlazorInitializer = {
  beforeStart: BeforeBlazorStartedCallback,
  afterStarted: AfterBlazorStartedCallback,
  beforeWebStart: BeforeBlazorWebStartedCallback,
  afterWebStarted: AfterBlazorWebStartedCallback,
  beforeWebAssemblyStart: BeforeBlazorWebAssemblyStartedCallback,
  afterWebAssemblyStarted: AfterBlazorWebAssemblyStartedCallback,
  beforeServerStart: BeforeBlazorServerStartedCallback,
  afterServerStarted: AfterBlazorServerStartedCallback,
};

export class JSInitializer {
  private afterStartedCallbacks: AfterBlazorStartedCallback[] = [];

  constructor(
    private singleRuntime = true,
    private logger?: Logger,
    afterstartedCallbacks?: AfterBlazorStartedCallback[],
    private webRendererId: number = 0
  ) {
    if (afterstartedCallbacks) {
      this.afterStartedCallbacks.push(...afterstartedCallbacks);
    }
  }

  async importInitializersAsync(initializerFiles: string[], initializerArguments: unknown[]): Promise<void> {
    // This code is not called on WASM, because library intializers are imported by runtime.

    await Promise.all(initializerFiles.map(f => importAndInvokeInitializer(this, f)));

    function adjustPath(path: string): string {
      // This is the same we do in JS interop with the import callback
      const base = document.baseURI;
      path = base.endsWith('/') ? `${base}${path}` : `${base}/${path}`;
      return path;
    }

    async function importAndInvokeInitializer(jsInitializer: JSInitializer, path: string): Promise<void> {
      const adjustedPath = adjustPath(path);
      const initializer = await import(/* webpackIgnore: true */ adjustedPath) as Partial<BlazorInitializer>;
      if (initializer === undefined) {
        return;
      }

      if (!jsInitializer.singleRuntime) {
        return runMultiRuntimeInitializers(jsInitializer, initializer, initializerArguments);
      } else {
        const { beforeStart, afterStarted, beforeWebAssemblyStart, afterWebAssemblyStarted, beforeServerStart, afterServerStarted } = initializer;
        let finalBeforeStart = beforeStart;
        if (jsInitializer.webRendererId === WebRendererId.Server && beforeServerStart) {
          finalBeforeStart = beforeServerStart as unknown as BeforeBlazorStartedCallback;
        }
        if (jsInitializer.webRendererId === WebRendererId.WebAssembly && beforeWebAssemblyStart) {
          finalBeforeStart = beforeWebAssemblyStart as unknown as BeforeBlazorStartedCallback;
        }
        let finalAfterStarted = afterStarted;
        if (jsInitializer.webRendererId === WebRendererId.Server && afterServerStarted) {
          finalAfterStarted = afterServerStarted;
        }
        if (jsInitializer.webRendererId === WebRendererId.WebAssembly && afterWebAssemblyStarted) {
          finalAfterStarted = afterWebAssemblyStarted;
        }

        return runClassicInitializers(jsInitializer, finalBeforeStart, finalAfterStarted, initializerArguments);
      }

      function runMultiRuntimeInitializers(
        jsInitializer: JSInitializer,
        initializerModule: Partial<BlazorInitializer>, initializerArguments: unknown[]): void | PromiseLike<void> {
        const options = initializerArguments[0] as WebStartOptions;
        const { beforeStart, afterStarted, beforeWebStart, afterWebStarted, beforeWebAssemblyStart, afterWebAssemblyStarted, beforeServerStart, afterServerStarted } = initializerModule;
        const runtimeSpecificExports = !!(beforeWebStart || afterWebStarted || beforeWebAssemblyStart || afterWebAssemblyStarted || beforeServerStart || afterServerStarted);
        const hasOnlyClassicInitializers = !!(!runtimeSpecificExports && (beforeStart || afterStarted));
        const runLegacyInitializers = hasOnlyClassicInitializers && options.enableClassicInitializers;
        if (hasOnlyClassicInitializers && !options.enableClassicInitializers) {
          // log warning "classic initializers will be ignored when multiple runtimes are used".
          // Skipping "adjustedPath" initializer.
          jsInitializer.logger?.log(
            LogLevel.Warning,
            `Initializer '${adjustedPath}' will be ignored because multiple runtimes are available. Use 'before(Web|WebAssembly|Server)Start' and 'after(Web|WebAssembly|Server)Started' instead.`
          );
        } else if (runLegacyInitializers) {
          return runClassicInitializers(jsInitializer, beforeStart, afterStarted, initializerArguments);
        }

        ensureInitializers(options);

        if (beforeWebAssemblyStart) {
          options.webAssembly.initializers.beforeStart.push(beforeWebAssemblyStart);
        }

        if (afterWebAssemblyStarted) {
          options.webAssembly.initializers.afterStarted.push(afterWebAssemblyStarted);
        }

        if (beforeServerStart) {
          options.circuit.initializers.beforeStart.push(beforeServerStart);
        }

        if (afterServerStarted) {
          options.circuit.initializers.afterStarted.push(afterServerStarted);
        }

        if (afterWebStarted) {
          jsInitializer.afterStartedCallbacks.push(afterWebStarted);
        }

        if (beforeWebStart) {
          return beforeWebStart(options);
        }
      }

      function runClassicInitializers(jsInitializer: JSInitializer, beforeStart: BeforeBlazorStartedCallback | undefined, afterStarted: AfterBlazorStartedCallback | undefined, initializerArguments: unknown[]): void | PromiseLike<void> {
        if (afterStarted) {
          jsInitializer.afterStartedCallbacks.push(afterStarted);
        }

        if (beforeStart) {
          return beforeStart(...initializerArguments);
        }
      }

      function ensureInitializers(options: Partial<WebStartOptions>):
        asserts options is OptionsWithInitializers {
        if (!options['webAssembly']) {
          options['webAssembly'] = ({ initializers: { beforeStart: [], afterStarted: [] } }) as unknown as WebAssemblyStartOptions;
        } else if (!options['webAssembly'].initializers) {
          options['webAssembly'].initializers = { beforeStart: [], afterStarted: [] };
        }

        if (!options['circuit']) {
          options['circuit'] = ({ initializers: { beforeStart: [], afterStarted: [] } }) as unknown as CircuitStartOptions;
        } else if (!options['circuit'].initializers) {
          options['circuit'].initializers = { beforeStart: [], afterStarted: [] };
        }
      }
    }
  }

  async invokeAfterStartedCallbacks(blazor: typeof Blazor): Promise<void> {
    const attached = getRendererAttachedPromise(this.webRendererId);
    if (attached) {
      await attached;
    }
    await Promise.all(this.afterStartedCallbacks.map(callback => callback(blazor)));
  }
}

type OptionsWithInitializers = {
  webAssembly: WebAssemblyStartOptions & { initializers: WebAssemblyInitializers },
  circuit: CircuitStartOptions & { initializers: ServerInitializers }
}
