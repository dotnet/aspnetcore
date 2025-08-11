// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/**
 * Represents a pending chunked image transfer.
 */
interface ChunkedTransfer {
  imgElement: HTMLImageElement;
  receivedChunks: Uint8Array[];
  chunksReceived: number;
  bytesReceived: number;
  totalBytes: number | null;
  mimeType: string;
  cacheKey: string;
  cacheStrategy: string;
}

/**
 * Provides functionality for rendering binary image data in Blazor components.
 */
export class BinaryImageComponent {
  private static readonly CACHE_NAME = 'blazor-image-cache';

  private static readonly CACHE_PREFIX = 'https://blazor-images/';

  private static blobUrls: WeakMap<HTMLImageElement, string> = new WeakMap();

  private static loadingImages: Set<HTMLImageElement> = new Set();

  private static pendingTransfers: Map<string, ChunkedTransfer> = new Map();

  // Track the active transfer id for each element, to remove stale transfers
  private static elementActiveTransfer: WeakMap<HTMLImageElement, string> = new WeakMap();

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
   * Initializes a dynamic chunked image transfer.
   */
  public static initChunkedTransfer(
    imgElement: HTMLImageElement,
    transferId: string,
    mimeType: string,
    cacheKey: string,
    cacheStrategy: string,
    totalBytes: number | null = null
  ): boolean {
    // Cancel any previous transfer for this element
    const previousId = this.elementActiveTransfer.get(imgElement);
    if (previousId && this.pendingTransfers.has(previousId)) {
      this.pendingTransfers.delete(previousId);
      const prevContainer = imgElement.parentElement;
      if (prevContainer) {
        prevContainer.style.removeProperty('--blazor-image-progress');
      }
    }

    this.elementActiveTransfer.set(imgElement, transferId);

    console.log(`Initializing chunked transfer ${transferId}${totalBytes ? ` (${totalBytes} bytes)` : ''}`);

    this.pendingTransfers.set(transferId, {
      imgElement: imgElement,
      receivedChunks: [],
      chunksReceived: 0,
      bytesReceived: 0,
      totalBytes: totalBytes,
      mimeType: mimeType,
      cacheKey: cacheKey,
      cacheStrategy: cacheStrategy,
    });

    if (imgElement) {
      this.loadingImages.add(imgElement);
      imgElement.dispatchEvent(new CustomEvent('blazorImageLoading'));
    }

    // Initialize progress CSS variables
    const containerElement = imgElement.parentElement;
    if (containerElement && totalBytes !== null) {
      containerElement.style.setProperty('--blazor-image-progress', '0');
    }

    return true;
  }

  /**
   * Adds a chunk to an in-progress dynamic chunked transfer.
   */
  public static addChunk(
    transferId: string,
    chunkData: Uint8Array
  ): boolean {
    const transfer = this.pendingTransfers.get(transferId);
    if (!transfer) {
      console.error(`Transfer ${transferId} not found`);
      return false;
    }

    // Ignore stale transfer
    if (this.elementActiveTransfer.get(transfer.imgElement) !== transferId) {
      this.pendingTransfers.delete(transferId);
      return false;
    }

    const chunk = new Uint8Array(chunkData);
    transfer.receivedChunks.push(chunk);
    transfer.chunksReceived++;
    transfer.bytesReceived += chunk.length;

    if (transfer.totalBytes !== null && transfer.totalBytes > 0) {
      const progress = Math.min(1, transfer.bytesReceived / transfer.totalBytes);

      const containerElement = transfer.imgElement.parentElement;
      if (containerElement) {
        containerElement.style.setProperty('--blazor-image-progress', progress.toString());
      }
    }

    if (transfer.imgElement) {
      transfer.imgElement.dispatchEvent(new CustomEvent('blazorImageProgress', {
        detail: {
          chunksReceived: transfer.chunksReceived,
          bytesReceived: transfer.bytesReceived,
          totalBytes: transfer.totalBytes,
          percentage: transfer.totalBytes ? Math.round((transfer.bytesReceived / transfer.totalBytes) * 100) : null,
        },
      }));
    }

    return true;
  }

  /**
   * Finalizes a dynamic chunked transfer by combining all chunks and setting the image.
   */
  public static async finalizeChunkedTransfer(transferId: string): Promise<boolean> {
    const transfer = this.pendingTransfers.get(transferId);
    if (!transfer) {
      console.error(`Transfer ${transferId} not found`);
      return false;
    }

    // Abort if stale
    if (this.elementActiveTransfer.get(transfer.imgElement) !== transferId) {
      this.pendingTransfers.delete(transferId);
      return false;
    }

    try {
      // Combine all chunks into complete data
      const totalSize = transfer.totalBytes || transfer.receivedChunks.reduce((sum, chunk) => sum + chunk.length, 0);
      const completeData = new Uint8Array(totalSize);
      let offset = 0;
      for (const chunk of transfer.receivedChunks) {
        completeData.set(chunk, offset);
        offset += chunk.length;
      }

      if (!transfer.imgElement) {
        console.error(`Element not found in transfer ${transferId}`);
        return false;
      }

      // Create blob from data
      const blob = new Blob([completeData], { type: transfer.mimeType });

      // Cache the blob if requested
      if (transfer.cacheKey && transfer.cacheStrategy === 'memory') {
        await this.cacheBlob(transfer.cacheKey, blob, transfer.mimeType);
      }

      // Create object URL for display
      const url = URL.createObjectURL(blob);

      // Clean up old URL if exists
      const oldUrl = this.blobUrls.get(transfer.imgElement);
      if (oldUrl) {
        URL.revokeObjectURL(oldUrl);
      }

      this.blobUrls.set(transfer.imgElement, url);
      transfer.imgElement.src = url;

      // Set up event handlers
      transfer.imgElement.onload = () => {
        this.loadingImages.delete(transfer.imgElement);

        const containerElement = transfer.imgElement.parentElement;
        if (containerElement) {
          containerElement.style.removeProperty('--blazor-image-progress');
        }

        transfer.imgElement.dispatchEvent(new CustomEvent('blazorImageLoaded'));
      };

      transfer.imgElement.onerror = (e) => {
        this.loadingImages.delete(transfer.imgElement);

        const containerElement = transfer.imgElement.parentElement;
        if (containerElement) {
          containerElement.style.removeProperty('--blazor-image-progress');
        }

        transfer.imgElement.dispatchEvent(new CustomEvent('blazorImageError', {
          detail: (e as ErrorEvent).message || 'Failed to load image',
        }));
      };

      this.pendingTransfers.delete(transferId);

      console.log(`Finalized transfer ${transferId} for cache key: ${transfer.cacheKey}`);
      return true;
    } catch (error) {
      console.error(`Error finalizing chunked transfer: ${error}`);
      this.pendingTransfers.delete(transferId);

      if (transfer.imgElement) {
        this.loadingImages.delete(transfer.imgElement);

        const containerElement = transfer.imgElement.parentElement;
        if (containerElement) {
          containerElement.style.removeProperty('--blazor-image-progress');
        }

        transfer.imgElement.dispatchEvent(new CustomEvent('blazorImageError', {
          detail: (error as Error).message || 'Failed to process chunked image',
        }));
      }

      return false;
    }
  }

  /**
   * Caches a blob using the Cache API
   */
  private static async cacheBlob(cacheKey: string, blob: Blob, mimeType: string): Promise<void> {
    try {
      const cache = await this.getCache();
      if (!cache) {
        return;
      }

      const cacheUrl = this.getCacheUrl(cacheKey);
      const response = new Response(blob, {
        headers: {
          'Content-Type': mimeType,
          'Content-Length': blob.size.toString(),
          'Cache-Control': 'private, max-age=604800', // 7 days
        },
      });

      await cache.put(cacheUrl, response);
      console.log(`Cached blob for key: ${cacheKey}`);
    } catch (error) {
      console.error(`Failed to cache blob for key ${cacheKey}:`, error);
    }
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

      // Set up event handlers
      imgElement.onload = () => {
        this.loadingImages.delete(imgElement);
        imgElement.dispatchEvent(new CustomEvent('blazorImageLoaded'));
      };

      imgElement.onerror = (e) => {
        this.loadingImages.delete(imgElement);
        imgElement.dispatchEvent(new CustomEvent('blazorImageError', {
          detail: (e as ErrorEvent).message || 'Failed to load cached image',
        }));
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
   * Checks if an image is currently loading
   */
  public static isLoading(imgElement: HTMLImageElement): boolean {
    return imgElement ? this.loadingImages.has(imgElement) : false;
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

    // Clear pending transfers
    this.pendingTransfers.clear();

    // Clear loading state
    this.loadingImages.clear();

    console.log('Cleared all image component state');
    return true;
  }
}
