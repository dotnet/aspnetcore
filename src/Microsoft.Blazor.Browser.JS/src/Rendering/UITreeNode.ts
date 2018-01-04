import { System_String, System_Array, Pointer } from '../Platform/Platform';
import { platform } from '../Environment';
const uiTreeNodeStructLength = 16;

// To minimise GC pressure, instead of instantiating a JS object to represent each tree node,
// we work in terms of pointers to the structs on the .NET heap, and use static functions that
// know how to read property values from those structs.

export function getTreeNodePtr(uiTreeEntries: System_Array, index: number): UITreeNodePointer {
  return platform.getArrayEntryPtr(uiTreeEntries, index, uiTreeNodeStructLength) as UITreeNodePointer;
}

export const uiTreeNode = {
  // The properties and memory layout must be kept in sync with the .NET equivalent in UITreeNode.cs
  nodeType: (node: UITreeNodePointer) => _readInt32Property(node, 0) as NodeType,
  elementName: (node: UITreeNodePointer) => _readStringProperty(node, 4),
  descendantsEndIndex: (node: UITreeNodePointer) => _readInt32Property(node, 8) as NodeType,
  textContent: (node: UITreeNodePointer) => _readStringProperty(node, 12),
};

export enum NodeType {
  // The values must be kept in sync with the .NET equivalent in UITreeNodeType.cs
  element = 1,
  text = 2
}

function _readInt32Property(baseAddress: Pointer, offsetBytes: number) {
  return platform.readHeapInt32(baseAddress, offsetBytes);
}

function _readStringProperty(baseAddress: Pointer, offsetBytes: number) {
  var managedString = platform.readHeapObject(baseAddress, offsetBytes) as System_String;
  return platform.toJavaScriptString(managedString);
}

// Nominal type to ensure only valid pointers are passed to the uiTreeNode functions.
// At runtime the values are just numbers.
export interface UITreeNodePointer extends Pointer { UITreeNodePointer__DO_NOT_IMPLEMENT: any }
