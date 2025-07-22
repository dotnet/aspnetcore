// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/**
 * Represents a pending chunked image transfer.
 */
interface ChunkedTransfer {
  elementId: string;
  receivedChunks: Uint8Array[];
  chunksReceived: number;
  totalChunks: number;
  totalSize: number;
  mimeType: string;
  cacheKey: string;
  cacheStrategy: string;
}

/**
 * Provides functionality for rendering binary image data in Blazor components.
 */
export class BinaryImageComponent {
  // Cache management
  private static blobUrls: Map<string, string> = new Map();

  private static memoryCache: Map<string, string> = new Map();

  private static loadingImages: Set<string> = new Set();

  private static pendingTransfers: Map<string, ChunkedTransfer> = new Map();

  /**
   * Creates an image from binary data with optional caching.
   * @param elementId - The ID of the target image element
   * @param imageBytes - The binary image data
   * @param mimeType - The MIME type of the image
   * @param cacheKey - A unique key for caching
   * @param cacheStrategy - The caching strategy to use ("none", "memory", "persistent")
   * @returns True if successful, false otherwise
   */
  public static createImageFromBytes(
    elementId: string,
    imageBytes: Uint8Array,
    mimeType: string,
    cacheKey: string,
    cacheStrategy = 'memory'
  ): boolean {
    console.log(`Creating image for ${elementId} with cacheKey: ${cacheKey}`);
    const imgElement = document.getElementById(elementId) as HTMLImageElement;
    if (!imgElement) {
      return false;
    }

    // Set loading state
    this.loadingImages.add(elementId);
    imgElement.dispatchEvent(new CustomEvent('blazorImageLoading'));

    try {
      // Check if we have this image cached by key
      let url: string | null = null;
      if (cacheKey && cacheStrategy === 'memory') {
        url = this.memoryCache.get(cacheKey) || null;
      }

      // If not in cache, create new Blob URL
      if (!url) {
        // Convert from byte array to Blob
        const blob = new Blob([imageBytes], { type: mimeType });
        url = URL.createObjectURL(blob);

        // Store in cache if we have a key
        if (cacheKey && cacheStrategy === 'memory') {
          this.memoryCache.set(cacheKey, url);
        }

        console.log(`Created blob URL for ${cacheKey}: ${url}`);
      } else {
        console.log(`Using cached blob URL for ${cacheKey}`);
      }

      // Store URL reference for this element (for cleanup)
      if (this.blobUrls.has(elementId)) {
        // If this element already had an image, revoke the old URL if not cached
        const oldUrl = this.blobUrls.get(elementId);
        if (oldUrl) {
          const isCached = Array.from(this.memoryCache.values()).includes(oldUrl);
          if (!isCached) {
            URL.revokeObjectURL(oldUrl);
            console.log(`Revoked old blob URL for ${elementId}`);
          }
        }
      }

      // Update the element with new URL
      this.blobUrls.set(elementId, url);
      imgElement.src = url;

      // Handle load and error events
      imgElement.onload = () => {
        this.loadingImages.delete(elementId);
        imgElement.dispatchEvent(new CustomEvent('blazorImageLoaded'));
      };

      imgElement.onerror = (e) => {
        this.loadingImages.delete(elementId);
        imgElement.dispatchEvent(new CustomEvent('blazorImageError', {
          detail: (e as ErrorEvent).message || 'Failed to load image',
        }));
      };

      return true;
    } catch (error) {
      console.error(`Error creating image from bytes: ${error}`);
      this.loadingImages.delete(elementId);
      imgElement.dispatchEvent(new CustomEvent('blazorImageError', {
        detail: (error as Error).message,
      }));
      return false;
    }
  }

  /**
     * Initializes a chunked image transfer.
     * @param elementId - The ID of the target image element
     * @param transferId - A unique ID for this transfer
     * @param totalChunks - The total number of chunks expected
     * @param totalSize - The total size of the complete image in bytes
     * @param mimeType - The MIME type of the image
     * @param cacheKey - A unique key for caching
     * @param cacheStrategy - The caching strategy to use
     * @returns True if initialization was successful
     */
  public static initChunkedTransfer(
    elementId: string,
    transferId: string,
    totalChunks: number,
    totalSize: number,
    mimeType: string,
    cacheKey: string,
    cacheStrategy: string
  ): boolean {
    console.log(`Initializing chunked transfer ${transferId} for ${elementId} with ${totalChunks} chunks (${totalSize} bytes)`);

    // Create transfer state
    this.pendingTransfers.set(transferId, {
      elementId: elementId,
      receivedChunks: new Array(totalChunks),
      chunksReceived: 0,
      totalChunks: totalChunks,
      totalSize: totalSize,
      mimeType: mimeType,
      cacheKey: cacheKey,
      cacheStrategy: cacheStrategy,
    });

    // Set loading state
    const imgElement = document.getElementById(elementId) as HTMLImageElement;
    if (imgElement) {
      this.loadingImages.add(elementId);
      imgElement.dispatchEvent(new CustomEvent('blazorImageLoading'));
    }

    return true;
  }

  /**
     * Adds a chunk to an in-progress chunked transfer.
     * @param transferId - The ID of the transfer
     * @param chunkIndex - The index of the chunk
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

    // Store chunk
    transfer.receivedChunks[chunkIndex] = new Uint8Array(chunkData);
    transfer.chunksReceived++;

    // Calculate and dispatch progress
    const progress = transfer.chunksReceived / transfer.totalChunks;
    const imgElement = document.getElementById(transfer.elementId) as HTMLImageElement;
    if (imgElement) {
      imgElement.dispatchEvent(new CustomEvent('blazorImageProgress', {
        detail: { progress: progress },
      }));
    }

    return true;
  }

  /**
     * Finalizes a chunked transfer by combining all chunks and setting the image.
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

    // Ensure all chunks were received
    if (transfer.chunksReceived !== transfer.totalChunks) {
      console.error(`Not all chunks received for ${transferId}: ${transfer.chunksReceived}/${transfer.totalChunks}`);
      return false;
    }

    try {
      // Concatenate all chunks
      let offset = 0;
      const completeData = new Uint8Array(transfer.totalSize);
      for (let i = 0; i < transfer.totalChunks; i++) {
        const chunk = transfer.receivedChunks[i];
        completeData.set(chunk, offset);
        offset += chunk.length;
      }

      // Create image from complete data
      const imgElement = document.getElementById(elementId) as HTMLImageElement;
      if (!imgElement) {
        console.error(`Element ${elementId} not found`);
        return false;
      }

      // Check if we have this image cached by key
      let url: string | null = null;
      if (transfer.cacheKey && transfer.cacheStrategy === 'memory') {
        url = this.memoryCache.get(transfer.cacheKey) || null;
      }

      // If not in cache, create new Blob URL
      if (!url) {
        const blob = new Blob([completeData], { type: transfer.mimeType });
        url = URL.createObjectURL(blob);

        // Store in cache if we have a key
        if (transfer.cacheKey && transfer.cacheStrategy === 'memory') {
          this.memoryCache.set(transfer.cacheKey, url);
        }

        console.log(`Created blob URL from chunked data for ${transfer.cacheKey}: ${url}`);
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

      // Update the element with new URL
      this.blobUrls.set(elementId, url);
      imgElement.src = url;

      // Set up event handlers
      imgElement.onload = () => {
        this.loadingImages.delete(elementId);
        imgElement.dispatchEvent(new CustomEvent('blazorImageLoaded'));
      };

      imgElement.onerror = (e) => {
        this.loadingImages.delete(elementId);
        imgElement.dispatchEvent(new CustomEvent('blazorImageError', {
          detail: (e as ErrorEvent).message || 'Failed to load image',
        }));
      };

      // Clean up transfer data
      this.pendingTransfers.delete(transferId);

      return true;
    } catch (error) {
      console.error(`Error finalizing chunked transfer: ${error}`);
      this.pendingTransfers.delete(transferId);

      const imgElement = document.getElementById(elementId) as HTMLImageElement;
      if (imgElement) {
        this.loadingImages.delete(elementId);
        imgElement.dispatchEvent(new CustomEvent('blazorImageError', {
          detail: (error as Error).message || 'Failed to process chunked image',
        }));
      }

      return false;
    }
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

    this.blobUrls.forEach(url => {
      URL.revokeObjectURL(url);
    });

    this.memoryCache.clear();
    this.blobUrls.clear();
    this.loadingImages.clear();
    console.log('Image cache cleared');

    return true;
  }

  /**
   * Cleans up everything.
   * @returns True if successful
   */
  public static clearAll(): boolean {
    return this.clearCache();
  }
}
