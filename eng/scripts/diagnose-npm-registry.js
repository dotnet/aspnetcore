// TEMPORARY diagnostic script for investigating internal/release/8.0 CI failures resolving
// ws@7.5.11 during `yarn install` (see: "Couldn't find package ws@^7.5.11 ... on the npm registry").
// This is not intended to be a permanent part of the build - remove once the root cause of the
// restore failure is understood and fixed.
'use strict';

const { execSync } = require('child_process');
const fs = require('fs');
const path = require('path');
const dns = require('dns');
const https = require('https');

function log(msg) {
  console.log(`[yarn-diag] ${msg}`);
}

function run(cmd) {
  try {
    return execSync(cmd, { encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'] }).trim();
  } catch (err) {
    return `ERROR running "${cmd}": ${err.message}`;
  }
}

log('==================== BEGIN NPM/YARN REGISTRY DIAGNOSTICS ====================');
log(`timestamp=${new Date().toISOString()}`);
log(`cwd=${process.cwd()}`);
log(`platform=${process.platform} arch=${process.arch}`);
log(`node ${process.version}`);
log(`yarn version: ${run('yarn --version')}`);
log(`npm version: ${run('npm --version')}`);

// Walk up from cwd to the filesystem root, printing any .npmrc found (or not found) at each level.
// This is the crux of the ws@7.5.11 investigation: yarn classic's config resolution for link:
// dependencies does not reliably walk up directories the way npm does.
let dir = process.cwd();
for (let i = 0; i < 10; i++) {
  const npmrc = path.join(dir, '.npmrc');
  if (fs.existsSync(npmrc)) {
    log(`.npmrc FOUND at ${npmrc}:`);
    for (const line of fs.readFileSync(npmrc, 'utf8').split(/\r?\n/)) {
      log(`    ${line}`);
    }
  } else {
    log(`.npmrc NOT found at ${npmrc}`);
  }
  const parent = path.dirname(dir);
  if (parent === dir) {
    break;
  }
  dir = parent;
}

// Dump any environment variables that could influence npm/yarn registry resolution.
for (const key of Object.keys(process.env).filter(k => /npm|yarn|registry|proxy/i.test(k))) {
  log(`env ${key}=${process.env[key]}`);
}

log('yarn config list:');
console.log(run('yarn config list'));

log('npm config list:');
console.log(run('npm config list'));

log('npm config get registry:');
console.log(run('npm config get registry'));

// DNS + HTTP reachability checks against both yarn's hardcoded default registry and the
// AzDO feed that .npmrc points at, to see whether Network Isolation (or something else) is
// blocking/rejecting either of them from this specific agent/job.
const hosts = ['registry.yarnpkg.com', 'pkgs.dev.azure.com', 'registry.npmjs.org'];

function checkDns(host) {
  return new Promise(resolve => {
    dns.lookup(host, (err, address) => {
      if (err) {
        log(`DNS lookup for ${host} FAILED: ${err.message}`);
      } else {
        log(`DNS lookup for ${host} -> ${address}`);
      }
      resolve();
    });
  });
}

function checkHttpGet(url) {
  return new Promise(resolve => {
    const start = Date.now();
    const req = https.get(url, { timeout: 15000 }, res => {
      log(`GET ${url} -> HTTP ${res.statusCode} (${Date.now() - start}ms)`);
      res.resume();
      res.on('end', resolve);
    });
    req.on('error', err => {
      log(`GET ${url} FAILED after ${Date.now() - start}ms: ${err.message}`);
      resolve();
    });
    req.on('timeout', () => {
      log(`GET ${url} TIMED OUT after ${Date.now() - start}ms`);
      req.destroy();
      resolve();
    });
  });
}

(async () => {
  for (const host of hosts) {
    await checkDns(host);
  }

  // Direct registry protocol lookups for the exact package/version at the center of the failure.
  await checkHttpGet('https://registry.yarnpkg.com/ws/-/ws-7.5.11.tgz');
  await checkHttpGet('https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public-npm/npm/registry/ws');
  await checkHttpGet('https://registry.npmjs.org/ws/7.5.11');

  log('==================== END NPM/YARN REGISTRY DIAGNOSTICS ====================');
})();
