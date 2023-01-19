export declare module DotNet {
    export type JsonReviver = ((key: any, value: any) => any);
    /**
     * Sets the specified .NET call dispatcher as the current instance so that it will be used
     * for future invocations.
     *
     * @param dispatcher An object that can dispatch calls from JavaScript to a .NET runtime.
     */
    export function attachDispatcher(dispatcher: DotNetCallDispatcher): void;
    /**
     * Adds a JSON reviver callback that will be used when parsing arguments received from .NET.
     * @param reviver The reviver to add.
     */
    export function attachReviver(reviver: JsonReviver): void;
    /**
     * Invokes the specified .NET public method synchronously. Not all hosting scenarios support
     * synchronous invocation, so if possible use invokeMethodAsync instead.
     *
     * @param assemblyName The short name (without key/version or .dll extension) of the .NET assembly containing the method.
     * @param methodIdentifier The identifier of the method to invoke. The method must have a [JSInvokable] attribute specifying this identifier.
     * @param args Arguments to pass to the method, each of which must be JSON-serializable.
     * @returns The result of the operation.
     */
    export function invokeMethod<T>(assemblyName: string, methodIdentifier: string, ...args: any[]): T;
    /**
     * Invokes the specified .NET public method asynchronously.
     *
     * @param assemblyName The short name (without key/version or .dll extension) of the .NET assembly containing the method.
     * @param methodIdentifier The identifier of the method to invoke. The method must have a [JSInvokable] attribute specifying this identifier.
     * @param args Arguments to pass to the method, each of which must be JSON-serializable.
     * @returns A promise representing the result of the operation.
     */
    export function invokeMethodAsync<T>(assemblyName: string, methodIdentifier: string, ...args: any[]): Promise<T>;
    /**
     * Creates a JavaScript object reference that can be passed to .NET via interop calls.
     *
     * @param jsObject The JavaScript Object used to create the JavaScript object reference.
     * @returns The JavaScript object reference (this will be the same instance as the given object).
     * @throws Error if the given value is not an Object.
     */
    export function createJSObjectReference(jsObject: any): any;
    /**
     * Creates a JavaScript data reference that can be passed to .NET via interop calls.
     *
     * @param streamReference The ArrayBufferView or Blob used to create the JavaScript stream reference.
     * @returns The JavaScript data reference (this will be the same instance as the given object).
     * @throws Error if the given value is not an Object or doesn't have a valid byteLength.
     */
    export function createJSStreamReference(streamReference: ArrayBuffer | ArrayBufferView | Blob | any): any;
    /**
     * Disposes the given JavaScript object reference.
     *
     * @param jsObjectReference The JavaScript Object reference.
     */
    export function disposeJSObjectReference(jsObjectReference: any): void;
    /**
     * Represents the type of result expected from a JS interop call.
     */
    export enum JSCallResultType {
        Default = 0,
        JSObjectReference = 1,
        JSStreamReference = 2,
        JSVoidResult = 3
    }
    /**
     * Represents the ability to dispatch calls from JavaScript to a .NET runtime.
     */
    export interface DotNetCallDispatcher {
        /**
         * Optional. If implemented, invoked by the runtime to perform a synchronous call to a .NET method.
         *
         * @param assemblyName The short name (without key/version or .dll extension) of the .NET assembly holding the method to invoke. The value may be null when invoking instance methods.
         * @param methodIdentifier The identifier of the method to invoke. The method must have a [JSInvokable] attribute specifying this identifier.
         * @param dotNetObjectId If given, the call will be to an instance method on the specified DotNetObject. Pass null or undefined to call static methods.
         * @param argsJson JSON representation of arguments to pass to the method.
         * @returns JSON representation of the result of the invocation.
         */
        invokeDotNetFromJS?(assemblyName: string | null, methodIdentifier: string, dotNetObjectId: number | null, argsJson: string): string | null;
        /**
         * Invoked by the runtime to begin an asynchronous call to a .NET method.
         *
         * @param callId A value identifying the asynchronous operation. This value should be passed back in a later call from .NET to JS.
         * @param assemblyName The short name (without key/version or .dll extension) of the .NET assembly holding the method to invoke. The value may be null when invoking instance methods.
         * @param methodIdentifier The identifier of the method to invoke. The method must have a [JSInvokable] attribute specifying this identifier.
         * @param dotNetObjectId If given, the call will be to an instance method on the specified DotNetObject. Pass null to call static methods.
         * @param argsJson JSON representation of arguments to pass to the method.
         */
        beginInvokeDotNetFromJS(callId: number, assemblyName: string | null, methodIdentifier: string, dotNetObjectId: number | null, argsJson: string): void;
        /**
         * Invoked by the runtime to complete an asynchronous JavaScript function call started from .NET
         *
         * @param callId A value identifying the asynchronous operation.
         * @param succeded Whether the operation succeeded or not.
         * @param resultOrError The serialized result or the serialized error from the async operation.
         */
        endInvokeJSFromDotNet(callId: number, succeeded: boolean, resultOrError: any): void;
        /**
         * Invoked by the runtime to transfer a byte array from JS to .NET.
         * @param id The identifier for the byte array used during revival.
         * @param data The byte array being transferred for eventual revival.
         */
        sendByteArray(id: number, data: Uint8Array): void;
    }
    /**
     * Receives incoming calls from .NET and dispatches them to JavaScript.
     */
    export const jsCallDispatcher: {
        /**
         * Finds the JavaScript function matching the specified identifier.
         *
         * @param identifier Identifies the globally-reachable function to be returned.
         * @param targetInstanceId The instance ID of the target JS object.
         * @returns A Function instance.
         */
        findJSFunction: typeof findJSFunction;
        /**
         * Disposes the JavaScript object reference with the specified object ID.
         *
         * @param id The ID of the JavaScript object reference.
         */
        disposeJSObjectReferenceById: typeof disposeJSObjectReferenceById;
        /**
         * Invokes the specified synchronous JavaScript function.
         *
         * @param identifier Identifies the globally-reachable function to invoke.
         * @param argsJson JSON representation of arguments to be passed to the function.
         * @param resultType The type of result expected from the JS interop call.
         * @param targetInstanceId The instance ID of the target JS object.
         * @returns JSON representation of the invocation result.
         */
        invokeJSFromDotNet: (identifier: string, argsJson: string, resultType: JSCallResultType, targetInstanceId: number) => string | null;
        /**
         * Invokes the specified synchronous or asynchronous JavaScript function.
         *
         * @param asyncHandle A value identifying the asynchronous operation. This value will be passed back in a later call to endInvokeJSFromDotNet.
         * @param identifier Identifies the globally-reachable function to invoke.
         * @param argsJson JSON representation of arguments to be passed to the function.
         * @param resultType The type of result expected from the JS interop call.
         * @param targetInstanceId The ID of the target JS object instance.
         */
        beginInvokeJSFromDotNet: (asyncHandle: number, identifier: string, argsJson: string, resultType: JSCallResultType, targetInstanceId: number) => void;
        /**
         * Receives notification that an async call from JS to .NET has completed.
         * @param asyncCallId The identifier supplied in an earlier call to beginInvokeDotNetFromJS.
         * @param success A flag to indicate whether the operation completed successfully.
         * @param resultJsonOrExceptionMessage Either the operation result as JSON, or an error message.
         */
        endInvokeDotNetFromJS: (asyncCallId: string, success: boolean, resultJsonOrExceptionMessage: string) => void;
        /**
         * Receives notification that a byte array is being transferred from .NET to JS.
         * @param id The identifier for the byte array used during revival.
         * @param data The byte array being transferred for eventual revival.
         */
        receiveByteArray: (id: number, data: Uint8Array) => void;
        /**
         * Supplies a stream of data being sent from .NET.
         *
         * @param streamId The identifier previously passed to JSRuntime's BeginTransmittingStream in .NET code
         * @param stream The stream data.
         */
        supplyDotNetStream: (streamId: number, stream: ReadableStream) => void;
    };
    function findJSFunction(identifier: string, targetInstanceId: number): Function;
    function disposeJSObjectReferenceById(id: number): void;
    export class DotNetObject {
        private _id;
        constructor(_id: number);
        invokeMethod<T>(methodIdentifier: string, ...args: any[]): T;
        invokeMethodAsync<T>(methodIdentifier: string, ...args: any[]): Promise<T>;
        dispose(): void;
        serializeAsArg(): {
            __dotNetObject: number;
        };
    }
    export {};
}
//# sourceMappingURL=Microsoft.JSInterop.d.ts.map