import { BootConfigResult } from './BootConfig';
import { System_String, System_Object } from './Platform';

export class WebAssemblyConfigLoader {
  static async initAsync(bootConfigResult: BootConfigResult): Promise<void> {
    window['Blazor']._internal.getApplicationEnvironment = () => BINDING.js_string_to_mono_string(bootConfigResult.applicationEnvironment);

    const configFiles = await Promise.all((bootConfigResult.bootConfig.config || [])
      .filter(name => name === 'appsettings.json' || name === `appsettings.${bootConfigResult.applicationEnvironment}.json`)
      .map(async name => ({ name, content: await getConfigBytes(name) })));

    window['Blazor']._internal.getConfig = (dotNetFileName: System_String) : System_Object | undefined => {
      const fileName = BINDING.conv_string(dotNetFileName);
      const resolvedFile = configFiles.find(f => f.name === fileName);
      return resolvedFile ? BINDING.js_typed_array_to_array(resolvedFile.content) : undefined;
    };

    async function getConfigBytes(file: string): Promise<Uint8Array> {
      const response = await fetch(file, {
        method: 'GET',
        credentials: 'include',
        cache: 'no-cache'
      });

      return new Uint8Array(await response.arrayBuffer());
    }
  }
}
