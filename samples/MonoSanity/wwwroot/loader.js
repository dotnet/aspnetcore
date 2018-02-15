(function () {
  window.initMono = function initMono(loadAssemblyUrls, onReadyCallback) {
    window.Browser = {
      init: function () { },
      asyncLoad: asyncLoad
    };

    window.Module = {
      print: function (line) { console.log(line); },
      printEr: function (line) { console.error(line); },
      wasmBinaryFile: '/_framework/wasm/mono.wasm',
      asmjsCodeFile: '/_framework/asmjs/mono.asm.js',
      preloadPlugins: [],
      preRun: [function () {
        preloadAssemblies(loadAssemblyUrls);
      }],
      postRun: [function () {
        var load_runtime = Module.cwrap('mono_wasm_load_runtime', null, ['string']);
        load_runtime('appBinDir');
        onReadyCallback();
      }]
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

    var stack = Module.Runtime.stackSave();
    try {
      var resultPtr = callMethod(method, null, args);
      return dotnetStringToJavaScriptString(resultPtr);
    }
    finally {
      Module.Runtime.stackRestore(stack);
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

  function preloadAssemblies(loadAssemblyUrls) {
    var loadBclAssemblies = [
      'netstandard',
      'mscorlib',
      'System',
      'System.Core',
    ];

    var allAssemblyUrls = loadAssemblyUrls
      .concat(loadBclAssemblies.map(function (name) { return '_framework/_bin/' + name + '.dll'; }));

    Module.FS_createPath('/', 'appBinDir', true, true);
    allAssemblyUrls.forEach(function (url) {
      FS.createPreloadedFile('appBinDir', getAssemblyNameFromUrl(url) + '.dll', url, true, false, null, function onError(err) {
        throw err;
      });
    });
  }

  function asyncLoad(url, onload, onerror) {
    var xhr = new XMLHttpRequest;
    xhr.open('GET', url, /* async: */ true);
    xhr.responseType = 'arraybuffer';
    xhr.onload = function xhr_onload() {
      if (xhr.status === 200 || xhr.status === 0 && xhr.response) {
        var asm = new Uint8Array(xhr.response);
        onload(asm);
      } else {
        onerror(xhr);
      }
    };
    xhr.onerror = onerror;
    xhr.send(null);
  }

  function callMethod(method, target, args) {
    var stack = Module.Runtime.stackSave();
    var invoke_method = Module.cwrap('mono_wasm_invoke_method', 'number', ['number', 'number', 'number']);

    try {
      var argsBuffer = Module.Runtime.stackAlloc(args.length);
      var exceptionFlagManagedInt = Module.Runtime.stackAlloc(4);
      for (var i = 0; i < args.length; ++i) {
        var argVal = args[i];
        if (typeof argVal === 'number') {
          var managedInt = Module.Runtime.stackAlloc(4);
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
      Module.Runtime.stackRestore(stack);
    }
  }

  function addScriptTagsToDocument() {
    // Load either the wasm or asm.js version of the Mono runtime
    var browserSupportsNativeWebAssembly = typeof WebAssembly !== 'undefined' && WebAssembly.validate;
    var monoRuntimeUrlBase = '/_framework/' + (browserSupportsNativeWebAssembly ? 'wasm' : 'asmjs');
    var monoRuntimeScriptUrl = monoRuntimeUrlBase + '/mono.js';

    if (!browserSupportsNativeWebAssembly) {
      // In the asmjs case, the initial memory structure is in a separate file we need to download
      var meminitXHR = Module['memoryInitializerRequest'] = new XMLHttpRequest();
      meminitXHR.open('GET', monoRuntimeUrlBase + '/mono.js.mem');
      meminitXHR.responseType = 'arraybuffer';
      meminitXHR.send(null);
    }

    var scriptElem = document.createElement('script');
    scriptElem.src = monoRuntimeScriptUrl;
    document.body.appendChild(scriptElem);
  }

  function getAssemblyNameFromUrl(url) {
    var lastSegment = url.substring(url.lastIndexOf('/') + 1);
    var queryStringStartPos = lastSegment.indexOf('?');
    var filename = queryStringStartPos < 0 ? lastSegment : lastSegment.substring(0, queryStringStartPos);
    return filename.replace(/\.dll$/, '');
  }

})();