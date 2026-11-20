import { beforeEach, describe, expect, jest, test } from "@jest/globals";
import * as DotNet from "../../src/JSInterop/Microsoft.JSInterop";

const jsObjectId = "__jsObjectId";
let lastAsyncResult: null | { callId: number, succeeded: boolean, resultOrError: any } = null;
const dotNetCallDispatcher: DotNet.DotNetCallDispatcher = {
    beginInvokeDotNetFromJS: function (callId: number, assemblyName: string | null, methodIdentifier: string, dotNetObjectId: number | null, argsJson: string): void { },
    sendByteArray: function (id: number, data: Uint8Array): void { },
    endInvokeJSFromDotNet: function (callId: number, succeeded: boolean, resultOrError: any): void {
        lastAsyncResult = { callId, succeeded, resultOrError };
    },
};
const dispatcher: DotNet.ICallDispatcher = DotNet.attachDispatcher(dotNetCallDispatcher);
const getObjectReferenceId = (obj: any) => DotNet.createJSObjectReference(obj)[jsObjectId];

describe("CallDispatcher", () => {
    describe("asynchronous failure payload", () => {
        beforeEach(() => { lastAsyncResult = null; });

        describe.each(["throw", "reject"])("%s", (completion) => {
            test.each<[string, () => Error]>([
                ["TypeError", () => new TypeError("Wrong type")],
                ["AbortError", () => new DOMException("Aborted", "AbortError")],
                ["custom name", () => Object.assign(new Error("Custom failure"), { name: "MyLibraryError" })],
                ["missing stack", () => Object.assign(new Error("No stack"), { stack: undefined })],
            ])("preserves Error names and diagnostics: %s", async (_description, createError) => {
                const error = createError();
                const testFunc = () => {
                    if (completion === "throw") {
                        throw error;
                    }
                    return Promise.reject(error);
                };
                const objectId = getObjectReferenceId({ testFunc });
                const formattedMessage = `${error.message}\n${error.stack}`;

                await dispatcher.beginInvokeJSFromDotNet(123, "testFunc", "[]", DotNet.JSCallResultType.Default, objectId, DotNet.JSCallType.FunctionCall);

                expect(lastAsyncResult).toStrictEqual({
                    callId: 123,
                    succeeded: false,
                    resultOrError: JSON.stringify([123, false, { name: error.name, formattedMessage }]),
                });
            });
        });

        test.each(["throw", "reject"])("keeps non-Error values as legacy strings for %s", async (completion) => {
            const thrownObject = { toString: () => "Custom object", toJSON: () => { throw new Error("Must not serialize thrown objects"); } };
            for (const [error, expected] of [["name:AbortError\nplain text", "name:AbortError\nplain text"], [null, "null"], [thrownObject, "Custom object"]]) {
                const testFunc = () => {
                    if (completion === "throw") {
                        throw error;
                    }
                    return Promise.reject(error);
                };
                const objectId = getObjectReferenceId({ testFunc });

                await dispatcher.beginInvokeJSFromDotNet(123, "testFunc", "[]", DotNet.JSCallResultType.Default, objectId, DotNet.JSCallType.FunctionCall);

                expect(lastAsyncResult).toStrictEqual({ callId: 123, succeeded: false, resultOrError: JSON.stringify([123, false, expected]) });
            }
        });

        test("does not serialize extra Error properties or a non-string name", async () => {
            const error = new Error("Failure");
            Object.defineProperty(error, "name", { value: { toJSON: () => { throw new Error("Must not serialize the name"); } } });
            Object.defineProperty(error, "extra", { enumerable: true, get: () => { throw new Error("Must not enumerate the error"); } });
            error.stack = "Original stack";
            const objectId = getObjectReferenceId({ testFunc: () => { throw error; } });

            await dispatcher.beginInvokeJSFromDotNet(123, "testFunc", "[]", DotNet.JSCallResultType.Default, objectId, DotNet.JSCallType.FunctionCall);

            expect(lastAsyncResult).toStrictEqual({ callId: 123, succeeded: false, resultOrError: JSON.stringify([123, false, { name: null, formattedMessage: "Failure\nOriginal stack" }]) });
        });

        test("delivers the legacy failure when the name getter throws", async () => {
            const error = new Error("Original message");
            error.stack = "Original stack";
            Object.defineProperty(error, "name", { get: () => { throw new Error("Name getter failed"); } });
            const endInvokeJSFromDotNet = jest.fn<DotNet.DotNetCallDispatcher["endInvokeJSFromDotNet"]>();
            const testDispatcher = DotNet.attachDispatcher({ ...dotNetCallDispatcher, endInvokeJSFromDotNet });
            const objectId = getObjectReferenceId({ testFunc: () => { throw error; } });

            await expect(testDispatcher.beginInvokeJSFromDotNet(456, "testFunc", "[]", DotNet.JSCallResultType.Default, objectId, DotNet.JSCallType.FunctionCall)).resolves.toBeUndefined();

            expect(endInvokeJSFromDotNet).toHaveBeenCalledTimes(1);
            expect(endInvokeJSFromDotNet.mock.calls).toStrictEqual([[456, false, JSON.stringify([456, false, "Original message\nOriginal stack"])]]);
        });

        test("reads the name getter once and transmits its string value", async () => {
            const error = new Error("Original message");
            error.stack = "Original stack";
            let nameReads = 0;
            Object.defineProperty(error, "name", { get: () => { nameReads++; return "MyLibraryError"; } });
            const endInvokeJSFromDotNet = jest.fn<DotNet.DotNetCallDispatcher["endInvokeJSFromDotNet"]>();
            const testDispatcher = DotNet.attachDispatcher({ ...dotNetCallDispatcher, endInvokeJSFromDotNet });
            const objectId = getObjectReferenceId({ testFunc: () => { throw error; } });

            await testDispatcher.beginInvokeJSFromDotNet(789, "testFunc", "[]", DotNet.JSCallResultType.Default, objectId, DotNet.JSCallType.FunctionCall);

            expect(nameReads).toBe(1);
            expect(endInvokeJSFromDotNet).toHaveBeenCalledTimes(1);
            expect(endInvokeJSFromDotNet.mock.calls).toStrictEqual([[789, false, JSON.stringify([789, false, { name: "MyLibraryError", formattedMessage: "Original message\nOriginal stack" }])]]);
        });

        test("propagates a failed error completion without retrying it", async () => {
            const transportError = new Error("Completion transport failed");
            const endInvokeJSFromDotNet = jest.fn<DotNet.DotNetCallDispatcher["endInvokeJSFromDotNet"]>(() => { throw transportError; });
            const testDispatcher = DotNet.attachDispatcher({ ...dotNetCallDispatcher, endInvokeJSFromDotNet });
            const objectId = getObjectReferenceId({ testFunc: () => { throw new Error("Original failure"); } });

            await expect(testDispatcher.beginInvokeJSFromDotNet(123, "testFunc", "[]", DotNet.JSCallResultType.Default, objectId, DotNet.JSCallType.FunctionCall)).rejects.toBe(transportError);

            expect(endInvokeJSFromDotNet).toHaveBeenCalledTimes(1);
        });

        test("checks a thrown proxy prototype once and delivers its original failure string", async () => {
            let prototypeLookups = 0;
            const error = new Proxy({ toString: () => "Original failure" }, {
                getPrototypeOf: (target) => {
                    prototypeLookups++;
                    if (prototypeLookups > 1) {
                        throw new Error("Prototype looked up twice");
                    }
                    return Reflect.getPrototypeOf(target);
                },
            });
            const endInvokeJSFromDotNet = jest.fn<DotNet.DotNetCallDispatcher["endInvokeJSFromDotNet"]>();
            const testDispatcher = DotNet.attachDispatcher({ ...dotNetCallDispatcher, endInvokeJSFromDotNet });
            const objectId = getObjectReferenceId({ testFunc: () => { throw error; } });

            await expect(testDispatcher.beginInvokeJSFromDotNet(246, "testFunc", "[]", DotNet.JSCallResultType.Default, objectId, DotNet.JSCallType.FunctionCall)).resolves.toBeUndefined();

            expect(prototypeLookups).toBe(1);
            expect(endInvokeJSFromDotNet).toHaveBeenCalledTimes(1);
            expect(endInvokeJSFromDotNet.mock.calls).toStrictEqual([[246, false, JSON.stringify([246, false, "Original failure"])]]);
        });

        test("preserves successful completions", async () => {
            const objectId = getObjectReferenceId({ testFunc: () => Promise.resolve({ value: 42 }) });

            await dispatcher.beginInvokeJSFromDotNet(123, "testFunc", "[]", DotNet.JSCallResultType.Default, objectId, DotNet.JSCallType.FunctionCall);

            expect(lastAsyncResult).toStrictEqual({ callId: 123, succeeded: true, resultOrError: '[123,true,{"value":42}]' });
        });

        test("does not send completions for zero handles", async () => {
            let calls = 0;
            const testFunc = () => { calls++; throw new TypeError("No response needed"); };
            const objectId = getObjectReferenceId({ testFunc });

            await dispatcher.beginInvokeJSFromDotNet(0, "testFunc", "[]", DotNet.JSCallResultType.Default, objectId, DotNet.JSCallType.FunctionCall);

            expect(calls).toBe(1);
            expect(lastAsyncResult).toBeNull();
        });

        test("does not serialize successful results for zero handles", async () => {
            const value = { toJSON: () => { throw new Error("Must not serialize the result"); } };
            const objectId = getObjectReferenceId({ testFunc: () => value });

            await dispatcher.beginInvokeJSFromDotNet(0, "testFunc", "[]", DotNet.JSCallResultType.Default, objectId, DotNet.JSCallType.FunctionCall);

            expect(lastAsyncResult).toBeNull();
        });

        test("leaves synchronous in-process exceptions unchanged", () => {
            const error = new TypeError("Synchronous failure");
            const objectId = getObjectReferenceId({ testFunc: () => { throw error; } });

            expect(() => dispatcher.invokeJSFromDotNet("testFunc", "[]", DotNet.JSCallResultType.Default, objectId, DotNet.JSCallType.FunctionCall)).toThrow(error);
            expect(lastAsyncResult).toBeNull();
        });
    });

    test("FunctionCall: Function with no arguments is invoked and returns value", () => {
        const testFunc = () => 1;
        const objectId = getObjectReferenceId({ testFunc });

        const result = dispatcher.invokeJSFromDotNet(
            "testFunc",
            "[]",
            DotNet.JSCallResultType.Default,
            objectId,
            DotNet.JSCallType.FunctionCall
        );
        expect(result).toBe("1");
    });

    test("FunctionCall: Function with arguments is invoked and returns value", () => {
        const testFunc = (a: number, b: number) => a + b;
        const objectId = getObjectReferenceId({ testFunc });

        const result = dispatcher.invokeJSFromDotNet(
            "testFunc",
            JSON.stringify([1, 2]),
            DotNet.JSCallResultType.Default,
            objectId,
            DotNet.JSCallType.FunctionCall
        );

        expect(result).toBe("3");
    });

    test("FunctionCall: Non-function value is invoked and throws", () => {
        const objectId = getObjectReferenceId({ x: 1 });

        expect(() => dispatcher.invokeJSFromDotNet(
            "x",
            "[]",
            DotNet.JSCallResultType.Default,
            objectId,
            DotNet.JSCallType.FunctionCall
        )).toThrowError("The value 'x' is not a function.");
    });

    test("FunctionCall: Function is invoked via async interop and returns value", () => {
        const testFunc = (a: number, b: number) => a + b;
        const objectId = getObjectReferenceId({ testFunc });

        const promise = dispatcher.beginInvokeJSFromDotNet(
            1,
            "testFunc",
            JSON.stringify([1, 2]),
            DotNet.JSCallResultType.Default,
            objectId,
            DotNet.JSCallType.FunctionCall
        );

        promise?.then(() => {
            expect(lastAsyncResult).toStrictEqual({ callId: 1, succeeded: true, resultOrError: "[1,true,3]" });
        });
    });

    test("FunctionCall: Non-function value is invoked via async interop and throws", () => {
        const objectId = getObjectReferenceId({ x: 1 });

        const promise = dispatcher.beginInvokeJSFromDotNet(
            1,
            "x",
            "[]",
            DotNet.JSCallResultType.Default,
            objectId,
            DotNet.JSCallType.FunctionCall
        );

        promise?.then(() => {
            expect(lastAsyncResult?.succeeded).toBe(false);
            expect(lastAsyncResult?.resultOrError).toMatch("The value 'x' is not a function.");
        });
    });

    test("FunctionCall: should handle functions that throw errors", () => {
        const testFunc = () => { throw new Error("Test error"); };
        const objectId = getObjectReferenceId({ testFunc });

        expect(() => dispatcher.invokeJSFromDotNet(
            "testFunc",
            "[]",
            DotNet.JSCallResultType.Default,
            objectId,
            DotNet.JSCallType.FunctionCall
        )).toThrowError("Test error");
    });

    test("ConstructorCall: Constructor function is invoked and returns reference to new object", () => {
        window["testCtor"] = function (this: { a: number }) { this.a = 10; };

        const result = dispatcher.invokeJSFromDotNet(
            "testCtor",
            "[]",
            DotNet.JSCallResultType.JSObjectReference,
            0,
            DotNet.JSCallType.ConstructorCall
        );

        expect(result).toMatch("__jsObjectId");
    });

    test("ConstructorCall: Class constructor is invoked and returns reference to the new instance", () => {
        const TestClass = class {
            a: number;
            constructor() { this.a = 10; }
        };
        const objectId = getObjectReferenceId({ TestClass });

        const result = dispatcher.invokeJSFromDotNet(
            "TestClass",
            "[]",
            DotNet.JSCallResultType.JSObjectReference,
            objectId,
            DotNet.JSCallType.ConstructorCall
        );

        expect(result).toMatch("__jsObjectId");
    });

    test("FunctionCall: URLSearchParams record keys survive argument revivers", () => {
        let receivedEntries: string[][] | undefined;
        const entries = [["hasOwnProperty", "property"], ["__jsObjectId", "value"]];
        window["testFunc"] = function (args: string[][]) {
            receivedEntries = args;
        };
        dispatcher.invokeJSFromDotNet(
            "testFunc",
            JSON.stringify([entries]),
            DotNet.JSCallResultType.JSVoidResult,
            0,
            DotNet.JSCallType.FunctionCall
        );

        expect(receivedEntries).toEqual(entries);
    });

    test("GetValue: Simple property value is retrieved", () => {
        const testObject = { a: 10 };
        const objectId = getObjectReferenceId(testObject);

        const result = dispatcher.invokeJSFromDotNet(
            "a",
            "[]",
            DotNet.JSCallResultType.Default,
            objectId,
            DotNet.JSCallType.GetValue
        );

        expect(result).toBe("10");
    });

    test("GetValue: Nested property value is retrieved", () => {
        const testObject = { a: { b: 20 } };
        const objectId = getObjectReferenceId(testObject);

        const result = dispatcher.invokeJSFromDotNet(
            "a.b",
            "[]",
            DotNet.JSCallResultType.Default,
            objectId,
            DotNet.JSCallType.GetValue
        );

        expect(result).toBe("20");
    });

    test("GetValue: Property defined on prototype is retrieved", () => {
        const grandParentPrototype = { a: 30 };
        const parentPrototype = Object.create(grandParentPrototype);
        const testObject = Object.create(parentPrototype);
        const objectId = getObjectReferenceId(testObject);

        const result = dispatcher.invokeJSFromDotNet(
            "a",
            "[]",
            DotNet.JSCallResultType.Default,
            objectId,
            DotNet.JSCallType.GetValue
        );

        expect(result).toBe("30");
    });

    test("GetValue: Reading undefined property throws", () => {
        const testObject = { a: 10 };
        const objectId = getObjectReferenceId(testObject);

        expect(() => dispatcher.invokeJSFromDotNet(
            "b",
            "[]",
            DotNet.JSCallResultType.Default,
            objectId,
            DotNet.JSCallType.GetValue
        )).toThrowError("The property 'b' is not defined or is not readable.");
    });

    test("GetValue: Object reference is retrieved", () => {
        const testObject = { a: { b: 20 } };
        const objectId = getObjectReferenceId(testObject);

        const result = dispatcher.invokeJSFromDotNet(
            "a",
            "[]",
            DotNet.JSCallResultType.JSObjectReference,
            objectId,
            DotNet.JSCallType.GetValue
        );

        expect(result).toMatch("__jsObjectId");
    });

    test("GetValue: Reading from setter-only property throws", () => {
        const testObject = { set a(_: any) { } };
        const objectId = getObjectReferenceId(testObject);

        expect(() => dispatcher.invokeJSFromDotNet(
            "a",
            "[]",
            DotNet.JSCallResultType.Default,
            objectId,
            DotNet.JSCallType.GetValue
        )).toThrowError("The property 'a' is not defined or is not readable");
    });

    test("SetValue: Simple property is updated", () => {
        const testObject = { a: 10 };
        const objectId = getObjectReferenceId(testObject);

        dispatcher.invokeJSFromDotNet(
            "a",
            JSON.stringify([20]),
            DotNet.JSCallResultType.JSVoidResult,
            objectId,
            DotNet.JSCallType.SetValue
        );

        expect(testObject.a).toBe(20);
    });

    test("SetValue: Nested property is updated", () => {
        const testObject = { a: { b: 10 } };
        const objectId = getObjectReferenceId(testObject);

        dispatcher.invokeJSFromDotNet(
            "a.b",
            JSON.stringify([20]),
            DotNet.JSCallResultType.JSVoidResult,
            objectId,
            DotNet.JSCallType.SetValue
        );

        expect(testObject.a.b).toBe(20);
    });

    test("SetValue: Undefined property can be set", () => {
        const testObject = { a: 10 };
        const objectId = getObjectReferenceId(testObject);

        dispatcher.invokeJSFromDotNet(
            "b",
            JSON.stringify([30]),
            DotNet.JSCallResultType.JSVoidResult,
            objectId,
            DotNet.JSCallType.SetValue
        );

        expect((testObject as any).b).toBe(30);
    });

    test("SetValue: Writing to getter-only property throws", () => {
        const testObject = { get a() { return 10; } };
        const objectId = getObjectReferenceId(testObject);

        expect(() => dispatcher.invokeJSFromDotNet(
            "a",
            JSON.stringify([20]),
            DotNet.JSCallResultType.JSVoidResult,
            objectId,
            DotNet.JSCallType.SetValue
        )).toThrowError("The property 'a' is not writable.");
    });

    test("SetValue: Writing to non-writable data property throws", () => {
        const testObject = Object.create({}, { a: { value: 10, writable: false } });
        const objectId = getObjectReferenceId(testObject);

        expect(() => dispatcher.invokeJSFromDotNet(
            "a",
            JSON.stringify([20]),
            DotNet.JSCallResultType.JSVoidResult,
            objectId,
            DotNet.JSCallType.SetValue
        )).toThrowError("The property 'a' is not writable.");
    });

    test("SetValue + GetValue: Updated primitive value is read", () => {
        const testObject = { a: 10 };
        const objectId = getObjectReferenceId(testObject);

        dispatcher.invokeJSFromDotNet(
            "a",
            JSON.stringify([20]),
            DotNet.JSCallResultType.JSVoidResult,
            objectId,
            DotNet.JSCallType.SetValue
        );

        const result = dispatcher.invokeJSFromDotNet(
            "a",
            "[]",
            DotNet.JSCallResultType.Default,
            objectId,
            DotNet.JSCallType.GetValue
        );

        expect(result).toBe("20");
    });

    test("SetValue + GetValue: Updated object value is read", () => {
        const objA = {};
        const objARef = DotNet.createJSObjectReference(objA);
        const objB = { x: 30 };
        const objBRef = DotNet.createJSObjectReference(objB);

        dispatcher.invokeJSFromDotNet(
            "b",
            JSON.stringify([objBRef]),
            DotNet.JSCallResultType.JSVoidResult,
            objARef[jsObjectId],
            DotNet.JSCallType.SetValue
        );

        const result = dispatcher.invokeJSFromDotNet(
            "b.x",
            "[]",
            DotNet.JSCallResultType.Default,
            objARef[jsObjectId],
            DotNet.JSCallType.GetValue
        );

        expect(result).toBe("30");
    });

    test("ConstructorCall + GetValue: Class constructor is invoked and the new instance value is retrieved", () => {
        const TestClass = class {
            a: number;
            constructor() { this.a = 20; }
        };
        const objectId = getObjectReferenceId({ TestClass });

        const result = dispatcher.invokeJSFromDotNet(
            "TestClass",
            "[]",
            DotNet.JSCallResultType.JSObjectReference,
            objectId,
            DotNet.JSCallType.ConstructorCall
        );
        const newObjectId = JSON.parse(result ?? "")[jsObjectId];

        const result2 = dispatcher.invokeJSFromDotNet(
            "a",
            "[]",
            DotNet.JSCallResultType.Default,
            newObjectId,
            DotNet.JSCallType.GetValue
        );

        expect(result2).toBe("20");
    });

    test("ConstructorCall + FunctionCall: Class constructor is invoked and method is invoked on the new instance", () => {
        const TestClass = class {
            f() { return 30; }
        };
        const objectId = getObjectReferenceId({ TestClass });

        const result = dispatcher.invokeJSFromDotNet(
            "TestClass",
            "[]",
            DotNet.JSCallResultType.JSObjectReference,
            objectId,
            DotNet.JSCallType.ConstructorCall
        );
        const newObjectId = JSON.parse(result ?? "")[jsObjectId];

        const result2 = dispatcher.invokeJSFromDotNet(
            "f",
            "[]",
            DotNet.JSCallResultType.Default,
            newObjectId,
            DotNet.JSCallType.FunctionCall
        );

        expect(result2).toBe("30");
    });

    test("createJSObjectReference: Handles null values without throwing", () => {
        const nullRef = DotNet.createJSObjectReference(null);
        expect(nullRef).toEqual({ [jsObjectId]: -1 });
    });

    test("createJSObjectReference: Handles undefined values without throwing", () => {
        const undefinedRef = DotNet.createJSObjectReference(undefined);
        expect(undefinedRef).toEqual({ [jsObjectId]: -1 });
    });

    test("disposeJSObjectReference: Safely handles null reference disposal", () => {
        const nullRef = DotNet.createJSObjectReference(null);
        expect(() => DotNet.disposeJSObjectReference(nullRef)).not.toThrow();
    });

    test("createJSObjectReference: Still throws for invalid types", () => {
        expect(() => DotNet.createJSObjectReference("string")).toThrow();
        expect(() => DotNet.createJSObjectReference(123)).toThrow();
        expect(() => DotNet.createJSObjectReference(true)).toThrow();
    });

    test("GetValue: Returns JSObjectReference with sentinel value for null property", () => {
        const testObject = { nullProp: null };
        const objectId = getObjectReferenceId(testObject);

        const result = dispatcher.invokeJSFromDotNet(
            "nullProp",
            "[]",
            DotNet.JSCallResultType.JSObjectReference,
            objectId,
            DotNet.JSCallType.GetValue
        );

        expect(result).toBe('{"__jsObjectId":-1}');
    });

    test("GetValue: Returns JSObjectReference with sentinel value for undefined property", () => {
        const testObject = { undefinedProp: undefined };
        const objectId = getObjectReferenceId(testObject);

        const result = dispatcher.invokeJSFromDotNet(
            "undefinedProp",
            "[]",
            DotNet.JSCallResultType.JSObjectReference,
            objectId,
            DotNet.JSCallType.GetValue
        );

        expect(result).toBe('{"__jsObjectId":-1}');
    });
});
