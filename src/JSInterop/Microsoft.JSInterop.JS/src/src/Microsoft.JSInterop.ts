// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This is a single-file self-contained module to avoid the need for a Webpack build

export module DotNet {
  export type JsonReviver = ((key: any, value: any) => any);
  const jsonRevivers: JsonReviver[] = [];

  const jsObjectIdKey = "__jsObjectId";
  const dotNetObjectRefKey = "__dotNetObject";
  const dotNetElementRefKey = "__internalId";
  const byteArrayRefKey = "__byte[]";
  const dotNetStreamRefKey = "__dotNetStream";
  const jsStreamReferenceLengthKey = "__jsStreamReferenceLength";

  // If undefined, no dispatcher has been attached yet.
  // If null, this means more than one dispatcher was attached, so no default can be determined.
  // Otherwise, there was only one dispatcher registered. We keep track of this instance to keep legacy APIs working.
  let defaultCallDispatcher: CallDispatcher | null | undefined;

  // Provides access to the "current" call dispatcher without having to flow it through nested function calls.
  let currentCallDispatcher: CallDispatcher | undefined;

  type InvocationHandlerCache = Map<string, { [k in JSCallType]?: Function }>;

  class JSObject {
      // We cache resolved members. Note that this means we can return stale values if the object has been modified since then.
      _cachedHandlers: InvocationHandlerCache;;

      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      constructor(private _jsObject: any) {
          this._cachedHandlers = new Map();
      }

      public resolveInvocationHandler(identifier: string, callType: JSCallType): Function {
          const cachedFunction = this._cachedHandlers.get(identifier)?.[callType];

          if (cachedFunction) {
              return cachedFunction;
          }

          const [parent, memberName] = findObjectMember(this._jsObject, identifier);
          const func = wrapJSCallAsFunction(parent, memberName, callType, identifier);
          this.addHandlerToCache(identifier, func, callType);

          return func;
      }

      public getWrappedObject() {
          return this._jsObject;
      }

      private addHandlerToCache(identifier: string, func: Function, callType: JSCallType) {
          const cachedIdentifier = this._cachedHandlers.get(identifier);

          if (cachedIdentifier) {
            cachedIdentifier[callType] = func;
          } else {
            this._cachedHandlers.set(identifier, { [callType]: func });
          }
      }
  }

   /**
   * Represents the type of operation that should be performed in JS.
   */
  export enum JSCallType {
      FunctionCall = 1,
      ConstructorCall = 2,
      GetValue = 3,
      SetValue = 4
  }

  const windowJSObjectId = 0;
  const cachedJSObjectsById: { [id: number]: JSObject } = {
      [windowJSObjectId]: new JSObject(window)
  };

  const windowObject = cachedJSObjectsById[windowJSObjectId];
  windowObject._cachedHandlers.set("import", { [JSCallType.FunctionCall]: (url: any) => {
      // In most cases developers will want to resolve dynamic imports relative to the base HREF.
      // However since we're the one calling the import keyword, they would be resolved relative to
      // this framework bundle URL. Fix this by providing an absolute URL.
      if (typeof url === "string" && url.startsWith("./")) {
          url = new URL(url.substring(2), document.baseURI).toString();
      }

      return import(/* webpackIgnore: true */ url);
  }});

  let nextJsObjectId = 1; // Start at 1 because zero is reserved for "window"

  /**
   * Creates a .NET call dispatcher to use for handling invocations between JavaScript and a .NET runtime.
   *
   * @param dotNetCallDispatcher An object that can dispatch calls from JavaScript to a .NET runtime.
   */
  export function attachDispatcher(dotNetCallDispatcher: DotNetCallDispatcher): ICallDispatcher {
      const result = new CallDispatcher(dotNetCallDispatcher);
      if (defaultCallDispatcher === undefined) {
          // This was the first dispatcher registered, so it becomes the default. This exists purely for
          // backwards compatibility.
          defaultCallDispatcher = result;
      } else if (defaultCallDispatcher) {
          // There is already a default dispatcher. Now that there are multiple to choose from, there can
          // be no acceptable default, so we nullify the default dispatcher.
          defaultCallDispatcher = null;
      }

      return result;
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
   * @deprecated Use DotNetObject to invoke instance methods instead.
   * @param assemblyName The short name (without key/version or .dll extension) of the .NET assembly containing the method.
   * @param methodIdentifier The identifier of the method to invoke. The method must have a [JSInvokable] attribute specifying this identifier.
   * @param args Arguments to pass to the method, each of which must be JSON-serializable.
   * @returns The result of the operation.
   */
  export function invokeMethod<T>(assemblyName: string, methodIdentifier: string, ...args: any[]): T | null {
      const dispatcher = getDefaultCallDispatcher();
      return dispatcher.invokeDotNetStaticMethod<T>(assemblyName, methodIdentifier, ...args);
  }

  /**
   * Invokes the specified .NET public method asynchronously.
   *
   * @deprecated Use DotNetObject to invoke instance methods instead.
   * @param assemblyName The short name (without key/version or .dll extension) of the .NET assembly containing the method.
   * @param methodIdentifier The identifier of the method to invoke. The method must have a [JSInvokable] attribute specifying this identifier.
   * @param args Arguments to pass to the method, each of which must be JSON-serializable.
   * @returns A promise representing the result of the operation.
   */
  export function invokeMethodAsync<T>(assemblyName: string, methodIdentifier: string, ...args: any[]): Promise<T> {
      const dispatcher = getDefaultCallDispatcher();
      return dispatcher.invokeDotNetStaticMethodAsync<T>(assemblyName, methodIdentifier, ...args);
  }

  /**
   * Creates a JavaScript object reference that can be passed to .NET via interop calls.
   *
   * @param jsObject The JavaScript Object used to create the JavaScript object reference.
   * @returns The JavaScript object reference (this will be the same instance as the given object).
   * @throws Error if the given value is not an Object.
   */
  export function createJSObjectReference(jsObject: any): any {
      if (jsObject === null || jsObject === undefined) {
          return {
              [jsObjectIdKey]: -1
          };
      }

      if (jsObject && (typeof jsObject === "object" || jsObject instanceof Function)) {
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

      if (typeof id === "number" && id !== -1) {
          disposeJSObjectReferenceById(id);
      }
  }

  function parseJsonWithRevivers(callDispatcher: CallDispatcher, json: string | null): any {
      currentCallDispatcher = callDispatcher;
      const result = json ? JSON.parse(json, (key, initialValue) => {
          // Invoke each reviver in order, passing the output from the previous reviver,
          // so that each one gets a chance to transform the value

          return jsonRevivers.reduce(
              (latestValue, reviver) => reviver(key, latestValue),
              initialValue
          );
      }) : null;
      currentCallDispatcher = undefined;
      return result;
  }

  function getDefaultCallDispatcher(): CallDispatcher {
      if (defaultCallDispatcher === undefined) {
          throw new Error("No call dispatcher has been set.");
      } else if (defaultCallDispatcher === null) {
          throw new Error("There are multiple .NET runtimes present, so a default dispatcher could not be resolved. Use DotNetObject to invoke .NET instance methods.");
      } else {
          return defaultCallDispatcher;
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
   * Represents the ability to facilitate function call dispatching between JavaScript and a .NET runtime.
   */
  export interface ICallDispatcher {
    /**
     * Invokes the specified synchronous JavaScript function.
     *
     * @param identifier Identifies the globally-reachable function to invoke.
     * @param argsJson JSON representation of arguments to be passed to the function.
     * @param resultType The type of result expected from the JS interop call.
     * @param targetInstanceId The instance ID of the target JS object.
     * @param callType The type of operation that should be performed in JS.
     * @returns JSON representation of the invocation result.
     */
    invokeJSFromDotNet(identifier: string, argsJson: string, resultType: JSCallResultType, targetInstanceId: number, callType: JSCallType): string | null;

    /**
     * Invokes the specified synchronous or asynchronous JavaScript function.
     *
     * @param asyncHandle A value identifying the asynchronous operation. This value will be passed back in a later call to endInvokeJSFromDotNet.
     * @param identifier Identifies the globally-reachable function to invoke.
     * @param argsJson JSON representation of arguments to be passed to the function.
     * @param resultType The type of result expected from the JS interop call.
     * @param targetInstanceId The ID of the target JS object instance.
     * @param callType The type of operation that should be performed in JS.
     */
    beginInvokeJSFromDotNet(asyncHandle: number, identifier: string, argsJson: string | null, resultType: JSCallResultType, targetInstanceId: number, callType: JSCallType): Promise<any>;

    /**
     * Receives notification that an async call from JS to .NET has completed.
     * @param asyncCallId The identifier supplied in an earlier call to beginInvokeDotNetFromJS.
     * @param success A flag to indicate whether the operation completed successfully.
     * @param resultJsonOrExceptionMessage Either the operation result as JSON, or an error message.
     */
    endInvokeDotNetFromJS(asyncCallId: string, success: boolean, resultJsonOrExceptionMessage: string): void;

    /**
     * Invokes the specified .NET public static method synchronously. Not all hosting scenarios support
     * synchronous invocation, so if possible use invokeMethodAsync instead.
     *
     * @param assemblyName The short name (without key/version or .dll extension) of the .NET assembly containing the method.
     * @param methodIdentifier The identifier of the method to invoke. The method must have a [JSInvokable] attribute specifying this identifier.
     * @param args Arguments to pass to the method, each of which must be JSON-serializable.
     * @returns The result of the operation.
     */
    invokeDotNetStaticMethod<T>(assemblyName: string, methodIdentifier: string, ...args: any[]): | null;

    /**
     * Invokes the specified .NET public static method asynchronously.
     *
     * @param assemblyName The short name (without key/version or .dll extension) of the .NET assembly containing the method.
     * @param methodIdentifier The identifier of the method to invoke. The method must have a [JSInvokable] attribute specifying this identifier.
     * @param args Arguments to pass to the method, each of which must be JSON-serializable.
     * @returns A promise representing the result of the operation.
     */
    invokeDotNetStaticMethodAsync<T>(assemblyName: string, methodIdentifier: string, ...args: any[]): Promise<T>;

    /**
     * Receives notification that a byte array is being transferred from .NET to JS.
     * @param id The identifier for the byte array used during revival.
     * @param data The byte array being transferred for eventual revival.
     */
    receiveByteArray(id: number, data: Uint8Array): void

    /**
     * Supplies a stream of data being sent from .NET.
     *
     * @param streamId The identifier previously passed to JSRuntime's BeginTransmittingStream in .NET code.
     * @param stream The stream data.
     */
    supplyDotNetStream(streamId: number, stream: ReadableStream): void;
  }

  class CallDispatcher implements ICallDispatcher {
      private readonly _byteArraysToBeRevived = new Map<number, Uint8Array>();

      private readonly _pendingDotNetToJSStreams = new Map<number, PendingStream>();

      private readonly _pendingAsyncCalls: { [id: number]: PendingAsyncCall<any> } = {};

      private _nextAsyncCallId = 1; // Start at 1 because zero signals "no response needed"

      // eslint-disable-next-line no-empty-function
      constructor(private readonly _dotNetCallDispatcher: DotNetCallDispatcher) {
      }

      getDotNetCallDispatcher(): DotNetCallDispatcher {
          return this._dotNetCallDispatcher;
      }

      invokeJSFromDotNet(identifier: string, argsJson: string, resultType: JSCallResultType, targetInstanceId: number, callType: JSCallType): string | null {
          const returnValue = this.processJSCall(targetInstanceId, identifier, callType, argsJson);
          const result = createJSCallResult(returnValue, resultType);

          return result === null || result === undefined
              ? null
              : stringifyArgs(this, result);
      }

      async beginInvokeJSFromDotNet(asyncHandle: number, identifier: string, argsJson: string | null, resultType: JSCallResultType, targetInstanceId: number, callType: JSCallType): Promise<any> {
          try {
            const valueOrPromise = this.processJSCall(targetInstanceId, identifier, callType, argsJson);

            // We only await the result if the caller wants to be notified about it
            if (asyncHandle) {
                const result = await valueOrPromise;
                const serializedResult = stringifyArgs(this, [
                    asyncHandle,
                    true,
                    createJSCallResult(result, resultType)
                ]);
                // On success, dispatch result back to .NET
                this._dotNetCallDispatcher.endInvokeJSFromDotNet(asyncHandle, true, serializedResult);
            }
          } catch(error: any) {
            if (asyncHandle) {
                const serializedError = JSON.stringify([
                    asyncHandle,
                    false,
                    formatError(error)
                ]);
                // On failure, dispatch error back to .NET
                this._dotNetCallDispatcher.endInvokeJSFromDotNet(asyncHandle, false, serializedError);
            }
          }
      }

      processJSCall(targetInstanceId: number, identifier: string, callType: JSCallType, argsJson: string | null) {
          const args = parseJsonWithRevivers(this, argsJson) ?? [];
          const func = findJSFunction(identifier, targetInstanceId, callType);
          return func(...args);
      }

      endInvokeDotNetFromJS(asyncCallId: string, success: boolean, resultJsonOrExceptionMessage: string): void {
          const resultOrError = success
              ? parseJsonWithRevivers(this, resultJsonOrExceptionMessage)
              : new Error(resultJsonOrExceptionMessage);
          this.completePendingCall(parseInt(asyncCallId, 10), success, resultOrError);
      }

      invokeDotNetStaticMethod<T>(assemblyName: string, methodIdentifier: string, ...args: any[]): T | null {
          return this.invokeDotNetMethod<T>(assemblyName, methodIdentifier, null, args);
      }

      invokeDotNetStaticMethodAsync<T>(assemblyName: string, methodIdentifier: string, ...args: any[]): Promise<T> {
          return this.invokeDotNetMethodAsync<T>(assemblyName, methodIdentifier, null, args);
      }

      invokeDotNetMethod<T>(assemblyName: string | null, methodIdentifier: string, dotNetObjectId: number | null, args: any[] | null): T | null {
        if (this._dotNetCallDispatcher.invokeDotNetFromJS) {
            const argsJson = stringifyArgs(this, args);
            const resultJson = this._dotNetCallDispatcher.invokeDotNetFromJS(assemblyName, methodIdentifier, dotNetObjectId, argsJson);
            return resultJson ? parseJsonWithRevivers(this, resultJson) : null;
        }

        throw new Error("The current dispatcher does not support synchronous calls from JS to .NET. Use invokeDotNetMethodAsync instead.");
    }

      invokeDotNetMethodAsync<T>(assemblyName: string | null, methodIdentifier: string, dotNetObjectId: number | null, args: any[] | null): Promise<T> {
          if (assemblyName && dotNetObjectId) {
              throw new Error(`For instance method calls, assemblyName should be null. Received '${assemblyName}'.`);
          }

          const asyncCallId = this._nextAsyncCallId++;
          const resultPromise = new Promise<T>((resolve, reject) => {
              this._pendingAsyncCalls[asyncCallId] = { resolve, reject };
          });

          try {
              const argsJson = stringifyArgs(this, args);
              this._dotNetCallDispatcher.beginInvokeDotNetFromJS(asyncCallId, assemblyName, methodIdentifier, dotNetObjectId, argsJson);
          } catch (ex) {
              // Synchronous failure
              this.completePendingCall(asyncCallId, false, ex);
          }

          return resultPromise;
      }

      receiveByteArray(id: number, data: Uint8Array): void {
          this._byteArraysToBeRevived.set(id, data);
      }

      processByteArray(id: number): Uint8Array | null {
          const result = this._byteArraysToBeRevived.get(id);
          if (!result) {
              return null;
          }

          this._byteArraysToBeRevived.delete(id);
          return result;
      }

      supplyDotNetStream(streamId: number, stream: ReadableStream) {
          if (this._pendingDotNetToJSStreams.has(streamId)) {
              // The receiver is already waiting, so we can resolve the promise now and stop tracking this
              const pendingStream = this._pendingDotNetToJSStreams.get(streamId)!;
              this._pendingDotNetToJSStreams.delete(streamId);
              pendingStream.resolve!(stream);
          } else {
              // The receiver hasn't started waiting yet, so track a pre-completed entry it can attach to later
              const pendingStream = new PendingStream();
              pendingStream.resolve!(stream);
              this._pendingDotNetToJSStreams.set(streamId, pendingStream);
          }
      }

      getDotNetStreamPromise(streamId: number): Promise<ReadableStream> {
          // We might already have started receiving the stream, or maybe it will come later.
          // We have to handle both possible orderings, but we can count on it coming eventually because
          // it's not something the developer gets to control, and it would be an error if it doesn't.
          let result: Promise<ReadableStream>;
          if (this._pendingDotNetToJSStreams.has(streamId)) {
              // We've already started receiving the stream, so no longer need to track it as pending
              result = this._pendingDotNetToJSStreams.get(streamId)!.streamPromise!;
              this._pendingDotNetToJSStreams.delete(streamId);
          } else {
              // We haven't started receiving it yet, so add an entry to track it as pending
              const pendingStream = new PendingStream();
              this._pendingDotNetToJSStreams.set(streamId, pendingStream);
              result = pendingStream.streamPromise;
          }

          return result;
      }

      private completePendingCall(asyncCallId: number, success: boolean, resultOrError: any) {
          if (!this._pendingAsyncCalls.hasOwnProperty(asyncCallId)) {
              throw new Error(`There is no pending async call with ID ${asyncCallId}.`);
          }

          const asyncCall = this._pendingAsyncCalls[asyncCallId];
          delete this._pendingAsyncCalls[asyncCallId];
          if (success) {
              asyncCall.resolve(resultOrError);
          } else {
              asyncCall.reject(resultOrError);
          }
      }
  }

  function formatError(error: Error | string): string {
      if (error instanceof Error) {
          return `${error.message}\n${error.stack}`;
      }

      return error ? error.toString() : "null";
  }

  export function findJSFunction(identifier: string, targetInstanceId: number, callType?: JSCallType): Function {
      const targetInstance = cachedJSObjectsById[targetInstanceId];

      if (targetInstance) {
          return targetInstance.resolveInvocationHandler(identifier, callType ?? JSCallType.FunctionCall);
      }

      throw new Error(`JS object instance with ID ${targetInstanceId} does not exist (has it been disposed?).`);
  }

  export function disposeJSObjectReferenceById(id: number) {
      delete cachedJSObjectsById[id];
  }

  /** Traverses the object hierarchy to find an object member specified by the identifier.
   *
   * @param obj Root object to search in.
   * @param identifier Complete identifier of the member to find, e.g. "document.location.href".
   * @returns A tuple containing the immediate parent of the member and the member name.
   */
  export function findObjectMember(obj: any, identifier: string): [any, string] {
      const keys = identifier.split(".");
      let current = obj;

      // First, we iterate over all but the last key. We throw error for missing intermediate keys.
      // Error handling in case of undefined last key depends on the type of operation.
      for (let i = 0; i < keys.length - 1; i++) {
          const key = keys[i];

          if (current && typeof current === 'object' && key in current) {
              current = current[key];
          } else {
              throw new Error(`Could not find '${identifier}' ('${key}' was undefined).`);
          }
      }

      return [current, keys[keys.length - 1]];
  }

  /** Takes an object member and a call type and returns a function that performs the operation specified by the call type on the member.
   *
   * @param parent Immediate parent of the accessed object member.
   * @param memberName Name (key) of the accessed member.
   * @param callType The type of the operation to perform on the member.
   * @param identifier The full member identifier. Only used for error messages.
   * @returns A function that performs the operation on the member.
   */
  function wrapJSCallAsFunction(parent: any, memberName: string, callType: JSCallType, identifier: string): Function {
      switch (callType) {
          case JSCallType.FunctionCall:
              const func = parent[memberName];
              if (func instanceof Function) {
                  return func.bind(parent);
              } else {
                  throw new Error(`The value '${identifier}' is not a function.`);
              }
          case JSCallType.ConstructorCall:
              const ctor = parent[memberName];
              if (ctor instanceof Function) {
                  const bound = ctor.bind(parent);
                  return (...args: any[]) => new bound(...args);
              } else {
                  throw new Error(`The value '${identifier}' is not a function.`);
              }
          case JSCallType.GetValue:
              if (!isReadableProperty(parent, memberName)) {
                  throw new Error(`The property '${identifier}' is not defined or is not readable.`);
              }
              return () => parent[memberName];
          case JSCallType.SetValue:
              if (!isWritableProperty(parent, memberName)) {
                  throw new Error(`The property '${identifier}' is not writable.`);
              }
              return (...args: any[]) => parent[memberName] = args[0];
      }
  }

  function isReadableProperty(obj: any, propName: string) {
      // Return false for missing property.
      if (!(propName in obj)) {
          return false;
      }

      // If the property is present we examine its descriptor, potentially needing to walk up the prototype chain.
      while (obj !== undefined) {
          const descriptor = Object.getOwnPropertyDescriptor(obj, propName);

          if (descriptor) {
              // Return true for data property
              if (descriptor.hasOwnProperty('value')) {
                  return true
              }

              // Return true for accessor property with defined getter.
              return descriptor.hasOwnProperty('get') && typeof descriptor.get === 'function';
          }

          obj = Object.getPrototypeOf(obj);
      }

      return false;
  }

  function isWritableProperty(obj: any, propName: string) {
      // Return true for missing property if the property can be added.
      if (!(propName in obj)) {
          return Object.isExtensible(obj);
      }

      // If the property is present we examine its descriptor, potentially needing to walk up the prototype chain.
      while (obj !== undefined) {
          const descriptor = Object.getOwnPropertyDescriptor(obj, propName);

          if (descriptor) {
              // Return true for writable data property.
              if (descriptor.hasOwnProperty('value') && descriptor.writable) {
                  return true;
              }

              // Return true for accessor property with defined setter.
              return descriptor.hasOwnProperty('set') && typeof descriptor.set === 'function';
          }

          obj = Object.getPrototypeOf(obj);
      }

      return false;
  }

  export class DotNetObject {
      // eslint-disable-next-line no-empty-function
      constructor(private readonly _id: number, private readonly _callDispatcher: CallDispatcher) {
      }

      public invokeMethod<T>(methodIdentifier: string, ...args: any[]): T | null {
          return this._callDispatcher.invokeDotNetMethod<T>(null, methodIdentifier, this._id, args);
      }

      public invokeMethodAsync<T>(methodIdentifier: string, ...args: any[]): Promise<T> {
          return this._callDispatcher.invokeDotNetMethodAsync<T>(null, methodIdentifier, this._id, args);
      }

      public dispose() {
          const promise = this._callDispatcher.invokeDotNetMethodAsync<any>(null, "__Dispose", this._id, null);
          promise.catch(error => console.error(error));
      }

      public serializeAsArg() {
          return { [dotNetObjectRefKey]: this._id };
      }
  }

  attachReviver(function reviveReference(key: any, value: any) {
      if (value && typeof value === "object") {
          if (value.hasOwnProperty(dotNetObjectRefKey)) {
              return new DotNetObject(value[dotNetObjectRefKey], currentCallDispatcher!);
          } else if (value.hasOwnProperty(jsObjectIdKey)) {
              const id = value[jsObjectIdKey];
              const jsObject = cachedJSObjectsById[id];

              if (jsObject) {
                  return jsObject.getWrappedObject();
              }

              throw new Error(`JS object instance with Id '${id}' does not exist. It may have been disposed.`);
          } else if (value.hasOwnProperty(byteArrayRefKey)) {
              const index = value[byteArrayRefKey];
              const byteArray = currentCallDispatcher!.processByteArray(index);
              if (byteArray === undefined) {
                  throw new Error(`Byte array index '${index}' does not exist.`);
              }
              return byteArray;
          } else if (value.hasOwnProperty(dotNetStreamRefKey)) {
              const streamId = value[dotNetStreamRefKey];
              const streamPromise = currentCallDispatcher!.getDotNetStreamPromise(streamId);
              return new DotNetStream(streamPromise);
          }
      }

      // Unrecognized - let another reviver handle it
      return value;
  });

  class DotNetStream {
      // eslint-disable-next-line no-empty-function
      constructor(private readonly _streamPromise: Promise<ReadableStream>) {
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
  function stringifyArgs(callDispatcher: CallDispatcher, args: any[] | null) {
      nextByteArrayIndex = 0;
      currentCallDispatcher = callDispatcher;
      const result = JSON.stringify(args, argReplacer);
      currentCallDispatcher = undefined;
      return result;
  }

  function getCaptureIdFromElement(element: Element): string | null {
    for (let i = 0; i < element.attributes.length; i++) {
      const attr = element.attributes[i];
      if (attr.name.startsWith('_bl_')) {
        return attr.name.substring(4);
      }
    }
    return null;
}

  function argReplacer(key: string, value: any) {
      if (value instanceof Element) {
          return { [dotNetElementRefKey]: getCaptureIdFromElement(value) };
      }
      if (value instanceof DotNetObject) {
          return value.serializeAsArg();
      } else if (value instanceof Uint8Array) {
          const dotNetCallDispatcher = currentCallDispatcher!.getDotNetCallDispatcher();
          dotNetCallDispatcher!.sendByteArray(nextByteArrayIndex, value);
          const jsonValue = { [byteArrayRefKey]: nextByteArrayIndex };
          nextByteArrayIndex++;
          return jsonValue;
      }

      return value;
  }
}
