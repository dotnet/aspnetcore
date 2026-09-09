// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using AGUI.Abstractions;
using Microsoft.AspNetCore.Components.AI;
using Microsoft.Extensions.AI;

namespace DojoAgent;

/// <summary>Transports native scenario updates without AG-UI's tool-call buffering or text conversion.</summary>
public static class DojoContentTransport
{
    /// <summary>The custom event carrying a complete native update.</summary>
    public const string EventName = "dojo.content-update";

    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    /// <summary>Wraps a server model to emit complete updates as custom events before AG-UI conversion.</summary>
    /// <param name="innerClient">The native scenario model.</param>
    /// <returns>The scenario's AG-UI content adapter.</returns>
    public static IChatClient WrapServer(IChatClient innerClient) => new ServerChatClient(innerClient);

    private static async IAsyncEnumerable<ChatResponseUpdate> EncodeUpdates(
        IAsyncEnumerable<ChatResponseUpdate> updates,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var update in updates.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            // Raw events bypass standard conversion, including buffering native tool calls.
            yield return new ChatResponseUpdate
            {
                RawRepresentation = new CustomEvent
                {
                    Name = EventName,
                    Value = JsonSerializer.SerializeToElement(update, SerializerOptions),
                },
            };
        }
    }

    /// <summary>Restores the native update carried by a dojo custom event.</summary>
    /// <param name="customEvent">The event received through AG-UI's HTTP/SSE transport.</param>
    /// <returns>The deserialized native chat update.</returns>
    public static ChatResponseUpdate Decode(CustomEvent customEvent)
    {
        ArgumentNullException.ThrowIfNull(customEvent);
        if (customEvent.Name != EventName)
        {
            throw new JsonException($"Unexpected dojo content event '{customEvent.Name}'.");
        }

        return Deserialize(customEvent.Value ?? throw new JsonException("A dojo content update is required."));
    }

    private static ChatResponseUpdate Deserialize(JsonElement payload)
    {
        var update = payload.Deserialize<ChatResponseUpdate>(SerializerOptions)
            ?? throw new JsonException("A dojo content update is required.");
        foreach (var content in update.Contents)
        {
            // System.Text.Json represents object-valued results as JsonElement by default.
            if (content is FunctionResultContent { Result: JsonElement { ValueKind: JsonValueKind.String } result } functionResult)
            {
                functionResult.Result = result.GetString();
            }
        }

        return update;
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(typeInfo =>
        {
            if (typeInfo.Type == typeof(AIContent))
            {
                typeInfo.PolymorphismOptions!.DerivedTypes.Add(
                    new JsonDerivedType(typeof(RichTextContent), "dojo-rich-text"));
            }
        });

        return new JsonSerializerOptions(AIJsonUtilities.DefaultOptions)
        {
            TypeInfoResolver = resolver,
            Converters = { new NodeConverter() },
        };
    }

    private sealed class ServerChatClient(IChatClient innerClient) : DelegatingChatClient(innerClient)
    {
        public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
            => EncodeUpdates(base.GetStreamingResponseAsync(messages, options, cancellationToken), cancellationToken);

        public override Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
            => GetStreamingResponseAsync(messages, options, cancellationToken)
                .ToChatResponseAsync(cancellationToken);
    }

    private sealed class NodeConverter : JsonConverter<RichTextNode>
    {
        private static readonly Dictionary<string, Type> NodeTypes = new Type[]
        {
            typeof(BlockQuoteNode), typeof(CodeBlockNode), typeof(DefinitionNode),
            typeof(EmphasisNode), typeof(FootnoteDefinitionNode), typeof(FootnoteNode),
            typeof(FootnoteReferenceNode), typeof(HeadingNode), typeof(HtmlNode),
            typeof(ImageNode), typeof(ImageReferenceNode), typeof(InlineCodeNode),
            typeof(LineBreakNode), typeof(LinkNode), typeof(LinkReferenceNode),
            typeof(ListItemNode), typeof(ListNode), typeof(ParagraphNode),
            typeof(StrikethroughNode), typeof(StrongNode), typeof(TableCellNode),
            typeof(TableNode), typeof(TableRowNode), typeof(TextNode), typeof(ThematicBreakNode),
        }.ToDictionary(type => type.Name, StringComparer.Ordinal);

        public override RichTextNode Read(
            ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;
            var kind = root.GetProperty("kind").GetString();
            if (kind is null || !NodeTypes.TryGetValue(kind, out var nodeType))
            {
                throw new JsonException($"Unsupported rich-text node '{kind}'.");
            }

            var value = root.GetProperty("node");
            var node = (RichTextNode)value.Deserialize(nodeType, options)!;
            // Children is a read-only view; concrete node deserialization restores properties only.
            var childrenProperty = options.PropertyNamingPolicy?.ConvertName(nameof(RichTextNode.Children))
                ?? nameof(RichTextNode.Children);
            foreach (var child in value.GetProperty(childrenProperty).EnumerateArray())
            {
                node.AddChild(child.Deserialize<RichTextNode>(options)!);
            }

            return node;
        }

        public override void Write(
            Utf8JsonWriter writer, RichTextNode value, JsonSerializerOptions options)
        {
            var nodeType = value.GetType();
            if (!NodeTypes.TryGetValue(nodeType.Name, out var knownType) || knownType != nodeType)
            {
                throw new JsonException($"Unsupported rich-text node '{nodeType}'.");
            }

            writer.WriteStartObject();
            writer.WriteString("kind", nodeType.Name);
            writer.WritePropertyName("node");
            JsonSerializer.Serialize(writer, value, nodeType, options);
            writer.WriteEndObject();
        }
    }
}
