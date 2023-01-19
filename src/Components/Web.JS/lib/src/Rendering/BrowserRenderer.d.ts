import { RenderBatch, ArrayBuilderSegment, RenderTreeEdit, RenderTreeFrame, ArrayValues } from './RenderBatch/RenderBatch';
import { EventDelegator } from './Events/EventDelegator';
import { LogicalElement } from './LogicalElements';
export declare class BrowserRenderer {
    eventDelegator: EventDelegator;
    private rootComponentIds;
    private childComponentLocations;
    constructor(browserRendererId: number);
    attachRootComponentToLogicalElement(componentId: number, element: LogicalElement, appendContent: boolean): void;
    updateComponent(batch: RenderBatch, componentId: number, edits: ArrayBuilderSegment<RenderTreeEdit>, referenceFrames: ArrayValues<RenderTreeFrame>): void;
    disposeComponent(componentId: number): void;
    disposeEventHandler(eventHandlerId: number): void;
    private attachComponentToElement;
    private applyEdits;
    private insertFrame;
    private insertElement;
    private trySetSelectValueFromOptionElement;
    private insertComponent;
    private insertText;
    private insertMarkup;
    private applyAttribute;
    private tryApplySpecialProperty;
    private applyInternalAttribute;
    private tryApplyValueProperty;
    private tryApplyCheckedProperty;
    private findClosestAncestorSelectElement;
    private insertFrameRange;
}
export interface ComponentDescriptor {
    start: Node;
    end: Node;
}
