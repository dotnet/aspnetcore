const removalBatches = new Set();
let renderBatchGate;
window.elementReferenceDisposalTest = {
  acknowledgedRemovals: 0,
  acknowledgedServerRemovals: 0,
  serverPersistedStateUpdates: 0,
  appliedServerRootRemovals: 0,
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
      const serverRootIds = new Set();
      const serverRootRemovalsByRenderBatch = new Map();
      const send = connection.send.bind(connection);
      connection.send = (method, ...args) => {
        if (method === 'UpdateRootComponents') {
          if (args[1]) {
            this.serverPersistedStateUpdates++;
          }
          const batch = JSON.parse(args[0]);
          if (batch.operations.some(operation => operation.type === 'remove')) {
            serverRemovalBatches.add(batch.batchId);
          }
        } else if (method === 'OnRenderCompleted' && args[1] === null) {
          for (const id of serverRootRemovalsByRenderBatch.get(args[0]) ?? []) {
            if (serverRootIds.delete(id)) {
              this.appliedServerRootRemovals++;
            }
          }
          serverRootRemovalsByRenderBatch.delete(args[0]);
        }
        return send(method, ...args);
      };
      const on = connection.on.bind(connection);
      on('JS.AttachComponent', componentId => { serverRootIds.add(componentId); });
      on('JS.EndUpdateRootComponents', batchId => {
        if (serverRemovalBatches.delete(batchId)) {
          this.acknowledgedServerRemovals++;
        }
      });
      connection.on = (method, callback) => on(method, method === 'JS.RenderBatch' ? async (batchId, data) => {
        const disposedRoots = getDisposedComponentIds(data).filter(id => serverRootIds.has(id));
        if (disposedRoots.length) {
          serverRootRemovalsByRenderBatch.set(batchId, disposedRoots);
        }

        if (renderBatchGate) {
          this.heldRenderBatches++;
          await renderBatchGate.promise;
          await callback(batchId, data);
          this.completedHeldRenderBatches++;
        } else {
          await callback(batchId, data);
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

function getDisposedComponentIds(data) {
  // Match OutOfProcessRenderBatch: the third-to-last int32 points to the count and disposed IDs.
  const view = new DataView(data.buffer, data.byteOffset, data.byteLength);
  const offset = view.getInt32(view.byteLength - 12, true);
  const count = view.getInt32(offset, true);
  return Array.from({ length: count }, (_, index) => view.getInt32(offset + 4 * (index + 1), true));
}

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
