// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.AI;
using Microsoft.JSInterop;
using System.Linq;

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Selects files and attaches them to the nearest <see cref="MessageInput"/>.
/// </summary>
public sealed class MessageAttachButton : ComponentBase, IDisposable, IAsyncDisposable
{
    private const string ModulePath =
        "./_content/Microsoft.AspNetCore.Components.AI/ai-chat.js";

    private CancellationTokenSource? _readCancellation;
    private MessageInputContext? _subscribedContext;
    private IDisposable? _changeSubscription;
    private IJSObjectReference? _module;
    private IJSObjectReference? _dropRegistration;
    private ElementReference _container;
    private bool _isDisposed;

    /// <summary>
    /// Gets or sets the nearest message input.
    /// </summary>
    [CascadingParameter]
    public MessageInputContext Context { get; set; } = default!;

    /// <summary>
    /// Gets or sets the JavaScript runtime used to register an optional file drop zone.
    /// </summary>
    [Inject]
    internal IJSRuntime JSRuntime { get; set; } = default!;

    /// <summary>
    /// Gets or sets a comma-separated list of file types accepted by the picker.
    /// </summary>
    [Parameter]
    public string? Accept { get; set; }

    /// <summary>
    /// Gets or sets whether the picker accepts multiple files.
    /// </summary>
    [Parameter]
    public bool Multiple { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of matching attachments.
    /// </summary>
    [Parameter]
    public int MaximumFileCount { get; set; } = 10;

    /// <summary>
    /// Gets or sets the maximum size of each selected file in bytes.
    /// </summary>
    [Parameter]
    public long MaximumFileSize { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// Gets or sets the maximum total size of matching attachments in bytes.
    /// </summary>
    [Parameter]
    public long MaximumTotalSize { get; set; } = 50 * 1024 * 1024;

    /// <summary>
    /// Gets or sets a callback that converts a selected browser file into AI content.
    /// </summary>
    [Parameter]
    public Func<IBrowserFile, CancellationToken, ValueTask<DataContent>>? AttachmentFactory { get; set; }

    /// <summary>
    /// Gets or sets the class applied to the underlying file input.
    /// </summary>
    [Parameter]
    public string? InputClass { get; set; }

    /// <summary>
    /// Gets or sets a CSS selector that identifies the nearest ancestor that accepts dropped
    /// files.
    /// </summary>
    [Parameter]
    public string? DropZoneSelector { get; set; }

    /// <summary>
    /// Gets or sets the visible content of the attachment button.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets additional attributes applied to the attachment button.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (ReferenceEquals(_subscribedContext, Context))
        {
            return;
        }

        _changeSubscription?.Dispose();
        _subscribedContext = Context;
        _changeSubscription = Context.RegisterOnChanged(
            () => _ = InvokeAsync(StateHasChanged));
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var disabled = Context.IsConversationBusy ||
            Context.IsComposing ||
            MaximumFileCount <= 0 ||
            MaximumTotalSize <= 0;

        builder.OpenElement(0, "label");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "class", CssClass());
        if (disabled)
        {
            builder.AddAttribute(3, "aria-disabled", "true");
        }
        builder.AddElementReferenceCapture(4, reference => _container = reference);

        if (ChildContent is not null)
        {
            builder.AddContent(5, ChildContent);
        }
        else
        {
            builder.AddContent(6, "Attach files");
        }

        builder.OpenComponent<InputFile>(10);
        builder.AddComponentParameter(
            11,
            nameof(InputFile.OnChange),
            EventCallback.Factory.Create<InputFileChangeEventArgs>(this, AddFilesAsync));
        builder.AddComponentParameter(12, "accept", Accept);
        builder.AddComponentParameter(13, "multiple", Multiple);
        builder.AddComponentParameter(14, "disabled", disabled);
        if (!string.IsNullOrWhiteSpace(InputClass))
        {
            builder.AddComponentParameter(15, "class", InputClass);
        }
        builder.CloseComponent();

        builder.CloseElement();
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender ||
            !RendererInfo.IsInteractive ||
            string.IsNullOrWhiteSpace(DropZoneSelector))
        {
            return;
        }

        try
        {
            _module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);
            _dropRegistration = await _module.InvokeAsync<IJSObjectReference>(
                "registerFileDropZone",
                _container,
                DropZoneSelector);
        }
        catch (JSException)
        {
            Context.SetErrorMessage(
                "File drag and drop could not be initialized. Use the attachment button instead.");
        }
    }

    private async Task AddFilesAsync(InputFileChangeEventArgs args)
    {
        Context.SetErrorMessage(null);

        IReadOnlyList<IBrowserFile> files;
        try
        {
            files = args.GetMultipleFiles(Multiple ? MaximumFileCount : 1);
        }
        catch (InvalidOperationException)
        {
            Context.SetErrorMessage($"Select no more than {MaximumFileCount} files.");
            return;
        }

        if (files.Any(file => !MatchesAccept(file.Name, file.ContentType)))
        {
            Context.SetErrorMessage("One or more selected files have an unsupported type.");
            return;
        }

        var existing = Context.Attachments.Where(
            attachment => MatchesAccept(attachment.Name, attachment.MediaType)).ToArray();
        if (existing.Length + files.Count > MaximumFileCount)
        {
            Context.SetErrorMessage($"Attach no more than {MaximumFileCount} matching files.");
            return;
        }

        if (files.Any(file => file.Size > MaximumFileSize))
        {
            Context.SetErrorMessage($"Each file must be {FormatBytes(MaximumFileSize)} or smaller.");
            return;
        }

        var existingSize = existing.Sum(attachment => (long)attachment.Data.Length);
        if (existingSize + files.Sum(file => file.Size) > MaximumTotalSize)
        {
            Context.SetErrorMessage($"Attachments must be {FormatBytes(MaximumTotalSize)} or smaller in total.");
            return;
        }

        _readCancellation?.Cancel();
        _readCancellation?.Dispose();
        _readCancellation = new CancellationTokenSource();
        var cancellationToken = _readCancellation.Token;
        Context.SetComposing(true);

        try
        {
            var attachments = new List<DataContent>(files.Count);
            foreach (var file in files)
            {
                var content = AttachmentFactory is null
                    ? await CreateAttachmentAsync(file, cancellationToken)
                    : await AttachmentFactory(file, cancellationToken);
                content.Name ??= Path.GetFileName(file.Name);
                attachments.Add(content);
            }

            if (existingSize + attachments.Sum(attachment => (long)attachment.Data.Length) >
                MaximumTotalSize)
            {
                Context.SetErrorMessage($"Attachments must be {FormatBytes(MaximumTotalSize)} or smaller in total.");
                return;
            }

            foreach (var attachment in attachments)
            {
                await Context.AddAttachmentAsync(attachment);
            }

            Context.SetStatusMessage(
                $"{attachments.Count} file{(attachments.Count == 1 ? string.Empty : "s")} attached.");
        }
        catch (IOException)
        {
            Context.SetErrorMessage($"Each file must be {FormatBytes(MaximumFileSize)} or smaller.");
        }
        catch (InvalidOperationException exception)
        {
            Context.SetErrorMessage(exception.Message);
        }
        catch (JSException)
        {
            Context.SetErrorMessage(
                "One or more selected files could not be processed by this browser.");
        }
        finally
        {
            Context.SetComposing(false);
        }
    }

    private async ValueTask<DataContent> CreateAttachmentAsync(
        IBrowserFile file,
        CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream(MaximumFileSize, cancellationToken);
        var content = await DataContent.LoadFromAsync(stream, file.ContentType, cancellationToken);
        content.Name = Path.GetFileName(file.Name);
        return content;
    }

    private bool MatchesAccept(string? name, string mediaType)
    {
        if (string.IsNullOrWhiteSpace(Accept))
        {
            return true;
        }

        foreach (var item in Accept.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (item.StartsWith('.', StringComparison.Ordinal) &&
                string.Equals(Path.GetExtension(name), item, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (item.EndsWith("/*", StringComparison.Ordinal) &&
                mediaType.StartsWith(item.AsSpan(0, item.Length - 1), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(mediaType, item, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private string CssClass()
    {
        var css = "sc-ai-input__attach";
        if (AdditionalAttributes?.TryGetValue("class", out var value) == true &&
            value is string additionalClass)
        {
            css = $"{css} {additionalClass}";
        }

        return css;
    }

    private static string FormatBytes(long bytes)
    {
        const long megabyte = 1024 * 1024;
        return bytes >= megabyte && bytes % megabyte == 0
            ? $"{bytes / megabyte} MB"
            : $"{bytes} bytes";
    }

    /// <summary>
    /// Cancels any in-progress file read.
    /// </summary>
    public void Dispose()
    {
        _changeSubscription?.Dispose();
        _readCancellation?.Cancel();
        _readCancellation?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Cancels any in-progress file read and releases browser drop-zone resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        Dispose();

        if (_dropRegistration is not null)
        {
            try
            {
                await _dropRegistration.InvokeVoidAsync("dispose");
                await _dropRegistration.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }
    }
}
