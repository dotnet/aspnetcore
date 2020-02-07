import { toAbsoluteUri } from '../Services/NavigationManager';

export class WebAssemblyResourceLoader {
  private usedCacheKeys: { [key: string]: boolean } = {};
  private networkLoads: { [name: string]: LoadLogEntry } = {};
  private cacheLoads: { [name: string]: LoadLogEntry } = {};

  static async initAsync(): Promise<WebAssemblyResourceLoader> {
    const bootConfigResponse = await fetch('_framework/blazor.boot.json', {
      method: 'GET',
      credentials: 'include'
    });

    const relativeBaseHref = document.baseURI.substring(document.location.origin.length);
    const cacheName = `blazorresources:${relativeBaseHref}`;
    return new WebAssemblyResourceLoader(
      await bootConfigResponse.json(),
      await caches.open(cacheName));
  }

  constructor (public readonly bootConfig: BootJsonData, private cache: Cache)
  {
  }

  loadResources(resources: ResourceList, url: (name: string) => string): LoadingResource[] {
    return Object.keys(resources)
      .map(name => this.loadResource(name, url(name), resources[name]));
  }

  loadResource(name: string, url: string, contentHash: string): LoadingResource {
    const networkFetchOptions: RequestInit = { cache: 'no-cache' };
    if (!this.bootConfig.cacheBootResources) {
      // Bypass the entire cache flow, including checking hashes, etc.
      // This gives developers an easy opt-out if they don't like anything about the default cache mechanism
      const response = fetch(url, networkFetchOptions);
      const data = response.then(r => r.clone().arrayBuffer());
      return { name, url, response, data };
    }

    if (!contentHash || contentHash.length === 0) {
      throw new Error('Content hash is required');
    }

    const cacheKey = toAbsoluteUri(`${url}.${contentHash}`);
    this.usedCacheKeys[cacheKey] = true;

    const responseInfoPromise = (async () => {
      // Try to load from cache
      const cachedResponse = await this.cache.match(cacheKey);
      if (cachedResponse) {
        const responseBytes = parseInt(cachedResponse.headers.get('content-length') || '0');
        this.cacheLoads[name] = { responseBytes };
        return { response: cachedResponse, isNetworkResponse: false };
      }

      // It's not cached (or caching is now disabled), so fetch from network
      const networkResponse = await fetch(url, networkFetchOptions);
      return { response: networkResponse, isNetworkResponse: true };
    })();

    // We add to the cache as an independent background task, without chaining it onto the 'response' promise.
    // This is because for WebAssembly.instantiateStreaming to make sense, we have to give it the
    // response object before the body has been fully fetched from the network, which in turn implies
    // we can't validate the content hash as part of that promise chain.
    const response = responseInfoPromise.then(responseInfo => responseInfo.response.clone());
    const data = responseInfoPromise.then(async responseInfo =>
      responseInfo.isNetworkResponse
        ? this.tryValidateAndCacheAsync(name, cacheKey, contentHash, responseInfo.response)
        : responseInfo.response.arrayBuffer() // For cached responses, we already validated before caching
    );
    return { name, url, response, data };
  }

  logToConsole() {
    const cacheLoadsEntries = Object.values(this.cacheLoads);
    const networkLoadsEntries = Object.values(this.networkLoads);
    const cacheResponseBytes = countTotalBytes(cacheLoadsEntries);
    const networkResponseBytes = countTotalBytes(networkLoadsEntries);
    const totalResponseBytes = cacheResponseBytes + networkResponseBytes;
    const linkerDisabledWarning = this.bootConfig.linkerEnabled ? '%c' : '\n%cThis application was built with linking (tree shaking) disabled. Published applications will be significantly smaller.';

    console.groupCollapsed(`%cblazor%c Loaded ${toDataSizeString(totalResponseBytes)} resources${linkerDisabledWarning}`, 'background: purple; color: white; padding: 1px 3px; border-radius: 3px;', 'font-weight: bold;', 'font-weight: normal;');

    if (cacheLoadsEntries.length) {
      console.groupCollapsed(`Loaded ${toDataSizeString(cacheResponseBytes)} resources from cache`);
      console.table(this.cacheLoads);
      console.groupEnd();
    }

    if (networkLoadsEntries.length) {
      console.groupCollapsed(`Loaded ${toDataSizeString(networkResponseBytes)} resources from network`);
      console.table(this.networkLoads);
      console.groupEnd();
    }

    console.groupEnd();
  }

  async purgeUnusedCacheEntriesAsync() {
    // We want to keep the cache small because, even though the browser will evict entries if it
    // gets too big, we don't want to be considered problematic by the end user viewing storage stats
    const cachedRequests = await this.cache.keys();
    const deletionPromises = cachedRequests.map(async cachedRequest => {
      if (!(cachedRequest.url in this.usedCacheKeys)) {
        await this.cache.delete(cachedRequest);
      }
    });

    return Promise.all(deletionPromises);
  }

  private async tryValidateAndCacheAsync(name: string, cacheKey: string, expectedContentHash: string, networkResponse: Response): Promise<ArrayBuffer> {
    const responseBuffer = await networkResponse.arrayBuffer();

    // Now is an ideal moment to capture the performance stats for the request, since it
    // only just completed and is most likely to still be in the buffer. However this is
    // only done on a 'best effort' basis. Even if we do receive an entry, some of its
    // properties may be blanked out if it was a CORS request.
    const performanceEntry = getPerformanceEntry(networkResponse.url);
    const responseBytes = (performanceEntry && performanceEntry.encodedBodySize) || undefined;
    this.networkLoads[name] = { responseBytes };

    // crypto.subtle is only enabled on localhost and HTTPS origins
    // We only write to the cache if we can validate the content hashes
    if (typeof crypto !== 'undefined' && !!crypto.subtle) {
      await assertContentHashMatchesAsync(name, responseBuffer, expectedContentHash);

      // Build a custom response object so we can track extra data such as responseBytes
      // We can't rely on the server sending content-length (ASP.NET Core doesn't by default)
      await this.cache.put(cacheKey, new Response(responseBuffer, {
        headers: {
          'content-type': networkResponse.headers.get('content-type') || '',
          'content-length': (responseBytes || networkResponse.headers.get('content-length') || '').toString()
        }
      }));
    }

    return responseBuffer;
  }
}

function countTotalBytes(loads: LoadLogEntry[]) {
  return loads.reduce((prev, item) => prev + (item.responseBytes || 0), 0);
}

function toDataSizeString(byteCount: number) {
  return `${(byteCount / (1024*1024)).toFixed(2)} MB`;
}

function getPerformanceEntry(url: string): PerformanceResourceTiming | undefined {
  if (typeof performance !== 'undefined') {
    return performance.getEntriesByName(url)[0] as PerformanceResourceTiming;
  }
}

async function assertContentHashMatchesAsync(name: string, data: ArrayBuffer, expectedHashPrefix: string) {
  const actualHashBuffer = await crypto.subtle.digest('SHA-256', data);
  const actualHash = new Uint8Array(actualHashBuffer);
  for (var byteIndex = 0; byteIndex*2 < expectedHashPrefix.length; byteIndex++) {
    const expectedByte = parseInt(expectedHashPrefix.substr(byteIndex * 2, 2), 16);
    const actualByte = actualHash[byteIndex];
    if (actualByte !== expectedByte) {
      const actualHashString = Array.from(actualHash).map(b => b.toString(16).padStart(2, '0')).join('');
      throw new Error(`Resource hash mismatch for '${name}'. Expected prefix: '${expectedHashPrefix}'. Actual hash: '${actualHashString}'`);
    }
  }
}

// Keep in sync with bootJsonData in Microsoft.AspNetCore.Blazor.Build
interface BootJsonData {
  readonly entryAssembly: string;
  readonly resources: ResourceGroups;
  readonly debugBuild: boolean;
  readonly linkerEnabled: boolean;
  readonly cacheBootResources: boolean;
}

interface ResourceGroups {
  readonly wasm: ResourceList;
  readonly assembly: ResourceList;
  readonly pdb?: ResourceList;
}

interface LoadLogEntry {
  responseBytes: number | undefined;
}

export interface LoadingResource {
  name: string;
  url: string;
  response: Promise<Response>;
  data: Promise<ArrayBuffer>;
}

type ResourceList = { [name: string]: string };
