import { System_Array, Pointer } from '../Platform/Platform';
import { platform } from '../Environment';
const renderTreeEditStructLength = 16;

export function getRenderTreeEditPtr(renderTreeEdits: System_Array<RenderTreeEditPointer>, index: number) {
  return platform.getArrayEntryPtr(renderTreeEdits, index, renderTreeEditStructLength);
}

export const renderTreeEdit = {
  // The properties and memory layout must be kept in sync with the .NET equivalent in RenderTreeEdit.cs
  type: (edit: RenderTreeEditPointer) => platform.readInt32Field(edit, 0) as EditType,
  siblingIndex: (edit: RenderTreeEditPointer) => platform.readInt32Field(edit, 4),
  newTreeIndex: (edit: RenderTreeEditPointer) => platform.readInt32Field(edit, 8),
  removedAttributeName: (edit: RenderTreeEditPointer) => platform.readStringField(edit, 12),
};

export enum EditType {
  prependFrame = 1,
  removeFrame = 2,
  setAttribute = 3,
  removeAttribute = 4,
  updateText = 5,
  stepIn = 6,
  stepOut = 7,
}

// Nominal type to ensure only valid pointers are passed to the renderTreeEdit functions.
// At runtime the values are just numbers.
export interface RenderTreeEditPointer extends Pointer { RenderTreeEditPointer__DO_NOT_IMPLEMENT: any }
