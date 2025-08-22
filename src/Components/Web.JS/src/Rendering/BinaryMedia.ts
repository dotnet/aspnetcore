// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Logger, LogLevel } from '../Platform/Logging/Logger';
import { ConsoleLogger } from '../Platform/Logging/Loggers';

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

  private static observer: MutationObserver | null = null;

  private static controllers: WeakMap<HTMLElement, AbortController> = new WeakMap();

  private static initializeObserver(): void {
    if (this.observer) {
      return;
    }

    this.observer = new MutationObserver((mutations) => {
      for (const mutation of mutations) {
        // Handle removed nodes
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

        // Handle attribute changes on tracked elements
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

    this.observer.observe(document.body, {
      childList: true,
      subtree: true,
      attributes: true,
      attributeFilter: ['src', 'href'],
    });
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

    this.initializeObserver();

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

    if (targetAttr === 'src') {
      (element as HTMLImageElement | HTMLVideoElement).src = url;
    } else {
      (element as HTMLAnchorElement).href = url;
    }

    this.setupEventHandlers(element, cacheKey);
  }

  private static async streamAndCreateUrl(
    element: HTMLElement,
    streamRef: { stream: () => Promise<ReadableStream<Uint8Array>> },
    mimeType: string,
    cacheKey: string,
    totalBytes: number | null,
    targetAttr: 'src' | 'href'
  ): Promise<string | null> {
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

    const chunks: Uint8Array[] = [];
    let bytesRead = 0;
    let aborted = false;
    let resultUrl: string | null = null;

    try {
      for await (const chunk of this.iterateStream(displayStream, controller.signal)) {
        if (controller.signal.aborted) { // Stream aborted due to a new setImageAsync call with a key change
          aborted = true;
          break;
        }
        chunks.push(chunk);
        bytesRead += chunk.byteLength;

        if (totalBytes) {
          const progress = Math.min(1, bytesRead / totalBytes);
          element.style.setProperty('--blazor-media-progress', progress.toString());
        }
      }

      if (!aborted) {
        if (bytesRead === 0) {
          if (typeof totalBytes === 'number' && totalBytes > 0) {
            throw new Error('Stream was already consumed or at end position');
          }
          resultUrl = null;
        } else {
          const combined = this.combineChunks(chunks);
          const blob = new Blob([combined], { type: mimeType });
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

  private static setupEventHandlers(
    element: HTMLElement,
    cacheKey: string | null = null
  ): void {
    const onLoad = (_e: Event) => {
      if (!cacheKey || BinaryMedia.activeCacheKey.get(element) === cacheKey) {
        BinaryMedia.loadingElements.delete(element);
        element.style.removeProperty('--blazor-media-progress');
      }
      element.removeEventListener('error', onError);
    };

    const onError = (_e: Event) => {
      if (!cacheKey || BinaryMedia.activeCacheKey.get(element) === cacheKey) {
        BinaryMedia.loadingElements.delete(element);
        element.style.removeProperty('--blazor-media-progress');
      }
      element.removeEventListener('load', onLoad);
    };

    element.addEventListener('load', onLoad, { once: true });
    element.addEventListener('error', onError, { once: true });
  }

  // Added: trigger a download using BinaryMedia pipeline (formerly BinaryFileDownload)
  public static async downloadAsync(
    element: HTMLElement,
    streamRef: { stream: () => Promise<ReadableStream<Uint8Array>> } | null,
    mimeType: string,
    cacheKey: string,
    totalBytes: number | null,
    fileName?: string | null,
    attemptNativePicker = true,
  ): Promise<MediaLoadResult> {
    if (!element || !cacheKey) {
      return { success: false, fromCache: false, objectUrl: null, error: 'Invalid parameters' };
    }

    const nativePickerAvailable = attemptNativePicker && typeof (window as unknown as { showSaveFilePicker?: unknown }).showSaveFilePicker === 'function';

    // Try cache first
    let cacheHitBlob: Blob | null = null;
    let _fromCache = false;
    try {
      const cache = await this.getCache();
      if (cache) {
        const cached = await cache.match(encodeURIComponent(cacheKey));
        if (cached) {
          cacheHitBlob = await cached.blob();
          _fromCache = true;
        }
      }
    } catch (err) {
      this.logger.log(LogLevel.Debug, `Cache lookup failed (download): ${err}`);
    }

    // If we have a cache hit and native picker is available, stream blob to file directly
    if (cacheHitBlob && nativePickerAvailable) {
      try {
        const handle = await (window as unknown as { showSaveFilePicker: (opts: any) => Promise<any> }).showSaveFilePicker({ suggestedName: fileName || cacheKey }); // eslint-disable-line @typescript-eslint/no-explicit-any
        const writer = await handle.createWritable();
        const stream = cacheHitBlob.stream();
        const result = await this.writeStreamToFile(element, stream as ReadableStream<Uint8Array>, writer, totalBytes);
        if (result === 'success') {
          return { success: true, fromCache: true, objectUrl: null };
        }
        // aborted treated as failure
        return { success: false, fromCache: true, objectUrl: null, error: 'Aborted' };
      } catch (pickerErr) {
        // User might have cancelled; fall back to anchor download if we still have blob
        this.logger.log(LogLevel.Debug, `Native picker path failed or cancelled: ${pickerErr}`);
      }
    }

    if (cacheHitBlob && !nativePickerAvailable) {
      // Fallback anchor path using cached blob
      const url = URL.createObjectURL(cacheHitBlob);
      this.triggerDownload(url, (fileName || cacheKey));
      return { success: true, fromCache: true, objectUrl: url };
    }

    if (!streamRef) {
      return { success: false, fromCache: false, objectUrl: null, error: 'No stream provided' };
    }

    // Stream and optionally cache (dup logic from streamAndCreateUrl, without setting element attributes)
    this.loadingElements.add(element);
    const controller = new AbortController();
    this.controllers.set(element, controller);

    try {
      const readable = await streamRef.stream();

      // If native picker available, we can stream directly to file, optionally tee for cache
      if (nativePickerAvailable) {
        try {
          const handle = await (window as unknown as { showSaveFilePicker: (opts: any) => Promise<any> }).showSaveFilePicker({ suggestedName: fileName || cacheKey }); // eslint-disable-line @typescript-eslint/no-explicit-any
          const writer = await handle.createWritable();

          let workingStream: ReadableStream<Uint8Array> = readable;
          let cacheStream: ReadableStream<Uint8Array> | null = null;
          if (cacheKey) {
            const cache = await this.getCache();
            if (cache) {
              const tees = readable.tee();
              workingStream = tees[0];
              cacheStream = tees[1];
              cache.put(encodeURIComponent(cacheKey), new Response(cacheStream)).catch(err => {
                this.logger.log(LogLevel.Debug, `Failed to put cache entry (download/native): ${err}`);
              });
            }
          }

          const writeResult = await this.writeStreamToFile(element, workingStream, writer, totalBytes, controller);
          if (writeResult === 'success') {
            return { success: true, fromCache: false, objectUrl: null };
          }
          if (writeResult === 'aborted') {
            return { success: false, fromCache: false, objectUrl: null, error: 'Aborted' };
          }
        } catch (pickerErr) {
          this.logger.log(LogLevel.Debug, `Native picker streaming path failed or cancelled after selection: ${pickerErr}`);
          // Fall through to in-memory blob fallback
        }
      }

      // In-memory path (existing logic)
      let displayStream: ReadableStream<Uint8Array> = readable;
      if (cacheKey) {
        const cache = await this.getCache();
        if (cache) {
          const [display, cacheStream] = readable.tee();
          displayStream = display;
          cache.put(encodeURIComponent(cacheKey), new Response(cacheStream)).catch(err => {
            this.logger.log(LogLevel.Debug, `Failed to put cache entry (download): ${err}`);
          });
        }
      }

      const chunks: Uint8Array[] = [];
      let bytesRead = 0;
      for await (const chunk of this.iterateStream(displayStream, controller.signal)) {
        if (controller.signal.aborted) {
          return { success: false, fromCache: false, objectUrl: null, error: 'Aborted' };
        }
        chunks.push(chunk);
        bytesRead += chunk.byteLength;
        if (totalBytes) {
          const progress = Math.min(1, bytesRead / totalBytes);
          element.style.setProperty('--blazor-media-progress', progress.toString());
        }
      }

      if (bytesRead === 0) {
        return { success: false, fromCache: false, objectUrl: null, error: 'Empty stream' };
      }

      const combined = this.combineChunks(chunks);
      const blob = new Blob([combined], { type: mimeType });
      const url = URL.createObjectURL(blob);
      this.triggerDownload(url, fileName || cacheKey);
      return { success: true, fromCache: false, objectUrl: url };
    } catch (error) {
      this.logger.log(LogLevel.Debug, `Error in downloadAsync: ${error}`);
      return { success: false, fromCache: false, objectUrl: null, error: String(error) };
    } finally {
      if (this.controllers.get(element) === controller) {
        this.controllers.delete(element);
      }
      this.loadingElements.delete(element);
      element.style.removeProperty('--blazor-media-progress');
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
          document.body.removeChild(a);
        } catch {
          // ignore
        }
      }, 0);
    } catch {
      // ignore
    }
  }

  // Helper to stream data directly to a FileSystemWritableFileStream with progress & abort handling
  private static async writeStreamToFile(
    element: HTMLElement,
    stream: ReadableStream<Uint8Array>,
    writer: any, // eslint-disable-line @typescript-eslint/no-explicit-any
    totalBytes: number | null,
    controller?: AbortController
  ): Promise<'success' | 'aborted' | 'error'> {
    const reader = stream.getReader();
    let written = 0;
    try {
      for (;;) {
        if (controller?.signal.aborted) {
          try {
            await writer.abort();
          } catch {
            /* ignore */
          }
          element.style.removeProperty('--blazor-media-progress');
          return 'aborted';
        }
        const { done, value } = await reader.read();
        if (done) {
          break;
        }
        if (value) {
          await writer.write(value);
          written += value.byteLength;
          if (totalBytes) {
            const progress = Math.min(1, written / totalBytes);
            element.style.setProperty('--blazor-media-progress', progress.toString());
          }
        }
      }
      await writer.close();
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
      try {
        reader.releaseLock?.();
      } catch {
        /* ignore */
      }
    }
  }
}
