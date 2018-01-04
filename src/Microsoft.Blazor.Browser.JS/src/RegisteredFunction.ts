import { System_String } from './Platform/Platform';
import { platform } from './Environment';

const registeredFunctions: { [identifier: string]: Function } = {};

// Code in Mono 'driver.c' looks for the registered functions here
window['__blazorRegisteredFunctions'] = registeredFunctions;

export function registerFunction(identifier: string, implementation: Function) {
  if (registeredFunctions.hasOwnProperty(identifier)) {
    throw new Error(`A function with the identifier '${identifier}' has already been registered.`);
  }

  registeredFunctions[identifier] = implementation;
}

// Handle the JSON-marshalled RegisteredFunction.Invoke calls
registerFunction('__blazor_InvokeJson', (identifier: System_String, ...argsJson: System_String[]) => {
  const identifierJsString = platform.toJavaScriptString(identifier);
  if (!(registeredFunctions && registeredFunctions.hasOwnProperty(identifierJsString))) {
    throw new Error(`Could not find registered function with name "${identifierJsString}".`);
  }
  const funcInstance = registeredFunctions[identifierJsString];
  const args = argsJson.map(json => JSON.parse(platform.toJavaScriptString(json)));
  const result = funcInstance.apply(null, args);
  if (result !== null && result !== undefined) {
    const resultJson = JSON.stringify(result);
    return platform.toDotNetString(resultJson);
  } else {
    return null;
  }
});
