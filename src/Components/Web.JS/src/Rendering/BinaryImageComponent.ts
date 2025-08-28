// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Logger, LogLevel } from '../Platform/Logging/Logger';
import { ConsoleLogger } from '../Platform/Logging/Loggers';

export interface ImageLoadResult {
  success: boolean;
  fromCache: boolean;
  objectUrl: string | null;
  error?: string;
}

/**
 * Provides functionality for rendering binary image data in Blazor components.
 */
export class BinaryImageComponent {
  private static readonly CACHE_NAME = 'blazor-image-cache';

  private static cachePromise?: Promise<Cache | null> = undefined;

  private static logger: Logger = new ConsoleLogger(LogLevel.Warning);

  private static loadingImages: Set<HTMLImageElement> = new Set();

  private static activeCacheKey: WeakMap<HTMLImageElement, string> = new WeakMap();

  private static trackedImages: WeakMap<HTMLImageElement, { url: string; cacheKey: string }> = new WeakMap();

  private static observersByParent: WeakMap<Element, MutationObserver> = new WeakMap();

  private static controllers: WeakMap<HTMLImageElement, AbortController> = new WeakMap();

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
              const element = node as Element;

              if (element.tagName === 'IMG' && this.trackedImages.has(element as HTMLImageElement)) {
                this.revokeTrackedUrl(element as HTMLImageElement);
              }

              // Any tracked descendants
              element.querySelectorAll('img').forEach((img) => {
                if (this.trackedImages.has(img as HTMLImageElement)) {
                  this.revokeTrackedUrl(img as HTMLImageElement);
                }
              });
            }
          }
        }

        // Handle src attribute changes on tracked images
        if (mutation.type === 'attributes' && (mutation as MutationRecord).attributeName === 'src') {
          const img = (mutation.target as Element) as HTMLImageElement;
          if (this.trackedImages.has(img)) {
            const tracked = this.trackedImages.get(img);
            if (tracked && img.src !== tracked.url) {
              this.revokeTrackedUrl(img);
            }
          }
        }
      }
    });

    observer.observe(parent, {
      childList: true,
      attributes: true,
      attributeFilter: ['src'],
    });

    this.observersByParent.set(parent, observer);
  }

  private static revokeTrackedUrl(img: HTMLImageElement): void {
    const tracked = this.trackedImages.get(img);
    if (tracked) {
      try {
        URL.revokeObjectURL(tracked.url);
      } catch {
        // ignore
      }
      this.trackedImages.delete(img);
      this.loadingImages.delete(img);
      this.activeCacheKey.delete(img);
    }

    // Abort any in-flight stream tied to this element
    const controller = this.controllers.get(img);
    if (controller) {
      try {
        controller.abort();
      } catch {
        // ignore
      }
      this.controllers.delete(img);
    }
  }

  /**
   * Single entry point for setting image - handles cache check and streaming
   */
  public static async setImageAsync(
    imgElement: HTMLImageElement,
    streamRef: { stream: () => Promise<ReadableStream<Uint8Array>> } | null,
    mimeType: string,
    cacheKey: string,
    totalBytes: number | null
  ): Promise<ImageLoadResult> {
    if (!imgElement || !cacheKey) {
      return { success: false, fromCache: false, objectUrl: null, error: 'Invalid parameters' };
    }

    // Ensure we are observing this image's parent
    const parent = imgElement.parentElement;
    if (parent) {
      this.initializeParentObserver(parent);
    }

    // If there was a previous different key for this element, abort its in-flight operation
    const previousKey = this.activeCacheKey.get(imgElement);
    if (previousKey && previousKey !== cacheKey) {
      const prevController = this.controllers.get(imgElement);
      if (prevController) {
        try {
          prevController.abort();
        } catch {
          // ignore
        }
        this.controllers.delete(imgElement);
      }
    }

    this.activeCacheKey.set(imgElement, cacheKey);

    try {
      // Try cache first
      try {
        const cache = await this.getCache();
        if (cache) {
          const cachedResponse = await cache.match(encodeURIComponent(cacheKey));
          if (cachedResponse) {
            const blob = await cachedResponse.blob();
            const url = URL.createObjectURL(blob);

            this.setImageUrl(imgElement, url, cacheKey);

            return { success: true, fromCache: true, objectUrl: url };
          }
        }
      } catch (err) {
        this.logger.log(LogLevel.Debug, `Cache lookup failed: ${err}`);
      }

      if (streamRef) {
        const url = await this.streamAndCreateUrl(imgElement, streamRef, mimeType, cacheKey, totalBytes);
        if (url) {
          return { success: true, fromCache: false, objectUrl: url };
        }
      }

      return { success: false, fromCache: false, objectUrl: null, error: 'No/empty stream provided and not in cache' };
    } catch (error) {
      this.logger.log(LogLevel.Debug, `Error in setImageAsync: ${error}`);
      return { success: false, fromCache: false, objectUrl: null, error: String(error) };
    }
  }

  private static setImageUrl(imgElement: HTMLImageElement, url: string, cacheKey: string): void {
    const tracked = this.trackedImages.get(imgElement);
    if (tracked) {
      try {
        URL.revokeObjectURL(tracked.url);
      } catch {
        // ignore
      }
    }

    this.trackedImages.set(imgElement, { url, cacheKey });

    imgElement.src = url;

    this.setupEventHandlers(imgElement, cacheKey);
  }

  private static async streamAndCreateUrl(
    imgElement: HTMLImageElement,
    streamRef: { stream: () => Promise<ReadableStream<Uint8Array>> },
    mimeType: string,
    cacheKey: string,
    totalBytes: number | null
  ): Promise<string | null> {
    this.loadingImages.add(imgElement);

    // Create and track an AbortController for this element
    const controller = new AbortController();
    this.controllers.set(imgElement, controller);

    const readable = await streamRef.stream();
    let displayStream = readable;

    if (cacheKey) {
      const cache = await this.getCache();
      if (cache) {
        const [display, cacheStream] = readable.tee();
        displayStream = display;

        cache.put(encodeURIComponent(cacheKey), new Response(cacheStream)).catch(err => {
          this.logger.log(LogLevel.Debug, `Failed to cache: ${err}`);
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
          imgElement.style.setProperty('--blazor-image-progress', progress.toString());
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
          this.setImageUrl(imgElement, url, cacheKey);
          resultUrl = url;
        }
      } else {
        resultUrl = null;
      }
    } finally {
      if (this.controllers.get(imgElement) === controller) {
        this.controllers.delete(imgElement);
      }
      this.loadingImages.delete(imgElement);
      imgElement.style.removeProperty('--blazor-image-progress');
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
    imgElement: HTMLImageElement,
    cacheKey: string | null = null
  ): void {
    const onLoad = (_e: Event) => {
      if (!cacheKey || BinaryImageComponent.activeCacheKey.get(imgElement) === cacheKey) {
        BinaryImageComponent.loadingImages.delete(imgElement);
        imgElement.style.removeProperty('--blazor-image-progress');
      }
    };

    const onError = (_e: Event) => {
      if (!cacheKey || BinaryImageComponent.activeCacheKey.get(imgElement) === cacheKey) {
        BinaryImageComponent.loadingImages.delete(imgElement);
        imgElement.style.removeProperty('--blazor-image-progress');
        imgElement.setAttribute('data-state', 'error');
      }
    };

    imgElement.addEventListener('load', onLoad, { once: true });
    imgElement.addEventListener('error', onError, { once: true });
  }

  /**
   * Opens or creates the cache storage
   */
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

  /**
   * Async iterator over a ReadableStream that ensures proper cancellation when iteration stops early.
   */
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
}
