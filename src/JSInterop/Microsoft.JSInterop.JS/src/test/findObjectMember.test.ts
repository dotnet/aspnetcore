import { expect } from "@jest/globals";
import { DotNet } from "../src/Microsoft.JSInterop";

describe("findObjectMember", () => {
    const testObject = {
        a: {
            b: {
                c: 42,
                d: function () { return "hello"; },
                e: class { constructor() { } }
            }
        }
    };

    test("Resolves nested member", () => {
        const result = DotNet.findObjectMember(testObject, "a.b.c");
        expect(result).toEqual([testObject.a.b, "c"]);
    });

    test("Resolves undefined last-level member", () => {
        const result = DotNet.findObjectMember(testObject, "a.f");
        expect(result).toEqual([testObject.a, "f"]);
    });

    test("Throws for undefined intermediate member", () => {
        expect(() => DotNet.findObjectMember(testObject, "a.f.g")).toThrow("Could not find 'a.f.g' ('f' was undefined).");
    });
});
