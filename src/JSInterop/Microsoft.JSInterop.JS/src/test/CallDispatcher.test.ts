import { expect } from "@jest/globals";
import { DotNet } from "../src/Microsoft.JSInterop";

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

    test("NewCall: Constructor function is invoked and returns reference to new object", () => {
        window["testCtor"] = function () { this.a = 10; };

        const result = dispatcher.invokeJSFromDotNet(
            "testCtor",
            "[]",
            DotNet.JSCallResultType.JSObjectReference,
            0,
            DotNet.JSCallType.NewCall
        );

        expect(result).toMatch("__jsObjectId");
    });

    test("NewCall: Class constructor is invoked and returns reference to the new instance", () => {
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
            DotNet.JSCallType.NewCall
        );

        expect(result).toMatch("__jsObjectId");
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

    test("NewCall + GetValue: Class constructor is invoked and the new instance value is retrieved", () => {
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
            DotNet.JSCallType.NewCall
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

    test("NewCall + FunctionCall: Class constructor is invoked and method is invoked on the new instance", () => {
        const TestClass = class {
            f() { return 30; }
        };
        const objectId = getObjectReferenceId({ TestClass });

        const result = dispatcher.invokeJSFromDotNet(
            "TestClass",
            "[]",
            DotNet.JSCallResultType.JSObjectReference,
            objectId,
            DotNet.JSCallType.NewCall
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
});