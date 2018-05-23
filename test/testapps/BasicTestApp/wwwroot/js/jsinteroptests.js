
// We'll store the results from the tests here
var results = {};

function invokeDotNetInteropMethodsAsync() {
  console.log('Invoking void sync methods.');
  Blazor.invokeDotNetMethod(createMethodOptions('VoidParameterless'));
  Blazor.invokeDotNetMethod(createMethodOptions('VoidWithOneParameter'), ...createArgumentList(1));
  Blazor.invokeDotNetMethod(createMethodOptions('VoidWithTwoParameters'), ...createArgumentList(2));
  Blazor.invokeDotNetMethod(createMethodOptions('VoidWithThreeParameters'), ...createArgumentList(3));
  Blazor.invokeDotNetMethod(createMethodOptions('VoidWithFourParameters'), ...createArgumentList(4));
  Blazor.invokeDotNetMethod(createMethodOptions('VoidWithFiveParameters'), ...createArgumentList(5));
  Blazor.invokeDotNetMethod(createMethodOptions('VoidWithSixParameters'), ...createArgumentList(6));
  Blazor.invokeDotNetMethod(createMethodOptions('VoidWithSevenParameters'), ...createArgumentList(7));
  Blazor.invokeDotNetMethod(createMethodOptions('VoidWithEightParameters'), ...createArgumentList(8));

  console.log('Invoking returning sync methods.');
  results['result1'] = Blazor.invokeDotNetMethod(createMethodOptions('ReturnArray'));
  results['result2'] = Blazor.invokeDotNetMethod(createMethodOptions('EchoOneParameter'), ...createArgumentList(1));
  results['result3'] = Blazor.invokeDotNetMethod(createMethodOptions('EchoTwoParameters'), ...createArgumentList(2));
  results['result4'] = Blazor.invokeDotNetMethod(createMethodOptions('EchoThreeParameters'), ...createArgumentList(3));
  results['result5'] = Blazor.invokeDotNetMethod(createMethodOptions('EchoFourParameters'), ...createArgumentList(4));
  results['result6'] = Blazor.invokeDotNetMethod(createMethodOptions('EchoFiveParameters'), ...createArgumentList(5));
  results['result7'] = Blazor.invokeDotNetMethod(createMethodOptions('EchoSixParameters'), ...createArgumentList(6));
  results['result8'] = Blazor.invokeDotNetMethod(createMethodOptions('EchoSevenParameters'), ...createArgumentList(7));
  results['result9'] = Blazor.invokeDotNetMethod(createMethodOptions('EchoEightParameters'), ...createArgumentList(8));

  console.log('Invoking void async methods.');
  return Blazor.invokeDotNetMethodAsync(createMethodOptions('VoidParameterlessAsync'))
    .then(() => Blazor.invokeDotNetMethodAsync(createMethodOptions('VoidWithOneParameterAsync'), ...createArgumentList(1)))
    .then(() => Blazor.invokeDotNetMethodAsync(createMethodOptions('VoidWithTwoParametersAsync'), ...createArgumentList(2)))
    .then(() => Blazor.invokeDotNetMethodAsync(createMethodOptions('VoidWithThreeParametersAsync'), ...createArgumentList(3)))
    .then(() => Blazor.invokeDotNetMethodAsync(createMethodOptions('VoidWithFourParametersAsync'), ...createArgumentList(4)))
    .then(() => Blazor.invokeDotNetMethodAsync(createMethodOptions('VoidWithFiveParametersAsync'), ...createArgumentList(5)))
    .then(() => Blazor.invokeDotNetMethodAsync(createMethodOptions('VoidWithSixParametersAsync'), ...createArgumentList(6)))
    .then(() => Blazor.invokeDotNetMethodAsync(createMethodOptions('VoidWithSevenParametersAsync'), ...createArgumentList(7)))
    .then(() => Blazor.invokeDotNetMethodAsync(createMethodOptions('VoidWithEightParametersAsync'), ...createArgumentList(8)))
    .then(() => {
      console.log('Invoking returning async methods.');
      return Blazor.invokeDotNetMethodAsync(createMethodOptions('ReturnArrayAsync'))
        .then(r => results['result1Async'] = r)
        .then(() => Blazor.invokeDotNetMethodAsync(createMethodOptions('EchoOneParameterAsync'), ...createArgumentList(1)))
        .then(r => results['result2Async'] = r)
        .then(() => Blazor.invokeDotNetMethodAsync(createMethodOptions('EchoTwoParametersAsync'), ...createArgumentList(2)))
        .then(r => results['result3Async'] = r)
        .then(() => Blazor.invokeDotNetMethodAsync(createMethodOptions('EchoThreeParametersAsync'), ...createArgumentList(3)))
        .then(r => results['result4Async'] = r)
        .then(() => Blazor.invokeDotNetMethodAsync(createMethodOptions('EchoFourParametersAsync'), ...createArgumentList(4)))
        .then(r => results['result5Async'] = r)
        .then(() => Blazor.invokeDotNetMethodAsync(createMethodOptions('EchoFiveParametersAsync'), ...createArgumentList(5)))
        .then(r => results['result6Async'] = r)
        .then(() => Blazor.invokeDotNetMethodAsync(createMethodOptions('EchoSixParametersAsync'), ...createArgumentList(6)))
        .then(r => results['result7Async'] = r)
        .then(() => Blazor.invokeDotNetMethodAsync(createMethodOptions('EchoSevenParametersAsync'), ...createArgumentList(7)))
        .then(r => results['result8Async'] = r)
        .then(() => Blazor.invokeDotNetMethodAsync(createMethodOptions('EchoEightParametersAsync'), ...createArgumentList(8)))
        .then(r => results['result9Async'] = r);
    })
    .then(() => {
      console.log('Invoking methods that throw exceptions');
      try {
        Blazor.invokeDotNetMethod(createMethodOptions('ThrowException'))
      } catch (e) {
        results['ThrowException'] = e.message;
      }

      try {
        Blazor.invokeDotNetMethodAsync(createMethodOptions('AsyncThrowSyncException'));
      } catch (e) {
        results['AsyncThrowSyncException'] = e.message;
      }

      return Blazor.invokeDotNetMethodAsync(createMethodOptions('AsyncThrowAsyncException'))
        .catch(e => {
          results['AsyncThrowAsyncException'] = e.message;
          return Promise.resolve();
        })
        .then(() => console.log('Done invoking interop methods'));
    });
}

function createMethodOptions(methodName) {
  return {
    type: {
      assembly: 'BasicTestApp',
      name: 'BasicTestApp.InteropTest.JavaScriptInterop'
    },
    method: {
      name: methodName
    }
  };
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

Blazor.registerFunction('BasicTestApp.Interop.InvokeDotNetInteropMethodsAsync', invokeDotNetInteropMethodsAsync);
Blazor.registerFunction('BasicTestApp.Interop.CollectResults', collectInteropResults);

Blazor.registerFunction('BasicTestApp.Interop.FunctionThrows', functionThrowsException);
Blazor.registerFunction('BasicTestApp.Interop.AsyncFunctionThrowsSyncException', asyncFunctionThrowsSyncException);
Blazor.registerFunction('BasicTestApp.Interop.AsyncFunctionThrowsAsyncException', asyncFunctionThrowsAsyncException);

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
