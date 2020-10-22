import '@microsoft/dotnet-js-interop';

export const domFunctions = {
  focus,
};

function focus(element: HTMLElement): void {
  if (element instanceof HTMLElement) {
    element.focus();
  } else {
    throw new Error('Unable to focus an invalid element.');
  }
}
