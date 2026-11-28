// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Specifies which data from one argument is sent to .NET. When null, the whole value is sent. An object sends only the named
// properties, each with its own shape, so that a browser object such as an event can be copied.
// { $known: [...] } sends the name of the global that is the value, such as "localStorage".
// { $json: true } sends validated JSON as text, or a projection error for the listener's .NET logging path.
export type ArgumentShape = null | KnownObjectShape | JsonShape | { [property: string]: ArgumentShape };

interface JsonShape {
  $json: true;
}

interface KnownObjectShape {
  $known: string[];
}

export interface DotNetObject {
  invokeMethodAsync<T>(methodIdentifier: string, ...args: unknown[]): Promise<T>;
}

// Creates a JS function that calls the .NET Invoke method of the callback reference with one argument per shape.
// Arguments beyond the shapes are dropped. The returned promise settles when the .NET method completes.
// E.g. [{ key: null }, null] + [{ key: "theme", oldValue: "light" }, "hello", 123] → Invoke({ key: "theme" }, "hello")
export function createCallback(callbackReference: DotNetObject, shapesJson: string): (...args: unknown[]) => Promise<unknown> {
  const shapes: ArgumentShape[] = JSON.parse(shapesJson);

  return (...args) => callbackReference.invokeMethodAsync('Invoke', ...shapes.map((shape, index) => toDotNet(args[index], shape)));
}

export function toDotNet(value: unknown, shape: ArgumentShape): unknown {
  if (shape === null) {
    return toTransferable(value);
  }

  if (isJsonShape(shape)) {
    try {
      return { json: JSON.stringify(copyJson(value, new Set())) };
    } catch (error) {
      // Send projection failures through the normal listener error logging path in .NET.
      return { error: error instanceof Error ? error.message : String(error) };
    }
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

  function copyJson(value: unknown, ancestors: Set<object>): unknown {
    if (value === null || typeof value === 'string' || typeof value === 'boolean') {
      return value;
    }

    if (typeof value === 'number' && Number.isFinite(value)) {
      return value;
    }

    if (typeof value !== 'object' || value === null ||
      (!Array.isArray(value) && Object.getPrototypeOf(value) !== Object.prototype && Object.getPrototypeOf(value) !== null)) {
      throw new TypeError('Received message contains a value that is not JSON.');
    }

    if (ancestors.has(value)) {
      throw new TypeError('Received message contains a cycle.');
    }

    ancestors.add(value);
    try {
      if (Array.isArray(value)) {
        if (Object.keys(value).length !== value.length) {
          throw new TypeError('Received message contains an array that is not JSON.');
        }

        return Array.from(value, item => copyJson(item, ancestors));
      }

      return Object.fromEntries(Object.entries(value).map(([name, item]) => [name, copyJson(item, ancestors)]));
    } finally {
      ancestors.delete(value);
    }
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

function isJsonShape(shape: Exclude<ArgumentShape, null>): shape is JsonShape {
  return '$json' in shape && shape.$json === true;
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
