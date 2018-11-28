const pendingCallbacksByEventName = {};

// Returns a promise that resolves the next time we receive the specified event
export function receiveEvent(name) {
  let capturedResolver;
  const resultPromise = new Promise(resolve => {
    capturedResolver = resolve;
  });

  pendingCallbacksByEventName[name] = pendingCallbacksByEventName[name] || [];
  pendingCallbacksByEventName[name].push(capturedResolver);

  return resultPromise;
}

// Listen for messages forwarded from the child frame
window.receiveBenchmarkEvent = function (name) {
  const callbacks = pendingCallbacksByEventName[name];
  delete pendingCallbacksByEventName[name];
  callbacks && callbacks.forEach(callback => callback());
}
