export function getFileNameFromUrl(url: string) {
  // This could also be called "get last path segment from URL", but the primary
  // use case is to extract things that look like filenames
  const lastSegment = url.substring(url.lastIndexOf('/') + 1);
  const queryStringStartPos = lastSegment.indexOf('?');
  return queryStringStartPos < 0 ? lastSegment : lastSegment.substring(0, queryStringStartPos);
}

export function getAssemblyNameFromUrl(url: string) {
  return getFileNameFromUrl(url).replace(/\.dll$/, '');
}
