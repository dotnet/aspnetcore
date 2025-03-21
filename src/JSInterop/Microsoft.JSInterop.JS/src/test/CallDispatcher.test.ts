import { expect } from "@jest/globals";
import { DotNet } from "../src/Microsoft.JSInterop";

const jsObjectId = "__jsObjectId";
const dotNetCallDispatcher: DotNet.DotNetCallDispatcher = {
    beginInvokeDotNetFromJS: function (callId: number, assemblyName: string | null, methodIdentifier: string, dotNetObjectId: number | null, argsJson: string): void { },
    endInvokeJSFromDotNet: function (callId: number, succeeded: boolean, resultOrError: any): void { },
    sendByteArray: function (id: number, data: Uint8Array): void { }
}
const dispatcher: DotNet.ICallDispatcher = DotNet.attachDispatcher(dotNetCallDispatcher);

describe("CallDispatcher", () => {
    test("should handle functions with no arguments", () => {
        const mockFunc = jest.fn(() => 1);
        const objectId = DotNet.createJSObjectReference({ mockFunc })[jsObjectId];

        const invocationInfo: DotNet.JSInvocationInfo = {
            asyncHandle: 0,
            targetInstanceId: objectId,
            identifier: "mockFunc",
            callType: DotNet.JSCallType.FunctionCall,
            resultType: DotNet.JSCallResultType.Default,
            argsJson: null
        };

        const result = dispatcher.invokeJSFromDotNet(invocationInfo);
        expect(result).toBe("1");
        expect(mockFunc).toHaveBeenCalledWith();
    });

    test("should call the function with provided arguments and return the result", () => {
        const mockFunc = jest.fn((a, b) => a + b);
        const objectId = DotNet.createJSObjectReference({ mockFunc })[jsObjectId];

        const invocationInfo: DotNet.JSInvocationInfo = {
            asyncHandle: 0,
            targetInstanceId: objectId,
            identifier: "mockFunc",
            callType: DotNet.JSCallType.FunctionCall,
            resultType: DotNet.JSCallResultType.Default,
            argsJson: JSON.stringify([1, 2])
        };

        const result = dispatcher.invokeJSFromDotNet(invocationInfo);

        expect(result).toBe("3");
        expect(mockFunc).toHaveBeenCalledWith(1, 2);
    });

    test("should throw an error if the provided value is not a function", () => {
        const mockFunc = jest.fn((a, b) => a + b);
        const objectId = DotNet.createJSObjectReference({ mockFunc })[jsObjectId];

        const invocationInfo: DotNet.JSInvocationInfo = {
            asyncHandle: 0,
            targetInstanceId: objectId,
            identifier: "notAFunction",
            callType: DotNet.JSCallType.FunctionCall,
            resultType: DotNet.JSCallResultType.Default,
            argsJson: JSON.stringify([1, 2])
        };

        expect(() => dispatcher.invokeJSFromDotNet(invocationInfo)).toThrowError("The value 'notAFunction' is not a function.");
    });

    test("should handle functions that throw errors", () => {
        const mockFunc = jest.fn(() => { throw new Error("Test error"); });
        const objectId = DotNet.createJSObjectReference({ mockFunc })[jsObjectId];

        const invocationInfo: DotNet.JSInvocationInfo = {
            asyncHandle: 0,
            targetInstanceId: objectId,
            identifier: "mockFunc",
            callType: DotNet.JSCallType.FunctionCall,
            resultType: DotNet.JSCallResultType.Default,
            argsJson: null
        };

        expect(() => dispatcher.invokeJSFromDotNet(invocationInfo)).toThrowError("Test error");
    });

    test("get value simple", () => {
        const testObject = { a: 10 };
        const objectId = DotNet.createJSObjectReference(testObject)[jsObjectId];

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

    test("get value nested", () => {
        const testObject = { a: { b: 20 } };
        const objectId = DotNet.createJSObjectReference(testObject)[jsObjectId];

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

    test("get value undefined throws", () => {
        const testObject = { a: 10 };
        const objectId = DotNet.createJSObjectReference(testObject)[jsObjectId];

        const invocationInfo: DotNet.JSInvocationInfo = {
            asyncHandle: 0,
            targetInstanceId: objectId,
            identifier: "b",
            callType: DotNet.JSCallType.GetValue,
            resultType: DotNet.JSCallResultType.Default,
            argsJson: null
        };

        expect(() => dispatcher.invokeJSFromDotNet(invocationInfo)).toThrowError(`The property 'b' is not defined or is not readable.`);
    });

    test("get value object ref", () => {
        const testObject = { a: { b: 20 } };
        const objectId = DotNet.createJSObjectReference(testObject)[jsObjectId];

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

    test("get value object deref", () => {
        const testObject = { a: { b: 20 } };
        const objectId = DotNet.createJSObjectReference(testObject)[jsObjectId];

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
});
