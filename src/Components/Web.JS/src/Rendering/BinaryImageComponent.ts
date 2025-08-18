// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Logger, LogLevel } from '../Platform/Logging/Logger';
import { ConsoleLogger } from '../Platform/Logging/Loggers';

/**
 * Provides functionality for rendering binary image data in Blazor components.
 */
export class BinaryImageComponent {
  private static readonly CACHE_NAME = 'blazor-image-cache';

  private static logger: Logger = new ConsoleLogger(LogLevel.Warning);

  private static blobUrls: WeakMap<HTMLImageElement, string> = new WeakMap();

  private static loadingImages: Set<HTMLImageElement> = new Set();

  private static activeCacheKey: WeakMap<HTMLImageElement, string> = new WeakMap();

  /**
   * Opens or creates the cache storage
   */
  private static async getCache(): Promise<Cache | null> {
    try {
      if (!('caches' in window)) {
        this.logger.log(LogLevel.Warning, 'Cache API not supported in this browser');
        return null;
      }
      return await caches.open(this.CACHE_NAME);
    } catch (error) {
      this.logger.log(LogLevel.Error, 'Failed to open cache');
      if (error instanceof Error) {
        this.logger.log(LogLevel.Error, error);
      } else {
        this.logger.log(LogLevel.Error, String(error));
      }
      return null;
    }
  }

  /**
   * Tries to set image from cache
   */
  public static async trySetFromCache(imgElement: HTMLImageElement, cacheKey: string): Promise<boolean> {
    if (!cacheKey || !imgElement) {
      this.logger.log(LogLevel.Warning, 'Invalid cache key or image element');
      this.logger.log(LogLevel.Debug, `Cache key: ${cacheKey}, Image element: ${imgElement}`);
      return false;
    }
    // this.logger.log(LogLevel.Debug, 'Using cache');


    try {
      const cache = await this.getCache();
      if (!cache) {
        return false;
      }

      const cacheUrl = encodeURIComponent(cacheKey);
      const cachedResponse = await cache.match(cacheUrl);

      if (!cachedResponse) {
        this.logger.log(LogLevel.Debug, `Cache miss for key: ${cacheKey}`);
        return false;
      }

      this.logger.log(LogLevel.Debug, `Cache hit for key: ${cacheKey}`);

      // Get blob from cached response
      const blob = await cachedResponse.blob();

      // Create object URL for display
      const url = URL.createObjectURL(blob);

      // Clean up old URL if exists
      const oldUrl = this.blobUrls.get(imgElement);
      if (oldUrl) {
        URL.revokeObjectURL(oldUrl);
      }

      this.blobUrls.set(imgElement, url);
      imgElement.src = url;

      imgElement.onload = () => {
        this.loadingImages.delete(imgElement);
      };

      imgElement.onerror = () => {
        this.loadingImages.delete(imgElement);
      };

      return true;
    } catch (error) {
      this.logger.log(LogLevel.Error, `Error loading from cache for key ${cacheKey}`);
      if (error instanceof Error) {
        this.logger.log(LogLevel.Error, error);
      } else {
        this.logger.log(LogLevel.Error, String(error));
      }
      return false;
    }
  }

  /**
   * Revokes the blob URL for an image element
   */
  public static revokeImageUrl(imgElement: HTMLImageElement): boolean {
    if (!imgElement) {
      return false;
    }

    const url = this.blobUrls.get(imgElement);
    if (url) {
      URL.revokeObjectURL(url);
      this.blobUrls.delete(imgElement);
      this.logger.log(LogLevel.Debug, 'Revoked blob URL for element');
    }

    this.loadingImages.delete(imgElement);
    return true;
  }

  /**
   * Clears the cache
   */
  public static async clearCache(): Promise<boolean> {
    try {
      const cache = await this.getCache();
      if (!cache) {
        return false;
      }

      // Get all cached requests
      const requests = await cache.keys();

      // Delete all cached entries
      await Promise.all(requests.map(request => cache.delete(request)));

      this.logger.log(LogLevel.Information, `Cleared ${requests.length} cached images`);
      return true;
    } catch (error) {
      this.logger.log(LogLevel.Error, 'Failed to clear cache');
      if (error instanceof Error) {
        this.logger.log(LogLevel.Error, error);
      } else {
        this.logger.log(LogLevel.Error, String(error));
      }
      return false;
    }
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

  /**
   * Loads an image from a stream reference, caching the result.
   */
  public static async loadImageFromStream(
    imgElement: HTMLImageElement,
    streamRef: { stream: () => Promise<ReadableStream<Uint8Array>> },
    mimeType: string,
    cacheKey: string,
    totalBytes: number | null
  ): Promise<boolean> {
    if (!imgElement || !streamRef) {
      this.logger.log(LogLevel.Warning, 'Invalid element or stream reference');
      return false;
    }
    // Record active cache key for stale detection
    this.activeCacheKey.set(imgElement, cacheKey);
    try {
      this.loadingImages.add(imgElement);

      const readable = await streamRef.stream();

      // Tee the original stream so one branch goes into Cache API as a streamed Response
      let displayStream: ReadableStream<Uint8Array> = readable;
      if (cacheKey) {
        try {
          const cache = await this.getCache();
          if (cache) {
            const [displayBranch, cacheBranch] = readable.tee();
            displayStream = displayBranch;
            const cacheResponse = new Response(cacheBranch);

            try {
              await cache.put(encodeURIComponent(cacheKey), cacheResponse);
            } catch (err) {
              this.logger.log(LogLevel.Error, 'Failed to cache streamed response');
              if (err instanceof Error) {
                this.logger.log(LogLevel.Error, err);
              } else {
                this.logger.log(LogLevel.Error, String(err));
              }
            }
          }
        } catch (err) {
          this.logger.log(LogLevel.Error, 'Error setting up stream caching');
          if (err instanceof Error) {
            this.logger.log(LogLevel.Error, err);
          } else {
            this.logger.log(LogLevel.Error, String(err));
          }
        }
      }

      let bytesRead = 0;
      const accumulatedChunks: Uint8Array[] = [];

      for await (const value of this.iterateStream(displayStream)) {
        if (this.activeCacheKey.get(imgElement) !== cacheKey) {
          return false;
        }
        if (value) {
          bytesRead += value.byteLength;
          accumulatedChunks.push(value);
          if (totalBytes) {
            const progress = Math.min(1, bytesRead / totalBytes);
            const containerElement = imgElement.parentElement;
            if (containerElement) {
              containerElement.style.setProperty('--blazor-image-progress', progress.toString());
            }
          }
        }
      }

      if (this.activeCacheKey.get(imgElement) !== cacheKey) {
        return false;
      }

      let combined: Uint8Array;
      if (accumulatedChunks.length === 1) {
        combined = accumulatedChunks[0];
      } else {
        const total = accumulatedChunks.reduce((s, c) => s + c.byteLength, 0);
        combined = new Uint8Array(total);
        let offset = 0;
        for (const c of accumulatedChunks) {
          combined.set(c, offset);
          offset += c.byteLength;
        }
      }

      const blob = new Blob([combined], { type: mimeType });
      if (this.activeCacheKey.get(imgElement) !== cacheKey) {
        return false;
      }

      const url = URL.createObjectURL(blob);
      const oldUrl = this.blobUrls.get(imgElement);
      if (oldUrl) {
        URL.revokeObjectURL(oldUrl);
      }
      this.blobUrls.set(imgElement, url);
      imgElement.src = url;

      imgElement.onload = () => {
        if (this.activeCacheKey.get(imgElement) === cacheKey) {
          this.loadingImages.delete(imgElement);
          const containerElement = imgElement.parentElement;
          if (containerElement) {
            containerElement.style.removeProperty('--blazor-image-progress');
          }
        }
      };

      imgElement.onerror = () => {
        if (this.activeCacheKey.get(imgElement) === cacheKey) {
          this.loadingImages.delete(imgElement);
          const containerElement = imgElement.parentElement;
          if (containerElement) {
            containerElement.style.removeProperty('--blazor-image-progress');
          }
        }
      };

      return true;
    } catch (error) {
      if (this.activeCacheKey.get(imgElement) === cacheKey) {
        this.logger.log(LogLevel.Error, 'Failed to load image from stream reference');
        if (error instanceof Error) {
          this.logger.log(LogLevel.Error, error);
        } else {
          this.logger.log(LogLevel.Error, String(error));
        }
        this.loadingImages.delete(imgElement);
      }
      return false;
    }
  }
}
