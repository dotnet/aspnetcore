// This is a single-file self-contained module to avoid the need for a Webpack build

module DotNet {
  (window as any).DotNet = DotNet; // Ensure reachable from anywhere

  export type JsonReviver = ((key: any, value: any) => any);
  const jsonRevivers: JsonReviver[] = [];

  const pendingAsyncCalls: { [id: number]: PendingAsyncCall<any> } = {};
  const cachedJSFunctions: { [identifier: string]: Function } = {};
  let nextAsyncCallId = 1; // Start at 1 because zero signals "no response needed"

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
    const dispatcher = getRequiredDispatcher();
    if (dispatcher.invokeDotNetFromJS) {
      const argsJson = JSON.stringify(args);
      return dispatcher.invokeDotNetFromJS(assemblyName, methodIdentifier, argsJson);
    } else {
      throw new Error('The current dispatcher does not support synchronous calls from JS to .NET. Use invokeAsync instead.');
    }
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
    const asyncCallId = nextAsyncCallId++;
    const resultPromise = new Promise<T>((resolve, reject) => {
      pendingAsyncCalls[asyncCallId] = { resolve, reject };
    });

    try {
      const argsJson = JSON.stringify(args);
      getRequiredDispatcher().beginInvokeDotNetFromJS(asyncCallId, assemblyName, methodIdentifier, argsJson);
    } catch(ex) {
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
   * Represents the ability to dispatch calls from JavaScript to a .NET runtime.
   */
  export interface DotNetCallDispatcher {
    /**
     * Optional. If implemented, invoked by the runtime to perform a synchronous call to a .NET method.
     * 
     * @param assemblyName The short name (without key/version or .dll extension) of the .NET assembly holding the method to invoke.
     * @param methodIdentifier The identifier of the method to invoke. The method must have a [JSInvokable] attribute specifying this identifier.
     * @param argsJson JSON representation of arguments to pass to the method.
     * @returns The result of the invocation.
     */
    invokeDotNetFromJS?(assemblyName: string, methodIdentifier: string, argsJson: string): any;

    /**
     * Invoked by the runtime to begin an asynchronous call to a .NET method.
     *
     * @param callId A value identifying the asynchronous operation. This value should be passed back in a later call from .NET to JS.
     * @param assemblyName The short name (without key/version or .dll extension) of the .NET assembly holding the method to invoke.
     * @param methodIdentifier The identifier of the method to invoke. The method must have a [JSInvokable] attribute specifying this identifier.
     * @param argsJson JSON representation of arguments to pass to the method.
     */
    beginInvokeDotNetFromJS(callId: number, assemblyName: string, methodIdentifier: string, argsJson: string): void;
  }

  /**
   * Receives incoming calls from .NET and dispatches them to JavaScript.
   */
  export const jsCallDispatcher = {
    /**
     * Finds the JavaScript function matching the specified identifier.
     *
     * @param identifier Identifies the globally-reachable function to be returned.
     * @returns A Function instance.
     */
    findJSFunction,

    /**
     * Invokes the specified synchronous JavaScript function.
     *
     * @param identifier Identifies the globally-reachable function to invoke.
     * @param argsJson JSON representation of arguments to be passed to the function.
     * @returns JSON representation of the invocation result.
     */
    invokeJSFromDotNet: (identifier: string, argsJson: string) => {
      const result = findJSFunction(identifier).apply(null, parseJsonWithRevivers(argsJson));
      return result === null || result === undefined
        ? null
        : JSON.stringify(result);
    },

    /**
     * Invokes the specified synchronous or asynchronous JavaScript function.
     *
     * @param asyncHandle A value identifying the asynchronous operation. This value will be passed back in a later call to endInvokeJSFromDotNet.
     * @param identifier Identifies the globally-reachable function to invoke.
     * @param argsJson JSON representation of arguments to be passed to the function.
     */
    beginInvokeJSFromDotNet: (asyncHandle: number, identifier: string, argsJson: string): void => {
      // Coerce synchronous functions into async ones, plus treat
      // synchronous exceptions the same as async ones
      const promise = new Promise<any>(resolve => {
        const synchronousResultOrPromise = findJSFunction(identifier).apply(null, parseJsonWithRevivers(argsJson));
        resolve(synchronousResultOrPromise);
      });

      // We only listen for a result if the caller wants to be notified about it
      if (asyncHandle) {
        // On completion, dispatch result back to .NET
        // Not using "await" because it codegens a lot of boilerplate
        promise.then(
          result => getRequiredDispatcher().beginInvokeDotNetFromJS(0, 'Microsoft.JSInterop', 'DotNetDispatcher.EndInvoke', JSON.stringify([asyncHandle, true, result])),
          error => getRequiredDispatcher().beginInvokeDotNetFromJS(0, 'Microsoft.JSInterop', 'DotNetDispatcher.EndInvoke', JSON.stringify([asyncHandle, false, formatError(error)]))
        );
      }
    },

    /**
     * Receives notification that an async call from JS to .NET has completed.
     * @param asyncCallId The identifier supplied in an earlier call to beginInvokeDotNetFromJS.
     * @param success A flag to indicate whether the operation completed successfully.
     * @param resultOrExceptionMessage Either the operation result or an error message.
     */
    endInvokeDotNetFromJS: (asyncCallId: string, success: boolean, resultOrExceptionMessage: any): void => {
      const resultOrError = success ? resultOrExceptionMessage : new Error(resultOrExceptionMessage);
      completePendingCall(parseInt(asyncCallId), success, resultOrError);
    }
  }

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

  function formatError(error: any): string {
    if (error instanceof Error) {
      return `${error.message}\n${error.stack}`;
    } else {
      return error ? error.toString() : 'null';
    }
  }
  
  function findJSFunction(identifier: string): Function {
    if (cachedJSFunctions.hasOwnProperty(identifier)) {
      return cachedJSFunctions[identifier];
    }

    let result: any = window;
    let resultIdentifier = 'window';
    identifier.split('.').forEach(segment => {
      if (segment in result) {
        result = result[segment];
        resultIdentifier += '.' + segment;
      } else {
        throw new Error(`Could not find '${segment}' in '${resultIdentifier}'.`);
      }
    });

    if (result instanceof Function) {
      return result;
    } else {
      throw new Error(`The value '${resultIdentifier}' is not a function.`);
    }
  }
}
