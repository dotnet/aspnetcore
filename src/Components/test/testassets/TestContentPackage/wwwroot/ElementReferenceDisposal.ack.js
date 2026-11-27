const removalBatches = new Set();
let renderBatchGate;
window.elementReferenceDisposalTest = {
  acknowledgedRemovals: 0,
  acknowledgedServerRemovals: 0,
  failedDisposalAcknowledgements: 0,
  successfulDisposalAcknowledgements: 0,
  serverConnections: 0,
  circuitDisposals: 0,
  captures: new Map(),
  heldRenderBatches: 0,
  completedHeldRenderBatches: 0,
  holdRenderBatches() {
    renderBatchGate = Promise.withResolvers();
  },
  releaseRenderBatches() {
    renderBatchGate?.resolve();
    renderBatchGate = undefined;
  },
  configureSignalR(builder) {
    if (new URLSearchParams(location.search).get('testLongPolling') === 'true') {
      builder.withUrl('_blazor', 4); // HttpTransportType.LongPolling
    }

    const build = builder.build.bind(builder);
    builder.build = () => {
      this.serverConnections++;
      const connection = build();
      const serverRemovalBatches = new Set();
      const send = connection.send.bind(connection);
      connection.send = (method, ...args) => {
        if (method === 'UpdateRootComponents') {
          const batch = JSON.parse(args[0]);
          if (batch.operations.some(operation => operation.type === 'remove')) {
            serverRemovalBatches.add(batch.batchId);
          }
        }
        return send(method, ...args);
      };
      const on = connection.on.bind(connection);
      on('JS.EndUpdateRootComponents', batchId => {
        if (serverRemovalBatches.delete(batchId)) {
          this.acknowledgedServerRemovals++;
        }
      });
      connection.on = (method, callback) => on(method, method === 'JS.RenderBatch' ? async (...args) => {
        if (renderBatchGate) {
          this.heldRenderBatches++;
          await renderBatchGate.promise;
          await callback(...args);
          this.completedHeldRenderBatches++;
        } else {
          await callback(...args);
        }
      } : callback);
      return connection;
    };
  },
  isMissing: element => element === null,
  referenceIsMissing(captureId) {
    return JSON.parse(Blazor._internal.invokeJSJson(
      'elementReferenceDisposalTest.isMissing', 0, 0,
      JSON.stringify([{ __internalId: captureId }]), 0, 1));
  },
  referencesReleased() {
    return [...this.captures.values()].every(id => this.referenceIsMissing(id));
  },
};

const originalFetch = window.fetch;
window.fetch = async (...args) => {
  const test = window.elementReferenceDisposalTest;
  const options = args[1];
  const isDisposalAcknowledgement = options?.method === 'POST'
    && options.body instanceof ArrayBuffer
    && new TextDecoder().decode(options.body).includes('OnRootComponentDisposalCompleted');
  if (isDisposalAcknowledgement && test.failNextDisposalAcknowledgement) {
    test.failNextDisposalAcknowledgement = false;
    test.failedDisposalAcknowledgements++;
    return new Response(null, { status: 503 });
  }

  const response = await originalFetch(...args);
  if (isDisposalAcknowledgement && response.ok) {
    test.successfulDisposalAcknowledgements++;
  }
  if (args[0] === '_blazor/disconnect' && args[1]?.method === 'POST' && response.ok) {
    window.elementReferenceDisposalTest.circuitDisposals++;
  }
  return response;
};

// Observe the real removal acknowledgement before releasing any disposal gate.
Object.defineProperty(window, 'Blazor', {
  configurable: true,
  set(blazor) {
    Object.defineProperty(window, 'Blazor', { value: blazor, configurable: true, writable: true, enumerable: true });
    observeAssignment(blazor._internal, 'updateRootComponents', original => (operations, state) => {
      const batch = JSON.parse(operations);
      if (batch.operations.some(operation => operation.type === 'remove')) {
        removalBatches.add(batch.batchId);
      }
      return original(operations, state);
    });
    observeAssignment(blazor._internal, 'endUpdateRootComponents', original => batchId => {
      original(batchId);
      if (removalBatches.delete(batchId)) {
        window.elementReferenceDisposalTest.acknowledgedRemovals++;
      }
    });
  },
});

function observeAssignment(target, name, wrap) {
  Object.defineProperty(target, name, {
    configurable: true,
    set(value) {
      Object.defineProperty(target, name, { value: wrap(value), configurable: true, writable: true, enumerable: true });
    },
  });
}
