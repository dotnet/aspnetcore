// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

public class MessageInput : IComponent, IDisposable
{
    private RenderHandle _renderHandle;
    private AgentContext _agentContext = default!;
    private string? _placeholder;
    private RenderFragment? _leadingActions;
    private RenderFragment? _trailingActions;
    private string _text = "";
    private bool _isDisabled;
    private IDisposable? _statusSub;
    private bool _allowAttachments;
    private string? _acceptFileTypes;
    private readonly List<AttachedFile> _attachments = new();
    private int _fileInputKey;

    [CascadingParameter]
    public AgentContext AgentContext { get; set; } = default!;

    [Parameter]
    public string? Placeholder { get; set; }

    [Parameter]
    public RenderFragment? LeadingActions { get; set; }

    [Parameter]
    public RenderFragment? TrailingActions { get; set; }

    [Parameter]
    public bool AllowAttachments { get; set; }

    [Parameter]
    public string? AcceptFileTypes { get; set; }

    void IComponent.Attach(RenderHandle renderHandle)
    {
        _renderHandle = renderHandle;
    }

    Task IComponent.SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);
        _agentContext = AgentContext
            ?? throw new InvalidOperationException(
                "MessageInput must be inside an AgentBoundary.");
        _placeholder = Placeholder;
        _leadingActions = LeadingActions;
        _trailingActions = TrailingActions;
        _allowAttachments = AllowAttachments;
        _acceptFileTypes = AcceptFileTypes;

        _statusSub = _agentContext.RegisterOnStatusChanged(status =>
        {
            _isDisabled = status == ConversationStatus.Streaming;
            Render();
        });

        Render();
        return Task.CompletedTask;
    }

    private void Render()
    {
        _renderHandle.Render(builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "sc-ai-input");

            if (_leadingActions is not null)
            {
                builder.AddContent(2, _leadingActions);
            }

            // Attach button (left side) — label wrapping a hidden InputFile
            if (_allowAttachments)
            {
                builder.OpenElement(20, "label");
                builder.AddAttribute(21, "class", "sc-ai-input__attach");
                builder.AddAttribute(22, "aria-label", "Attach file");

                builder.OpenComponent<InputFile>(23);
                builder.SetKey(_fileInputKey);
                builder.AddComponentParameter(24, "OnChange",
                    EventCallback.Factory.Create<InputFileChangeEventArgs>(this, OnFilesSelected));
                builder.AddComponentParameter(25, "AdditionalAttributes",
                    (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                    {
                        ["accept"] = _acceptFileTypes ?? "image/*",
                        ["multiple"] = true,
                        ["class"] = "sc-ai-input__file",
                        ["aria-hidden"] = "true",
                    });
                builder.CloseComponent();

                // Paperclip SVG icon
                builder.AddMarkupContent(26,
                    "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M21.44 11.05l-9.19 9.19a6 6 0 0 1-8.49-8.49l9.19-9.19a4 4 0 0 1 5.66 5.66l-9.2 9.19a2 2 0 0 1-2.83-2.83l8.49-8.48\"/></svg>");
                builder.CloseElement(); // label
            }

            // Main input area (file names + textarea)
            builder.OpenElement(3, "div");
            builder.AddAttribute(4, "class", "sc-ai-input__body");

            // Attachment names
            if (_attachments.Count > 0)
            {
                builder.OpenElement(5, "div");
                builder.AddAttribute(6, "class", "sc-ai-input__attachments");

                for (var i = 0; i < _attachments.Count; i++)
                {
                    var attachment = _attachments[i];
                    var index = i;

                    builder.OpenElement(7, "span");
                    builder.AddAttribute(8, "class", "sc-ai-attachment-preview");

                    builder.OpenElement(9, "span");
                    builder.AddAttribute(10, "class", "sc-ai-attachment-preview__file");
                    builder.AddContent(11, attachment.Name);
                    builder.CloseElement();

                    builder.OpenElement(13, "button");
                    builder.AddAttribute(14, "type", "button");
                    builder.AddAttribute(15, "class", "sc-ai-attachment-preview__remove");
                    builder.AddAttribute(16, "aria-label", $"Remove {attachment.Name}");
                    builder.AddAttribute(17, "onclick",
                        EventCallback.Factory.Create(this, () => RemoveAttachment(index)));
                    builder.AddContent(18, "\u00d7");
                    builder.CloseElement(); // remove button

                    builder.CloseElement(); // preview
                }

                builder.CloseElement(); // attachments container
            }

            builder.OpenElement(10, "textarea");
            builder.AddAttribute(11, "class", "sc-ai-input__textarea");
            builder.AddAttribute(12, "placeholder",
                _placeholder ?? "Type a message...");
            builder.AddAttribute(13, "disabled", _isDisabled);
            builder.AddAttribute(14, "value", _text);
            builder.AddAttribute(15, "aria-label", _placeholder ?? "Type a message...");
            builder.AddAttribute(16, "oninput",
                EventCallback.Factory.Create<ChangeEventArgs>(
                    this, e => _text = e.Value?.ToString() ?? ""));
            builder.AddAttribute(17, "onkeydown",
                EventCallback.Factory.Create<KeyboardEventArgs>(
                    this, OnKeyDown));
            builder.CloseElement(); // textarea

            builder.CloseElement(); // input body

            if (_trailingActions is not null)
            {
                builder.AddContent(50, _trailingActions);
            }
            else
            {
                builder.OpenElement(50, "button");
                builder.AddAttribute(51, "type", "button");
                builder.AddAttribute(52, "class", "sc-ai-input__send");
                builder.AddAttribute(53, "disabled", _isDisabled);
                builder.AddAttribute(54, "aria-label", "Send message");
                builder.AddAttribute(55, "onclick",
                    EventCallback.Factory.Create(this, SubmitAsync));

                // Send icon SVG
                builder.AddMarkupContent(56,
                    "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M22 2 11 13\"/><path d=\"M22 2 15 22 11 13 2 9z\"/></svg>");

                builder.CloseElement();
            }

            builder.CloseElement(); // div
        });
    }

    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    private async Task OnFilesSelected(InputFileChangeEventArgs e)
    {
        foreach (var file in e.GetMultipleFiles())
        {
            using var stream = file.OpenReadStream(MaxFileSize);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            _attachments.Add(new AttachedFile(file.Name, file.ContentType, ms.ToArray()));
        }

        _fileInputKey++;
        Render();
    }

    private void RemoveAttachment(int index)
    {
        if (index >= 0 && index < _attachments.Count)
        {
            _attachments.RemoveAt(index);
            Render();
        }
    }

    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey && !_isDisabled)
        {
            await SubmitAsync();
        }
    }

    private async Task SubmitAsync()
    {
        if (string.IsNullOrWhiteSpace(_text))
        {
            return;
        }

        if (_isDisabled)
        {
            return;
        }

        var contents = new List<AIContent>();
        contents.Add(new TextContent(_text));

        foreach (var attachment in _attachments)
        {
            contents.Add(new DataContent(attachment.Data, attachment.MediaType));
        }

        _text = "";
        _attachments.Clear();
        Render();

        var message = new ChatMessage(ChatRole.User, contents);
        await _agentContext.SendMessageAsync(message);
    }

    public void Dispose()
    {
        _statusSub?.Dispose();
    }

    internal sealed class AttachedFile
    {
        public AttachedFile(string name, string mediaType, byte[] data)
        {
            Name = name;
            MediaType = mediaType;
            Data = data;
        }

        public string Name { get; }
        public string MediaType { get; }
        public byte[] Data { get; }
    }
}
