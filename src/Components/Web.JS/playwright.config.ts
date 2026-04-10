// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './test/Validation/integration',
  timeout: 30_000,
  retries: 0,
  use: {
    baseURL: 'http://localhost:5588',
    browserName: 'chromium',
  },
  webServer: {
    command: 'node test/Validation/serve-fixtures.mjs',
    port: 5588,
    reuseExistingServer: !process.env.CI,
  },
});
