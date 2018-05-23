import { platform } from '../Environment';
import { System_String, Pointer, MethodHandle } from '../Platform/Platform';
import { getRegisteredFunction } from './RegisteredFunction';
import { error } from 'util';

export interface MethodOptions {
  type: TypeIdentifier;
  method: MethodIdentifier;
}

// Keep in sync with InvocationResult.cs
export interface InvocationResult {
  succeeded: boolean;
  result?: any;
  message?: string;
}

export interface MethodIdentifier {
  name: string;
  typeArguments?: { [key: string]: TypeIdentifier }
  parameterTypes?: TypeIdentifier[];
}

export interface TypeIdentifier {
  assembly: string;
  name: string;
  typeArguments?: { [key: string]: TypeIdentifier };
}

export function invokeDotNetMethod<T>(methodOptions: MethodOptions, ...args: any[]): (T | null) {
  return invokeDotNetMethodCore(methodOptions, null, ...args);
}

const registrations = {};
let findDotNetMethodHandle: MethodHandle;

function getFindDotNetMethodHandle() {
  if (findDotNetMethodHandle === undefined) {
    findDotNetMethodHandle = platform.findMethod(
      'Microsoft.AspNetCore.Blazor.Browser',
      'Microsoft.AspNetCore.Blazor.Browser.Interop',
      'InvokeDotNetFromJavaScript',
      'FindDotNetMethod');
  }
  return findDotNetMethodHandle;
}

function resolveRegistration(methodOptions: MethodOptions) {
  const findDotNetMethodHandle = getFindDotNetMethodHandle();
  const assemblyEntry = registrations[methodOptions.type.assembly];
  const typeEntry = assemblyEntry && assemblyEntry[methodOptions.type.name];
  const registration = typeEntry && typeEntry[methodOptions.method.name];
  if (registration !== undefined) {
    return registration;
  } else {

    const serializedOptions = platform.toDotNetString(JSON.stringify(methodOptions));
    const result = platform.callMethod(findDotNetMethodHandle, null, [serializedOptions]);
    const registration = platform.toJavaScriptString(result as System_String);

    if (assemblyEntry === undefined) {
      const assembly = {};
      const type = {};
      registrations[methodOptions.type.assembly] = assembly;
      assembly[methodOptions.type.name] = type;
      type[methodOptions.method.name] = registration;
    } else if (typeEntry === undefined) {
      const type = {};
      assemblyEntry[methodOptions.type.name] = type;
      type[methodOptions.method.name] = registration;
    } else {
      typeEntry[methodOptions.method.name] = registration;
    }

    return registration;
  }
}

let invokeDotNetMethodHandle: MethodHandle;

function getInvokeDotNetMethodHandle() {
  if (invokeDotNetMethodHandle === undefined) {
    invokeDotNetMethodHandle = platform.findMethod(
      'Microsoft.AspNetCore.Blazor.Browser',
      'Microsoft.AspNetCore.Blazor.Browser.Interop',
      'InvokeDotNetFromJavaScript',
      'InvokeDotNetMethod');
  }
  return invokeDotNetMethodHandle;
}

function invokeDotNetMethodCore<T>(methodOptions: MethodOptions, callbackId: string | null, ...args: any[]): (T | null) {
  const invokeDotNetMethodHandle = getInvokeDotNetMethodHandle();
  const registration = resolveRegistration(methodOptions);

  const packedArgs = packArguments(args);

  const serializedCallback = callbackId != null ? platform.toDotNetString(callbackId) : null;
  const serializedArgs = platform.toDotNetString(JSON.stringify(packedArgs));
  const serializedRegistration = platform.toDotNetString(registration);
  const serializedResult = platform.callMethod(invokeDotNetMethodHandle, null, [serializedRegistration, serializedCallback, serializedArgs]);

  const result = JSON.parse(platform.toJavaScriptString(serializedResult as System_String));
  if (result.succeeded) {
    return result.result;
  } else {
    throw new Error(result.message);
  }
}

// We don't have to worry about overflows here. Number.MAX_SAFE_INTEGER in JS is 2^53-1
let globalId = 0;

export function invokeDotNetMethodAsync<T>(methodOptions: MethodOptions, ...args: any[]): Promise<T | null> {
  const callbackId = (globalId++).toString();

  const result = new Promise<T | null>((resolve, reject) => {
    TrackedReference.track(callbackId, (invocationResult: InvocationResult) => {
      // We got invoked, so we unregister ourselves.
      TrackedReference.untrack(callbackId);
      if (invocationResult.succeeded) {
        resolve(invocationResult.result);
      } else {
        reject(new Error(invocationResult.message));
      }
    });
  });

  invokeDotNetMethodCore(methodOptions, callbackId, ...args);

  return result;
}

export function invokePromiseCallback(id: string, invocationResult: InvocationResult): void {
  const callback = TrackedReference.get(id) as Function;
  callback.call(null, invocationResult);
}

function packArguments(args: any[]) {
  const result = {};
  if (args.length == 0) {
    return result;
  }

  if (args.length > 7) {
    for (let i = 0; i < 7; i++) {
      result[`argument${[i + 1]}`] = args[i];
    }
    result['argument8'] = packArguments(args.slice(7));
  } else {
    for (let i = 0; i < args.length; i++) {
      result[`argument${[i + 1]}`] = args[i];
    }
  }

  return result;
}

class TrackedReference {
  private static references: { [key: string]: any } = {};

  public static track(id: string, trackedObject: any): void {
    const refs = TrackedReference.references;
    if (refs[id] !== undefined) {
      throw new Error(`An element with id '${id}' is already being tracked.`);
    }

    refs[id] = trackedObject;
  }

  public static untrack(id: string): void {
    const refs = TrackedReference.references;
    const result = refs[id];
    if (result === undefined) {
      throw new Error(`An element with id '${id}' is not being being tracked.`);
    }

    refs[id] = undefined;
  }

  public static get(id: string): any {
    const refs = TrackedReference.references;
    const result = refs[id];
    if (result === undefined) {
      throw new Error(`An element with id '${id}' is not being being tracked.`);
    }

    return result;
  }
}
