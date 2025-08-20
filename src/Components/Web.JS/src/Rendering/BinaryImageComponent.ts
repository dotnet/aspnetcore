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

  private static observer: MutationObserver | null = null;

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
              const element = node as Element;

              if (element.tagName === 'IMG' && this.trackedImages.has(element as unknown as HTMLImageElement)) {
                this.revokeTrackedUrl(element as unknown as HTMLImageElement);
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

    this.observer.observe(document.body, {
      childList: true,
      subtree: true,
      attributes: true,
      attributeFilter: ['src'],
    });
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

    // Initialize global observer on first use
    this.initializeObserver();

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

    for await (const chunk of this.iterateStream(displayStream)) {
      if (this.activeCacheKey.get(imgElement) !== cacheKey) {
        return null;
      }

      chunks.push(chunk);
      bytesRead += chunk.byteLength;

      if (totalBytes) {
        const progress = Math.min(1, bytesRead / totalBytes);
        imgElement.style.setProperty('--blazor-image-progress', progress.toString());
      }
    }

    if (bytesRead === 0) {
      if (typeof totalBytes === 'number' && totalBytes > 0) {
        throw new Error('Stream was already consumed or at end position');
      }
      return null;
    }

    const combined = this.combineChunks(chunks);
    const blob = new Blob([combined], { type: mimeType });
    const url = URL.createObjectURL(blob);

    this.setImageUrl(imgElement, url, cacheKey);

    return url;
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
      imgElement.removeEventListener('error', onError);
    };

    const onError = (_e: Event) => {
      if (!cacheKey || BinaryImageComponent.activeCacheKey.get(imgElement) === cacheKey) {
        BinaryImageComponent.loadingImages.delete(imgElement);
        imgElement.style.removeProperty('--blazor-image-progress');
      }
      imgElement.removeEventListener('load', onLoad);
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
  private static async *iterateStream(stream: ReadableStream<Uint8Array>): AsyncGenerator<Uint8Array, void, unknown> {
    const reader = stream.getReader();
    try {
      while (true) {
        const { done, value } = await reader.read();
        if (done) {
          return;
        }
        if (value) {
          yield value;
        }
      }
    } finally {
      try {
        await reader.cancel();
      } catch {
        // ignore
      }
      try {
        reader.releaseLock?.();
      } catch {
        // ignore
      }
    }
  }
}
