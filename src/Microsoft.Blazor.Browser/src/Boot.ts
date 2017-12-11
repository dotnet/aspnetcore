import { platform } from './Environment';
import { getAssemblyNameFromUrl } from './Platform/DotNet';

async function boot() {
  // Read startup config from the <script> element that's importing this file
  const allScriptElems = document.getElementsByTagName('script');
  const thisScriptElem = allScriptElems[allScriptElems.length - 1];
  const entryPoint = thisScriptElem.getAttribute('main');
  if (!entryPoint) {
    throw new Error('Missing "main" attribute on Blazor script tag.');
  }
  const entryPointAssemblyName = getAssemblyNameFromUrl(entryPoint);
  const referenceAssembliesCommaSeparated = thisScriptElem.getAttribute('references') || '';
  const referenceAssemblies = referenceAssembliesCommaSeparated
    .split(',')
    .map(s => s.trim())
    .filter(s => !!s);

  // Determine the URLs of the assemblies we want to load
  const loadAssemblyUrls = [entryPoint]
    .concat(referenceAssemblies)
    .map(filename => `/_framework/_bin/${filename}`);

  try {
    await platform.start(loadAssemblyUrls);
  } catch (ex) {
    throw new Error(`Failed to start platform. Reason: ${ex}`);
  }

  // Start up the application
  platform.callEntryPoint(entryPointAssemblyName, []);
}

boot();
