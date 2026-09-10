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

    var jsObjectReference = DotNet.createJSObjectReference({
        prop: 'successful',
        noop: function () { }
    });

    var returnedObject = DotNet.invokeMethod(assemblyName, 'RoundTripJSObjectReference', jsObjectReference);
    results['roundTripJSObjectReference'] = returnedObject && returnedObject.prop;

    DotNet.disposeJSObjectReference(jsObjectReference);
    results['invokeDisposedJSObjectReferenceException'] = DotNet.invokeMethod(assemblyName, 'InvokeDisposedJSObjectReferenceException', jsObjectReference);

    var byteArray = new Uint8Array([ 1, 5, 7, 17, 200, 138 ]);
    var returnedByteArray = DotNet.invokeMethod(assemblyName, 'RoundTripByteArray', byteArray);
    results['roundTripByteArrayFromJS'] = returnedByteArray;

    var byteArrayWrapper = { 'strVal': "Some string", 'byteArrayVal': byteArray, 'intVal': 42 };
    var returnedByteArrayWrapper = DotNet.invokeMethod(assemblyName, 'RoundTripByteArrayWrapperObject', byteArrayWrapper);
    results['roundTripByteArrayWrapperObjectFromJS'] = returnedByteArrayWrapper;

    // Note the following .NET Stream Reference E2E tests are synchronous for the test execution
    // however the validation is async (due to the nature of stream validations).
    var streamRef = DotNet.invokeMethod(assemblyName, 'GetDotNetStreamReference');
    results['requestDotNetStreamReference'] = await validateDotNetStreamReference(streamRef);
    var streamWrapper = DotNet.invokeMethod(assemblyName, 'GetDotNetStreamWrapperReference');
    results['requestDotNetStreamWrapperReference'] = await validateDotNetStreamWrapperReference(streamWrapper);

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

  var jsObjectReference = DotNet.createJSObjectReference({
    prop: 'successful',
    noop: function () { }
  });

  var returnedObject = await DotNet.invokeMethodAsync(assemblyName, 'RoundTripJSObjectReferenceAsync', jsObjectReference);
  results['roundTripJSObjectReferenceAsync'] = returnedObject && returnedObject.prop;

  DotNet.disposeJSObjectReference(jsObjectReference);
  results['invokeDisposedJSObjectReferenceExceptionAsync'] = await DotNet.invokeMethodAsync(assemblyName, 'InvokeDisposedJSObjectReferenceExceptionAsync', jsObjectReference);

  var byteArray = new Uint8Array([ 1, 5, 7, 17, 200, 138 ]);
  var returnedByteArray = await DotNet.invokeMethodAsync(assemblyName, 'RoundTripByteArrayAsync', byteArray);
  results['roundTripByteArrayAsyncFromJS'] = returnedByteArray;

  var byteArrayWrapper = { 'strVal': "Some string", 'byteArrayVal': byteArray, 'intVal': 42 };
  var returnedByteArrayWrapper = await DotNet.invokeMethodAsync(assemblyName, 'RoundTripByteArrayWrapperObjectAsync', byteArrayWrapper);
  results['roundTripByteArrayWrapperObjectAsyncFromJS'] = returnedByteArrayWrapper;

  const largeArray = Array.from({ length: 100000 }).map((_, index) => index % 256);
  const largeByteArray = new Uint8Array(largeArray);
  const jsStreamReference = DotNet.createJSStreamReference(largeByteArray);
  results['jsToDotNetStreamParameterAsync'] = await DotNet.invokeMethodAsync(assemblyName, 'JSToDotNetStreamParameterAsync', jsStreamReference);

  var streamWrapper = { 'strVal': "SomeStr", 'jsStreamReferenceVal': jsStreamReference, 'intVal': 5 };
  results['jsToDotNetStreamWrapperObjectParameterAsync'] = await DotNet.invokeMethodAsync(assemblyName, 'JSToDotNetStreamWrapperObjectParameterAsync', streamWrapper);

  var streamRef = await DotNet.invokeMethodAsync(assemblyName, 'GetDotNetStreamReferenceAsync');
  results['requestDotNetStreamReferenceAsync'] = await validateDotNetStreamReference(streamRef);
  var wrapper = await DotNet.invokeMethodAsync(assemblyName, 'GetDotNetStreamWrapperReferenceAsync');
  results['requestDotNetStreamWrapperReferenceAsync'] = await validateDotNetStreamWrapperReference(wrapper);

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

class TestClass {
    constructor(text) {
        this.text = text;
    }

    getTextLength() {
        return this.text.length;
    }
}

const testObject = {
    num: 10,
    get getOnlyProperty() {
        return 20;
    },
    set setOnlyProperty(value) {
        this.num = value;
    },
    nullProperty: null
}

window.jsInteropTests = {
  invokeDotNetInteropMethodsAsync: invokeDotNetInteropMethodsAsync,
  collectInteropResults: collectInteropResults,
  functionThrowsException: functionThrowsException,
  asyncFunctionThrowsSyncException: asyncFunctionThrowsSyncException,
  asyncFunctionThrowsAsyncException: asyncFunctionThrowsAsyncException,
  returnUndefined: returnUndefined,
  returnNull: returnNull,
  returnPrimitive: returnPrimitive,
  returnPrimitiveAsync: returnPrimitiveAsync,
  returnJSObjectReference: returnJSObjectReference,
  addViaJSObjectReference: addViaJSObjectReference,
  receiveDotNetObjectByRef: receiveDotNetObjectByRef,
  receiveDotNetObjectByRefAsync: receiveDotNetObjectByRefAsync,
  receiveDotNetStreamReference: receiveDotNetStreamReference,
  receiveDotNetStreamWrapperReference: receiveDotNetStreamWrapperReference,
  returnElementReference: returnElementReference,
  TestClass: TestClass,
  nonConstructorFunction: () => { return 42; },
  testObject: testObject,
};

function returnUndefined() {
  return undefined;
}

function returnNull() {
  return null;
}

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

function roundTripByteArray(byteArray) {
  if (byteArray.constructor !== Uint8Array) {
    throw new Error('roundTripByteArray did not receive a byte array.');
  }
  return byteArray;
}

function roundTripByteArrayAsync(byteArray) {
  return new Promise((resolve, reject) => {
    setTimeout(function () {
      if (byteArray.constructor !== Uint8Array) {
        reject('roundTripByteArrayAsync did not receive a byte array.');
      }
      resolve(byteArray);
    }, 100);
  });
}

function roundTripByteArrayWrapperObject(byteArrayWrapperObject) {
  if (byteArrayWrapperObject.byteArrayVal.constructor !== Uint8Array) {
    throw new Error('roundTripByteArrayWrapperObject did not receive a byte array.');
  }
  return byteArrayWrapperObject;
}

function jsToDotNetStreamReturnValueAsync() {
  return new Promise((resolve, reject) => {
    setTimeout(function () {
      const largeArray = Array.from({ length: 100000 }).map((_, index) => index % 256);
      resolve(new Uint8Array(largeArray));
    }, 100);
  });
}

function jsToDotNetStreamReturnValue() {
  const largeArray = Array.from({ length: 100000 }).map((_, index) => index % 256);
  return new Uint8Array(largeArray);
}

function jsToDotNetStreamWrapperObjectReturnValueAsync() {
  return new Promise((resolve, reject) => {
    setTimeout(function () {
      const largeArray = Array.from({ length: 100000 }).map((_, index) => index % 256);
      const byteArray = new Uint8Array(largeArray);
      const jsStreamReference = DotNet.createJSStreamReference(byteArray);
      const returnValue = { strVal: 'SomeStr', intVal: 5, jsStreamReferenceVal: jsStreamReference }
      resolve(returnValue);
    }, 100);
  });
}

function roundTripByteArrayWrapperObjectAsync(byteArrayWrapperObject) {
  return new Promise((resolve, reject) => {
    setTimeout(function () {
      if (byteArrayWrapperObject.byteArrayVal.constructor !== Uint8Array) {
        reject('roundTripByteArrayWrapperObjectAsync did not receive a byte array.');
      }
      resolve(byteArrayWrapperObject);
    }, 100);
  });
}

function returnJSObjectReference() {
  return {
    identity: function (value) {
      return value;
    },
    getWindow: function() {
      return window;
    },
    nonFunction: 123,
    nested: {
      add: function (a, b) {
        return a + b;
      }
    },
    dispose: function () {
      DotNet.disposeJSObjectReference(this);
    },
  };
}

function returnElementReference(element) {
  return element;
}

function addViaJSObjectReference(jsObjectReference, a, b) {
  return jsObjectReference.nested.add(a, b);
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

async function validateDotNetStreamReference(streamRef) {
  const data = new Uint8Array(await streamRef.arrayBuffer());
  const isValid = data.length == 100000 && data.every((value, index) => value == index % 256);
  return isValid ? "Success" : `Failure, got length ${data.length} with data ${data}`;
}

async function validateDotNetStreamWrapperReference(wrapper) {
  const isValid = await validateDotNetStreamReference(wrapper.dotNetStreamReferenceVal) == "Success" &&
    wrapper.strVal == "somestr" &&
    wrapper.intVal == 25;
  return isValid ? "Success" : `Failure, got ${JSON.stringify(wrapper)}`;
}

async function receiveDotNetStreamReference(streamRef) {
  return await validateDotNetStreamReference(streamRef);
}

async function receiveDotNetStreamWrapperReference(wrapper) {
  return await validateDotNetStreamWrapperReference(wrapper);
}
