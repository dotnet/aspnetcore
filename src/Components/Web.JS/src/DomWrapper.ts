import '@microsoft/dotnet-js-interop';

export const internalFunctions = {
  focus,
};

function focus(element: HTMLElement): void {
  element.focus();
}
