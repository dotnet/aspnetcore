// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// What one argument sends to .NET. null sends the whole value. An object sends only the named
// properties, each with its own shape, so that a browser object such as an event can be copied.
// { $known: [...] } sends the name of the global that is the value, such as "localStorage".
export type ArgumentShape = null | KnownObjectShape | { [property: string]: ArgumentShape };

interface KnownObjectShape {
  $known: string[];
}

export interface DotNetObject {
  invokeMethodAsync<T>(methodIdentifier: string, ...args: unknown[]): Promise<T>;
}

// Creates a function that calls the Invoke method of the callback reference with one argument per shape.
// Arguments beyond the shapes are dropped. The returned promise settles when the .NET method completes.
export function createCallback(callbackReference: DotNetObject, shapesJson: string): (...args: unknown[]) => Promise<unknown> {
  const shapes: ArgumentShape[] = JSON.parse(shapesJson);

  return (...args) => callbackReference.invokeMethodAsync('Invoke', ...shapes.map((shape, index) => toDotNet(args[index], shape)));
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

  if (isKnownObjectShape(shape)) {
    return toKnownName(value, shape.$known);
  }

  const copy: Record<string, unknown> = {};
  for (const [name, propertyShape] of Object.entries(shape)) {
    copy[name] = toDotNet((value as Record<string, unknown>)[name], propertyShape);
  }

  return copy;
}

// Web IDL dictionaries and sequences arrive as plain objects and arrays, so they are copied.
// Any other object needs a shape that says how to send it. Without one it becomes a marker with its type name,
// because a reference created here would have no owner to dispose it.
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

  return { __unsupported: typeNameOf(value) };
}

function isKnownObjectShape(shape: Exclude<ArgumentShape, null>): shape is KnownObjectShape {
  return Array.isArray((shape as Partial<KnownObjectShape>).$known);
}

function toKnownName(value: unknown, names: string[]): unknown {
  for (const name of names) {
    try {
      if ((globalThis as Record<string, unknown>)[name] === value) {
        return name;
      }
    } catch {
      // Reading a global such as localStorage throws when the browser blocks it, so it cannot be the value.
    }
  }

  return toTransferable(value);
}

function typeNameOf(value: object): string {
  return (value as { constructor?: { name?: string } }).constructor?.name
    || Object.prototype.toString.call(value).slice('[object '.length, -1);
}
