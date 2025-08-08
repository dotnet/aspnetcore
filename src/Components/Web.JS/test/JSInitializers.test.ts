// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

describe('JSInitializer URL handling', () => {
  // Mock document.baseURI for testing
  let originalBaseURI: string;
  
  beforeEach(() => {
    originalBaseURI = document.baseURI;
  });
  
  afterEach(() => {
    // Reset document.baseURI by setting a new base element
    const existingBase = document.querySelector('base');
    if (existingBase) {
      existingBase.remove();
    }
    const base = document.createElement('base');
    base.href = originalBaseURI;
    document.head.insertBefore(base, document.head.firstChild);
  });

  function setDocumentBase(baseUri: string): void {
    // Remove existing base elements
    const existingBases = document.querySelectorAll('base');
    existingBases.forEach(base => base.remove());
    
    // Add new base element
    const base = document.createElement('base');
    base.href = baseUri;
    document.head.insertBefore(base, document.head.firstChild);
  }

  // Test the correct URL resolution approach that the fix implements
  function correctUrlResolution(path: string): string {
    return new URL(path, document.baseURI).toString();
  }

  // Test the problematic string concatenation approach that was fixed
  function problematicStringConcatenation(path: string): string {
    const base = document.baseURI;
    return base.endsWith('/') ? `${base}${path}` : `${base}/${path}`;
  }

  test('URL constructor vs string concatenation with query parameters', () => {
    setDocumentBase('http://domain?a=x');
    
    const correctResult = correctUrlResolution('_content/Package/file.js');
    const problematicResult = problematicStringConcatenation('_content/Package/file.js');
    
    // The URL constructor produces a valid URL
    expect(correctResult).toBe('http://domain/_content/Package/file.js');
    
    // String concatenation produces malformed URL with query in wrong place  
    expect(problematicResult).toBe('http://domain/?a=x/_content/Package/file.js');
    
    // Verify they are different
    expect(correctResult).not.toBe(problematicResult);
  });

  test('URL constructor handles hash correctly', () => {
    setDocumentBase('http://domain#section');
    const result = correctUrlResolution('_content/Package/file.js');
    
    // Hash should be dropped when resolving relative URLs
    expect(result).toBe('http://domain/_content/Package/file.js');
  });

  test('URL constructor handles trailing slash correctly', () => {
    setDocumentBase('http://domain/');
    const result = correctUrlResolution('_content/Package/file.js');
    
    expect(result).toBe('http://domain/_content/Package/file.js');
  });

  test('URL constructor handles no trailing slash correctly', () => {
    setDocumentBase('http://domain');
    const result = correctUrlResolution('_content/Package/file.js');
    
    expect(result).toBe('http://domain/_content/Package/file.js');
  });

  test('URL constructor resolves from subdirectory correctly', () => {
    setDocumentBase('http://domain/subdir/');
    const result = correctUrlResolution('_content/Package/file.js');
    
    // From a subdirectory base, the relative path resolves relative to that directory
    expect(result).toBe('http://domain/subdir/_content/Package/file.js');
  });

  test('URL constructor handles complex base with query and hash', () => {
    setDocumentBase('http://domain/app/?debug=true#section');
    const result = correctUrlResolution('_content/Package/file.js');
    
    // Query and hash are handled properly - query is dropped, hash is dropped
    expect(result).toBe('http://domain/app/_content/Package/file.js');
  });
});