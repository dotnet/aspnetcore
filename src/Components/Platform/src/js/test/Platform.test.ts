// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { createCallback, DotNetObject, toDotNet } from '../Platform';

// Browser objects such as events expose their members as getters on the prototype.
class BrowserObject {
  get name(): string {
    return 'browser';
  }

  get child(): BrowserObject | null {
    return null;
  }
}

function createTarget(): DotNetObject & { calls: unknown[][] } {
  const calls: unknown[][] = [];
  return {
    calls,
    invokeMethodAsync: <T>(methodIdentifier: string, ...args: unknown[]) => {
      calls.push([methodIdentifier, ...args]);
      return Promise.resolve('result' as T);
    },
  };
}

describe('createCallback', () => {
  test('forwards every argument that has a shape to Invoke', async () => {
    const target = createTarget();
    const callback = createCallback(target, '[null, null, null]');

    const result = await callback('a', 2, { three: 3 });

    expect(result).toBe('result');
    expect(target.calls).toEqual([['Invoke', 'a', 2, { three: 3 }]]);
  });

  test('drops arguments beyond the shapes and sends null for missing ones', async () => {
    const target = createTarget();

    await createCallback(target, '[null]')('a', 'b');
    await createCallback(target, '[null, null]')('a');

    expect(target.calls).toEqual([['Invoke', 'a'], ['Invoke', 'a', undefined]]);
    expect(JSON.stringify(target.calls[1])).toBe('["Invoke","a",null]');
  });

  test('copies only the shaped properties of an event', async () => {
    const target = createTarget();
    const event = new Event('change');
    Object.defineProperty(event, 'changed', { get: () => [{ name: 'a', value: '1' }] });
    Object.defineProperty(event, 'deleted', { get: () => [{ name: 'b' }] });

    await createCallback(target, '[{"changed":null,"deleted":null}]')(event);

    expect(JSON.stringify(target.calls[0])).toBe('["Invoke",{"changed":[{"name":"a","value":"1"}],"deleted":[{"name":"b"}]}]');
  });
});

describe('toDotNet', () => {
  test('copies primitives, plain objects, and arrays', () => {
    expect(toDotNet(null, null)).toBeNull();
    expect(toDotNet('text', null)).toBe('text');
    expect(toDotNet({ a: [1, { b: true }] }, null)).toEqual({ a: [1, { b: true }] });
  });

  test('sends a marker with the type name for objects that are not plain', () => {
    const value = toDotNet({ port: new BrowserObject(), dates: [new Date(0)], callback: () => 1 }, null);

    expect(value).toEqual({
      port: { __unsupported: 'BrowserObject' },
      dates: [{ __unsupported: 'Date' }],
      callback: { __unsupported: 'Function' },
    });
  });

  test('applies nested shapes and applies a shape to each array item', () => {
    const shape = { name: null, child: { name: null } };

    expect(toDotNet(new BrowserObject(), shape)).toEqual({ name: 'browser', child: null });
    expect(toDotNet([new BrowserObject()], { name: null })).toEqual([{ name: 'browser' }]);
  });

  test('sends an empty object for an empty shape', () => {
    expect(toDotNet(new BrowserObject(), {})).toEqual({});
    expect(toDotNet(undefined, {})).toBeNull();
  });
});
