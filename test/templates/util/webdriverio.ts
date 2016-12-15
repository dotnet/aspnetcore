// Workaround for missing '.value' property on WebdriverIO.Client<RawResult<T>> that should be of type T
// Can't notify TypeScript that the property exists directly, because the interface merging feature doesn't
// appear to support pattern matching in such a way that WebdriverIO.Client<T> is extended only when T
// itself extends RawResult<U> for some U.
export function getValue<T>(client: WebdriverIO.Client<WebdriverIO.RawResult<T>>): T {
    return (client as any).value;
}

// The official type declarations for getCssProperty are completely wrong. This function matches runtime behaviour.
export function getCssPropertyValue<T>(client: WebdriverIO.Client<T>, selector: string, cssProperty: string): string {
    return (client.getCssProperty(selector, cssProperty) as any).value;
}
