// The node test environment has no DOM: Microsoft.JSInterop reads window when it loads and checks instanceof Element when it serializes.
(globalThis as any).window ??= globalThis;
(globalThis as any).Element ??= class Element { };
