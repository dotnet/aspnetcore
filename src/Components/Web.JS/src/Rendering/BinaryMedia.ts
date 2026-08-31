// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Logger, LogLevel } from '../Platform/Logging/Logger';
import { ConsoleLogger } from '../Platform/Logging/Loggers';

// Minimal File System Access API typings
interface FileSystemWritableFileStream {
  write(data: BufferSource | Blob | Uint8Array): Promise<void>;
  close(): Promise<void>;
  abort(): Promise<void>;
}
interface FileSystemFileHandle {
  createWritable(): Promise<FileSystemWritableFileStream>;
}
interface SaveFilePickerOptions {
  suggestedName?: string;
}
declare global {
  interface Window {
    showSaveFilePicker?: (options?: SaveFilePickerOptions) => Promise<FileSystemFileHandle>;
  }
}

export interface MediaLoadResult {
  success: boolean;
  fromCache: boolean;
  objectUrl: string | null;
  error?: string;
}

/**
 * Provides functionality for rendering binary media data in Blazor components.
 */
export class BinaryMedia {
  private static readonly CACHE_NAME = 'blazor-media-cache';

  private static cachePromise?: Promise<Cache | null> = undefined;

  private static logger: Logger = new ConsoleLogger(LogLevel.Warning);

  private static loadingElements: Set<HTMLElement> = new Set();

  private static activeCacheKey: WeakMap<HTMLElement, string> = new WeakMap();

  private static tracked: WeakMap<HTMLElement, { url: string; cacheKey: string; attr: 'src' | 'href' }> = new WeakMap();

  private static observersByParent: WeakMap<Element, MutationObserver> = new WeakMap();

  private static controllers: WeakMap<HTMLElement, AbortController> = new WeakMap();

  private static initializeParentObserver(parent: Element): void {
    if (this.observersByParent.has(parent)) {
      return;
    }

    const observer = new MutationObserver((mutations) => {
      for (const mutation of mutations) {
        // Handle removed nodes within this parent subtree
        if (mutation.type === 'childList') {
          for (const node of Array.from(mutation.removedNodes)) {
            if (node.nodeType === Node.ELEMENT_NODE) {
              const element = node as HTMLElement;

              // If the removed element itself is tracked, revoke
              if (this.tracked.has(element)) {
                this.revokeTrackedUrl(element);
              }

              // Any tracked descendants (look for elements that might carry src or href)
              element.querySelectorAll('[src],[href]').forEach((child) => {
                const childEl = child as HTMLElement;
                if (this.tracked.has(childEl)) {
                  this.revokeTrackedUrl(childEl);
                }
              });
            }
          }
        }

        // Attribute changes in this subtree
        if (mutation.type === 'attributes') {
          const attrName = (mutation as MutationRecord).attributeName;
          if (attrName === 'src' || attrName === 'href') {
            const element = mutation.target as HTMLElement;
            const tracked = this.tracked.get(element);
            if (tracked && tracked.attr === attrName) {
              const current = element.getAttribute(attrName) || '';
              if (!current || current !== tracked.url) {
                this.revokeTrackedUrl(element);
              }
            }
          }
        }
      }
    });

    observer.observe(parent, {
      childList: true,
      attributes: true,
      attributeFilter: ['src', 'href'],
    });

    this.observersByParent.set(parent, observer);
  }

  private static revokeTrackedUrl(el: HTMLElement): void {
    const tracked = this.tracked.get(el);
    if (tracked) {
      try {
        URL.revokeObjectURL(tracked.url);
      } catch {
        // ignore
      }
      this.tracked.delete(el);
      this.loadingElements.delete(el);
      this.activeCacheKey.delete(el);
    }
    // Abort any in-flight stream tied to this element
    const controller = this.controllers.get(el);
    if (controller) {
      try {
        controller.abort();
      } catch {
        // ignore
      }
      this.controllers.delete(el);
    }
  }

  /**
   * Single entry point for setting media content - handles cache check and streaming.
   */
  public static async setContentAsync(
    element: HTMLElement,
    streamRef: { stream: () => Promise<ReadableStream<Uint8Array>> } | null,
    mimeType: string,
    cacheKey: string,
    totalBytes: number | null,
    targetAttr: 'src' | 'href'
  ): Promise<MediaLoadResult> {
    if (!element || !cacheKey) {
      return { success: false, fromCache: false, objectUrl: null, error: 'Invalid parameters' };
    }

    // Ensure we are observing this element's parent
    const parent = element.parentElement;
    if (parent) {
      this.initializeParentObserver(parent);
    }

    // If there was a previous different key for this element, abort its in-flight operation
    const previousKey = this.activeCacheKey.get(element);
    if (previousKey && previousKey !== cacheKey) {
      const prevController = this.controllers.get(element);
      if (prevController) {
        try {
          prevController.abort();
        } catch {
          // ignore
        }
        this.controllers.delete(element);
      }
    }

    this.activeCacheKey.set(element, cacheKey);

    try {
      // Try cache first
      try {
        const cache = await this.getCache();
        if (cache) {
          const cachedResponse = await cache.match(encodeURIComponent(cacheKey));
          if (cachedResponse) {
            const blob = await cachedResponse.blob();
            const url = URL.createObjectURL(blob);

            this.setUrl(element, url, cacheKey, targetAttr);
            return { success: true, fromCache: true, objectUrl: url };
          }
        }
      } catch (err) {
        this.logger.log(LogLevel.Debug, `Cache lookup failed: ${err}`);
      }

      if (streamRef) {
        const url = await this.streamAndCreateUrl(element, streamRef, mimeType, cacheKey, totalBytes, targetAttr);
        if (url) {
          return { success: true, fromCache: false, objectUrl: url };
        }
      }

      return { success: false, fromCache: false, objectUrl: null, error: 'No/empty stream provided and not in cache' };
    } catch (error) {
      this.logger.log(LogLevel.Debug, `Error in setContentAsync: ${error}`);
      return { success: false, fromCache: false, objectUrl: null, error: String(error) };
    }
  }

  private static async streamAndCreateUrl(
    element: HTMLElement,
    streamRef: { stream: () => Promise<ReadableStream<Uint8Array>> },
    mimeType: string,
    cacheKey: string,
    totalBytes: number | null,
    targetAttr: 'src' | 'href'
  ): Promise<string | null> {

    // if (targetAttr === 'src' && element instanceof HTMLVideoElement) {
    //   try {
    //     const mediaSourceUrl = await this.tryMediaSourceVideoStreaming(
    //       element,
    //       streamRef,
    //       mimeType,
    //       cacheKey,
    //       totalBytes
    //     );
    //     if (mediaSourceUrl) {
    //       return mediaSourceUrl;
    //     }
    //   } catch (msErr) {
    //     this.logger.log(LogLevel.Debug, `MediaSource video streaming path failed, falling back. Error: ${msErr}`);
    //   }
    // }

    this.loadingElements.add(element);

    // Create and track an AbortController for this element
    const controller = new AbortController();
    this.controllers.set(element, controller);

    const readable = await streamRef.stream();
    let displayStream = readable;

    if (cacheKey) {
      const cache = await this.getCache();
      if (cache) {
        const [display, cacheStream] = readable.tee();
        displayStream = display;
        cache.put(encodeURIComponent(cacheKey), new Response(cacheStream)).catch(err => {
          this.logger.log(LogLevel.Debug, `Failed to put cache entry: ${err}`);
        });
      }
    }

    let resultUrl: string | null = null;
    try {
      const { aborted, chunks, bytesRead } = await this.readAllChunks(element, displayStream, controller, totalBytes);

      if (!aborted) {
        if (bytesRead === 0) {
          if (typeof totalBytes === 'number' && totalBytes > 0) {
            throw new Error('Stream was already consumed or at end position');
          }
          resultUrl = null;
        } else {
          const combined = this.combineChunks(chunks);
          const baseMimeType = this.extractBaseMimeType(mimeType);
          const blob = new Blob([combined.slice()], { type: baseMimeType });
          const url = URL.createObjectURL(blob);
          this.setUrl(element, url, cacheKey, targetAttr);
          resultUrl = url;
        }
      } else {
        resultUrl = null;
      }
    } finally {
      if (this.controllers.get(element) === controller) {
        this.controllers.delete(element);
      }
      this.loadingElements.delete(element);
      element.style.removeProperty('--blazor-media-progress');
    }

    return resultUrl;
  }

  private static async readAllChunks(
    element: HTMLElement,
    stream: ReadableStream<Uint8Array>,
    controller: AbortController,
    totalBytes: number | null
  ): Promise<{ aborted: boolean; chunks: Uint8Array[]; bytesRead: number }> {
    const chunks: Uint8Array[] = [];
    let bytesRead = 0;
    for await (const chunk of this.iterateStream(stream, controller.signal)) {
      if (controller.signal.aborted) {
        return { aborted: true, chunks, bytesRead };
      }
      chunks.push(chunk);
      bytesRead += chunk.byteLength;
      if (totalBytes) {
        const progress = Math.min(1, bytesRead / totalBytes);
        element.style.setProperty('--blazor-media-progress', progress.toString());
      }
    }
    return { aborted: controller.signal.aborted, chunks, bytesRead };
  }

  private static combineChunks(chunks: Uint8Array[]): Uint8Array {
    if (chunks.length === 1) {
      return chunks[0];
    }

    const total = chunks.reduce((sum, chunk) => sum + chunk.byteLength, 0);
    const combined = new Uint8Array(total);
    let offset = 0;
    for (const chunk of chunks) {
      combined.set(chunk, offset);
      offset += chunk.byteLength;
    }
    return combined;
  }

  private static setUrl(element: HTMLElement, url: string, cacheKey: string, targetAttr: 'src' | 'href'): void {
    const tracked = this.tracked.get(element);
    if (tracked) {
      try {
        URL.revokeObjectURL(tracked.url);
      } catch {
        // ignore
      }
    }

    this.tracked.set(element, { url, cacheKey, attr: targetAttr });

    this.setupEventHandlers(element, cacheKey);

    if (targetAttr === 'src') {
      (element as HTMLImageElement | HTMLVideoElement).src = url;
    } else {
      (element as HTMLAnchorElement).href = url;
    }
  }

  // Streams binary content to a user-selected file when possible,
  // otherwise falls back to buffering in memory and triggering a blob download via an anchor.
  public static async downloadAsync(
    element: HTMLElement,
    streamRef: { stream: () => Promise<ReadableStream<Uint8Array>> } | null,
    mimeType: string,
    totalBytes: number | null,
    fileName: string,
  ): Promise<boolean> {
    if (!element || !fileName || !streamRef) {
      return false;
    }

    this.loadingElements.add(element);
    const controller = new AbortController();
    this.controllers.set(element, controller);

    try {
      const readable = await streamRef.stream();

      // Native picker direct-to-file streaming available
      if (typeof window.showSaveFilePicker === 'function') {
        try {
          const handle = await window.showSaveFilePicker({ suggestedName: fileName });

          const writer = await handle.createWritable();
          const writeResult = await this.writeStreamToFile(element, readable, writer, totalBytes, controller);
          if (writeResult === 'success') {
            return true;
          }
          if (writeResult === 'aborted') {
            return false;
          }
        } catch (pickerErr) {
          this.logger.log(LogLevel.Debug, `Native picker streaming path failed or cancelled: ${pickerErr}`);
        }
      }

      // In-memory fallback: read all bytes then trigger anchor download
      const readResult = await this.readAllChunks(element, readable, controller, totalBytes);
      if (readResult.aborted) {
        return false;
      }
      const combined = this.combineChunks(readResult.chunks);
      const baseMimeType = this.extractBaseMimeType(mimeType);
      const blob = new Blob([combined.slice()], { type: baseMimeType });
      const url = URL.createObjectURL(blob);
      this.triggerDownload(url, fileName);

      return true;
    } catch (error) {
      this.logger.log(LogLevel.Debug, `Error in downloadAsync: ${error}`);
      return false;
    } finally {
      if (this.controllers.get(element) === controller) {
        this.controllers.delete(element);
      }
      this.loadingElements.delete(element);
      element.style.removeProperty('--blazor-media-progress');
    }
  }

  private static async writeStreamToFile(
    element: HTMLElement,
    stream: ReadableStream<Uint8Array>,
    writer: FileSystemWritableFileStream,
    totalBytes: number | null,
    controller?: AbortController
  ): Promise<'success' | 'aborted' | 'error'> {
    let written = 0;
    try {
      for await (const chunk of this.iterateStream(stream, controller?.signal)) {
        if (controller?.signal.aborted) {
          try {
            await writer.abort();
          } catch {
            /* ignore */
          }
          element.style.removeProperty('--blazor-media-progress');
          return 'aborted';
        }
        try {
          await writer.write(chunk);
        } catch (wErr) {
          if (controller?.signal.aborted) {
            try {
              await writer.abort();
            } catch {
              /* ignore */
            }
            return 'aborted';
          }
          return 'error';
        }
        written += chunk.byteLength;
        if (totalBytes) {
          const progress = Math.min(1, written / totalBytes);
          element.style.setProperty('--blazor-media-progress', progress.toString());
        }
      }

      if (controller?.signal.aborted) {
        try {
          await writer.abort();
        } catch {
          /* ignore */
        }
        element.style.removeProperty('--blazor-media-progress');
        return 'aborted';
      }

      try {
        await writer.close();
      } catch (closeErr) {
        if (controller?.signal.aborted) {
          return 'aborted';
        }
        return 'error';
      }
      return 'success';
    } catch (e) {
      try {
        await writer.abort();
      } catch {
        /* ignore */
      }
      return controller?.signal.aborted ? 'aborted' : 'error';
    } finally {
      element.style.removeProperty('--blazor-media-progress');
    }
  }

  private static triggerDownload(url: string, fileName: string): void {
    try {
      const a = document.createElement('a');
      a.href = url;
      a.download = fileName;
      a.style.display = 'none';
      document.body.appendChild(a);
      a.click();

      setTimeout(() => {
        try {
          a.remove();
          URL.revokeObjectURL(url);
        } catch {
          // ignore
        }
      }, 0);
    } catch {
      // ignore
    }
  }

  private static async getCache(): Promise<Cache | null> {
    if (!('caches' in window)) {
      this.logger.log(LogLevel.Warning, 'Cache API not supported in this browser');
      return null;
    }

    if (!this.cachePromise) {
      this.cachePromise = (async () => {
        try {
          return await caches.open(this.CACHE_NAME);
        } catch (error) {
          this.logger.log(LogLevel.Debug, `Failed to open cache: ${error}`);
          return null;
        }
      })();
    }

    const cache = await this.cachePromise;
    // If opening failed previously, allow retry next time
    if (!cache) {
      this.cachePromise = undefined;
    }
    return cache;
  }

  private static setupEventHandlers(
    element: HTMLElement,
    cacheKey: string | null = null
  ): void {
    const clearIfActive = () => {
      if (!cacheKey || BinaryMedia.activeCacheKey.get(element) === cacheKey) {
        BinaryMedia.loadingElements.delete(element);
        element.style.removeProperty('--blazor-media-progress');
      }
    };

    const onLoad = (_e: Event) => {
      clearIfActive();
    };

    const onError = (_e: Event) => {
      if (!cacheKey || BinaryMedia.activeCacheKey.get(element) === cacheKey) {
        BinaryMedia.loadingElements.delete(element);
        element.style.removeProperty('--blazor-media-progress');
        element.setAttribute('data-state', 'error');
      }
    };

    element.addEventListener('load', onLoad, { once: true });
    element.addEventListener('error', onError, { once: true });

    if (element instanceof HTMLVideoElement) {
      const onLoadedData = (_e: Event) => clearIfActive();
      element.addEventListener('loadeddata', onLoadedData, { once: true });
    }
  }

  private static async *iterateStream(stream: ReadableStream<Uint8Array>, signal?: AbortSignal): AsyncGenerator<Uint8Array, void, unknown> {
    const reader = stream.getReader();
    let finished = false;
    try {
      while (true) {
        if (signal?.aborted) {
          try {
            await reader.cancel();
          } catch {
            // ignore
          }
          return;
        }
        const { done, value } = await reader.read();
        if (done) {
          finished = true;
          return;
        }
        if (value) {
          yield value;
        }
      }
    } finally {
      if (!finished) {
        try {
          await reader.cancel();
        } catch {
          // ignore
        }
      }
      try {
        reader.releaseLock?.();
      } catch {
        // ignore
      }
    }
  }

  /**
   * Extracts the base MIME type from a MIME type that may contain codecs.
   * Examples: "video/mp4; codecs=\"avc1.64001E\"" -> "video/mp4"
   */
  private static extractBaseMimeType(mimeType: string): string {
    const semicolonIndex = mimeType.indexOf(';');
    return semicolonIndex !== -1 ? mimeType.substring(0, semicolonIndex).trim() : mimeType;
  }

  private static async tryMediaSourceVideoStreaming(
    element: HTMLVideoElement,
    streamRef: { stream: () => Promise<ReadableStream<Uint8Array>> },
    mimeType: string,
    cacheKey: string,
    totalBytes: number | null
  ): Promise<string | null> {
    try {
      if (!('MediaSource' in window) || !MediaSource.isTypeSupported(mimeType)) {
        return null;
      }
    } catch {
      return null;
    }

    this.loadingElements.add(element);
    const controller = new AbortController();
    this.controllers.set(element, controller);

    const mediaSource = new MediaSource();
    const objectUrl = URL.createObjectURL(mediaSource);

    this.setUrl(element, objectUrl, cacheKey, 'src');

    try {
      await new Promise<void>((resolve, reject) => {
        const onOpen = () => resolve();
        mediaSource.addEventListener('sourceopen', onOpen, { once: true });
        mediaSource.addEventListener('error', () => reject(new Error('MediaSource error event')), { once: true });
      });

      if (controller.signal.aborted) {
        return null;
      }

      const sourceBuffer: SourceBuffer = mediaSource.addSourceBuffer(mimeType);

      const originalStream = await streamRef.stream();
      let displayStream: ReadableStream<Uint8Array> = originalStream;

      if (cacheKey) {
        try {
          const cache = await this.getCache();
          if (cache) {
            const [display, cacheStream] = originalStream.tee();
            displayStream = display;
            cache.put(encodeURIComponent(cacheKey), new Response(cacheStream))
              .catch(err => this.logger.log(LogLevel.Debug, `Failed to put cache entry (MediaSource path): ${err}`));
          }
        } catch (cacheErr) {
          this.logger.log(LogLevel.Debug, `Cache setup failed (MediaSource path): ${cacheErr}`);
        }
      }

      let bytesRead = 0;

      for await (const chunk of this.iterateStream(displayStream, controller.signal)) {
        if (controller.signal.aborted) {
          break;
        }

        // Wait until sourceBuffer ready
        if (sourceBuffer.updating) {
          await new Promise<void>((resolve) => {
            const handler = () => resolve();
            sourceBuffer.addEventListener('updateend', handler, { once: true });
          });
          if (controller.signal.aborted) {
            break;
          }
        }

        try {
          const copy = new Uint8Array(chunk.byteLength);
          copy.set(chunk);
          sourceBuffer.appendBuffer(copy);
        } catch (appendErr) {
          this.logger.log(LogLevel.Debug, `SourceBuffer append failed: ${appendErr}`);
          try {
            mediaSource.endOfStream();
          } catch {
            // ignore
          }
          break;
        }

        bytesRead += chunk.byteLength;
        if (totalBytes) {
          const progress = Math.min(1, bytesRead / totalBytes);
          element.style.setProperty('--blazor-media-progress', progress.toString());
        }
      }

      if (controller.signal.aborted) {
        try {
          URL.revokeObjectURL(objectUrl);
        } catch {
          // ignore
        }
        return null;
      }

      // Wait for any pending update to finish before ending stream
      if (sourceBuffer.updating) {
        await new Promise<void>((resolve) => {
          const handler = () => resolve();
          sourceBuffer.addEventListener('updateend', handler, { once: true });
        });
      }
      try {
        mediaSource.endOfStream();
      } catch {
        // ignore
      }

      this.loadingElements.delete(element);
      element.style.removeProperty('--blazor-media-progress');

      return objectUrl;
    } catch (err) {
      try {
        URL.revokeObjectURL(objectUrl);
      } catch {
        // ignore
      }
      // Remove tracking so fallback can safely set a new URL
      this.revokeTrackedUrl(element);
      if (controller.signal.aborted) {
        return null;
      }
      return null;
    } finally {
      if (this.controllers.get(element) === controller) {
        this.controllers.delete(element);
      }
      if (controller.signal.aborted) {
        this.loadingElements.delete(element);
        element.style.removeProperty('--blazor-media-progress');
      }
    }
  }
}
