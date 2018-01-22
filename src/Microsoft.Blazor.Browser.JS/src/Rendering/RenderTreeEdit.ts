import { System_Array, Pointer } from '../Platform/Platform';
import { platform } from '../Environment';
const renderTreeEditStructLength = 12;

export function getRenderTreeEditPtr(renderTreeEdits: System_Array, index: number): RenderTreeEditPointer {
  return platform.getArrayEntryPtr(renderTreeEdits, index, renderTreeEditStructLength) as RenderTreeEditPointer;
}

export const renderTreeEdit = {
  // The properties and memory layout must be kept in sync with the .NET equivalent in RenderTreeEdit.cs
  type: (edit: RenderTreeEditPointer) => platform.readInt32Field(edit, 0) as EditType,
  newTreeIndex: (edit: RenderTreeEditPointer) => platform.readInt32Field(edit, 4),
  removedAttributeName: (edit: RenderTreeEditPointer) => platform.readStringField(edit, 8),
};

export enum EditType {
  continue = 1,
  prependNode = 2,
  removeNode = 3,
  setAttribute = 4,
  removeAttribute = 5,
  updateText = 6,
  stepIn = 7,
  stepOut = 8,
}

// Nominal type to ensure only valid pointers are passed to the renderTreeEdit functions.
// At runtime the values are just numbers.
export interface RenderTreeEditPointer extends Pointer { RenderTreeEditPointer__DO_NOT_IMPLEMENT: any }
