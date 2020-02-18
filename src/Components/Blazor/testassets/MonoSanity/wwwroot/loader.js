(function () {
  // Implement just enough of the DotNet.* API surface for unmarshalled interop calls to work
  // in the cases used in this project
  window.DotNet = {
    jsCallDispatcher: {
      findJSFunction: function (identifier) {
        return window[identifier];
      }
    }
  };

  window.initMono = function initMono(loadAssemblyUrls, onReadyCallback) {
    window.Module = {
      locateFile: function (fileName) {
        return fileName === 'dotnet.wasm' ? '/_framework/wasm/dotnet.wasm' : fileName;
      },
      onRuntimeInitialized: function () {
        var allAssemblyUrls = loadAssemblyUrls.concat([
          'netstandard.dll',
          'mscorlib.dll',
          'System.dll',
          'System.Core.dll',
          'System.Net.Http.dll',
          'WebAssembly.Bindings.dll',
          'WebAssembly.Net.Http.dll'
        ]);

        // For these tests we're using Mono's built-in mono_load_runtime_and_bcl util.
        // In real apps we don't use this because we want to have more fine-grained
        // control over how the requests are issued, what gets logged, etc., so for
        // real apps Blazor's Boot.WebAssembly.ts implements its own equivalent.
        MONO.mono_load_runtime_and_bcl(
          /* vfx_prefix */ 'myapp', // Virtual filesystem root - arbitrary value
          /* deploy_prefix */ '_framework/_bin',
          /* enable_debugging */ 1,
          allAssemblyUrls,
          onReadyCallback
        );
      }
    };

    addScriptTagsToDocument();
  };

  window.invokeMonoMethod = function invokeMonoMethod(assemblyName, namespace, typeName, methodName, args) {
    var assembly_load = Module.cwrap('mono_wasm_assembly_load', 'number', ['string']);
    var find_class = Module.cwrap('mono_wasm_assembly_find_class', 'number', ['number', 'string', 'string']);
    var find_method = Module.cwrap('mono_wasm_assembly_find_method', 'number', ['number', 'string', 'number']);

    var assembly = assembly_load(assemblyName);
    var type = find_class(assembly, namespace, typeName);
    var method = find_method(type, methodName, -1);

    var stack = Module.stackSave();
    try {
      var resultPtr = callMethod(method, null, args);
      return dotnetStringToJavaScriptString(resultPtr);
    }
    finally {
      Module.stackRestore(stack);
    }
  };

  window.dotnetStringToJavaScriptString = function dotnetStringToJavaScriptString(mono_obj) {
    if (mono_obj === 0)
      return null;
    var mono_string_get_utf8 = Module.cwrap('mono_wasm_string_get_utf8', 'number', ['number']);
    var raw = mono_string_get_utf8(mono_obj);
    var res = Module.UTF8ToString(raw);
    Module._free(raw);
    return res;
  };

  window.javaScriptStringToDotNetString = function dotnetStringToJavaScriptString(javaScriptString) {
    var mono_string = Module.cwrap('mono_wasm_string_from_js', 'number', ['string']);
    return mono_string(javaScriptString);
  };

  function callMethod(method, target, args) {
    var stack = Module.stackSave();
    var invoke_method = Module.cwrap('mono_wasm_invoke_method', 'number', ['number', 'number', 'number']);

    try {
      var argsBuffer = Module.stackAlloc(args.length);
      var exceptionFlagManagedInt = Module.stackAlloc(4);
      for (var i = 0; i < args.length; ++i) {
        var argVal = args[i];
        if (typeof argVal === 'number') {
          var managedInt = Module.stackAlloc(4);
          Module.setValue(managedInt, argVal, 'i32');
          Module.setValue(argsBuffer + i * 4, managedInt, 'i32');
        } else if (typeof argVal === 'string') {
          var managedString = javaScriptStringToDotNetString(argVal);
          Module.setValue(argsBuffer + i * 4, managedString, 'i32');
        } else {
          throw new Error('Unsupported arg type: ' + typeof argVal);
        }
      }
      Module.setValue(exceptionFlagManagedInt, 0, 'i32');

      var res = invoke_method(method, target, argsBuffer, exceptionFlagManagedInt);

      if (Module.getValue(exceptionFlagManagedInt, 'i32') !== 0) {
        throw new Error(dotnetStringToJavaScriptString(res));
      }

      return res;
    } finally {
      Module.stackRestore(stack);
    }
  }

  function addScriptTagsToDocument() {
    var browserSupportsNativeWebAssembly = typeof WebAssembly !== 'undefined' && WebAssembly.validate;
    if (!browserSupportsNativeWebAssembly) {
      throw new Error('This browser does not support WebAssembly.');
    }

    var scriptElem = document.createElement('script');
    scriptElem.src = '/_framework/wasm/dotnet.js';
    document.body.appendChild(scriptElem);
  }

})();
