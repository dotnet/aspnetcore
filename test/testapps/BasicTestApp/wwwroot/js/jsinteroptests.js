/// <reference path="../../../../../src/microsoft.jsinterop/javascriptruntime/dist/microsoft.jsinterop.d.ts" />

// We'll store the results from the tests here
var results = {};
var assemblyName = 'BasicTestApp';

function invokeDotNetInteropMethodsAsync() {
  console.log('Invoking void sync methods.');
  DotNet.invokeMethod(assemblyName, 'VoidParameterless');
  DotNet.invokeMethod(assemblyName, 'VoidWithOneParameter', ...createArgumentList(1));
  DotNet.invokeMethod(assemblyName, 'VoidWithTwoParameters', ...createArgumentList(2));
  DotNet.invokeMethod(assemblyName, 'VoidWithThreeParameters', ...createArgumentList(3));
  DotNet.invokeMethod(assemblyName, 'VoidWithFourParameters', ...createArgumentList(4));
  DotNet.invokeMethod(assemblyName, 'VoidWithFiveParameters', ...createArgumentList(5));
  DotNet.invokeMethod(assemblyName, 'VoidWithSixParameters', ...createArgumentList(6));
  DotNet.invokeMethod(assemblyName, 'VoidWithSevenParameters', ...createArgumentList(7));
  DotNet.invokeMethod(assemblyName, 'VoidWithEightParameters', ...createArgumentList(8));

  console.log('Invoking returning sync methods.');
  results['result1'] = DotNet.invokeMethod(assemblyName, 'ReturnArray');
  results['result2'] = DotNet.invokeMethod(assemblyName, 'EchoOneParameter', ...createArgumentList(1));
  results['result3'] = DotNet.invokeMethod(assemblyName, 'EchoTwoParameters', ...createArgumentList(2));
  results['result4'] = DotNet.invokeMethod(assemblyName, 'EchoThreeParameters', ...createArgumentList(3));
  results['result5'] = DotNet.invokeMethod(assemblyName, 'EchoFourParameters', ...createArgumentList(4));
  results['result6'] = DotNet.invokeMethod(assemblyName, 'EchoFiveParameters', ...createArgumentList(5));
  results['result7'] = DotNet.invokeMethod(assemblyName, 'EchoSixParameters', ...createArgumentList(6));
  results['result8'] = DotNet.invokeMethod(assemblyName, 'EchoSevenParameters', ...createArgumentList(7));
  results['result9'] = DotNet.invokeMethod(assemblyName, 'EchoEightParameters', ...createArgumentList(8));

  console.log('Invoking void async methods.');
  return DotNet.invokeMethodAsync(assemblyName, 'VoidParameterlessAsync')
    .then(() => DotNet.invokeMethodAsync(assemblyName, 'VoidWithOneParameterAsync', ...createArgumentList(1)))
    .then(() => DotNet.invokeMethodAsync(assemblyName, 'VoidWithTwoParametersAsync', ...createArgumentList(2)))
    .then(() => DotNet.invokeMethodAsync(assemblyName, 'VoidWithThreeParametersAsync', ...createArgumentList(3)))
    .then(() => DotNet.invokeMethodAsync(assemblyName, 'VoidWithFourParametersAsync', ...createArgumentList(4)))
    .then(() => DotNet.invokeMethodAsync(assemblyName, 'VoidWithFiveParametersAsync', ...createArgumentList(5)))
    .then(() => DotNet.invokeMethodAsync(assemblyName, 'VoidWithSixParametersAsync', ...createArgumentList(6)))
    .then(() => DotNet.invokeMethodAsync(assemblyName, 'VoidWithSevenParametersAsync', ...createArgumentList(7)))
    .then(() => DotNet.invokeMethodAsync(assemblyName, 'VoidWithEightParametersAsync', ...createArgumentList(8)))
    .then(() => {
      console.log('Invoking returning async methods.');
      return DotNet.invokeMethodAsync(assemblyName, 'ReturnArrayAsync')
        .then(r => results['result1Async'] = r)
        .then(() => DotNet.invokeMethodAsync(assemblyName, 'EchoOneParameterAsync', ...createArgumentList(1)))
        .then(r => results['result2Async'] = r)
        .then(() => DotNet.invokeMethodAsync(assemblyName, 'EchoTwoParametersAsync', ...createArgumentList(2)))
        .then(r => results['result3Async'] = r)
        .then(() => DotNet.invokeMethodAsync(assemblyName, 'EchoThreeParametersAsync', ...createArgumentList(3)))
        .then(r => results['result4Async'] = r)
        .then(() => DotNet.invokeMethodAsync(assemblyName, 'EchoFourParametersAsync', ...createArgumentList(4)))
        .then(r => results['result5Async'] = r)
        .then(() => DotNet.invokeMethodAsync(assemblyName, 'EchoFiveParametersAsync', ...createArgumentList(5)))
        .then(r => results['result6Async'] = r)
        .then(() => DotNet.invokeMethodAsync(assemblyName, 'EchoSixParametersAsync', ...createArgumentList(6)))
        .then(r => results['result7Async'] = r)
        .then(() => DotNet.invokeMethodAsync(assemblyName, 'EchoSevenParametersAsync', ...createArgumentList(7)))
        .then(r => results['result8Async'] = r)
        .then(() => DotNet.invokeMethodAsync(assemblyName, 'EchoEightParametersAsync', ...createArgumentList(8)))
        .then(r => results['result9Async'] = r);
    })
    .then(() => {
      console.log('Invoking methods that throw exceptions');
      try {
        DotNet.invokeMethod(assemblyName, 'ThrowException');
      } catch (e) {
        results['ThrowException'] = e.message;
      }

      return DotNet.invokeMethodAsync(assemblyName, 'AsyncThrowSyncException')
        .catch(e => {
          results['AsyncThrowSyncException'] = e.message;

          return DotNet.invokeMethodAsync(assemblyName, 'AsyncThrowAsyncException');
        }).catch(e => {
          results['AsyncThrowAsyncException'] = e.message;

          console.log('Done invoking interop methods');
        });
    });
}

function createArgumentList(argumentNumber){
  const array = new Array(argumentNumber);
  if (argumentNumber === 0) {
    return [];
  }
  for (var i = 0; i < argumentNumber; i++) {
    switch (i) {
      case 0:
        array[i] = {
          id: argumentNumber,
          isValid: argumentNumber % 2 === 0,
          data: {
            source: `Some random text with at least ${argumentNumber} characters`,
            start: argumentNumber,
            length: argumentNumber
          }
        };
        break;
      case 1:
        array[i] = argumentNumber;
        break;
      case 2:
        array[i] = argumentNumber * 2;
        break;
      case 3:
        array[i] = argumentNumber * 4;
        break;
      case 4:
        array[i] = argumentNumber * 8;
        break;
      case 5:
        array[i] = argumentNumber + 0.25;
        break;
      case 6:
        array[i] = Array.apply(null, Array(argumentNumber)).map((v, i) => i + 0.5);
        break;
      case 7:
        array[i] = {
          source: `Some random text with at least ${i} characters`,
          start: argumentNumber + 1,
          length: argumentNumber + 1
        }
        break;
      default:
        console.log(i);
        throw new Error('Invalid argument count!');
    }
  }

  return array;
}

window.jsInteropTests = {
  invokeDotNetInteropMethodsAsync: invokeDotNetInteropMethodsAsync,
  collectInteropResults: collectInteropResults,
  functionThrowsException: functionThrowsException,
  asyncFunctionThrowsSyncException: asyncFunctionThrowsSyncException,
  asyncFunctionThrowsAsyncException: asyncFunctionThrowsAsyncException
};

function functionThrowsException() {
  throw new Error('Function threw an exception!');
}

function asyncFunctionThrowsSyncException() {
  throw new Error('Function threw a sync exception!');
}

function asyncFunctionThrowsAsyncException() {
  return new Promise((resolve, reject) => {
    setTimeout(() => reject(new Error('Function threw an async exception!')), 3000);
  });
}

function collectInteropResults() {
  let result = {};
  let properties = Object.getOwnPropertyNames(results);
  for (let i = 0; i < properties.length; i++) {
    let property = properties[i];
    result[property] = btoa(JSON.stringify(results[property]));
  }

  return result;
}
