// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Minimal static file server for Playwright integration tests.
// Serves HTML fixtures and the built validation JS bundle.

import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { join, extname } from 'node:path';
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
  let filePath;

  if (url.pathname.startsWith('/dist/')) {
    // Serve built JS files from dist/Debug
    filePath = join(distDir, url.pathname.slice('/dist/'.length));
  } else {
    // Serve HTML fixtures
    const requestedPath = url.pathname === '/' ? '/index.html' : url.pathname;
    filePath = join(fixturesDir, requestedPath);
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
