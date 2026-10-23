const removalBatches = new Set();
window.elementReferenceDisposalTest = {
  acknowledgedRemovals: 0,
  captures: new Map(),
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
