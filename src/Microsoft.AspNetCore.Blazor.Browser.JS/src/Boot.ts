import { platform } from './Environment';
import { getAssemblyNameFromUrl } from './Platform/DotNet';
import './Rendering/Renderer';
import './GlobalExports';

async function boot() {
  // Read startup config from the <script> element that's importing this file
  const allScriptElems = document.getElementsByTagName('script');
  const thisScriptElem = document.currentScript || allScriptElems[allScriptElems.length - 1];
  const entryPointDll = thisScriptElem.getAttribute('main');
  if (!entryPointDll) {
    throw new Error('Missing "main" attribute on Blazor script tag.');
  }
  // TODO:  should method be a required, non-empty field or just an optional *hint*?
  const entryPointMethod = thisScriptElem.getAttribute('entry-point');
  const entryPointAssemblyName = getAssemblyNameFromUrl(entryPointDll);
  const referenceAssembliesCommaSeparated = thisScriptElem.getAttribute('references') || '';
  const referenceAssemblies = referenceAssembliesCommaSeparated
    .split(',')
    .map(s => s.trim())
    .filter(s => !!s);

  // Determine the URLs of the assemblies we want to load
  const loadAssemblyUrls = [entryPointDll]
    .concat(referenceAssemblies)
    .map(filename => `/_framework/_bin/${filename}`);

  try {
    await platform.start(loadAssemblyUrls);
  } catch (ex) {
    throw new Error(`Failed to start platform. Reason: ${ex}`);
  }

  // Start up the application
  platform.callEntryPoint(entryPointAssemblyName, entryPointMethod, []);
}

boot();
