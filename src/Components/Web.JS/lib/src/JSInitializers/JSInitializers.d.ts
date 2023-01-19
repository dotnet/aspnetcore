import { Blazor } from '../GlobalExports';
export type AfterBlazorStartedCallback = (blazor: typeof Blazor) => Promise<void>;
export declare class JSInitializer {
    private afterStartedCallbacks;
    importInitializersAsync(initializerFiles: string[], initializerArguments: unknown[]): Promise<void>;
    invokeAfterStartedCallbacks(blazor: typeof Blazor): Promise<void>;
}
