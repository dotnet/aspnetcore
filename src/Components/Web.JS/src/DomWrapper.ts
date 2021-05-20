import '@microsoft/dotnet-js-interop';

export const domFunctions = {
  focus,
};

function focus(element: HTMLElement, preventScroll: boolean): void {
  if (element instanceof HTMLElement) {
    element.focus({ preventScroll });
  } else {
    throw new Error('Unable to focus an invalid element.');
  }
}
