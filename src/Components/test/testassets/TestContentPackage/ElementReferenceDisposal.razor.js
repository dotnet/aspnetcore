const pendingDisposals = new Map();

export function initialize(element, id) {
  sessionStorage.removeItem(`element-reference-disposed-${id}`);
  sessionStorage.removeItem(`element-reference-disposal-pending-${id}`);
  sessionStorage.setItem(`element-reference-pointermoves-${id}`, '0');
  let release;
  const completion = new Promise(resolve => { release = resolve; });
  pendingDisposals.set(id, { completion, release });
  const handler = () => {
    const key = `element-reference-pointermoves-${id}`;
    sessionStorage.setItem(key, String(Number(sessionStorage.getItem(key)) + 1));
  };
  document.addEventListener('pointermove', handler);
  element.elementReferenceDisposal = { id, handler };
  const captureAttribute = element.getAttributeNames().find(name => name.startsWith('_bl_'));
  if (!captureAttribute) {
    throw new Error(`No element reference capture for '${id}'.`);
  }
  window.elementReferenceDisposalTest.captures.set(id, captureAttribute.substring(4));
}

export function dispose(element, id) {
  if (!element || element.elementReferenceDisposal.id !== id || element.isConnected) {
    throw new Error(`Delayed element reference was not revived for '${id}'.`);
  }

  document.removeEventListener('pointermove', element.elementReferenceDisposal.handler);
  delete element.elementReferenceDisposal;
  sessionStorage.setItem(`element-reference-disposed-${id}`, 'true');
}

export async function waitForDisposal(id) {
  const pendingDisposal = pendingDisposals.get(id);
  if (!pendingDisposal) {
    throw new Error(`No pending disposal for '${id}'.`);
  }

  sessionStorage.setItem(`element-reference-disposal-pending-${id}`, 'true');
  await pendingDisposal.completion;
  pendingDisposals.delete(id);
  sessionStorage.setItem(`element-reference-disposal-pending-${id}`, 'false');
}

export function releaseDisposal(id) {
  const pendingDisposal = pendingDisposals.get(id);
  if (!pendingDisposal) {
    throw new Error(`No pending disposal for '${id}'.`);
  }

  pendingDisposal.release();
}

export function releaseAllDisposals() {
  for (const pendingDisposal of pendingDisposals.values()) {
    pendingDisposal.release();
  }
}
