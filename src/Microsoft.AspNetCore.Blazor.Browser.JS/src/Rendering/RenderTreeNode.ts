import { System_String, System_Array, Pointer } from '../Platform/Platform';
import { platform } from '../Environment';
const renderTreeNodeStructLength = 40;

// To minimise GC pressure, instead of instantiating a JS object to represent each tree node,
// we work in terms of pointers to the structs on the .NET heap, and use static functions that
// know how to read property values from those structs.

export function getTreeNodePtr(renderTreeEntries: System_Array<RenderTreeNodePointer>, index: number) {
  return platform.getArrayEntryPtr(renderTreeEntries, index, renderTreeNodeStructLength);
}

export const renderTreeNode = {
  // The properties and memory layout must be kept in sync with the .NET equivalent in RenderTreeNode.cs
  nodeType: (node: RenderTreeNodePointer) => platform.readInt32Field(node, 4) as NodeType,
  elementName: (node: RenderTreeNodePointer) => platform.readStringField(node, 8),
  descendantsEndIndex: (node: RenderTreeNodePointer) => platform.readInt32Field(node, 12) as NodeType,
  textContent: (node: RenderTreeNodePointer) => platform.readStringField(node, 16),
  attributeName: (node: RenderTreeNodePointer) => platform.readStringField(node, 20),
  attributeValue: (node: RenderTreeNodePointer) => platform.readStringField(node, 24),
  componentId: (node: RenderTreeNodePointer) => platform.readInt32Field(node, 32),
};

export enum NodeType {
  // The values must be kept in sync with the .NET equivalent in RenderTreeNodeType.cs
  element = 1,
  text = 2,
  attribute = 3,
  component = 4,
}

// Nominal type to ensure only valid pointers are passed to the renderTreeNode functions.
// At runtime the values are just numbers.
export interface RenderTreeNodePointer extends Pointer { RenderTreeNodePointer__DO_NOT_IMPLEMENT: any }
