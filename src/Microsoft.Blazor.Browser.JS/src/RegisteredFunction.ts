const registeredFunctions = {};

// Code in Mono 'driver.c' looks for the registered functions here
window['__blazorRegisteredFunctions'] = registeredFunctions;

export function registerFunction(identifier: string, implementation: Function) {
  if (registeredFunctions.hasOwnProperty(identifier)) {
    throw new Error(`A function with the identifier '${identifier}' has already been registered.`);
  }

  registeredFunctions[identifier] = implementation;
}
