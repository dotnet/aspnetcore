// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// What one argument sends to .NET. null sends the whole value. An object sends only the named
// properties, each with its own shape, so that a browser object such as an event can be copied.
export type ArgumentShape = null | { [property: string]: ArgumentShape };

export interface DotNetObject {
  invokeMethodAsync<T>(methodIdentifier: string, ...args: unknown[]): Promise<T>;
}

interface DotNetGlobal {
  createJSObjectReference(jsObject: unknown): unknown;
}

// Creates a function that calls the Invoke method of the target with one argument per shape.
// Arguments beyond the shapes are dropped. The returned promise settles when the .NET method completes.
export function createCallback(target: DotNetObject, shapesJson: string): (...args: unknown[]) => Promise<unknown> {
  const shapes: ArgumentShape[] = JSON.parse(shapesJson);

  return (...args) => target.invokeMethodAsync('Invoke', ...shapes.map((shape, index) => toDotNet(args[index], shape)));
}

export function toDotNet(value: unknown, shape: ArgumentShape): unknown {
  if (shape === null) {
    return toTransferable(value);
  }

  if (value === null || value === undefined) {
    return null;
  }

  if (Array.isArray(value)) {
    return value.map(item => toDotNet(item, shape));
  }

  const copy: Record<string, unknown> = {};
  for (const [name, propertyShape] of Object.entries(shape)) {
    copy[name] = toDotNet((value as Record<string, unknown>)[name], propertyShape);
  }

  return copy;
}

// Web IDL dictionaries and sequences arrive as plain objects and arrays, so they are copied.
// Any other object is a browser object whose identity must survive, so it crosses as a reference.
export function toTransferable(value: unknown): unknown {
  if (value === null || (typeof value !== 'object' && typeof value !== 'function')) {
    return value;
  }

  if (Array.isArray(value)) {
    return value.map(toTransferable);
  }

  const prototype = Object.getPrototypeOf(value);
  if (prototype === Object.prototype || prototype === null) {
    return Object.fromEntries(Object.entries(value).map(([name, item]) => [name, toTransferable(item)]));
  }

  return (globalThis as unknown as { DotNet: DotNetGlobal }).DotNet.createJSObjectReference(value);
}
