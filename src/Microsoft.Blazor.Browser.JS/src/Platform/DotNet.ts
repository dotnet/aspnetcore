export function getAssemblyNameFromUrl(url: string) {
  const lastSegment = url.substring(url.lastIndexOf('/') + 1);
  const queryStringStartPos = lastSegment.indexOf('?');
  const filename = queryStringStartPos < 0 ? lastSegment : lastSegment.substring(0, queryStringStartPos);
  return filename.replace(/\.dll$/, '');
}
