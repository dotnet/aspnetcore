import { DotNet } from '@microsoft/dotnet-js-interop';

export interface IComponentRenderer {
  RenderRootComponent: (typeNameOrAlias: string, selector: string, parameters: object) => Promise<DotNet.DotNetObject>
}

export class ComponentProxy {
  element: HTMLElement;
  public id: number;
  public handler?: DotNet.DotNetObject;
  public disposed: boolean;

  constructor(id: number, element: HTMLElement) {
    this.id = id;
    this.element = element;
    this.disposed = false;
  }

  public setHandler(componentHandle: DotNet.DotNetObject) {
    if (this.handler) {
      throw new Error('Handler already asigned.');
    }
    this.handler = componentHandle;
    // Consider using the finalization registry to maximize the chances to cleanup dotnet memory
    // even when the developer makes a mistake.
    // https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/FinalizationRegistry
  }

  public setParameters(parameters: object): Promise<void> {
    this.ensureHandler();
    return this.handler!.invokeMethodAsync('SetParametersAsync', parameters);
  }

  public async dispose(): Promise<void> {
    this.ensureHandler();

    const handler = this.handler!;
    this.handler = undefined;
    this.disposed = true;
    await handler.invokeMethodAsync("DisposeAsync");
    this.element.innerHTML = '';
  }

  public async raiseEvent(eventName: string, eventArgs: unknown) {
    this.element.dispatchEvent(new CustomEvent(eventName, { detail: eventArgs }));
  }

  private ensureHandler() {
    if (this.disposed) {
      throw new Error('The component has already been disposed.');
    }
    if (!this.handler) {
      throw new Error('Handler for component not assigned.');
    }
  }
}
