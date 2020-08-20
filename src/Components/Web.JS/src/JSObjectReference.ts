import { DotNet } from '@microsoft/dotnet-js-interop';

export const jsObjectReference = {
  dispose,
};

function dispose(id: number): void {
  DotNet.jsCallDispatcher.disposeJSObjectReference(id);
}
