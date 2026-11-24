// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { parseArgs } from 'node:util';
import { pin, roles } from './pin.mjs';
import { outcomes } from './outcomes.mjs';

const usage = `Usage:
  node l3.mjs pin --repo OWNER/REPO [--mirror PATH] --run RUN_ID-ATTEMPT [--pr NUMBER]
    [--head SHA] [--baseTip SHA] [--mergeBase SHA] [--workflow SHA] [--guidance SHA]
  node l3.mjs outcomes --repo OWNER/REPO --run RUN_ID --attempt N --output PATH

Default mirror: %LOCALAPPDATA%\\aspnet-review-l3\\mirror.git`;

try
{
  const [verb, ...args] = process.argv.slice(2);
  if (!verb || verb === '--help') console.log(usage);
  else
  {
    const required = verb === 'pin' ? ['repo', 'run'] :
      verb === 'outcomes' ? ['repo', 'run', 'attempt', 'output'] : null;
    if (!required) throw new Error(`Unknown command: ${verb}`);
    const flags = [...required, ...(verb === 'pin' ? ['mirror', 'pr', ...roles] : [])];
    const { values } = parseArgs({ args, options: Object.fromEntries(flags.map(flag => [flag, { type: 'string' }])) });
    for (const flag of required)
      if (!values[flag]) throw new Error(`Missing --${flag}.`);
    if (verb === 'pin')
    {
      const localAppData = process.env.LOCALAPPDATA;
      const mirror = values.mirror ?? (localAppData && join(localAppData, 'aspnet-review-l3', 'mirror.git'));
      if (!mirror) throw new Error('Specify --mirror PATH when LOCALAPPDATA is unset.');
      const commits = Object.fromEntries(roles.filter(role => Object.hasOwn(values, role)).map(role => [role, values[role]]));
      const report = pin({ ...values, mirror, commits });
      console.log(JSON.stringify(report, null, 2));
      if (!report.offlineRestoreVerified) process.exitCode = 1;
    }
    else
    {
      const result = outcomes(values);
      writeFileSync(values.output, `${JSON.stringify(result, null, 2)}\n`);
      if (result.agentLog.status === 'fetch-failed')
        console.error(`Warning: the agent job log could not be read (gh exit ${result.agentLog.ghExit}); log-derived commits are unknown.`);
    }
  }
}
catch (error)
{
  console.error(error.message);
  process.exitCode = 1;
}
