import { platform } from '../Environment';
import { System_String } from '../Platform/Platform';
import { getRegisteredFunction } from './RegisteredFunction';
import { getElementByCaptureId } from '../Rendering/ElementReferenceCapture';

const elementRefKey = '_blazorElementRef'; // Keep in sync with ElementRef.cs

export function invokeWithJsonMarshalling(identifier: System_String, ...argsJson: System_String[]) {
  const identifierJsString = platform.toJavaScriptString(identifier);
  const funcInstance = getRegisteredFunction(identifierJsString);
  const args = argsJson.map(json => JSON.parse(platform.toJavaScriptString(json), jsonReviver));
  const result = funcInstance.apply(null, args);
  if (result !== null && result !== undefined) {
    const resultJson = JSON.stringify(result);
    return platform.toDotNetString(resultJson);
  } else {
    return null;
  }
}

function jsonReviver(key: string, value: any): any {
  if (value && typeof value === 'object' && value.hasOwnProperty(elementRefKey) && typeof value[elementRefKey] === 'number') {
    return getElementByCaptureId(value[elementRefKey]);
  }

  return value;
}