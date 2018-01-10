import { platform } from '../Environment';
import { System_String } from '../Platform/Platform';
import { getRegisteredFunction } from './RegisteredFunction';

export function invokeWithJsonMarshalling(identifier: System_String, ...argsJson: System_String[]) {
  const identifierJsString = platform.toJavaScriptString(identifier);
  const funcInstance = getRegisteredFunction(identifierJsString);
  const args = argsJson.map(json => JSON.parse(platform.toJavaScriptString(json)));
  const result = funcInstance.apply(null, args);
  if (result !== null && result !== undefined) {
    const resultJson = JSON.stringify(result);
    return platform.toDotNetString(resultJson);
  } else {
    return null;
  }
}
