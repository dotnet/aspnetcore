import { attachRootComponentToLogicalElement } from '../../Rendering/Renderer';
import { toLogicalRootCommentElement } from '../../Rendering/LogicalElements';

export interface EndComponentComment {
  componentId: number;
  node: Comment;
  index: number;
}

export interface StartComponentComment {
  node: Comment;
  rendererId: number;
  componentId: number;
  circuitId: string;
}

// Represent pairs of start end comments indicating a component that was registered
// in markup (such as a prerendered component)
export interface MarkupRegistrationTags {
  start: StartComponentComment;
  end: EndComponentComment;
}

export class ComponentDescriptor {
  public registrationTags: MarkupRegistrationTags;

  public componentId: number;

  public circuitId: string;

  public rendererId: number;

  public constructor(componentId: number, circuitId: string, rendererId: number, descriptor: MarkupRegistrationTags) {
    this.componentId = componentId;
    this.circuitId = circuitId;
    this.rendererId = rendererId;
    this.registrationTags = descriptor;
  }

  public initialize(): void {
    const startEndPair = { start: this.registrationTags.start.node, end: this.registrationTags.end.node };

    const logicalElement = toLogicalRootCommentElement(startEndPair.start, startEndPair.end);
    attachRootComponentToLogicalElement(this.rendererId, logicalElement, this.componentId);
  }
}
