export class WebAssemblyResourceLoader {
  private log: { [name: string]: LoadLogEntry } = {};

  static async initAsync(): Promise<WebAssemblyResourceLoader> {
    const bootConfigResponse = await fetch('_framework/blazor.boot.json', {
      method: 'GET',
      credentials: 'include'
    });

    return new WebAssemblyResourceLoader(await bootConfigResponse.json());
  }

  constructor (public readonly bootConfig: BootJsonData)
  {
  }

  loadResources(resources: ResourceList, url: (name: string) => string): LoadingResource[] {
    return Object.keys(resources)
      .map(name => this.loadResource(name, url(name), resources[name]));
  }

  logToConsole() {
    console.table(this.log);
  }

  private loadResource(name: string, url: string, contentHash: string): LoadingResource {
    const data = (async () => {
      const response = await fetch(url, { cache: 'no-cache' });
      const data = await response.arrayBuffer();

      // Now is an ideal moment to capture the performance stats for the request, since it
      // only just completed and is most likely to still be in the buffer. However this is
      // only done on a 'best effort' basis. Even if we do receive an entry, some of its
      // properties may be blanked out if it was a CORS request.
      const performanceEntry = getPerformanceEntry(response.url);
      const transferredBytes = (performanceEntry && performanceEntry.encodedBodySize) || undefined;
      this.log[name] = { url, transferredBytes };

      return data;
    })();

    return { name, url, data };
  }
}

function getPerformanceEntry(url: string): PerformanceResourceTiming | undefined {
  if (typeof performance !== 'undefined') {
    return performance.getEntriesByName(url)[0] as PerformanceResourceTiming;
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
  url: string;
  transferredBytes: number | undefined;
}

export interface LoadingResource {
  name: string;
  url: string;
  data: Promise<ArrayBuffer>;
}

type ResourceList = { [name: string]: string };
