import { toAbsoluteUri } from "../Services/NavigationManager";

export class WebAssemblyResourceLoader {
  private usedCacheKeys: { [key: string]: boolean } = {};
  private networkLoads: { [name: string]: LoadLogEntry } = {};
  private cacheLoads: { [name: string]: LoadLogEntry } = {};

  static async initAsync(): Promise<WebAssemblyResourceLoader> {
    const bootConfigResponse = await fetch('_framework/blazor.boot.json', {
      method: 'GET',
      credentials: 'include'
    });

    return new WebAssemblyResourceLoader(
      await bootConfigResponse.json(),
      await caches.open('blazor-resources'));
  }

  constructor (public readonly bootConfig: BootJsonData, private cache: Cache)
  {
  }

  loadResources(resources: ResourceList, url: (name: string) => string): LoadingResource[] {
    return Object.keys(resources)
      .map(name => this.loadResource(name, url(name), resources[name]));
  }

  logToConsole() {
    const cacheLoadsEntries = Object.values(this.cacheLoads);
    const networkLoadsEntries = Object.values(this.networkLoads);
    const cacheTransferredBytes = countTotalBytes(cacheLoadsEntries);
    const networkTransferredBytes = countTotalBytes(networkLoadsEntries);
    const totalTransferredBytes = cacheTransferredBytes + networkTransferredBytes;
    const linkerDisabledWarning = this.bootConfig.linkerEnabled ? '%c' : '\n%cThis application was built with linking (tree shaking) disabled. Published applications will be significantly smaller.';

    console.groupCollapsed(`%cblazor%c Loaded ${toDataSizeString(totalTransferredBytes)} resources${linkerDisabledWarning}`, 'background: purple; color: white; padding: 1px 3px; border-radius: 3px;', 'font-weight: bold;', 'font-weight: normal;');

    if (cacheLoadsEntries.length) {
      console.groupCollapsed(`Loaded ${toDataSizeString(cacheTransferredBytes)} resources from cache`);
      console.table(this.cacheLoads);
      console.groupEnd();
    }

    if (networkLoadsEntries.length) {
      console.groupCollapsed(`Loaded ${toDataSizeString(networkTransferredBytes)} resources from network`);
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

  private loadResource(name: string, url: string, contentHash: string): LoadingResource {
    const data = (async () => {
      // Try to load from cache
      const cacheKey = toAbsoluteUri(`${url}.${contentHash}`);
      this.usedCacheKeys[cacheKey] = true;
      const cachedResponse = await this.cache.match(cacheKey);
      let responseBuffer: ArrayBuffer;

      if (cachedResponse) {
        responseBuffer = await cachedResponse.arrayBuffer();
        const transferredBytes = parseInt(cachedResponse.headers.get('content-length') || '0');
        this.cacheLoads[name] = { transferredBytes };
      } else {
        // It's not in the cache, so fetch from network
        const networkResponse = await fetch(url, { cache: 'no-cache' });
        responseBuffer = await networkResponse.arrayBuffer();

        // Now is an ideal moment to capture the performance stats for the request, since it
        // only just completed and is most likely to still be in the buffer. However this is
        // only done on a 'best effort' basis. Even if we do receive an entry, some of its
        // properties may be blanked out if it was a CORS request.
        const performanceEntry = getPerformanceEntry(networkResponse.url);
        const transferredBytes = (performanceEntry && performanceEntry.encodedBodySize) || undefined;
        this.networkLoads[name] = { transferredBytes };

        // crypto.subtle is only enabled on localhost and HTTPS origins
        // We only write to the cache if we can validate the content hashes
        if (typeof crypto !== 'undefined' && !!crypto.subtle) {
          await assertContentHashMatchesAsync(name, responseBuffer, contentHash);

          // Build a custom response object so we can track extra data such as transferredBytes
          // We can't rely on the server sending content-length (ASP.NET Core doesn't by default)
          await this.cache.put(cacheKey, new Response(responseBuffer, {
            headers: {
              'content-length': (transferredBytes || networkResponse.headers.get('content-length') || '').toString()
            }
          }));
        }
      }

      return responseBuffer;
    })();

    return { name, url, data };
  }
}

function countTotalBytes(loads: LoadLogEntry[]) {
  return loads.reduce((prev, item) => prev + (item.transferredBytes || 0), 0);
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
  readonly linkerEnabled: boolean;
}

interface ResourceGroups {
  readonly assembly: ResourceList;
  readonly pdb?: ResourceList;
}

interface LoadLogEntry {
  transferredBytes: number | undefined;
}

export interface LoadingResource {
  name: string;
  url: string;
  data: Promise<ArrayBuffer>;
}

type ResourceList = { [name: string]: string };
