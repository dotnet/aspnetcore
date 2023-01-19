import '@microsoft/dotnet-js-interop';
export declare const domFunctions: {
    focus: typeof focus;
    focusBySelector: typeof focusBySelector;
};
declare function focus(element: HTMLOrSVGElement, preventScroll: boolean): void;
declare function focusBySelector(selector: string): void;
export {};
