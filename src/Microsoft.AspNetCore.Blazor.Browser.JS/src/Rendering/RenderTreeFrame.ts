import { System_String, System_Array, Pointer } from '../Platform/Platform';
import { platform } from '../Environment';
const renderTreeFrameStructLength = 40;

// To minimise GC pressure, instead of instantiating a JS object to represent each tree frame,
// we work in terms of pointers to the structs on the .NET heap, and use static functions that
// know how to read property values from those structs.

export function getTreeFramePtr(renderTreeEntries: System_Array<RenderTreeFramePointer>, index: number) {
  return platform.getArrayEntryPtr(renderTreeEntries, index, renderTreeFrameStructLength);
}

export const renderTreeFrame = {
  // The properties and memory layout must be kept in sync with the .NET equivalent in RenderTreeFrame.cs
  frameType: (frame: RenderTreeFramePointer) => platform.readInt32Field(frame, 4) as FrameType,
  elementName: (frame: RenderTreeFramePointer) => platform.readStringField(frame, 8),
  descendantsEndIndex: (frame: RenderTreeFramePointer) => platform.readInt32Field(frame, 12) as FrameType,
  textContent: (frame: RenderTreeFramePointer) => platform.readStringField(frame, 16),
  attributeName: (frame: RenderTreeFramePointer) => platform.readStringField(frame, 20),
  attributeValue: (frame: RenderTreeFramePointer) => platform.readStringField(frame, 24),
  componentId: (frame: RenderTreeFramePointer) => platform.readInt32Field(frame, 32),
};

export enum FrameType {
  // The values must be kept in sync with the .NET equivalent in RenderTreeFrameType.cs
  element = 1,
  text = 2,
  attribute = 3,
  component = 4,
}

// Nominal type to ensure only valid pointers are passed to the renderTreeFrame functions.
// At runtime the values are just numbers.
export interface RenderTreeFramePointer extends Pointer { RenderTreeFramePointer__DO_NOT_IMPLEMENT: any }
