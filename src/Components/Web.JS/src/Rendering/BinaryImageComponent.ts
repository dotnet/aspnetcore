// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/**
 * Provides functionality for rendering binary image data in Blazor components.
 */
export class BinaryImageComponent {
  // Cache management
  private static blobUrls: Map<string, string> = new Map();

  private static memoryCache: Map<string, string> = new Map();

  private static loadingImages: Set<string> = new Set();

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
