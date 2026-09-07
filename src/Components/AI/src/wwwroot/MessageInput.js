export function registerMessageInput(textarea, callbacks) {
    let isBusy = false;

    const handleTextareaKeyDown = event => {
        if (event.key === "Enter" &&
            !event.shiftKey &&
            !event.isComposing &&
            event.keyCode !== 229 &&
            !event.repeat) {
            event.preventDefault();
        }
    };

    const handleDocumentKeyDown = event => {
        if (isBusy && event.key === "Escape" && !event.repeat) {
            event.preventDefault();
            callbacks.invokeMethodAsync("HandleEscapeAsync").catch(error => {
                console.warn("Message input Escape shortcut failed.", error);
            });
        }
    };

    textarea.addEventListener("keydown", handleTextareaKeyDown);
    document.addEventListener("keydown", handleDocumentKeyDown);

    return {
        setBusy(value) {
            isBusy = value;
        },
        dispose() {
            textarea.removeEventListener("keydown", handleTextareaKeyDown);
            document.removeEventListener("keydown", handleDocumentKeyDown);
        },
    };
}

export function registerFileDropZone(container, selector) {
    const input = container.querySelector('input[type="file"]');
    const dropZone = container.closest(selector) ?? document.querySelector(selector);
    if (!input || !dropZone) {
        throw new Error(`Could not find the file input or drop zone '${selector}'.`);
    }

    dropZone.dataset.scAiDropZone = "true";
    let dragDepth = 0;

    const containsFiles = event =>
        Array.from(event.dataTransfer?.types ?? []).includes("Files");

    const setActive = active => {
        dropZone.classList.toggle("sc-ai-drop-zone--active", active);
    };

    const handleDragEnter = event => {
        if (!containsFiles(event)) {
            return;
        }

        event.preventDefault();
        dragDepth++;
        setActive(true);
    };

    const handleDragOver = event => {
        if (!containsFiles(event)) {
            return;
        }

        event.preventDefault();
        event.dataTransfer.dropEffect = "copy";
    };

    const handleDragLeave = event => {
        if (!containsFiles(event)) {
            return;
        }

        dragDepth = Math.max(0, dragDepth - 1);
        if (dragDepth === 0) {
            setActive(false);
        }
    };

    const handleDrop = event => {
        if (!containsFiles(event)) {
            return;
        }

        event.preventDefault();
        dragDepth = 0;
        setActive(false);
        if (input.disabled || !event.dataTransfer?.files.length) {
            return;
        }

        input.files = event.dataTransfer.files;
        input.dispatchEvent(new Event("change", { bubbles: true }));
    };

    dropZone.addEventListener("dragenter", handleDragEnter);
    dropZone.addEventListener("dragover", handleDragOver);
    dropZone.addEventListener("dragleave", handleDragLeave);
    dropZone.addEventListener("drop", handleDrop);

    return {
        dispose() {
            setActive(false);
            delete dropZone.dataset.scAiDropZone;
            dropZone.removeEventListener("dragenter", handleDragEnter);
            dropZone.removeEventListener("dragover", handleDragOver);
            dropZone.removeEventListener("dragleave", handleDragLeave);
            dropZone.removeEventListener("drop", handleDrop);
        },
    };
}

export function isAudioCaptureSupported() {
    return typeof globalThis.MediaRecorder === "function" &&
        typeof navigator?.mediaDevices?.getUserMedia === "function";
}

export function createAudioRecorder(maximumBytes) {
    return new AudioRecorder(maximumBytes);
}

export function isLiveSpeechRecognitionSupported() {
    return typeof (globalThis.SpeechRecognition ?? globalThis.webkitSpeechRecognition) === "function";
}

export function createLiveSpeechRecognizer(callbacks, language) {
    return new LiveSpeechRecognizer(callbacks, language);
}

class LiveSpeechRecognizer {
    constructor(callbacks, language) {
        const Recognition = globalThis.SpeechRecognition ?? globalThis.webkitSpeechRecognition;
        if (typeof Recognition !== "function") {
            throw new Error("Browser speech recognition is not supported.");
        }

        this.callbacks = callbacks;
        this.recognition = new Recognition();
        this.recognition.continuous = true;
        this.recognition.interimResults = true;
        if (language) {
            this.recognition.lang = language;
        }

        this.requested = false;
        this.starting = false;
        this.started = false;
        this.restartTimer = undefined;
        this.recognition.addEventListener("start", () => {
            this.starting = false;
            this.started = true;
            if (!this.requested) {
                this.recognition.abort();
                return;
            }

            this.callbacks.invokeMethodAsync("OnStartedAsync").catch(error => {
                console.warn("Live speech start handling failed.", error);
            });
        });
        this.recognition.addEventListener("result", event => {
            let finalTranscript = "";
            let interimTranscript = "";
            for (let index = event.resultIndex; index < event.results.length; index++) {
                const result = event.results[index];
                const transcript = result[0]?.transcript ?? "";
                if (result.isFinal) {
                    finalTranscript += transcript;
                } else {
                    interimTranscript += transcript;
                }
            }

            this.callbacks.invokeMethodAsync(
                "OnResultAsync",
                finalTranscript.trim(),
                interimTranscript.trim()).catch(error => {
                    console.warn("Live speech result handling failed.", error);
                });
        });
        this.recognition.addEventListener("error", event => {
            this.starting = false;
            if (event.error === "no-speech" || event.error === "aborted") {
                return;
            }

            const error = event.error ?? "unknown";
            const isFatal = error === "not-allowed" ||
                error === "service-not-allowed" ||
                error === "language-not-supported" ||
                error === "bad-grammar";
            if (isFatal) {
                this.requested = false;
            }

            this.callbacks.invokeMethodAsync("OnErrorAsync", error, isFatal)
                .catch(error => {
                    console.warn("Live speech error handling failed.", error);
                });
        });
        this.recognition.addEventListener("end", () => {
            this.starting = false;
            this.started = false;
            if (this.requested) {
                this.restartTimer = setTimeout(() => this.start(), 250);
            }
        });
    }

    start() {
        this.requested = true;
        clearTimeout(this.restartTimer);
        if (!this.started && !this.starting) {
            this.starting = true;
            try {
                this.recognition.start();
            } catch (error) {
                this.starting = false;
                throw error;
            }
        }
    }

    stop() {
        this.requested = false;
        clearTimeout(this.restartTimer);
        if (this.starting) {
            this.starting = false;
            this.recognition.abort();
        } else if (this.started) {
            this.recognition.stop();
        }
    }

    dispose() {
        this.requested = false;
        this.starting = false;
        this.started = false;
        clearTimeout(this.restartTimer);
        this.recognition.abort();
    }
}

class AudioRecorder {
    constructor(maximumBytes) {
        this.maximumBytes = maximumBytes;
        this.recorder = undefined;
        this.stream = undefined;
        this.chunks = [];
        this.startedAt = 0;
    }

    async start() {
        if (this.recorder?.state === "recording") {
            return;
        }

        this.stream = await navigator.mediaDevices.getUserMedia({ audio: true });
        const mimeType = getPreferredAudioType();
        this.recorder = mimeType
            ? new MediaRecorder(this.stream, { mimeType })
            : new MediaRecorder(this.stream);
        this.chunks = [];
        this.recorder.addEventListener("dataavailable", event => {
            if (event.data.size > 0) {
                this.chunks.push(event.data);
            }
        });
        this.startedAt = performance.now();
        this.recorder.start(250);
    }

    async stop() {
        if (!this.recorder || this.recorder.state !== "recording") {
            return {
                streamReference: null,
                mimeType: "",
                size: 0,
                tooLarge: false,
            };
        }

        const activeRecorder = this.recorder;
        const remainingCaptureTime = Math.max(0, 600 - (performance.now() - this.startedAt));
        if (remainingCaptureTime > 0) {
            await new Promise(resolve => setTimeout(resolve, remainingCaptureTime));
        }

        return new Promise((resolve, reject) => {
            activeRecorder.addEventListener("error", event => {
                this.dispose();
                reject(event.error);
            }, { once: true });
            activeRecorder.addEventListener("stop", async () => {
                try {
                    const mimeType =
                        activeRecorder.mimeType || this.chunks[0]?.type || "audio/webm";
                    const blob = new Blob(this.chunks, { type: mimeType });
                    if (blob.size > this.maximumBytes) {
                        this.dispose();
                        resolve({
                            streamReference: null,
                            mimeType,
                            size: blob.size,
                            tooLarge: true,
                        });
                        return;
                    }

                    const size = blob.size;
                    const streamReference = DotNet.createJSStreamReference(blob);
                    this.dispose();
                    resolve({ streamReference, mimeType, size, tooLarge: false });
                } catch (error) {
                    this.dispose();
                    reject(error);
                }
            }, { once: true });

            try {
                activeRecorder.requestData();
            } catch {
                // Some implementations flush pending data automatically when stop is called.
            }
            activeRecorder.stop();
        });
    }

    dispose() {
        this.stream?.getTracks().forEach(track => track.stop());
        this.stream = undefined;
        this.recorder = undefined;
        this.chunks = [];
        this.startedAt = 0;
    }
}

function getPreferredAudioType() {
    const candidates = [
        "audio/webm;codecs=opus",
        "audio/ogg;codecs=opus",
        "audio/mp4",
    ];
    return candidates.find(type => MediaRecorder.isTypeSupported(type)) ?? "";
}
