// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/**
 * Represents a pending chunked image transfer.
 */
interface ChunkedTransfer {
  elementId: string;
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
  private static blobUrls: Map<string, string> = new Map();

  private static memoryCache: Map<string, string> = new Map();

  private static loadingImages: Set<string> = new Set();

  private static pendingTransfers: Map<string, ChunkedTransfer> = new Map();

  /**
     * Initializes a dynamic chunked image transfer.
     * @param elementId - The ID of the target image element
     * @param transferId - A unique ID for this transfer
     * @param mimeType - The MIME type of the image
     * @param cacheKey - A unique key for caching
     * @param cacheStrategy - The caching strategy to use
     * @param totalBytes - The total number of bytes (null if unknown)
     * @returns True if initialization was successful
     */
  public static initChunkedTransfer(
    elementId: string,
    transferId: string,
    mimeType: string,
    cacheKey: string,
    cacheStrategy: string,
    totalBytes: number | null = null
  ): boolean {
    console.log(`Initializing dynamic chunked transfer ${transferId} for ${elementId}${totalBytes ? ` (${totalBytes} bytes)` : ''}`);

    this.pendingTransfers.set(transferId, {
      elementId: elementId,
      receivedChunks: [],
      chunksReceived: 0,
      bytesReceived: 0,
      totalBytes: totalBytes,
      mimeType: mimeType,
      cacheKey: cacheKey,
      cacheStrategy: cacheStrategy,
    });

    const imgElement = document.getElementById(elementId) as HTMLImageElement;
    if (imgElement) {
      this.loadingImages.add(elementId);
      imgElement.dispatchEvent(new CustomEvent('blazorImageLoading'));
    }

    // Initialize progress CSS variables
    const containerElement = document.getElementById(`${elementId}-container`);
    if (containerElement && totalBytes !== null) {
      containerElement.style.setProperty('--blazor-image-progress', '0');
    }

    return true;
  }

  /**
     * Adds a chunk to an in-progress dynamic chunked transfer.
     * @param transferId - The ID of the transfer
     * @param chunkIndex - The index of the chunk (for ordering, but chunks are appended dynamically)
     * @param chunkData - The binary data for this chunk
     * @returns True if the chunk was successfully added
     */
  public static addChunk(
    transferId: string,
    chunkIndex: number,
    chunkData: Uint8Array
  ): boolean {
    const transfer = this.pendingTransfers.get(transferId);
    if (!transfer) {
      console.error(`Transfer ${transferId} not found`);
      return false;
    }

    const chunk = new Uint8Array(chunkData);
    transfer.receivedChunks.push(chunk);
    transfer.chunksReceived++;
    transfer.bytesReceived += chunk.length;

    if (transfer.totalBytes !== null && transfer.totalBytes > 0) {
      const progress = Math.min(1, transfer.bytesReceived / transfer.totalBytes);

      const containerElement = document.getElementById(`${transfer.elementId}-container`);
      if (containerElement) {
        containerElement.style.setProperty('--blazor-image-progress', progress.toString());
      }
    }

    const imgElement = document.getElementById(transfer.elementId) as HTMLImageElement;
    if (imgElement) {
      imgElement.dispatchEvent(new CustomEvent('blazorImageProgress', {
        detail: {
          chunksReceived: transfer.chunksReceived,
          bytesReceived: transfer.bytesReceived,
          totalBytes: transfer.totalBytes,
          percentage: transfer.totalBytes ? Math.round((transfer.bytesReceived / transfer.totalBytes) * 100) : null,
          isDynamic: true,
        },
      }));
    }

    return true;
  }

  /**
     * Finalizes a dynamic chunked transfer by combining all chunks and setting the image.
     * @param transferId - The ID of the transfer to finalize
     * @param elementId - The ID of the target image element
     * @returns True if successfully finalized
     */
  public static finalizeChunkedTransfer(transferId: string, elementId: string): boolean {
    const transfer = this.pendingTransfers.get(transferId);
    if (!transfer) {
      console.error(`Transfer ${transferId} not found`);
      return false;
    }

    try {
      const totalSize = transfer.totalBytes || transfer.receivedChunks.reduce((sum, chunk) => sum + chunk.length, 0);
      const completeData = new Uint8Array(totalSize);
      let offset = 0;
      for (const chunk of transfer.receivedChunks) {
        completeData.set(chunk, offset);
        offset += chunk.length;
      }

      const imgElement = document.getElementById(elementId) as HTMLImageElement;
      if (!imgElement) {
        console.error(`Element ${elementId} not found`);
        return false;
      }

      let url: string | null = null;
      if (transfer.cacheKey && transfer.cacheStrategy === 'memory') {
        url = this.memoryCache.get(transfer.cacheKey) || null;
      }

      if (!url) {
        const blob = new Blob([completeData], { type: transfer.mimeType });
        url = URL.createObjectURL(blob);

        if (transfer.cacheKey && transfer.cacheStrategy === 'memory') {
          this.memoryCache.set(transfer.cacheKey, url);
        }

        console.log(`Created blob URL from dynamic chunked data for ${transfer.cacheKey}: ${url}`);
      }

      // Clean up old URL if exists
      if (this.blobUrls.has(elementId)) {
        const oldUrl = this.blobUrls.get(elementId);
        if (oldUrl) {
          const isCached = Array.from(this.memoryCache.values()).includes(oldUrl);
          if (!isCached) {
            URL.revokeObjectURL(oldUrl);
          }
        }
      }

      this.blobUrls.set(elementId, url);
      imgElement.src = url;

      // Set up event handlers
      imgElement.onload = () => {
        this.loadingImages.delete(elementId);

        const containerElement = document.getElementById(`${elementId}-container`);
        if (containerElement) {
          containerElement.style.removeProperty('--blazor-image-progress');
        }

        imgElement.dispatchEvent(new CustomEvent('blazorImageLoaded'));
      };

      imgElement.onerror = (e) => {
        this.loadingImages.delete(elementId);

        const containerElement = document.getElementById(`${elementId}-container`);
        if (containerElement) {
          containerElement.style.removeProperty('--blazor-image-progress');
        }

        imgElement.dispatchEvent(new CustomEvent('blazorImageError', {
          detail: (e as ErrorEvent).message || 'Failed to load image',
        }));
      };

      this.pendingTransfers.delete(transferId);

      return true;
    } catch (error) {
      console.error(`Error finalizing chunked transfer: ${error}`);
      this.pendingTransfers.delete(transferId);

      const imgElement = document.getElementById(elementId) as HTMLImageElement;
      if (imgElement) {
        this.loadingImages.delete(elementId);

        const containerElement = document.getElementById(`${elementId}-container`);
        if (containerElement) {
          containerElement.style.removeProperty('--blazor-image-progress');
        }

        imgElement.dispatchEvent(new CustomEvent('blazorImageError', {
          detail: (error as Error).message || 'Failed to process chunked image',
        }));
      }

      return false;
    }
  }

  /**
   * Checks if an image with the given cache key is already cached and sets it as the source.
   * @param elementId - The ID of the target image element
   * @param cacheKey - The cache key to look for
   * @returns True if the image was found in cache and set as source, false otherwise
   */
  public static trySetFromCache(elementId: string, cacheKey: string): boolean {
    if (!cacheKey) {
      return false;
    }

    const cachedUrl = this.memoryCache.get(cacheKey);
    if (!cachedUrl) {
      return false;
    }

    const imgElement = document.getElementById(elementId) as HTMLImageElement;
    if (!imgElement) {
      console.error(`Element ${elementId} not found`);
      return false;
    }

    console.log(`Setting image ${elementId} from cache with key: ${cacheKey}`);

    // Clean up old URL if exists
    if (this.blobUrls.has(elementId)) {
      const oldUrl = this.blobUrls.get(elementId);
      if (oldUrl) {
        const isCached = Array.from(this.memoryCache.values()).includes(oldUrl);
        if (!isCached) {
          URL.revokeObjectURL(oldUrl);
        }
      }
    }

    this.blobUrls.set(elementId, cachedUrl);
    imgElement.src = cachedUrl;

    // Set up event handlers
    imgElement.onload = () => {
      this.loadingImages.delete(elementId);
      imgElement.dispatchEvent(new CustomEvent('blazorImageLoaded'));
    };

    imgElement.onerror = (e) => {
      this.loadingImages.delete(elementId);
      imgElement.dispatchEvent(new CustomEvent('blazorImageError', {
        detail: (e as ErrorEvent).message || 'Failed to load cached image',
      }));
    };

    return true;
  }

  /**
   * Checks if an image is currently loading.
   * @param elementId - The ID of the image element
   * @returns True if loading, false otherwise
   */
  public static isLoading(elementId: string): boolean {
    return this.loadingImages.has(elementId);
  }

  /**
   * Revokes a specific blob URL.
   * @param elementId - The ID of the image element
   * @returns True if revoked, false if not found
   */
  public static revokeImageUrl(elementId: string): boolean {
    if (this.blobUrls.has(elementId)) {
      const url = this.blobUrls.get(elementId);

      if (url) {
        const isCached = Array.from(this.memoryCache.values()).includes(url);
        if (!isCached) {
          URL.revokeObjectURL(url);
          console.log(`Revoked blob URL for ${elementId}`);
        }
      }

      this.blobUrls.delete(elementId);
      this.loadingImages.delete(elementId);
      return true;
    }
    return false;
  }

  /**
   * Clears all blob URLs and cache.
   * @returns True if successful
   */
  public static clearCache(): boolean {
    // Revoke all blob URLs
    this.memoryCache.forEach(url => {
      URL.revokeObjectURL(url);
    });

    this.memoryCache.clear();
    console.log('Image cache cleared');

    return true;
  }

  /**
   * Cleans up everything.
   * @returns True if successful
   */
  public static clearAll(): boolean {
    this.clearCache();

    this.blobUrls.forEach(url => {
      URL.revokeObjectURL(url);
    });

    this.blobUrls.clear();
    this.loadingImages.clear();

    return true;
  }
}
