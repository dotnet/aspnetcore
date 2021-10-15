import { Blazor } from '../GlobalExports';
import { rendererAttached } from "../Rendering/WebRendererInteropMethods";

type BeforeBlazorStartedCallback = (...args: unknown[]) => Promise<void>;
export type AfterBlazorStartedCallback = (blazor: typeof Blazor) => Promise<void>;
type BlazorInitializer = { beforeStart: BeforeBlazorStartedCallback, afterStarted: AfterBlazorStartedCallback };

export class JSInitializer {
  private afterStartedCallbacks: AfterBlazorStartedCallback[] = [];

  async importInitializersAsync(initializerFiles: string[], initializerArguments: unknown[]): Promise<void> {
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
      const { beforeStart: beforeStart, afterStarted: afterStarted } = initializer;
      if (afterStarted) {
        jsInitializer.afterStartedCallbacks.push(afterStarted);
      }

      if (beforeStart) {
        return beforeStart(...initializerArguments);
      }
    }
  }

  async invokeAfterStartedCallbacks(blazor: typeof Blazor): Promise<void> {
    await rendererAttached;
    await Promise.all(this.afterStartedCallbacks.map(callback => callback(blazor)));
  }
}
