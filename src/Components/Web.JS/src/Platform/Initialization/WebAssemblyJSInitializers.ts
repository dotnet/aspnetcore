import { IBlazor } from "../../GlobalExports";
import { BootConfigResult, BootJsonDataExtension } from "../BootConfig";
import { WebAssemblyStartOptions } from "../WebAssemblyStartOptions";

type BeforeBlazorStartedCallback = (options: Partial<WebAssemblyStartOptions> | undefined, bootConfigResul?: BootJsonDataExtension) => Promise<void>;
type AfterBlazorStartedCallback = (blazor: IBlazor) => Promise<void>;
type BlazorInitializer = { beforeBlazorStarts: BeforeBlazorStartedCallback, afterBlazorStarted: AfterBlazorStartedCallback };

export class WebAssemblyJSInitializers {

  static async invokeInitializersAsync(bootConfigResult: BootConfigResult, candidateOptions: Partial<WebAssemblyStartOptions>): Promise<AfterBlazorStartedCallback[]> {

    const afterBlazorStartedCallbacks: AfterBlazorStartedCallback[] = [];
    if (bootConfigResult.bootConfig.libraryInitializers) {
      const initializerFiles = bootConfigResult.bootConfig.libraryInitializers;
      try {
        await Promise.all(Object.entries(initializerFiles).map(f => importAndInvokeInitializer(...f)));
      } catch (error) {
        console.warn(`A library initializer produced an error: '${error}'`);
      }
    }

    return afterBlazorStartedCallbacks;

    function adjustPath(path: string): string {
      // This is the same we do in JS interop with the import callback
      const base = document.baseURI;
      path = base.endsWith('/') ? `${base}${path}` : `${base}/${path}`;
      return path;
    }

    async function importAndInvokeInitializer(path: string, signature: string): Promise<void> {
      const adjustedPath = adjustPath(path);
      const initializer = await import(/* webpackIgnore: true */ adjustedPath) as Partial<BlazorInitializer>;
      if (initializer === undefined) {
        return;
      }
      const { beforeBlazorStarts, afterBlazorStarted } = initializer;
      if (afterBlazorStarted) {
        afterBlazorStartedCallbacks.push(afterBlazorStarted);
      }

      if (beforeBlazorStarts) {
        return beforeBlazorStarts(candidateOptions, bootConfigResult.bootConfig.extensions);
      }
    }
  }
}
