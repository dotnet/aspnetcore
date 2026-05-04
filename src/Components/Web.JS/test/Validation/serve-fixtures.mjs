// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Minimal static file server for Playwright integration tests.
// Serves HTML fixtures and the built validation JS bundle.

import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { join, extname, resolve, sep } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = fileURLToPath(new URL('.', import.meta.url));
const fixturesDir = join(__dirname, 'fixtures');
const distDir = join(__dirname, '..', '..', 'dist', 'Debug');

const mimeTypes = {
  '.html': 'text/html',
  '.js': 'application/javascript',
  '.css': 'text/css',
  '.map': 'application/json',
};

const server = createServer(async (req, res) => {
  const url = new URL(req.url, 'http://localhost');

  // Mock API endpoint for remote validation tests
  if (url.pathname === '/api/validate-username') {
    // Simulate network latency
    await new Promise(resolve => setTimeout(resolve, 100));
    const username = url.searchParams.get('Username') || '';
    // "taken" is the only invalid username
    const valid = username.toLowerCase() !== 'taken';
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify(valid ? true : 'This username is already taken.'));
    return;
  }

  if (url.pathname === '/api/validate-slow') {
    // Slow endpoint for testing pending state and abort
    await new Promise(resolve => setTimeout(resolve, 500));
    const value = url.searchParams.get('SlowField') || '';
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify(value === 'invalid' ? 'Invalid value.' : true));
    return;
  }

  if (url.pathname === '/api/validate-custom') {
    // Endpoint for custom async validator tests
    await new Promise(resolve => setTimeout(resolve, 100));
    const value = url.searchParams.get('CustomField') || '';
    const valid = value.startsWith('OK');
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify(valid ? true : 'Value must start with OK.'));
    return;
  }

  if (url.pathname === '/api/validate-fast') {
    // Fast endpoint for debounce timing tests (no artificial delay)
    const value = url.searchParams.get('FastField') || '';
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify(value === 'bad' ? 'Invalid.' : true));
    return;
  }

  let filePath;

  if (url.pathname.startsWith('/dist/')) {
    // Serve built JS files from dist/Debug
    filePath = resolve(distDir, url.pathname.slice('/dist/'.length));
    if (!filePath.startsWith(distDir + sep)) {
      res.writeHead(403);
      res.end('Forbidden');
      return;
    }
  } else {
    // Serve HTML fixtures (strip leading slash so join resolves under fixturesDir)
    const requestedPath = url.pathname === '/' ? 'index.html' : url.pathname.slice(1);
    filePath = resolve(fixturesDir, requestedPath);
    if (!filePath.startsWith(fixturesDir + sep)) {
      res.writeHead(403);
      res.end('Forbidden');
      return;
    }
  }

  try {
    const content = await readFile(filePath);
    const ext = extname(filePath);
    res.writeHead(200, { 'Content-Type': mimeTypes[ext] || 'application/octet-stream' });
    res.end(content);
  } catch {
    res.writeHead(404);
    res.end('Not found');
  }
});

server.listen(5588, () => {
  console.log('Fixture server listening on http://localhost:5588');
});
