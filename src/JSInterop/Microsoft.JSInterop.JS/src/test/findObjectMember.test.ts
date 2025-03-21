import { expect } from '@jest/globals';
import { DotNet } from "../src/Microsoft.JSInterop";

describe('findObjectMember', () => {
    let objectId: number;

    beforeAll(() => {
        objectId = DotNet.createJSObjectReference({
            a: {
                b: {
                    c: 42,
                    d: function () { return 'hello'; },
                    e: class { constructor() { } }
                }
            }
        })["__jsObjectId"];
    });

    test('resolves data member', () => {
        const result = DotNet.findObjectMember('a.b.c', objectId);
        expect(result).toEqual({ parent: { c: 42, d: expect.any(Function), e: expect.any(Function) }, name: 'c', func: undefined });
    });

    test('resolves function member', () => {
        const result = DotNet.findObjectMember('a.b.d', objectId);
        expect(result).toEqual({ parent: { c: 42, d: expect.any(Function), e: expect.any(Function) }, name: 'd', func: expect.any(Function) });
    });

    test('resolves constructor function member', () => {
        const result = DotNet.findObjectMember('a.b.e', objectId);
        expect(result).toEqual({ parent: { c: 42, d: expect.any(Function), e: expect.any(Function) }, name: 'e', func: expect.any(Function) });
    });

    test('resolves property member', () => {
        const result = DotNet.findObjectMember('a.b', objectId);
        expect(result).toEqual({ parent: { b: { c: 42, d: expect.any(Function), e: expect.any(Function) } }, name: 'b', func: undefined });
    });

    test('resolves undefined member', () => {
        const result = DotNet.findObjectMember('a.b.c.f', objectId);
        expect(result).toEqual({ parent: 42, name: 'f', func: undefined });
    });

    test('throws error for non-existent instance ID', () => {
        expect(() => DotNet.findObjectMember('a.b.c', 999)).toThrow('JS object instance with ID 999 does not exist (has it been disposed?).');
    });
});

describe('findObjectMember with window object', () => {
    test('resolves document.title', () => {
        document.title = 'Test Title';
        const result = DotNet.findObjectMember('document.title', 0);
        expect(result).toEqual({ parent: document, name: 'title', func: undefined });
    });

    test('resolves window.location', () => {
        const result = DotNet.findObjectMember('location', 0);
        expect(result).toEqual({ parent: expect.any(Object), name: 'location', func: undefined });
    });

    test('resolves window.alert', () => {
        const result = DotNet.findObjectMember('alert', 0);
        expect(result).toEqual({ parent: expect.any(Object), name: 'alert', func: expect.any(Function) });
    });

    test('resolves undefined for non-existent window member', () => {
        const result = DotNet.findObjectMember('nonExistentMember', 0);
        expect(result).toEqual({ parent: expect.any(Object), name: 'nonExistentMember', func: undefined });
    });
});