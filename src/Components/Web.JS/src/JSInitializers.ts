import { IBlazor } from "./GlobalExports";

type BeforeBlazorStartedCallback = (...args: unknown[]) => Promise<void>;
export type AfterBlazorStartedCallback = (blazor: IBlazor) => Promise<void>;
type BlazorInitializer = { beforeStart: BeforeBlazorStartedCallback, afterStarted: AfterBlazorStartedCallback };

export class JSInitializer {
  private afterStartedCallbacks: AfterBlazorStartedCallback[] = [];

  async importInitializersAsync(
    initializerFiles: string[],
    initializerArguments: unknown[]): Promise<void> {

    try {
      await Promise.all(initializerFiles.map(f => importAndInvokeInitializer(this, f)));
    } catch (error) {
      console.warn(`A library initializer produced an error before starting: '${error}'`);
    }

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

  async invokeAfterStartedCallbacks(blazor: IBlazor) {
    await Promise.all(this.afterStartedCallbacks.map(async callback => {
      try {
        await callback(blazor);
      } catch (error) {
        console.warn(`A library initializer produced an error after starting: '${error}'`);
      }
    }));
  }
}
