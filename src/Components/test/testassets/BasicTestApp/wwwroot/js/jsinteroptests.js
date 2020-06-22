// We'll store the results from the tests here
var results = {};
var assemblyName = 'BasicTestApp';

async function invokeDotNetInteropMethodsAsync(shouldSupportSyncInterop, dotNetObjectByRef, instanceMethodsTarget, genericDotNetObjectByRef) {
  if (shouldSupportSyncInterop) {
    console.log('Invoking void sync methods.');
    DotNet.invokeMethod(assemblyName, 'VoidParameterless');
    DotNet.invokeMethod(assemblyName, 'VoidWithOneParameter', ...createArgumentList(1, dotNetObjectByRef));
    DotNet.invokeMethod(assemblyName, 'VoidWithTwoParameters', ...createArgumentList(2, dotNetObjectByRef));
    DotNet.invokeMethod(assemblyName, 'VoidWithThreeParameters', ...createArgumentList(3, dotNetObjectByRef));
    DotNet.invokeMethod(assemblyName, 'VoidWithFourParameters', ...createArgumentList(4, dotNetObjectByRef));
    DotNet.invokeMethod(assemblyName, 'VoidWithFiveParameters', ...createArgumentList(5, dotNetObjectByRef));
    DotNet.invokeMethod(assemblyName, 'VoidWithSixParameters', ...createArgumentList(6, dotNetObjectByRef));
    DotNet.invokeMethod(assemblyName, 'VoidWithSevenParameters', ...createArgumentList(7, dotNetObjectByRef));
    DotNet.invokeMethod(assemblyName, 'VoidWithEightParameters', ...createArgumentList(8, dotNetObjectByRef));


    console.log('Invoking returning sync methods.');
    results['result1'] = DotNet.invokeMethod(assemblyName, 'ReturnArray');
    results['result2'] = DotNet.invokeMethod(assemblyName, 'EchoOneParameter', ...createArgumentList(1, dotNetObjectByRef));
    results['result3'] = DotNet.invokeMethod(assemblyName, 'EchoTwoParameters', ...createArgumentList(2, dotNetObjectByRef));
    results['result4'] = DotNet.invokeMethod(assemblyName, 'EchoThreeParameters', ...createArgumentList(3, dotNetObjectByRef));
    results['result5'] = DotNet.invokeMethod(assemblyName, 'EchoFourParameters', ...createArgumentList(4, dotNetObjectByRef));
    results['result6'] = DotNet.invokeMethod(assemblyName, 'EchoFiveParameters', ...createArgumentList(5, dotNetObjectByRef));
    results['result7'] = DotNet.invokeMethod(assemblyName, 'EchoSixParameters', ...createArgumentList(6, dotNetObjectByRef));
    results['result8'] = DotNet.invokeMethod(assemblyName, 'EchoSevenParameters', ...createArgumentList(7, dotNetObjectByRef));
    results['result9'] = DotNet.invokeMethod(assemblyName, 'EchoEightParameters', ...createArgumentList(8, dotNetObjectByRef));

    var returnDotNetObjectByRefResult = DotNet.invokeMethod(assemblyName, 'ReturnDotNetObjectByRef');
    results['resultReturnDotNetObjectByRefSync'] = DotNet.invokeMethod(assemblyName, 'ExtractNonSerializedValue', returnDotNetObjectByRefResult['Some sync instance']);

    var instanceMethodResult = instanceMethodsTarget.invokeMethod('InstanceMethod', {
      stringValue: 'My string',
      dtoByRef: dotNetObjectByRef
    });
    results['instanceMethodThisTypeName'] = instanceMethodResult.thisTypeName;
    results['instanceMethodStringValueUpper'] = instanceMethodResult.stringValueUpper;
    results['instanceMethodIncomingByRef'] = instanceMethodResult.incomingByRef;
    results['instanceMethodOutgoingByRef'] = DotNet.invokeMethod(assemblyName, 'ExtractNonSerializedValue', instanceMethodResult.outgoingByRef);
  }

  console.log('Invoking void async methods.');

  await DotNet.invokeMethodAsync(assemblyName, 'VoidParameterlessAsync');
  await DotNet.invokeMethodAsync(assemblyName, 'VoidWithOneParameterAsync', ...createArgumentList(1, dotNetObjectByRef));
  await DotNet.invokeMethodAsync(assemblyName, 'VoidWithTwoParametersAsync', ...createArgumentList(2, dotNetObjectByRef));
  await DotNet.invokeMethodAsync(assemblyName, 'VoidWithThreeParametersAsync', ...createArgumentList(3, dotNetObjectByRef));
  await DotNet.invokeMethodAsync(assemblyName, 'VoidWithFourParametersAsync', ...createArgumentList(4, dotNetObjectByRef));
  await DotNet.invokeMethodAsync(assemblyName, 'VoidWithFiveParametersAsync', ...createArgumentList(5, dotNetObjectByRef));
  await DotNet.invokeMethodAsync(assemblyName, 'VoidWithSixParametersAsync', ...createArgumentList(6, dotNetObjectByRef));
  await DotNet.invokeMethodAsync(assemblyName, 'VoidWithSevenParametersAsync', ...createArgumentList(7, dotNetObjectByRef));
  await DotNet.invokeMethodAsync(assemblyName, 'VoidWithEightParametersAsync', ...createArgumentList(8, dotNetObjectByRef));

  console.log('Invoking returning async methods.');
  results['result1Async'] = await DotNet.invokeMethodAsync(assemblyName, 'ReturnArrayAsync');
  results['result2Async'] = await DotNet.invokeMethodAsync(assemblyName, 'EchoOneParameterAsync', ...createArgumentList(1, dotNetObjectByRef));
  results['result3Async'] = await DotNet.invokeMethodAsync(assemblyName, 'EchoTwoParametersAsync', ...createArgumentList(2, dotNetObjectByRef));
  results['result4Async'] = await DotNet.invokeMethodAsync(assemblyName, 'EchoThreeParametersAsync', ...createArgumentList(3, dotNetObjectByRef));
  results['result5Async'] = await DotNet.invokeMethodAsync(assemblyName, 'EchoFourParametersAsync', ...createArgumentList(4, dotNetObjectByRef));
  results['result6Async'] = await DotNet.invokeMethodAsync(assemblyName, 'EchoFiveParametersAsync', ...createArgumentList(5, dotNetObjectByRef));
  results['result7Async'] = await DotNet.invokeMethodAsync(assemblyName, 'EchoSixParametersAsync', ...createArgumentList(6, dotNetObjectByRef));
  results['result8Async'] = await DotNet.invokeMethodAsync(assemblyName, 'EchoSevenParametersAsync', ...createArgumentList(7, dotNetObjectByRef));
  results['result9Async'] = await DotNet.invokeMethodAsync(assemblyName, 'EchoEightParametersAsync', ...createArgumentList(8, dotNetObjectByRef));

  const returnDotNetObjectByRefAsync = await DotNet.invokeMethodAsync(assemblyName, 'ReturnDotNetObjectByRefAsync');
  results['resultReturnDotNetObjectByRefAsync'] = await DotNet.invokeMethodAsync(assemblyName, 'ExtractNonSerializedValue', returnDotNetObjectByRefAsync['Some async instance']);

  const instanceMethodAsync = await instanceMethodsTarget.invokeMethodAsync('InstanceMethodAsync', {
    stringValue: 'My string',
    dtoByRef: dotNetObjectByRef
  });

  results['instanceMethodThisTypeNameAsync'] = instanceMethodAsync.thisTypeName;
  results['instanceMethodStringValueUpperAsync'] = instanceMethodAsync.stringValueUpper;
  results['instanceMethodIncomingByRefAsync'] = instanceMethodAsync.incomingByRef;
  results['instanceMethodOutgoingByRefAsync'] = await DotNet.invokeMethodAsync(assemblyName, 'ExtractNonSerializedValue', instanceMethodAsync.outgoingByRef);

  console.log('Invoking generic type instance methods.');

  results['syncGenericInstanceMethod'] = await genericDotNetObjectByRef.invokeMethodAsync('Update', 'Updated value 1');
  results['asyncGenericInstanceMethod'] = await genericDotNetObjectByRef.invokeMethodAsync('UpdateAsync', 'Updated value 2');


  if (shouldSupportSyncInterop) {
    results['genericInstanceMethod'] = genericDotNetObjectByRef.invokeMethod('Update', 'Updated Value 3');
  }

  console.log('Invoking methods that throw exceptions');
  try {
    shouldSupportSyncInterop && DotNet.invokeMethod(assemblyName, 'ThrowException');
  } catch (e) {
    results['ThrowException'] = e.message;
  }

  try {
    await DotNet.invokeMethodAsync(assemblyName, 'AsyncThrowSyncException');
  } catch (e) {
    results['AsyncThrowSyncException'] = e.message;
  }

  try {
    await DotNet.invokeMethodAsync(assemblyName, 'AsyncThrowAsyncException');
  } catch (e) {
    results['AsyncThrowAsyncException'] = e.message;
  }

  console.log('Done invoking interop methods');
}

function createArgumentList(argumentNumber, dotNetObjectByRef) {
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
        array[i] = dotNetObjectByRef;
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
        };
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
  asyncFunctionThrowsAsyncException: asyncFunctionThrowsAsyncException,
  returnPrimitive: returnPrimitive,
  returnPrimitiveAsync: returnPrimitiveAsync,
  receiveDotNetObjectByRef: receiveDotNetObjectByRef,
  receiveDotNetObjectByRefAsync: receiveDotNetObjectByRefAsync
};

function returnPrimitive() {
  return 123;
}

function returnPrimitiveAsync() {
  return new Promise((resolve, reject) => {
    setTimeout(function () {
      resolve(123);
    }, 100);
  });
}

function returnArray() {
  return [{ source: 'first' }, { source: 'second' }];
}

function returnArrayAsync() {
  return new Promise((resolve, reject) => {
    setTimeout(function () {
      resolve(returnArray());
    }, 100);
  });
}

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

function asyncFunctionTakesLongerThanDefaultTimeoutToResolve() {
  return new Promise((resolve, reject) => {
    setTimeout(() => resolve(undefined), 5000);
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

function receiveDotNetObjectByRef(incomingData) {
  const stringValue = incomingData.stringValue;
  const testDto = incomingData.testDto;

  // To verify we received a proper reference to testDto, pass it back into .NET
  // to have it evaluate something that only .NET can know
  const testDtoNonSerializedValue = DotNet.invokeMethod(assemblyName, 'ExtractNonSerializedValue', testDto);

  // To show we can return a .NET object by ref anywhere in a complex structure,
  // return it among other values
  return {
    stringValueUpper: stringValue.toUpperCase(),
    testDtoNonSerializedValue: testDtoNonSerializedValue,
    testDto: testDto
  };
}

function receiveDotNetObjectByRefAsync(incomingData) {
  const stringValue = incomingData.stringValue;
  const testDto = incomingData.testDto;

  // To verify we received a proper reference to testDto, pass it back into .NET
  // to have it evaluate something that only .NET can know
  return DotNet.invokeMethodAsync(assemblyName, 'ExtractNonSerializedValue', testDto).then(testDtoNonSerializedValue => {
    // To show we can return a .NET object by ref anywhere in a complex structure,
    // return it among other values
    return {
      stringValueUpper: stringValue.toUpperCase(),
      testDtoNonSerializedValue: testDtoNonSerializedValue,
      testDto: testDto
    };
  });
}
