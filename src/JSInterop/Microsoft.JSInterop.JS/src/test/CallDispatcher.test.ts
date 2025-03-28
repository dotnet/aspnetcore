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
}
const dispatcher: DotNet.ICallDispatcher = DotNet.attachDispatcher(dotNetCallDispatcher);
const getObjectReferenceId = (obj: any) => DotNet.createJSObjectReference(obj)[jsObjectId];

describe("CallDispatcher", () => {
    test("FunctionCall: Function with no arguments is invoked and returns value", () => {
        const testFunc = jest.fn(() => 1);
        const objectId = getObjectReferenceId({ testFunc });

        const invocationInfo: DotNet.JSInvocationInfo = {
            asyncHandle: 0,
            targetInstanceId: objectId,
            identifier: "testFunc",
            callType: DotNet.JSCallType.FunctionCall,
            resultType: DotNet.JSCallResultType.Default,
            argsJson: null
        };

        const result = dispatcher.invokeJSFromDotNet(invocationInfo);
        expect(result).toBe("1");
        expect(testFunc).toHaveBeenCalled();
    });

    test("FunctionCall: Function with arguments is invoked and returns value", () => {
        const testFunc = jest.fn((a, b) => a + b);
        const objectId = getObjectReferenceId({ testFunc });

        const invocationInfo: DotNet.JSInvocationInfo = {
            asyncHandle: 0,
            targetInstanceId: objectId,
            identifier: "testFunc",
            callType: DotNet.JSCallType.FunctionCall,
            resultType: DotNet.JSCallResultType.Default,
            argsJson: JSON.stringify([1, 2])
        };

        const result = dispatcher.invokeJSFromDotNet(invocationInfo);

        expect(result).toBe("3");
        expect(testFunc).toHaveBeenCalledWith(1, 2);
    });

    test("FunctionCall: Non-function value is invoked and throws", () => {
        const objectId = getObjectReferenceId({ x: 1 });

        const invocationInfo: DotNet.JSInvocationInfo = {
            asyncHandle: 0,
            targetInstanceId: objectId,
            identifier: "x",
            callType: DotNet.JSCallType.FunctionCall,
            resultType: DotNet.JSCallResultType.Default,
            argsJson: null
        };

        expect(() => dispatcher.invokeJSFromDotNet(invocationInfo)).toThrowError("The value 'x' is not a function.");
    });

    test("FunctionCall: Function is invoked via async interop and returns value", () => {
        const testFunc = jest.fn((a, b) => a + b);
        const objectId = getObjectReferenceId({ testFunc });

        const invocationInfo: DotNet.JSInvocationInfo = {
            asyncHandle: 1,
            targetInstanceId: objectId,
            identifier: "testFunc",
            callType: DotNet.JSCallType.FunctionCall,
            resultType: DotNet.JSCallResultType.Default,
            argsJson: JSON.stringify([1, 2])
        };

        const promise = dispatcher.beginInvokeJSFromDotNet(invocationInfo);

        promise?.then(() => {
            expect(testFunc).toHaveBeenCalledWith(1, 2);
            expect(lastAsyncResult).toStrictEqual({ callId: 1, succeeded: true, resultOrError: "[1,true,3]" });
        });
    });

    test("FunctionCall: Non-function value is invoked via async interop and throws", () => {
        const objectId = getObjectReferenceId({ x: 1 });

        const invocationInfo: DotNet.JSInvocationInfo = {
            asyncHandle: 1,
            targetInstanceId: objectId,
            identifier: "x",
            callType: DotNet.JSCallType.FunctionCall,
            resultType: DotNet.JSCallResultType.Default,
            argsJson: null
        };

        const promise = dispatcher.beginInvokeJSFromDotNet(invocationInfo);

        promise?.then(() => {
            expect(lastAsyncResult?.succeeded).toBe(false);
            expect(lastAsyncResult?.resultOrError).toMatch("The value 'x' is not a function.");
        });
    });

    test("FunctionCall: should handle functions that throw errors", () => {
        const testFunc = jest.fn(() => { throw new Error("Test error"); });
        const objectId = getObjectReferenceId({ testFunc });

        const invocationInfo: DotNet.JSInvocationInfo = {
            asyncHandle: 0,
            targetInstanceId: objectId,
            identifier: "testFunc",
            callType: DotNet.JSCallType.FunctionCall,
            resultType: DotNet.JSCallResultType.Default,
            argsJson: null
        };

        expect(() => dispatcher.invokeJSFromDotNet(invocationInfo)).toThrowError("Test error");
    });

    test("NewCall: Constructor function is invoked and returns reference to new object", () => {
        window["testCtor"] = function () { this.a = 10; };

        const invocationInfo: DotNet.JSInvocationInfo = {
            asyncHandle: 0,
            targetInstanceId: 0,
            identifier: "testCtor",
            callType: DotNet.JSCallType.NewCall,
            resultType: DotNet.JSCallResultType.JSObjectReference,
            argsJson: null
        };

        const result = dispatcher.invokeJSFromDotNet(invocationInfo);

        expect(result).toMatch("__jsObjectId");
    });

    test("NewCall: Class constructor is invoked and returns reference to the new instance", () => {
        const TestClass = class {
            a: number;
            constructor() { this.a = 10; }
        };
        const objectId = getObjectReferenceId({ TestClass });

        const invocationInfo: DotNet.JSInvocationInfo = {
            asyncHandle: 0,
            targetInstanceId: objectId,
            identifier: "TestClass",
            callType: DotNet.JSCallType.NewCall,
            resultType: DotNet.JSCallResultType.JSObjectReference,
            argsJson: null
        };

        const result = dispatcher.invokeJSFromDotNet(invocationInfo);

        expect(result).toMatch("__jsObjectId");
    });

    test("GetValue: Simple property value is retrived", () => {
        const testObject = { a: 10 };
        const objectId = getObjectReferenceId(testObject);

        const invocationInfo: DotNet.JSInvocationInfo = {
            asyncHandle: 0,
            targetInstanceId: objectId,
            identifier: "a",
            callType: DotNet.JSCallType.GetValue,
            resultType: DotNet.JSCallResultType.Default,
            argsJson: null
        };

        const result = dispatcher.invokeJSFromDotNet(invocationInfo);

        expect(result).toBe("10");
    });

    test("GetValue: Nested property value is retrieved", () => {
        const testObject = { a: { b: 20 } };
        const objectId = getObjectReferenceId(testObject);

        const invocationInfo: DotNet.JSInvocationInfo = {
            asyncHandle: 0,
            targetInstanceId: objectId,
            identifier: "a.b",
            callType: DotNet.JSCallType.GetValue,
            resultType: DotNet.JSCallResultType.Default,
            argsJson: null
        };

        const result = dispatcher.invokeJSFromDotNet(invocationInfo);

        expect(result).toBe("20");
    });

    test("GetValue: Property defined on prototype is retrieved", () => {
        const grandParentPrototype = { a: 30 };
        const parentPrototype = Object.create(grandParentPrototype);
        const testObject = Object.create(parentPrototype);
        const objectId = getObjectReferenceId(testObject);

        const invocationInfo: DotNet.JSInvocationInfo = {
            asyncHandle: 0,
            targetInstanceId: objectId,
            identifier: "a",
            callType: DotNet.JSCallType.GetValue,
            resultType: DotNet.JSCallResultType.Default,
            argsJson: null
        };

        const result = dispatcher.invokeJSFromDotNet(invocationInfo);

        expect(result).toBe("30");
    });

    test("GetValue: Reading undefined property throws", () => {
        const testObject = { a: 10 };
        const objectId = getObjectReferenceId(testObject);

        const invocationInfo: DotNet.JSInvocationInfo = {
            asyncHandle: 0,
            targetInstanceId: objectId,
            identifier: "b",
            callType: DotNet.JSCallType.GetValue,
            resultType: DotNet.JSCallResultType.Default,
            argsJson: null
        };

        expect(() => dispatcher.invokeJSFromDotNet(invocationInfo)).toThrowError("The property 'b' is not defined or is not readable.");
    });

    test("GetValue: Object reference is retrieved", () => {
        const testObject = { a: { b: 20 } };
        const objectId = getObjectReferenceId(testObject);

        const invocationInfo: DotNet.JSInvocationInfo = {
            asyncHandle: 0,
            targetInstanceId: objectId,
            identifier: "a",
            callType: DotNet.JSCallType.GetValue,
            resultType: DotNet.JSCallResultType.JSObjectReference,
            argsJson: null
        };

        const result = dispatcher.invokeJSFromDotNet(invocationInfo);

        expect(result).toMatch("__jsObjectId");
    });

    test("GetValue: Object value is retrieved from reference with empty identifier", () => {
        const testObject = { a: { b: 20 } };
        const objectId = getObjectReferenceId(testObject);

        const invocationInfo: DotNet.JSInvocationInfo = {
            asyncHandle: 0,
            targetInstanceId: objectId,
            identifier: "",
            callType: DotNet.JSCallType.GetValue,
            resultType: DotNet.JSCallResultType.Default,
            argsJson: null
        };

        const result = dispatcher.invokeJSFromDotNet(invocationInfo);

        expect(result).toBe(JSON.stringify(testObject))
    });

    test("GetValue: Reading from setter-only property throws", () => {
        const testObject = { set a(_: any) { } };
        const objectId = getObjectReferenceId(testObject);

        const invocationInfo: DotNet.JSInvocationInfo = {
            asyncHandle: 0,
            targetInstanceId: objectId,
            identifier: "a",
            callType: DotNet.JSCallType.GetValue,
            resultType: DotNet.JSCallResultType.Default,
            argsJson: null
        };

        expect(() => dispatcher.invokeJSFromDotNet(invocationInfo)).toThrowError("The property 'a' is not defined or is not readable");
    });

    test("SetValue: Simple property is updated", () => {
        const testObject = { a: 10 };
        const objectId = getObjectReferenceId(testObject);

        const invocationInfo: DotNet.JSInvocationInfo = {
            asyncHandle: 0,
            targetInstanceId: objectId,
            identifier: "a",
            callType: DotNet.JSCallType.SetValue,
            resultType: DotNet.JSCallResultType.JSVoidResult,
            argsJson: JSON.stringify([20])
        };

        dispatcher.invokeJSFromDotNet(invocationInfo);

        expect(testObject.a).toBe(20);
    });

    test("SetValue: Nested property is updated", () => {
        const testObject = { a: { b: 10 } };
        const objectId = getObjectReferenceId(testObject);

        const invocationInfo: DotNet.JSInvocationInfo = {
            asyncHandle: 0,
            targetInstanceId: objectId,
            identifier: "a.b",
            callType: DotNet.JSCallType.SetValue,
            resultType: DotNet.JSCallResultType.JSVoidResult,
            argsJson: JSON.stringify([20])
        };

        dispatcher.invokeJSFromDotNet(invocationInfo);

        expect(testObject.a.b).toBe(20);
    });

    test("SetValue: Undefined property can be set", () => {
        const testObject = { a: 10 };
        const objectId = getObjectReferenceId(testObject);

        const invocationInfo: DotNet.JSInvocationInfo = {
            asyncHandle: 0,
            targetInstanceId: objectId,
            identifier: "b",
            callType: DotNet.JSCallType.SetValue,
            resultType: DotNet.JSCallResultType.JSVoidResult,
            argsJson: JSON.stringify([30])
        };

        dispatcher.invokeJSFromDotNet(invocationInfo);

        expect((testObject as any).b).toBe(30);
    });

    test("SetValue: Writing to getter-only property throws", () => {
        const testObject = { get a() { return 10; } };
        const objectId = getObjectReferenceId(testObject);

        const invocationInfo: DotNet.JSInvocationInfo = {
            asyncHandle: 0,
            targetInstanceId: objectId,
            identifier: "a",
            callType: DotNet.JSCallType.SetValue,
            resultType: DotNet.JSCallResultType.JSVoidResult,
            argsJson: JSON.stringify([20])
        };

        expect(() => dispatcher.invokeJSFromDotNet(invocationInfo)).toThrowError("The property 'a' is not writable.");
    });

    test("SetValue: Writing to non-writable data property throws", () => {
        const testObject = Object.create({}, { a: { value: 10, writable: false } });
        const objectId = getObjectReferenceId(testObject);

        const invocationInfo: DotNet.JSInvocationInfo = {
            asyncHandle: 0,
            targetInstanceId: objectId,
            identifier: "a",
            callType: DotNet.JSCallType.SetValue,
            resultType: DotNet.JSCallResultType.JSVoidResult,
            argsJson: JSON.stringify([20])
        };

        expect(() => dispatcher.invokeJSFromDotNet(invocationInfo)).toThrowError("The property 'a' is not writable.");
    });

    test("SetValue + GetValue: Updated primitive value is read", () => {
        const testObject = { a: 10 };
        const objectId = getObjectReferenceId(testObject);

        const invocationInfo: DotNet.JSInvocationInfo = {
            asyncHandle: 0,
            targetInstanceId: objectId,
            identifier: "a",
            callType: DotNet.JSCallType.SetValue,
            resultType: DotNet.JSCallResultType.JSVoidResult,
            argsJson: JSON.stringify([20])
        };

        dispatcher.invokeJSFromDotNet(invocationInfo);

        const invocationInfo2: DotNet.JSInvocationInfo = {
            asyncHandle: 0,
            targetInstanceId: objectId,
            identifier: "a",
            callType: DotNet.JSCallType.GetValue,
            resultType: DotNet.JSCallResultType.Default,
            argsJson: null
        };

        const result = dispatcher.invokeJSFromDotNet(invocationInfo2);

        expect(result).toBe("20");
    });

    test("SetValue + GetValue: Updated object value is read", () => {
        const objA = {};
        const objARef = DotNet.createJSObjectReference(objA);
        const objB = { x: 30 };
        const objBRef = DotNet.createJSObjectReference(objB);

        const invocationInfo: DotNet.JSInvocationInfo = {
            asyncHandle: 0,
            targetInstanceId: objARef[jsObjectId],
            identifier: "b",
            callType: DotNet.JSCallType.SetValue,
            resultType: DotNet.JSCallResultType.JSVoidResult,
            argsJson: JSON.stringify([objBRef])
        };

        dispatcher.invokeJSFromDotNet(invocationInfo);

        const invocationInfo2: DotNet.JSInvocationInfo = {
            asyncHandle: 0,
            targetInstanceId: objARef[jsObjectId],
            identifier: "",
            callType: DotNet.JSCallType.GetValue,
            resultType: DotNet.JSCallResultType.Default,
            argsJson: null
        };

        const result = dispatcher.invokeJSFromDotNet(invocationInfo2);
        const resultObj = JSON.parse(result ?? "{}");

        expect((resultObj as any).b.x).toBe(30);
    });

    test("NewCall + GetValue: Class constructor is invoked and the new instance value is retrieved", () => {
        const TestClass = class {
            a: number;
            constructor() { this.a = 20; }
        };
        const objectId = getObjectReferenceId({ TestClass });

        const invocationInfo: DotNet.JSInvocationInfo = {
            asyncHandle: 0,
            targetInstanceId: objectId,
            identifier: "TestClass",
            callType: DotNet.JSCallType.NewCall,
            resultType: DotNet.JSCallResultType.JSObjectReference,
            argsJson: null
        };

        const result = dispatcher.invokeJSFromDotNet(invocationInfo);
        const newObjectId = JSON.parse(result ?? "")[jsObjectId];

        const invocationInfo2: DotNet.JSInvocationInfo = {
            asyncHandle: 0,
            targetInstanceId: newObjectId,
            identifier: "a",
            callType: DotNet.JSCallType.GetValue,
            resultType: DotNet.JSCallResultType.Default,
            argsJson: null
        };

        const result2 = dispatcher.invokeJSFromDotNet(invocationInfo2);

        expect(result2).toBe("20");
    });

    test("NewCall + FunctionCall: Class constructor is invoked and method is invoked on the new instance", () => {
        const TestClass = class {
            f() { return 30; }
        };
        const objectId = getObjectReferenceId({ TestClass });

        const invocationInfo: DotNet.JSInvocationInfo = {
            asyncHandle: 0,
            targetInstanceId: objectId,
            identifier: "TestClass",
            callType: DotNet.JSCallType.NewCall,
            resultType: DotNet.JSCallResultType.JSObjectReference,
            argsJson: null
        };

        const result = dispatcher.invokeJSFromDotNet(invocationInfo);
        const newObjectId = JSON.parse(result ?? "")[jsObjectId];

        const invocationInfo2: DotNet.JSInvocationInfo = {
            asyncHandle: 0,
            targetInstanceId: newObjectId,
            identifier: "f",
            callType: DotNet.JSCallType.FunctionCall,
            resultType: DotNet.JSCallResultType.Default,
            argsJson: null
        };

        const result2 = dispatcher.invokeJSFromDotNet(invocationInfo2);

        expect(result2).toBe("30");
    });
});
