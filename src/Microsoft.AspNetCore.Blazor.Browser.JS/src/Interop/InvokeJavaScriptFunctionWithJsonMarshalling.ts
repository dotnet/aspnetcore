import { platform } from '../Environment';
import { System_String } from '../Platform/Platform';
import { getRegisteredFunction } from './RegisteredFunction';
import { invokeDotNetMethod, MethodOptions, InvocationResult } from './InvokeDotNetMethodWithJsonMarshalling';
import { getElementByCaptureId } from '../Rendering/ElementReferenceCapture';
import { System } from 'typescript';
import { error } from 'util';

const elementRefKey = '_blazorElementRef'; // Keep in sync with ElementRef.cs

export function invokeWithJsonMarshalling(identifier: System_String, ...argsJson: System_String[]) {
  let result: InvocationResult;
  const identifierJsString = platform.toJavaScriptString(identifier);
  const args = argsJson.map(json => JSON.parse(platform.toJavaScriptString(json), jsonReviver));

  try {
    result = { succeeded: true, result: invokeWithJsonMarshallingCore(identifierJsString, ...args) };
  } catch (e) {
    result = { succeeded: false, message: e instanceof Error ? `${e.message}\n${e.stack}` : (e ? e.toString() : null) };
  }

  const resultJson = JSON.stringify(result);
  return platform.toDotNetString(resultJson);
}

function invokeWithJsonMarshallingCore(identifier: string, ...args: any[]) {
  const funcInstance = getRegisteredFunction(identifier);
  const result = funcInstance.apply(null, args);
  if (result !== null && result !== undefined) {
    return result;
  } else {
    return null;
  }
}

const invokeDotNetCallback: MethodOptions = {
  type: {
    assembly: 'Microsoft.AspNetCore.Blazor.Browser',
    name: 'Microsoft.AspNetCore.Blazor.Browser.Interop.TaskCallback'
  },
  method: {
    name: 'InvokeTaskCallback'
  }
};

export function invokeWithJsonMarshallingAsync<T>(identifier: string, callbackId: string, ...argsJson: string[]) {
  const result = invokeWithJsonMarshallingCore(identifier, ...argsJson) as Promise<any>;

  result
    .then(res => invokeDotNetMethod(invokeDotNetCallback, callbackId, JSON.stringify({ succeeded: true, result: res })))
    .catch(reason => invokeDotNetMethod(
      invokeDotNetCallback,
      callbackId,
      JSON.stringify({ succeeded: false, message: (reason && reason.message) || (reason && reason.toString && reason.toString()) })));

  return null;
}


function jsonReviver(key: string, value: any): any {
  if (value && typeof value === 'object' && value.hasOwnProperty(elementRefKey) && typeof value[elementRefKey] === 'number') {
    return getElementByCaptureId(value[elementRefKey]);
  }

  return value;
}
