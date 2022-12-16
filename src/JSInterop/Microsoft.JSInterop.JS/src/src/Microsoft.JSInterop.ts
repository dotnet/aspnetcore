// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This is a single-file self-contained module to avoid the need for a Webpack build

export module DotNet {
  (window as any).DotNet = DotNet; // Ensure reachable from anywhere

  export type JsonReviver = ((key: any, value: any) => any);
  const jsonRevivers: JsonReviver[] = [];

  const byteArraysToBeRevived = new Map<number, Uint8Array>();
  const pendingDotNetToJSStreams = new Map<number, PendingStream>();

  const jsObjectIdKey = "__jsObjectId";
  const dotNetObjectRefKey = "__dotNetObject";
  const byteArrayRefKey = "__byte[]";
  const dotNetStreamRefKey = "__dotNetStream";
  const jsStreamReferenceLengthKey = "__jsStreamReferenceLength";

  class JSObject {
      _cachedFunctions: Map<string, Function>;

      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      constructor(private _jsObject: any) {
          this._cachedFunctions = new Map<string, Function>();
      }

      public findFunction(identifier: string) {
          const cachedFunction = this._cachedFunctions.get(identifier);

          if (cachedFunction) {
              return cachedFunction;
          }

          let result: any = this._jsObject;
          let lastSegmentValue: any;

          identifier.split(".").forEach(segment => {
              if (segment in result) {
                  lastSegmentValue = result;
                  result = result[segment];
              } else {
                  throw new Error(`Could not find '${identifier}' ('${segment}' was undefined).`);
              }
          });

          if (result instanceof Function) {
              result = result.bind(lastSegmentValue);
              this._cachedFunctions.set(identifier, result);
              return result;
          }

          throw new Error(`The value '${identifier}' is not a function.`);
      }

      public getWrappedObject() {
          return this._jsObject;
      }
  }

  const pendingAsyncCalls: { [id: number]: PendingAsyncCall<any> } = {};
  const windowJSObjectId = 0;
  const cachedJSObjectsById: { [id: number]: JSObject } = {
      [windowJSObjectId]: new JSObject(window)
  };

  cachedJSObjectsById[windowJSObjectId]._cachedFunctions.set("import", (url: any) => {

      // In most cases developers will want to resolve dynamic imports relative to the base HREF.
      // However since we're the one calling the import keyword, they would be resolved relative to
      // this framework bundle URL. Fix this by providing an absolute URL.
      if (typeof url === "string" && url.startsWith("./")) {
          url = new URL(url.substr(2), document.baseURI).toString();
      }

      return import(/* webpackIgnore: true */ url);
  });

  let nextAsyncCallId = 1; // Start at 1 because zero signals "no response needed"
  let nextJsObjectId = 1; // Start at 1 because zero is reserved for "window"

  let dotNetDispatcher: DotNetCallDispatcher | null = null;

  /**
   * Sets the specified .NET call dispatcher as the current instance so that it will be used
   * for future invocations.
   *
   * @param dispatcher An object that can dispatch calls from JavaScript to a .NET runtime.
   */
  export function attachDispatcher(dispatcher: DotNetCallDispatcher) {
      dotNetDispatcher = dispatcher;
  }

  /**
   * Adds a JSON reviver callback that will be used when parsing arguments received from .NET.
   * @param reviver The reviver to add.
   */
  export function attachReviver(reviver: JsonReviver) {
      jsonRevivers.push(reviver);
  }

  /**
   * Invokes the specified .NET public method synchronously. Not all hosting scenarios support
   * synchronous invocation, so if possible use invokeMethodAsync instead.
   *
   * @param assemblyName The short name (without key/version or .dll extension) of the .NET assembly containing the method.
   * @param methodIdentifier The identifier of the method to invoke. The method must have a [JSInvokable] attribute specifying this identifier.
   * @param args Arguments to pass to the method, each of which must be JSON-serializable.
   * @returns The result of the operation.
   */
  export function invokeMethod<T>(assemblyName: string, methodIdentifier: string, ...args: any[]): T {
      return invokePossibleInstanceMethod<T>(assemblyName, methodIdentifier, null, args);
  }

  /**
   * Invokes the specified .NET public method asynchronously.
   *
   * @param assemblyName The short name (without key/version or .dll extension) of the .NET assembly containing the method.
   * @param methodIdentifier The identifier of the method to invoke. The method must have a [JSInvokable] attribute specifying this identifier.
   * @param args Arguments to pass to the method, each of which must be JSON-serializable.
   * @returns A promise representing the result of the operation.
   */
  export function invokeMethodAsync<T>(assemblyName: string, methodIdentifier: string, ...args: any[]): Promise<T> {
      return invokePossibleInstanceMethodAsync(assemblyName, methodIdentifier, null, args);
  }

  /**
   * Creates a JavaScript object reference that can be passed to .NET via interop calls.
   *
   * @param jsObject The JavaScript Object used to create the JavaScript object reference.
   * @returns The JavaScript object reference (this will be the same instance as the given object).
   * @throws Error if the given value is not an Object.
   */
  export function createJSObjectReference(jsObject: any): any {
      if (jsObject && typeof jsObject === "object") {
          cachedJSObjectsById[nextJsObjectId] = new JSObject(jsObject);

          const result = {
              [jsObjectIdKey]: nextJsObjectId
          };

          nextJsObjectId++;

          return result;
      }

      throw new Error(`Cannot create a JSObjectReference from the value '${jsObject}'.`);
  }

  /**
   * Creates a JavaScript data reference that can be passed to .NET via interop calls.
   *
   * @param streamReference The ArrayBufferView or Blob used to create the JavaScript stream reference.
   * @returns The JavaScript data reference (this will be the same instance as the given object).
   * @throws Error if the given value is not an Object or doesn't have a valid byteLength.
   */
  export function createJSStreamReference(streamReference: ArrayBuffer | ArrayBufferView | Blob | any): any {
      let length = -1;

      // If we're given a raw Array Buffer, we interpret it as a `Uint8Array` as
      // ArrayBuffers' aren't directly readable.
      if (streamReference instanceof ArrayBuffer) {
          streamReference = new Uint8Array(streamReference);
      }

      if (streamReference instanceof Blob) {
          length = streamReference.size;
      } else if (streamReference.buffer instanceof ArrayBuffer) {
          if (streamReference.byteLength === undefined) {
              throw new Error(`Cannot create a JSStreamReference from the value '${streamReference}' as it doesn't have a byteLength.`);
          }

          length = streamReference.byteLength;
      } else {
          throw new Error("Supplied value is not a typed array or blob.");
      }

      const result: any = {
          [jsStreamReferenceLengthKey]: length
      };

      try {
          const jsObjectReference = createJSObjectReference(streamReference);
          result[jsObjectIdKey] = jsObjectReference[jsObjectIdKey];
      } catch (error) {
          throw new Error(`Cannot create a JSStreamReference from the value '${streamReference}'.`);
      }

      return result;
  }

  /**
   * Disposes the given JavaScript object reference.
   *
   * @param jsObjectReference The JavaScript Object reference.
   */
  export function disposeJSObjectReference(jsObjectReference: any): void {
      const id = jsObjectReference && jsObjectReference[jsObjectIdKey];

      if (typeof id === "number") {
          disposeJSObjectReferenceById(id);
      }
  }

  /**
   * Parses the given JSON string using revivers to restore args passed from .NET to JS.
   *
   * @param json The JSON stirng to parse.
   */
  function parseJsonWithRevivers(json: string): any {
      return json ? JSON.parse(json, (key, initialValue) => {
          // Invoke each reviver in order, passing the output from the previous reviver,
          // so that each one gets a chance to transform the value

          return jsonRevivers.reduce(
              (latestValue, reviver) => reviver(key, latestValue),
              initialValue
          );
      }) : null;
  }

  function invokePossibleInstanceMethod<T>(assemblyName: string | null, methodIdentifier: string, dotNetObjectId: number | null, args: any[] | null): T {
      const dispatcher = getRequiredDispatcher();
      if (dispatcher.invokeDotNetFromJS) {
          const argsJson = stringifyArgs(args);
          const resultJson = dispatcher.invokeDotNetFromJS(assemblyName, methodIdentifier, dotNetObjectId, argsJson);
          return resultJson ? parseJsonWithRevivers(resultJson) : null;
      }

      throw new Error("The current dispatcher does not support synchronous calls from JS to .NET. Use invokeMethodAsync instead.");
  }

  function invokePossibleInstanceMethodAsync<T>(assemblyName: string | null, methodIdentifier: string, dotNetObjectId: number | null, args: any[] | null): Promise<T> {
      if (assemblyName && dotNetObjectId) {
          throw new Error(`For instance method calls, assemblyName should be null. Received '${assemblyName}'.`);
      }

      const asyncCallId = nextAsyncCallId++;
      const resultPromise = new Promise<T>((resolve, reject) => {
          pendingAsyncCalls[asyncCallId] = { resolve, reject };
      });

      try {
          const argsJson = stringifyArgs(args);
          getRequiredDispatcher().beginInvokeDotNetFromJS(asyncCallId, assemblyName, methodIdentifier, dotNetObjectId, argsJson);
      } catch (ex) {
      // Synchronous failure
          completePendingCall(asyncCallId, false, ex);
      }

      return resultPromise;
  }

  function getRequiredDispatcher(): DotNetCallDispatcher {
      if (dotNetDispatcher !== null) {
          return dotNetDispatcher;
      }

      throw new Error("No .NET call dispatcher has been set.");
  }

  function completePendingCall(asyncCallId: number, success: boolean, resultOrError: any) {
      if (!pendingAsyncCalls.hasOwnProperty(asyncCallId)) {
          throw new Error(`There is no pending async call with ID ${asyncCallId}.`);
      }

      const asyncCall = pendingAsyncCalls[asyncCallId];
      delete pendingAsyncCalls[asyncCallId];
      if (success) {
          asyncCall.resolve(resultOrError);
      } else {
          asyncCall.reject(resultOrError);
      }
  }

  interface PendingAsyncCall<T> {
    resolve: (value?: T | PromiseLike<T>) => void;
    reject: (reason?: any) => void;
  }

  /**
   * Represents the type of result expected from a JS interop call.
   */
  // eslint-disable-next-line no-shadow
  export enum JSCallResultType {
    Default = 0,
    JSObjectReference = 1,
    JSStreamReference = 2,
    JSVoidResult = 3,
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
  export const jsCallDispatcher = {
      /**
       * Finds the JavaScript function matching the specified identifier.
       *
       * @param identifier Identifies the globally-reachable function to be returned.
       * @param targetInstanceId The instance ID of the target JS object.
       * @returns A Function instance.
       */
      findJSFunction, // Note that this is used by the JS interop code inside Mono WebAssembly itself

      /**
       * Disposes the JavaScript object reference with the specified object ID.
       *
       * @param id The ID of the JavaScript object reference.
       */
      disposeJSObjectReferenceById,

      /**
       * Invokes the specified synchronous JavaScript function.
       *
       * @param identifier Identifies the globally-reachable function to invoke.
       * @param argsJson JSON representation of arguments to be passed to the function.
       * @param resultType The type of result expected from the JS interop call.
       * @param targetInstanceId The instance ID of the target JS object.
       * @returns JSON representation of the invocation result.
       */
      invokeJSFromDotNet: (identifier: string, argsJson: string, resultType: JSCallResultType, targetInstanceId: number) => {
          const returnValue = findJSFunction(identifier, targetInstanceId).apply(null, parseJsonWithRevivers(argsJson));
          const result = createJSCallResult(returnValue, resultType);

          return result === null || result === undefined
              ? null
              : stringifyArgs(result);
      },

      /**
       * Invokes the specified synchronous or asynchronous JavaScript function.
       *
       * @param asyncHandle A value identifying the asynchronous operation. This value will be passed back in a later call to endInvokeJSFromDotNet.
       * @param identifier Identifies the globally-reachable function to invoke.
       * @param argsJson JSON representation of arguments to be passed to the function.
       * @param resultType The type of result expected from the JS interop call.
       * @param targetInstanceId The ID of the target JS object instance.
       */
      beginInvokeJSFromDotNet: (asyncHandle: number, identifier: string, argsJson: string, resultType: JSCallResultType, targetInstanceId: number): void => {
      // Coerce synchronous functions into async ones, plus treat
      // synchronous exceptions the same as async ones
          const promise = new Promise<any>(resolve => {
              const synchronousResultOrPromise = findJSFunction(identifier, targetInstanceId).apply(null, parseJsonWithRevivers(argsJson));
              resolve(synchronousResultOrPromise);
          });

          // We only listen for a result if the caller wants to be notified about it
          if (asyncHandle) {
              // On completion, dispatch result back to .NET
              // Not using "await" because it codegens a lot of boilerplate
              promise
                .then(result => stringifyArgs([asyncHandle, true, createJSCallResult(result, resultType)]))
                .then(
                  result => getRequiredDispatcher().endInvokeJSFromDotNet(asyncHandle, true, result),
                  error => getRequiredDispatcher().endInvokeJSFromDotNet(asyncHandle, false, JSON.stringify([
                      asyncHandle,
                      false,
                      formatError(error)
                  ]))
              );
          }
      },

      /**
       * Receives notification that an async call from JS to .NET has completed.
       * @param asyncCallId The identifier supplied in an earlier call to beginInvokeDotNetFromJS.
       * @param success A flag to indicate whether the operation completed successfully.
       * @param resultJsonOrExceptionMessage Either the operation result as JSON, or an error message.
       */
      endInvokeDotNetFromJS: (asyncCallId: string, success: boolean, resultJsonOrExceptionMessage: string): void => {
          const resultOrError = success
              ? parseJsonWithRevivers(resultJsonOrExceptionMessage)
              : new Error(resultJsonOrExceptionMessage);
          completePendingCall(parseInt(asyncCallId, 10), success, resultOrError);
      },

      /**
       * Receives notification that a byte array is being transferred from .NET to JS.
       * @param id The identifier for the byte array used during revival.
       * @param data The byte array being transferred for eventual revival.
       */
      receiveByteArray: (id: number, data: Uint8Array): void => {
          byteArraysToBeRevived.set(id, data);
      },

      /**
       * Supplies a stream of data being sent from .NET.
       *
       * @param streamId The identifier previously passed to JSRuntime's BeginTransmittingStream in .NET code
       * @param stream The stream data.
       */
      supplyDotNetStream: (streamId: number, stream: ReadableStream) => {
          if (pendingDotNetToJSStreams.has(streamId)) {
              // The receiver is already waiting, so we can resolve the promise now and stop tracking this
              const pendingStream = pendingDotNetToJSStreams.get(streamId)!;
              pendingDotNetToJSStreams.delete(streamId);
        pendingStream.resolve!(stream);
          } else {
              // The receiver hasn't started waiting yet, so track a pre-completed entry it can attach to later
              const pendingStream = new PendingStream();
        pendingStream.resolve!(stream);
        pendingDotNetToJSStreams.set(streamId, pendingStream);
          }
      }
  };

  function formatError(error: Error | string): string {
      if (error instanceof Error) {
          return `${error.message}\n${error.stack}`;
      }

      return error ? error.toString() : "null";
  }

  function findJSFunction(identifier: string, targetInstanceId: number): Function {
      const targetInstance = cachedJSObjectsById[targetInstanceId];

      if (targetInstance) {
          return targetInstance.findFunction(identifier);
      }

      throw new Error(`JS object instance with ID ${targetInstanceId} does not exist (has it been disposed?).`);
  }

  function disposeJSObjectReferenceById(id: number) {
      delete cachedJSObjectsById[id];
  }

  export class DotNetObject {
      // eslint-disable-next-line no-empty-function
      constructor(private _id: number) {
      }

      public invokeMethod<T>(methodIdentifier: string, ...args: any[]): T {
          return invokePossibleInstanceMethod<T>(null, methodIdentifier, this._id, args);
      }

      public invokeMethodAsync<T>(methodIdentifier: string, ...args: any[]): Promise<T> {
          return invokePossibleInstanceMethodAsync<T>(null, methodIdentifier, this._id, args);
      }

      public dispose() {
          const promise = invokePossibleInstanceMethodAsync<any>(null, "__Dispose", this._id, null);
          promise.catch(error => console.error(error));
      }

      public serializeAsArg() {
          return { __dotNetObject: this._id };
      }
  }

  attachReviver(function reviveReference(key: any, value: any) {
      if (value && typeof value === "object") {
          if (value.hasOwnProperty(dotNetObjectRefKey)) {
              return new DotNetObject(value[dotNetObjectRefKey]);
          } else if (value.hasOwnProperty(jsObjectIdKey)) {
              const id = value[jsObjectIdKey];
              const jsObject = cachedJSObjectsById[id];

              if (jsObject) {
                  return jsObject.getWrappedObject();
              }

              throw new Error(`JS object instance with Id '${id}' does not exist. It may have been disposed.`);
          } else if (value.hasOwnProperty(byteArrayRefKey)) {
              const index = value[byteArrayRefKey];
              const byteArray = byteArraysToBeRevived.get(index);
              if (byteArray === undefined) {
                  throw new Error(`Byte array index '${index}' does not exist.`);
              }
              byteArraysToBeRevived.delete(index);

              return byteArray;
          } else if (value.hasOwnProperty(dotNetStreamRefKey)) {
              return new DotNetStream(value[dotNetStreamRefKey]);
          }
      }

      // Unrecognized - let another reviver handle it
      return value;
  });

  class DotNetStream {
      private _streamPromise: Promise<ReadableStream>;

      constructor(streamId: number) {
          // This constructor runs when we're JSON-deserializing some value from the .NET side.
          // At this point we might already have started receiving the stream, or maybe it will come later.
          // We have to handle both possible orderings, but we can count on it coming eventually because
          // it's not something the developer gets to control, and it would be an error if it doesn't.
          if (pendingDotNetToJSStreams.has(streamId)) {
              // We've already started receiving the stream, so no longer need to track it as pending
              this._streamPromise = pendingDotNetToJSStreams.get(streamId)!.streamPromise!;
              pendingDotNetToJSStreams.delete(streamId);
          } else {
              // We haven't started receiving it yet, so add an entry to track it as pending
              const pendingStream = new PendingStream();
              pendingDotNetToJSStreams.set(streamId, pendingStream);
              this._streamPromise = pendingStream.streamPromise;
          }
      }

      /**
       * Supplies a readable stream of data being sent from .NET.
       */
      stream(): Promise<ReadableStream> {
          return this._streamPromise;
      }

      /**
       * Supplies a array buffer of data being sent from .NET.
       * Note there is a JavaScript limit on the size of the ArrayBuffer equal to approximately 2GB.
       */
      async arrayBuffer(): Promise<ArrayBuffer> {
          return new Response(await this.stream()).arrayBuffer();
      }
  }

  class PendingStream {
      streamPromise: Promise<ReadableStream>;

      resolve?: (value: ReadableStream) => void;

      reject?: (reason: any) => void;

      constructor() {
          this.streamPromise = new Promise((resolve, reject) => {
              this.resolve = resolve;
              this.reject = reject;
          });
      }
  }

  function createJSCallResult(returnValue: any, resultType: JSCallResultType) {
      switch (resultType) {
      case JSCallResultType.Default:
          return returnValue;
      case JSCallResultType.JSObjectReference:
          return createJSObjectReference(returnValue);
      case JSCallResultType.JSStreamReference:
          return createJSStreamReference(returnValue);
      case JSCallResultType.JSVoidResult:
          return null;
      default:
          throw new Error(`Invalid JS call result type '${resultType}'.`);
      }
  }

  let nextByteArrayIndex = 0;
  function stringifyArgs(args: any[] | null) {
      nextByteArrayIndex = 0;
      return JSON.stringify(args, argReplacer);
  }

  function argReplacer(key: string, value: any) {
      if (value instanceof DotNetObject) {
          return value.serializeAsArg();
      } else if (value instanceof Uint8Array) {
      dotNetDispatcher!.sendByteArray(nextByteArrayIndex, value);
      const jsonValue = { [byteArrayRefKey]: nextByteArrayIndex };
      nextByteArrayIndex++;
      return jsonValue;
      }

      return value;
  }
}
