import { System_String, System_Array, Pointer } from '../Platform/Platform';
import { platform } from '../Environment';
const renderTreeNodeStructLength = 36;

// To minimise GC pressure, instead of instantiating a JS object to represent each tree node,
// we work in terms of pointers to the structs on the .NET heap, and use static functions that
// know how to read property values from those structs.

export function getTreeNodePtr(renderTreeEntries: System_Array, index: number): RenderTreeNodePointer {
  return platform.getArrayEntryPtr(renderTreeEntries, index, renderTreeNodeStructLength) as RenderTreeNodePointer;
}

export const renderTreeNode = {
  // The properties and memory layout must be kept in sync with the .NET equivalent in RenderTreeNode.cs
  nodeType: (node: RenderTreeNodePointer) => _readInt32Property(node, 0) as NodeType,
  elementName: (node: RenderTreeNodePointer) => _readStringProperty(node, 4),
  descendantsEndIndex: (node: RenderTreeNodePointer) => _readInt32Property(node, 8) as NodeType,
  textContent: (node: RenderTreeNodePointer) => _readStringProperty(node, 12),
  attributeName: (node: RenderTreeNodePointer) => _readStringProperty(node, 16),
  attributeValue: (node: RenderTreeNodePointer) => _readStringProperty(node, 20),
  attributeEventHandlerValue: (node: RenderTreeNodePointer) => _readObjectProperty(node, 24),
  componentId: (node: RenderTreeNodePointer) => _readInt32Property(node, 28),
  component: (node: RenderTreeNodePointer) => _readObjectProperty(node, 32),
};

export enum NodeType {
  // The values must be kept in sync with the .NET equivalent in RenderTreeNodeType.cs
  element = 1,
  text = 2,
  attribute = 3,
  component = 4,
}

function _readInt32Property(baseAddress: Pointer, offsetBytes: number) {
  return platform.readHeapInt32(baseAddress, offsetBytes);
}

function _readObjectProperty(baseAddress: Pointer, offsetBytes: number) {
  return platform.readHeapObject(baseAddress, offsetBytes);
}

function _readStringProperty(baseAddress: Pointer, offsetBytes: number) {
  var managedString = platform.readHeapObject(baseAddress, offsetBytes) as System_String;
  return platform.toJavaScriptString(managedString);
}

// Nominal type to ensure only valid pointers are passed to the renderTreeNode functions.
// At runtime the values are just numbers.
export interface RenderTreeNodePointer extends Pointer { RenderTreeNodePointer__DO_NOT_IMPLEMENT: any }
