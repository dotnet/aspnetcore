import { expect } from "@jest/globals";
import { DotNet } from "../src/Microsoft.JSInterop";

describe("findObjectMember", () => {
    let objectId: number;

    beforeAll(() => {
        objectId = DotNet.createJSObjectReference({
            a: {
                b: {
                    c: 42,
                    d: function () { return "hello"; },
                    e: class { constructor() { } }
                }
            }
        })["__jsObjectId"];
    });

    test("Resolves data member", () => {
        const result = DotNet.findObjectMember("a.b.c", objectId);
        expect(result).toEqual({ parent: { c: 42, d: expect.any(Function), e: expect.any(Function) }, name: "c", func: undefined });
    });

    test("Resolves function member", () => {
        const result = DotNet.findObjectMember("a.b.d", objectId);
        expect(result).toEqual({ parent: { c: 42, d: expect.any(Function), e: expect.any(Function) }, name: "d", func: expect.any(Function) });
    });

    test("Resolves constructor function member", () => {
        const result = DotNet.findObjectMember("a.b.e", objectId);
        expect(result).toEqual({ parent: { c: 42, d: expect.any(Function), e: expect.any(Function) }, name: "e", func: expect.any(Function) });
    });

    test("Resolves property member", () => {
        const result = DotNet.findObjectMember("a.b", objectId);
        expect(result).toEqual({ parent: { b: { c: 42, d: expect.any(Function), e: expect.any(Function) } }, name: "b", func: undefined });
    });

    test("Resolves undefined member", () => {
        const result = DotNet.findObjectMember("a.b.c.f", objectId);
        expect(result).toEqual({ parent: 42, name: "f", func: undefined });
    });

    test("Throws error for non-existent instance ID", () => {
        expect(() => DotNet.findObjectMember("a.b.c", 999)).toThrow("JS object instance with ID 999 does not exist (has it been disposed?).");
    });
});

describe("findObjectMember with window object", () => {
    test("Resolves window.location", () => {
        const result = DotNet.findObjectMember("location", 0);
        expect(result).toEqual({ parent: expect.any(Object), name: "location", func: undefined });
    });

    test("Resolves document.title", () => {
        document.title = "Test Title";
        const result = DotNet.findObjectMember("document.title", 0);
        expect(result).toEqual({ parent: document, name: "title", func: undefined });
    });

    test("Resolves undefined for non-existent window member", () => {
        const result = DotNet.findObjectMember("nonExistentMember", 0);
        expect(result).toEqual({ parent: expect.any(Object), name: "nonExistentMember", func: undefined });
    });
});