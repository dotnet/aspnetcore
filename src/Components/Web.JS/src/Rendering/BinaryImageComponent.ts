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
  private static blobUrls: Map<HTMLImageElement, string> = new Map();

  private static memoryCache: Map<string, string> = new Map();

  private static loadingImages: Set<HTMLImageElement> = new Set();

  private static pendingTransfers: Map<string, ChunkedTransfer> = new Map();

  /**
     * Initializes a dynamic chunked image transfer.
     * @param imgElement - The HTMLImageElement reference
     * @param transferId - A unique ID for this transfer
     * @param mimeType - The MIME type of the image
     * @param cacheKey - A unique key for caching
     * @param cacheStrategy - The caching strategy to use
     * @param totalBytes - The total number of bytes (null if unknown)
     * @returns True if initialization was successful
     */
  public static initChunkedTransfer(
    imgElement: HTMLImageElement,
    transferId: string,
    mimeType: string,
    cacheKey: string,
    cacheStrategy: string,
    totalBytes: number | null = null
  ): boolean {
    console.log(`Initializing dynamic chunked transfer ${transferId} for element${totalBytes ? ` (${totalBytes} bytes)` : ''}`);

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
     * @param transferId - The ID of the transfer
     * @param chunkData - The binary data for this chunk
     * @returns True if the chunk was successfully added
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
     * @param transferId - The ID of the transfer to finalize
     * @returns True if successfully finalized
     */
  public static finalizeChunkedTransfer(transferId: string): boolean {
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

      if (!transfer.imgElement) {
        console.error(`Element not found in transfer ${transferId}`);
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
      if (this.blobUrls.has(transfer.imgElement)) {
        const oldUrl = this.blobUrls.get(transfer.imgElement);
        if (oldUrl) {
          const isCached = Array.from(this.memoryCache.values()).includes(oldUrl);
          if (!isCached) {
            URL.revokeObjectURL(oldUrl);
          }
        }
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
   * Checks if an image with the given cache key is already cached and sets it as the source.
   * @param imgElement - The HTMLImageElement reference
   * @param cacheKey - The cache key to look for
   * @returns True if the image was found in cache and set as source, false otherwise
   */
  public static trySetFromCache(imgElement: HTMLImageElement, cacheKey: string): boolean {
    if (!cacheKey) {
      return false;
    }

    const cachedUrl = this.memoryCache.get(cacheKey);
    if (!cachedUrl) {
      return false;
    }

    if (!imgElement) {
      console.error('Element not provided');
      return false;
    }

    console.log(`Setting image from cache with key: ${cacheKey}`);

    // Clean up old URL if exists
    if (this.blobUrls.has(imgElement)) {
      const oldUrl = this.blobUrls.get(imgElement);
      if (oldUrl) {
        const isCached = Array.from(this.memoryCache.values()).includes(oldUrl);
        if (!isCached) {
          URL.revokeObjectURL(oldUrl);
        }
      }
    }

    this.blobUrls.set(imgElement, cachedUrl);
    imgElement.src = cachedUrl;

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
  }

  /**
   * Checks if an image is currently loading.
   * @param imgElement - The HTMLImageElement reference
   * @returns True if loading, false otherwise
   */
  public static isLoading(imgElement: HTMLImageElement): boolean {
    return imgElement ? this.loadingImages.has(imgElement) : false;
  }

  /**
   * Revokes a specific blob URL.
   * @param imgElement - The HTMLImageElement reference
   * @returns True if revoked, false if not found
   */
  public static revokeImageUrl(imgElement: HTMLImageElement): boolean {
    if (!imgElement) {
      return false;
    }

    if (this.blobUrls.has(imgElement)) {
      const url = this.blobUrls.get(imgElement);

      if (url) {
        const isCached = Array.from(this.memoryCache.values()).includes(url);
        if (!isCached) {
          URL.revokeObjectURL(url);
          console.log('Revoked blob URL for element');
        }
      }

      this.blobUrls.delete(imgElement);
      this.loadingImages.delete(imgElement);
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
