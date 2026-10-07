// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Specifies how an <see cref="AudioCaptureButton"/> converts speech to text.
/// </summary>
public enum SpeechRecognitionMode
{
    /// <summary>
    /// Records audio and uses the configured backend transcription callback.
    /// </summary>
    BackendTranscription,

    /// <summary>
    /// Shows browser-recognized text while recording, then replaces it with the
    /// configured backend transcription.
    /// </summary>
    BrowserSpeechRecognitionWithBackendTranscription,

    /// <summary>
    /// Uses browser speech recognition without recording or uploading audio.
    /// </summary>
    BrowserSpeechRecognition,
}
