// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/**
 * Provides functionality for rendering binary image data in Blazor components.
 */
export class BinaryImageComponent {
  private static readonly CACHE_NAME = 'blazor-image-cache';

  private static readonly CACHE_PREFIX = 'https://blazor-images/';

  private static blobUrls: WeakMap<HTMLImageElement, string> = new WeakMap();

  private static loadingImages: Set<HTMLImageElement> = new Set();

  private static activeCacheKey: WeakMap<HTMLImageElement, string> = new WeakMap();

  /**
   * Opens or creates the cache storage
   */
  private static async getCache(): Promise<Cache | null> {
    try {
      if (!('caches' in window)) {
        console.warn('Cache API not supported in this browser');
        return null;
      }
      return await caches.open(this.CACHE_NAME);
    } catch (error) {
      console.error('Failed to open cache:', error);
      return null;
    }
  }

  /**
   * Creates a cache URL from a cache key
   */
  private static getCacheUrl(cacheKey: string): string {
    return `${this.CACHE_PREFIX}${encodeURIComponent(cacheKey)}`;
  }

  /**
   * Tries to set image from cache
   */
  public static async trySetFromCache(imgElement: HTMLImageElement, cacheKey: string): Promise<boolean> {
    if (!cacheKey || !imgElement) {
      console.warn('Invalid cache key or image element');
      console.warn(`Cache key: ${cacheKey}, Image element: ${imgElement}`);
      return false;
    }

    try {
      const cache = await this.getCache();
      if (!cache) {
        console.warn('Cache not available');
        return false;
      }

      const cacheUrl = this.getCacheUrl(cacheKey);
      const cachedResponse = await cache.match(cacheUrl);

      if (!cachedResponse) {
        console.log(`Cache miss for key: ${cacheKey}`);
        return false;
      }

      console.log(`Cache hit for key: ${cacheKey}`);

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
      console.error(`Error loading from cache for key ${cacheKey}:`, error);
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
      console.log('Revoked blob URL for element');
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

      console.log(`Cleared ${requests.length} cached images`);
      return true;
    } catch (error) {
      console.error('Failed to clear cache:', error);
      return false;
    }
  }

  /**
   * Cleans up everything
   */
  public static async clearAll(): Promise<boolean> {
    // Clear cache
    await this.clearCache();

    // Clear loading state
    this.loadingImages.clear();

    console.log('Cleared all image component state');
    return true;
  }

  /**
   * Loads an image from a stream reference, caching the result.
   */
  public static async loadImageFromStream(
    imgElement: HTMLImageElement,
    streamRef: { stream: () => Promise<ReadableStream<Uint8Array>> },
    mimeType: string,
    cacheKey: string,
    cacheStrategy: string,
    totalBytes: number | null
  ): Promise<boolean> {
    if (!imgElement || !streamRef) {
      console.warn('Invalid element or stream reference');
      return false;
    }
    // Record active cache key for stale detection
    this.activeCacheKey.set(imgElement, cacheKey);
    try {
      this.loadingImages.add(imgElement);

      const readable = await streamRef.stream();

      // Tee the original stream so one branch goes into Cache API as a streamed Response
      let displayStream: ReadableStream<Uint8Array> = readable;
      if (cacheStrategy === 'memory' && cacheKey) {
        try {
          const cache = await this.getCache();
          if (cache) {
            const [displayBranch, cacheBranch] = readable.tee();
            displayStream = displayBranch;
            const cacheResponse = new Response(cacheBranch);

            try {
              await cache.put(this.getCacheUrl(cacheKey), cacheResponse);
            } catch (err) {
              console.error('Failed to cache streamed response', err);
            }
          }
        } catch (err) {
          console.error('Error setting up stream caching', err);
        }
      }

      let bytesRead = 0;
      const accumulatedChunks: Uint8Array[] = [];
      const reader = displayStream.getReader();

      for (;;) {
        if (this.activeCacheKey.get(imgElement) !== cacheKey) {
          try {
            reader.cancel();
          } catch {
            // ignore
          }
          return false;
        }
        const { done, value } = await reader.read();
        if (done) {
          break;
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
        console.error('Failed to load image from stream reference', error);
        this.loadingImages.delete(imgElement);
      }
      return false;
    }
  }
}
