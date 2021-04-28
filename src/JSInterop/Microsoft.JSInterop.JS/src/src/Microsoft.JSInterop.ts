// This is a single-file self-contained module to avoid the need for a Webpack build

export module DotNet {
  (window as any).DotNet = DotNet; // Ensure reachable from anywhere

  export type JsonReviver = ((key: any, value: any, byteArrays: Uint8Array[] | undefined) => any);
  const jsonRevivers: JsonReviver[] = [];

  class JSObject {
    _cachedFunctions: Map<string, Function>;

    constructor(private _jsObject: any)
    {
      this._cachedFunctions = new Map<string, Function>();
    }

    public findFunction(identifier: string) {
      const cachedFunction = this._cachedFunctions.get(identifier);

      if (cachedFunction) {
        return cachedFunction;
      }

      let result: any = this._jsObject;
      let lastSegmentValue: any;

      identifier.split('.').forEach(segment => {
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
      } else {
        throw new Error(`The value '${identifier}' is not a function.`);
      }
    }

    public getWrappedObject() {
      return this._jsObject;
    }
  }

  const jsObjectIdKey = "__jsObjectId";

  const pendingAsyncCalls: { [id: number]: PendingAsyncCall<any> } = {};
  const windowJSObjectId = 0;
  const cachedJSObjectsById: { [id: number]: JSObject } = {
    [windowJSObjectId]: new JSObject(window),
  };

  cachedJSObjectsById[windowJSObjectId]._cachedFunctions.set('import', (url: any) => {
    // In most cases developers will want to resolve dynamic imports relative to the base HREF.
    // However since we're the one calling the import keyword, they would be resolved relative to
    // this framework bundle URL. Fix this by providing an absolute URL.
    if (typeof url === 'string' && url.startsWith('./')) {
      url = document.baseURI + url.substr(2);
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
    if (jsObject && typeof jsObject === 'object') {
      cachedJSObjectsById[nextJsObjectId] = new JSObject(jsObject);

      const result = {
        [jsObjectIdKey]: nextJsObjectId
      };

      nextJsObjectId++;

      return result;
    } else {
      throw new Error(`Cannot create a JSObjectReference from the value '${jsObject}'.`);
    }
  }

  /**
   * Disposes the given JavaScript object reference.
   *
   * @param jsObjectReference The JavaScript Object reference.
   */
  export function disposeJSObjectReference(jsObjectReference: any): void {
    const id = jsObjectReference && jsObjectReference[jsObjectIdKey];

    if (typeof id === 'number') {
      disposeJSObjectReferenceById(id);
    }
  }

  /**
   * Parses the given JSON string using revivers to restore args passed from .NET to JS.
   *
   * @param json The JSON string to parse.
   * @param byteArrays The byte array data to revive.
   */
  export function parseJsonWithRevivers(json: string, byteArrays?: Uint8Array[] | undefined): any {
    return json ? JSON.parse(json, (key, initialValue) => {
      // Invoke each reviver in order, passing the output from the previous reviver,
      // so that each one gets a chance to transform the value
      return jsonRevivers.reduce(
        (latestValue, reviver) => reviver(key, latestValue, byteArrays),
        initialValue
      );
    }) : null;
  }

  function invokePossibleInstanceMethod<T>(assemblyName: string | null, methodIdentifier: string, dotNetObjectId: number | null, args: any[] | null): T {
    const dispatcher = getRequiredDispatcher();
    if (dispatcher.invokeDotNetFromJS) {
      const byteArrays : Uint8Array[] = [];
      const argsJson = JSON.stringify(args, (key, value) => argReplacer(key, value, byteArrays));
      const resultJson = dispatcher.invokeDotNetFromJS(assemblyName, methodIdentifier, dotNetObjectId, argsJson, byteArrays);
      return resultJson ? parseJsonWithRevivers(resultJson) : null;
    } else {
      throw new Error('The current dispatcher does not support synchronous calls from JS to .NET. Use invokeMethodAsync instead.');
    }
  }

  function invokePossibleInstanceMethodAsync<T>(assemblyName: string | null, methodIdentifier: string, dotNetObjectId: number | null, args: any[] | null): Promise<T> {
    if (assemblyName && dotNetObjectId) {
      throw new Error(`For instance method calls, assemblyName should be null. Received '${assemblyName}'.`) ;
    }

    const asyncCallId = nextAsyncCallId++;
    const resultPromise = new Promise<T>((resolve, reject) => {
      pendingAsyncCalls[asyncCallId] = { resolve, reject };
    });

    try {
      const byteArrays : Uint8Array[] = [];
      const argsJson = JSON.stringify(args, (key, value) => argReplacer(key, value, byteArrays));
      getRequiredDispatcher().beginInvokeDotNetFromJS(asyncCallId, assemblyName, methodIdentifier, dotNetObjectId, argsJson, byteArrays);
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

    throw new Error('No .NET call dispatcher has been set.');
  }

  function completePendingCall(callId: number, success: boolean, resultOrError: any) {
    if (!pendingAsyncCalls.hasOwnProperty(callId)) {
      throw new Error(`There is no pending async call with ID ${callId}.`);
    }

    const asyncCall = pendingAsyncCalls[callId];
    delete pendingAsyncCalls[callId];
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
  export enum JSCallResultType {
    Default = 0,
    JSObjectReference = 1
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
     * @param byteArrays Byte array data extracted from the arguments for direct transfer.
     * @returns JSON representation of the result of the invocation.
     */
    invokeDotNetFromJS?(assemblyName: string | null, methodIdentifier: string, dotNetObjectId: number | null, argsJson: string, byteArrays: Uint8Array[] | null): string | null;

    /**
     * Invoked by the runtime to begin an asynchronous call to a .NET method.
     *
     * @param callId A value identifying the asynchronous operation. This value should be passed back in a later call from .NET to JS.
     * @param assemblyName The short name (without key/version or .dll extension) of the .NET assembly holding the method to invoke. The value may be null when invoking instance methods.
     * @param methodIdentifier The identifier of the method to invoke. The method must have a [JSInvokable] attribute specifying this identifier.
     * @param dotNetObjectId If given, the call will be to an instance method on the specified DotNetObject. Pass null to call static methods.
     * @param argsJson JSON representation of arguments to pass to the method.
     * @param byteArrays Byte array data extracted from the arguments for direct transfer.
     */
    beginInvokeDotNetFromJS(callId: number, assemblyName: string | null, methodIdentifier: string, dotNetObjectId: number | null, argsJson: string, byteArrays: Uint8Array[] | null): void;

    /**
     * Invoked by the runtime to complete an asynchronous JavaScript function call started from .NET
     *
     * @param callId A value identifying the asynchronous operation.
     * @param succeded Whether the operation succeeded or not.
     * @param resultOrError The serialized result or the serialized error from the async operation.
     * @param byteArrays Byte array data extracted from the arguments for direct transfer.
     */
    endInvokeJSFromDotNet(callId: number, succeeded: boolean, resultOrError: any, byteArrays: Uint8Array[] | null): void;
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
     * @param byteArrays Byte array data extracted from the arguments for direct transfer.
     * @param resultType The type of result expected from the JS interop call.
     * @param targetInstanceId The instance ID of the target JS object.
     * @returns JSON representation of the invocation result.
     */
    invokeJSFromDotNet: (identifier: string, argsJson: string, byteArrays: Uint8Array[], resultType: JSCallResultType, targetInstanceId: number) => {
      const returnValue = findJSFunction(identifier, targetInstanceId).apply(null, parseJsonWithRevivers(argsJson, byteArrays));
      const result = createJSCallResult(returnValue, resultType);

      const returnedByteArrays : Uint8Array[] = [];
      return result === null || result === undefined
        ? null
        : JSON.stringify(result, (key, value) => argReplacer(key, value, returnedByteArrays)); // TODO; what is this used for? Seems like it doesn't actually invoke anything
    },

    /**
     * Invokes the specified synchronous or asynchronous JavaScript function.
     *
     * @param asyncHandle A value identifying the asynchronous operation. This value will be passed back in a later call to endInvokeJSFromDotNet.
     * @param identifier Identifies the globally-reachable function to invoke.
     * @param argsJson JSON representation of arguments to be passed to the function.
     * @param byteArrays Byte array data extracted from the arguments for direct transfer.
     * @param resultType The type of result expected from the JS interop call.
     * @param targetInstanceId The ID of the target JS object instance.
     */
    beginInvokeJSFromDotNet: (asyncHandle: number, identifier: string, argsJson: string, byteArrays: Uint8Array[], resultType: JSCallResultType, targetInstanceId: number): void => {
      // Coerce synchronous functions into async ones, plus treat
      // synchronous exceptions the same as async ones
      const promise = new Promise<any>(resolve => {
        const synchronousResultOrPromise = findJSFunction(identifier, targetInstanceId).apply(null, parseJsonWithRevivers(argsJson, byteArrays));
        resolve(synchronousResultOrPromise);
      });

      // We only listen for a result if the caller wants to be notified about it
      if (asyncHandle) {
        // On completion, dispatch result back to .NET
        // Not using "await" because it codegens a lot of boilerplate
        const byteArrays : Uint8Array[] = [];
        promise.then(
          result => getRequiredDispatcher().endInvokeJSFromDotNet(asyncHandle, true, JSON.stringify([asyncHandle, true, createJSCallResult(result, resultType)], (key, value) => argReplacer(key, value, byteArrays)), byteArrays),
          error => getRequiredDispatcher().endInvokeJSFromDotNet(asyncHandle, false, JSON.stringify([asyncHandle, false, formatError(error)]), null)
        );
      }
    },

    /**
     * Receives notification that an async call from JS to .NET has completed.
     * @param callId The identifier supplied in an earlier call to beginInvokeDotNetFromJS.
     * @param success A flag to indicate whether the operation completed successfully.
     * @param resultOrExceptionMessage Either the operation result or an error message.
     * @param byteArrays Byte array data extracted from the arguments for direct transfer.
     */
    endInvokeDotNetFromJS: (callId: string, success: boolean, resultOrExceptionMessage: any): void => {
      const resultOrError = success ? resultOrExceptionMessage : new Error(resultOrExceptionMessage);
      completePendingCall(parseInt(callId), success, resultOrError);
    }
  }

  function formatError(error: any): string {
    if (error instanceof Error) {
      return `${error.message}\n${error.stack}`;
    } else {
      return error ? error.toString() : 'null';
    }
  }

  function findJSFunction(identifier: string, targetInstanceId: number): Function {
    let targetInstance = cachedJSObjectsById[targetInstanceId];

    if (targetInstance) {
      return targetInstance.findFunction(identifier);
    } else {
      throw new Error(`JS object instance with ID ${targetInstanceId} does not exist (has it been disposed?).`);
    }
  }

  function disposeJSObjectReferenceById(id: number) {
    delete cachedJSObjectsById[id];
  }

  class DotNetObject {
    constructor(private _id: number) {
    }

    public invokeMethod<T>(methodIdentifier: string, ...args: any[]): T {
      return invokePossibleInstanceMethod<T>(null, methodIdentifier, this._id, args);
    }

    public invokeMethodAsync<T>(methodIdentifier: string, ...args: any[]): Promise<T> {
      return invokePossibleInstanceMethodAsync<T>(null, methodIdentifier, this._id, args);
    }

    public dispose() {
      const promise = invokePossibleInstanceMethodAsync<any>(null, '__Dispose', this._id, null);
      promise.catch(error => console.error(error));
    }

    public serializeAsArg() {
      return { __dotNetObject: this._id };
    }
  }

  const dotNetObjectRefKey = '__dotNetObject';
  attachReviver(function reviveDotNetObject(key: any, value: any, byteArrays: Uint8Array[] | undefined) {
    if (value && typeof value === 'object' && value.hasOwnProperty(dotNetObjectRefKey)) {
      return new DotNetObject(value.__dotNetObject);
    }

    // Unrecognized - let another reviver handle it
    return value;
  });

  attachReviver(function reviveJSObjectReference(key: any, value: any, byteArrays: Uint8Array[] | undefined) {
    if (value && typeof value === 'object' && value.hasOwnProperty(jsObjectIdKey)) {
      const id = value[jsObjectIdKey];
      const jsObject = cachedJSObjectsById[id];

      if (jsObject) {
        return jsObject.getWrappedObject();
      } else {
        throw new Error(`JS object instance with ID ${id} does not exist (has it been disposed?).`);
      }
    }

    // Unrecognized - let another reviver handle it
    return value;
  });

  const byteArrayRefKey = '__byte[]';
  attachReviver(function reviveByteArray(key: any, value: any, byteArrays: Uint8Array[] | undefined) {
    if (value && typeof value === 'object' && value.hasOwnProperty(byteArrayRefKey) && byteArrays) {
      const index = value[byteArrayRefKey];

      if (index >= byteArrays.length || index < 0) {
        throw new Error(`Byte array index '${index}' does not exist.`)
      }

      return byteArrays[value[byteArrayRefKey]];
    }

    // Unrecognized - let another reviver handle it
    return value;
  });

  function createJSCallResult(returnValue: any, resultType: JSCallResultType) {
    switch (resultType) {
      case JSCallResultType.Default:
        return returnValue;
      case JSCallResultType.JSObjectReference:
        return createJSObjectReference(returnValue);
      default:
        throw new Error(`Invalid JS call result type '${resultType}'.`);
    }
  }

  function argReplacer(key: string, value: any, byteArrays: Uint8Array[]) {
    if (value instanceof DotNetObject)
    {
      return value.serializeAsArg();
    }
    else if (value instanceof Uint8Array)
    {
      var index = byteArrays.length;
      byteArrays.push(value);
      return { [byteArrayRefKey]: index };
    }

    return value;
  }
}
