// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;

#nullable enable

namespace Ignitor
{
    public static class RenderBatchReader
    {
        private const int ReferenceFrameSize = 20;

        public static RenderBatch Read(ReadOnlySpan<byte> data)
        {
            var sections = Sections.Parse(data);
            var strings = ReadStringTable(data, sections.GetStringTableIndexes(data));
            var diffs = ReadUpdatedComponents(data, sections.GetUpdatedComponentIndexes(data), strings);
            var frames = ReadReferenceFrames(sections.GetReferenceFrameData(data), strings);
            var disposedComponentIds = ReadDisposedComponentIds(data);
            var disposedEventHandlerIds = ReadDisposedEventHandlerIds(data);
            return new RenderBatch(diffs, frames, disposedComponentIds, disposedEventHandlerIds);
        }

        private static string[] ReadStringTable(ReadOnlySpan<byte> data, ReadOnlySpan<byte> indexes)
        {
            var result = new string[indexes.Length / 4];

            for (var i = 0; i < indexes.Length; i += 4)
            {
                var index = BitConverter.ToInt32(indexes.Slice(i, 4));

                // The string table entries are all length-prefixed UTF8 blobs
                var length = (int)ReadUnsignedLEB128(data, index, out var numLEB128Bytes);
                var value = Encoding.UTF8.GetString(data.Slice(index + numLEB128Bytes, length));
                result[i / 4] = value;
            }

            return result;
        }

        private static ArrayRange<RenderTreeDiff> ReadUpdatedComponents(ReadOnlySpan<byte> data, ReadOnlySpan<byte> indexes, string[] strings)
        {
            var result = new RenderTreeDiff[indexes.Length / 4];

            for (var i = 0; i < indexes.Length; i += 4)
            {
                var index = BitConverter.ToInt32(indexes.Slice(i, 4));

                var componentId = BitConverter.ToInt32(data.Slice(index, 4));
                var editCount = BitConverter.ToInt32(data.Slice(index + 4, 4));

                var editData = data.Slice(index + 8);
                var edits = new RenderTreeEdit[editCount];
                for (var j = 0; j < editCount; j++)
                {
                    var type = (RenderTreeEditType)BitConverter.ToInt32(editData.Slice(0, 4));
                    var siblingIndex = BitConverter.ToInt32(editData.Slice(4, 4));

                    // ReferenceFrameIndex and MoveToSiblingIndex share a slot, so this reads
                    // whichever one applies to the edit type
                    var referenceFrameIndex = BitConverter.ToInt32(editData.Slice(8, 4));
                    var removedAttributeName = ReadString(editData.Slice(12, 4), strings);

                    editData = editData.Slice(16);

                    switch (type)
                    {
                        case RenderTreeEditType.UpdateText:
                            edits[j] = RenderTreeEdit.UpdateText(siblingIndex, referenceFrameIndex);
                            break;

                        case RenderTreeEditType.UpdateMarkup:
                            edits[j] = RenderTreeEdit.UpdateMarkup(siblingIndex, referenceFrameIndex);
                            break;

                        case RenderTreeEditType.SetAttribute:
                            edits[j] = RenderTreeEdit.SetAttribute(siblingIndex, referenceFrameIndex);
                            break;

                        case RenderTreeEditType.RemoveAttribute:
                            edits[j] = RenderTreeEdit.RemoveAttribute(siblingIndex, removedAttributeName);
                            break;

                        case RenderTreeEditType.PrependFrame:
                            edits[j] = RenderTreeEdit.PrependFrame(siblingIndex, referenceFrameIndex);
                            break;

                        case RenderTreeEditType.RemoveFrame:
                            edits[j] = RenderTreeEdit.RemoveFrame(siblingIndex);
                            break;

                        case RenderTreeEditType.StepIn:
                            edits[j] = RenderTreeEdit.StepIn(siblingIndex);
                            break;

                        case RenderTreeEditType.StepOut:
                            edits[j] = RenderTreeEdit.StepOut();
                            break;

                        case RenderTreeEditType.PermutationListEntry:
                            edits[j] = RenderTreeEdit.PermutationListEntry(siblingIndex, referenceFrameIndex);
                            break;

                        case RenderTreeEditType.PermutationListEnd:
                            edits[j] = RenderTreeEdit.PermutationListEnd();
                            break;

                        default:
                            throw new InvalidOperationException("Unknown edit type:" + type);
                    }
                }

                result[i / 4] = new RenderTreeDiff(componentId, ToArrayBuilderSegment(edits));
            }

            return new ArrayRange<RenderTreeDiff>(result, result.Length);
        }

        private static ArrayBuilderSegment<T> ToArrayBuilderSegment<T>(T[] entries)
        {
            var builder = new ArrayBuilder<T>();
            builder.Append(entries, 0, entries.Length);
            return builder.ToSegment(0, entries.Length);
        }

        private static ArrayRange<RenderTreeFrame> ReadReferenceFrames(ReadOnlySpan<byte> data, string[] strings)
        {
            var result = new RenderTreeFrame[data.Length / ReferenceFrameSize];

            for (var i = 0; i < data.Length; i += ReferenceFrameSize)
            {
                var frameData = data.Slice(i, ReferenceFrameSize);

                var type = (RenderTreeFrameType)BitConverter.ToInt32(frameData.Slice(0, 4));

                // We want each frame to take up the same number of bytes, so that the
                // recipient can index into the array directly instead of having to
                // walk through it.
                // Since we can fit every frame type into 16 bytes, use that as the
                // common size. For smaller frames, we add padding to expand it to
                // 16 bytes.
                switch (type)
                {
                    case RenderTreeFrameType.Attribute:
                        var attributeName = ReadString(frameData.Slice(4, 4), strings);
                        var attributeValue = ReadString(frameData.Slice(8, 4), strings);
                        var attributeEventHandlerId = BitConverter.ToUInt64(frameData.Slice(12, 8));
                        result[i / ReferenceFrameSize] = RenderTreeFrame.Attribute(0, attributeName, attributeValue).WithAttributeEventHandlerId(attributeEventHandlerId);
                        break;

                    case RenderTreeFrameType.Component:
                        var componentSubtreeLength = BitConverter.ToInt32(frameData.Slice(4, 4));
                        var componentId = BitConverter.ToInt32(frameData.Slice(8, 4)); // Nowhere to put this without creating a ComponentState
                        result[i / ReferenceFrameSize] = RenderTreeFrame.ChildComponent(0, componentType: null)
                            .WithComponentSubtreeLength(componentSubtreeLength)
                            .WithComponent(new ComponentState(componentId));
                        break;

                    case RenderTreeFrameType.ComponentReferenceCapture:
                        // Client doesn't process these, skip.
                        result[i / ReferenceFrameSize] = RenderTreeFrame.ComponentReferenceCapture(0, null, 0);
                        break;

                    case RenderTreeFrameType.Element:
                        var elementSubtreeLength = BitConverter.ToInt32(frameData.Slice(4, 4));
                        var elementName = ReadString(frameData.Slice(8, 4), strings);
                        result[i / ReferenceFrameSize] = RenderTreeFrame.Element(0, elementName).WithElementSubtreeLength(elementSubtreeLength);
                        break;

                    case RenderTreeFrameType.ElementReferenceCapture:
                        var referenceCaptureId = ReadString(frameData.Slice(4, 4), strings);
                        result[i / ReferenceFrameSize] = RenderTreeFrame.ElementReferenceCapture(0, null)
                            .WithElementReferenceCaptureId(referenceCaptureId);
                        break;

                    case RenderTreeFrameType.Region:
                        var regionSubtreeLength = BitConverter.ToInt32(frameData.Slice(4, 4));
                        result[i / ReferenceFrameSize] = RenderTreeFrame.Region(0).WithRegionSubtreeLength(regionSubtreeLength);
                        break;

                    case RenderTreeFrameType.Text:
                        var text = ReadString(frameData.Slice(4, 4), strings);
                        result[i / ReferenceFrameSize] = RenderTreeFrame.Text(0, text);
                        break;

                    case RenderTreeFrameType.Markup:
                        var markup = ReadString(frameData.Slice(4, 4), strings);
                        result[i / ReferenceFrameSize] = RenderTreeFrame.Markup(0, markup);
                        break;

                    default:
                        throw new ArgumentException($"Unsupported frame type: {type}");
                }
            }

            return new ArrayRange<RenderTreeFrame>(result, result.Length);
        }

        private static ArrayRange<int> ReadDisposedComponentIds(ReadOnlySpan<byte> data)
        {
            return new ArrayRange<int>(Array.Empty<int>(), 0);
        }

        private static ArrayRange<ulong> ReadDisposedEventHandlerIds(ReadOnlySpan<byte> data)
        {
            return new ArrayRange<ulong>(Array.Empty<ulong>(), 0);
        }

        private static string? ReadString(ReadOnlySpan<byte> data, string[] strings)
        {
            var index = BitConverter.ToInt32(data.Slice(0, 4));
            return index >= 0 ? strings[index] : null;
        }

        private static uint ReadUnsignedLEB128(ReadOnlySpan<byte> data, int startOffset, out int numBytesRead)
        {
            var result = (uint)0;
            var shift = 0;
            var currentByte = (byte)128;
            numBytesRead = 0;

            for (var count = 0; count < 4 && currentByte >= 128; count++)
            {
                currentByte = data[startOffset + count];
                result += (uint)(currentByte & 0x7f) << shift;
                shift += 7;
                numBytesRead++;
            }

            return result;
        }

        private readonly struct Sections
        {
            public static Sections Parse(ReadOnlySpan<byte> data)
            {
                return new Sections(
                    BitConverter.ToInt32(data.Slice(data.Length - 20, 4)),
                    BitConverter.ToInt32(data.Slice(data.Length - 16, 4)),
                    BitConverter.ToInt32(data.Slice(data.Length - 12, 4)),
                    BitConverter.ToInt32(data.Slice(data.Length - 8, 4)),
                    BitConverter.ToInt32(data.Slice(data.Length - 4, 4)));
            }

            private readonly int _updatedComponents;
            private readonly int _referenceFrames;
            private readonly int _disposedComponentIds;
            private readonly int _disposedEventHandlerIds;
            private readonly int _strings;

            public Sections(int updatedComponents, int referenceFrames, int disposedComponentIds, int disposedEventHandlerIds, int strings)
            {
                _updatedComponents = updatedComponents;
                _referenceFrames = referenceFrames;
                _disposedComponentIds = disposedComponentIds;
                _disposedEventHandlerIds = disposedEventHandlerIds;
                _strings = strings;
            }

            public ReadOnlySpan<byte> GetUpdatedComponentIndexes(ReadOnlySpan<byte> data)
            {
                // This is count-prefixed contiguous array of of integers.
                var count = BitConverter.ToInt32(data.Slice(_updatedComponents, 4));
                return data.Slice(_updatedComponents + 4, count * 4);
            }

            public ReadOnlySpan<byte> GetReferenceFrameData(ReadOnlySpan<byte> data)
            {
                // This is a count-prefixed contiguous array of RenderTreeFrame.
                var count = BitConverter.ToInt32(data.Slice(_referenceFrames, 4));
                return data.Slice(_referenceFrames + 4, count * ReferenceFrameSize);
            }

            public ReadOnlySpan<byte> GetStringTableIndexes(ReadOnlySpan<byte> data)
            {
                // This is a contiguous array of integers delimited by the end of the data section.
                return data.Slice(_strings, data.Length - 20 - _strings);
            }
        }
    }
}
#nullable restore
