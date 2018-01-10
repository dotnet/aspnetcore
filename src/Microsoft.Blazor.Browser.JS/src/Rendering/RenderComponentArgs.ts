import { Pointer, System_Array } from '../Platform/Platform';
import { platform } from '../Environment';

// Keep in sync with the RenderComponentArgs struct in .NET code
export const renderComponentArgs = {
  browserRendererId: (obj: RenderComponentArgsPointer) => platform.readInt32Field(obj, 0),
  componentId: (obj: RenderComponentArgsPointer) => platform.readInt32Field(obj, 4),
  renderTree: (obj: RenderComponentArgsPointer) => platform.readObjectField(obj, 8) as System_Array,
  renderTreeLength: (obj: RenderComponentArgsPointer) => platform.readInt32Field(obj, 12),
}

// Nominal type to ensure only valid pointers are passed to the renderComponentArgs functions.
// At runtime the values are just numbers.
export interface RenderComponentArgsPointer extends Pointer { RenderComponentArgsPointer__DO_NOT_IMPLEMENT: any }
