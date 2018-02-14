import { System_String, System_Array, Pointer } from '../Platform/Platform';
import { platform } from '../Environment';
const renderTreeFrameStructLength = 28;

// To minimise GC pressure, instead of instantiating a JS object to represent each tree frame,
// we work in terms of pointers to the structs on the .NET heap, and use static functions that
// know how to read property values from those structs.

export function getTreeFramePtr(renderTreeEntries: System_Array<RenderTreeFramePointer>, index: number) {
  return platform.getArrayEntryPtr(renderTreeEntries, index, renderTreeFrameStructLength);
}

export const renderTreeFrame = {
  // The properties and memory layout must be kept in sync with the .NET equivalent in RenderTreeFrame.cs
  frameType: (frame: RenderTreeFramePointer) => platform.readInt32Field(frame, 4) as FrameType,
  subtreeLength: (frame: RenderTreeFramePointer) => platform.readInt32Field(frame, 8) as FrameType,
  componentId: (frame: RenderTreeFramePointer) => platform.readInt32Field(frame, 12),
  elementName: (frame: RenderTreeFramePointer) => platform.readStringField(frame, 16),
  textContent: (frame: RenderTreeFramePointer) => platform.readStringField(frame, 16),
  attributeName: (frame: RenderTreeFramePointer) => platform.readStringField(frame, 16),
  attributeValue: (frame: RenderTreeFramePointer) => platform.readStringField(frame, 24),
  attributeEventHandlerId: (frame: RenderTreeFramePointer) => platform.readInt32Field(frame, 8),
};

export enum FrameType {
  // The values must be kept in sync with the .NET equivalent in RenderTreeFrameType.cs
  element = 1,
  text = 2,
  attribute = 3,
  component = 4,
  region = 5,
}

// Nominal type to ensure only valid pointers are passed to the renderTreeFrame functions.
// At runtime the values are just numbers.
export interface RenderTreeFramePointer extends Pointer { RenderTreeFramePointer__DO_NOT_IMPLEMENT: any }
