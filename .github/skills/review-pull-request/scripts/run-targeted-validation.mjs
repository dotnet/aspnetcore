import { spawnSync } from 'node:child_process';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import { prepareValidationDependencies } from './prepare-validation-dependencies.mjs';

const selectorPattern = /^[A-Za-z_][A-Za-z0-9_.+]*$/;
const maxOutputTailChars = 4096;
const generateFilesProjectRelative = 'eng/tools/GenerateFiles/GenerateFiles.csproj';
const generatedBuildFiles = [
  'artifacts/bin/GenerateFiles/Directory.Build.props',
  'artifacts/bin/GenerateFiles/Directory.Build.targets',
];

export function parseValidationArgs(argv) {
  let worktree = null;
  let head = null;
  let project = null;
  let managedOnly = false;
  const classes = [];
  const methods = [];

  for (let index = 0; index < argv.length; index++) {
    const arg = argv[index];
    const next = () => {
      if (index + 1 >= argv.length) {
        throw new Error(`Missing value for ${arg}.`);
      }
      index++;
      return argv[index];
    };
    switch (arg) {
      case '--worktree':
        worktree = next();
        break;
      case '--head':
        head = next();
        break;
      case '--project':
        project = next();
        break;
      case '--class':
        classes.push(next());
        break;
      case '--method':
        methods.push(next());
        break;
      case '--managed-only':
        managedOnly = true;
        break;
      default:
        throw new Error(`Unsupported argument: ${arg}`);
    }
  }

  if (!worktree || !head || !project) {
    throw new Error(
      'Usage: run-targeted-validation.mjs --worktree WORKTREE --head FROZEN_HEAD --project PROJECT.csproj ' +
      '[--class CLASS] [--method CLASS.METHOD] [--managed-only]');
  }
  if (!/^[a-f0-9]{40}$/.test(head)) {
    throw new Error('Expected a full frozen validation head.');
  }
  if (classes.length === 0 && methods.length === 0) {
    throw new Error('Specify at least one --class or --method test selector.');
  }
  for (const selector of [...classes, ...methods]) {
    if (!selectorPattern.test(selector)) {
      throw new Error(`Invalid test selector: ${selector}`);
    }
  }
  for (const method of methods) {
    const separator = method.lastIndexOf('.');
    if (separator <= 0 || separator === method.length - 1) {
      throw new Error(`Invalid test method selector (expected Namespace.Class.Method): ${method}`);
    }
  }
  return { worktree, head, project, classes, methods, managedOnly };
}

function isOutsideRoot(relative) {
  return !relative || relative === '..' || relative.startsWith(`..${path.sep}`) || path.isAbsolute(relative);
}

function resolveContainedProject(resolvedWorktree, projectInput, lexicalWorktree = resolvedWorktree) {
  if (typeof projectInput !== 'string' || projectInput.length === 0 || projectInput.includes('\0')) {
    throw new Error('Project path must be a non-empty path inside the validation worktree.');
  }
  let relative;
  if (path.isAbsolute(projectInput)) {
    const resolvedInput = path.resolve(projectInput);
    const fromCanonical = path.relative(resolvedWorktree, resolvedInput);
    const fromLexical = path.relative(lexicalWorktree, resolvedInput);
    relative = !isOutsideRoot(fromCanonical) ? fromCanonical : fromLexical;
  } else {
    relative = path.relative(resolvedWorktree, path.resolve(resolvedWorktree, projectInput));
  }
  if (isOutsideRoot(relative)) {
    throw new Error('Project path must stay within the validation worktree.');
  }
  const parts = relative.split(path.sep);
  let current = resolvedWorktree;
  for (const part of parts) {
    current = path.join(current, part);
    const stat = fs.lstatSync(current, { throwIfNoEntry: false });
    if (!stat) {
      throw new Error(`Selected project path does not exist: ${relative.replaceAll('\\', '/')}`);
    }
    if (stat.isSymbolicLink()) {
      throw new Error('Selected project path must not traverse a symlink.');
    }
  }
  const candidate = path.join(resolvedWorktree, relative);
  if (!candidate.endsWith('.csproj') || !fs.statSync(candidate).isFile()) {
    throw new Error('Selected project path must be an existing .csproj file.');
  }
  return {
    absoluteProject: candidate,
    relativeProject: relative.replaceAll('\\', '/'),
    projectBaseName: path.basename(candidate, '.csproj'),
  };
}

function defaultSpawn(command, args, options) {
  const result = spawnSync(command, args, {
    cwd: options.cwd,
    env: options.env,
    encoding: 'utf8',
    timeout: options.timeout ?? 1_200_000,
    maxBuffer: 64 * 1024 * 1024,
  });
  if (result.error) {
    throw result.error;
  }
  return {
    status: result.status ?? 1,
    stdout: result.stdout ?? '',
    stderr: result.stderr ?? '',
  };
}

function verifyWorktreeHead(resolvedWorktree, expectedHead, spawn) {
  const scratch = fs.mkdtempSync(path.join(os.tmpdir(), 'review-head-check-'));
  try {
    const hooks = path.join(scratch, 'disabled-hooks');
    fs.mkdirSync(hooks);
    const env = {
      PATH: process.env.PATH,
      HOME: scratch,
      GIT_CONFIG_GLOBAL: path.join(scratch, 'absent-config'),
      GIT_CONFIG_NOSYSTEM: '1',
      GIT_TERMINAL_PROMPT: '0',
    };
    for (const name of ['SystemRoot', 'TEMP', 'TMP']) {
      if (process.env[name]) {
        env[name] = process.env[name];
      }
    }
    const result = spawn('git', [
      '-c', `core.hooksPath=${hooks}`,
      '-c', 'credential.helper=',
      '-c', `safe.directory=${resolvedWorktree}`,
      '-C', resolvedWorktree,
      'rev-parse', 'HEAD',
    ], { cwd: resolvedWorktree, env, timeout: 30_000 });
    if (result.status !== 0 || result.stdout.trim() !== expectedHead) {
      throw new Error('Validation worktree head differs from the frozen head.');
    }
  } finally {
    fs.rmSync(scratch, { recursive: true, force: true });
  }
}

function detectTreeCapabilities(resolvedWorktree) {
  const componentsTargets = path.join(resolvedWorktree, 'src', 'Components', 'Directory.Build.targets');
  const componentsTargetsText = fs.existsSync(componentsTargets)
    ? fs.readFileSync(componentsTargets, 'utf8')
    : '';
  const generateFilesProject = path.join(resolvedWorktree, ...generateFilesProjectRelative.split('/'));
  return {
    supportsIncludeComponentsWebAssets: componentsTargetsText.includes('IncludeComponentsWebAssets'),
    hasGenerateFilesProject: fs.existsSync(generateFilesProject),
  };
}

function hasGeneratedBuildInputs(resolvedWorktree) {
  return generatedBuildFiles.every(relativePath =>
    fs.existsSync(path.join(resolvedWorktree, ...relativePath.split('/'))));
}

function parseXunitResultXml(xmlText) {
  const assemblies = [...xmlText.matchAll(/<assembly\b([^>]*)>/g)];
  if (assemblies.length === 0) {
    return null;
  }
  const readInt = (attributes, name) => {
    const match = new RegExp(`\\b${name}="(\\d+)"`).exec(attributes);
    return match ? Number(match[1]) : 0;
  };
  let total = 0;
  let passed = 0;
  let failed = 0;
  let skipped = 0;
  for (const [, attributes] of assemblies) {
    total += readInt(attributes, 'total');
    passed += readInt(attributes, 'passed');
    failed += readInt(attributes, 'failed');
    skipped += readInt(attributes, 'skipped');
  }
  const testEntries = [...xmlText.matchAll(/<test\b([^>]*)>/g)].map(([, attributes]) => {
    const type = /\btype="([^"]+)"/.exec(attributes)?.[1] ?? '';
    const method = /\bmethod="([^"]+)"/.exec(attributes)?.[1] ?? '';
    const name = /\bname="([^"]+)"/.exec(attributes)?.[1] ?? '';
    const result = /\bresult="([^"]+)"/.exec(attributes)?.[1] ?? '';
    return { type, method, name, result };
  });
  return {
    total,
    executed: Math.max(0, total - skipped),
    passed,
    failed,
    skipped,
    testEntries,
  };
}

function matchesRequestedSelectors(testEntries, classes, methods) {
  const executedEntries = testEntries.filter(
    entry => entry.result === 'Pass' || entry.result === 'Fail'
  );
  if (executedEntries.length === 0) {
    return false;
  }
  for (const expectedClass of classes) {
    const matched = executedEntries.some(entry =>
      entry.type
        ? entry.type === expectedClass || entry.type.startsWith(`${expectedClass}+`)
        : entry.name.startsWith(`${expectedClass}.`));
    if (!matched) {
      return false;
    }
  }
  for (const expectedMethod of methods) {
    const lastDot = expectedMethod.lastIndexOf('.');
    const expectedType = lastDot > 0 ? expectedMethod.slice(0, lastDot) : '';
    const expectedMember = lastDot > 0 ? expectedMethod.slice(lastDot + 1) : expectedMethod;
    const matched = executedEntries.some(entry => {
      const typeMatches = !expectedType || !entry.type ||
        entry.type === expectedType || entry.type.startsWith(`${expectedType}+`);
      const memberMatches =
        (expectedType && entry.method === expectedMember) ||
        entry.name === expectedMethod ||
        entry.name.startsWith(`${expectedMethod}(`);
      return typeMatches && memberMatches;
    });
    if (!matched) {
      return false;
    }
  }
  return true;
}

export function runTargetedValidation(
  rawOptions,
  {
    spawn = defaultSpawn,
    prepareDependencies = prepareValidationDependencies,
    platform = process.platform,
  } = {}) {
  const options = Array.isArray(rawOptions) ? parseValidationArgs(rawOptions) : parseValidationArgs([
    '--worktree', rawOptions.worktree,
    '--head', rawOptions.head,
    '--project', rawOptions.project,
    ...(rawOptions.classes ?? []).flatMap(item => ['--class', item]),
    ...(rawOptions.methods ?? []).flatMap(item => ['--method', item]),
    ...(rawOptions.managedOnly ? ['--managed-only'] : []),
  ]);

  const worktreeStat = fs.lstatSync(options.worktree, { throwIfNoEntry: false });
  if (!worktreeStat?.isDirectory() || worktreeStat.isSymbolicLink()) {
    throw new Error('Validation worktree must be a non-symlink directory.');
  }
  const lexicalWorktree = path.resolve(options.worktree);
  const resolvedWorktree = fs.realpathSync(options.worktree);
  verifyWorktreeHead(resolvedWorktree, options.head, spawn);

  const { absoluteProject, relativeProject, projectBaseName } =
    resolveContainedProject(resolvedWorktree, options.project, lexicalWorktree);
  const limitations = [];
  let dependencies = [];
  try {
    dependencies = prepareDependencies(resolvedWorktree, options.head);
  } catch (error) {
    if (error?.code !== 'VALIDATION_DEPENDENCY_FETCH_FAILED') {
      throw error;
    }
    limitations.push(
      'dependency-unavailable: pinned validation dependency could not be fetched; projects requiring it may fail to build');
  }
  const capabilities = detectTreeCapabilities(resolvedWorktree);

  const isWindows = platform === 'win32';
  const propertyPrefix = isWindows ? '/p:' : '-p:';
  const managedFlags = [];
  if (options.managedOnly) {
    managedFlags.push(`${propertyPrefix}BuildNodeJS=false`);
    if (capabilities.supportsIncludeComponentsWebAssets) {
      managedFlags.push(`${propertyPrefix}IncludeComponentsWebAssets=false`);
    }
    limitations.push(
      'managed-only: excludes browser, JavaScript, static web assets, and package content');
  }

  const baseCommand = isWindows
    ? [
        'cmd.exe', '/d', '/s', '/c', 'eng\\build.cmd',
        '-projects', absoluteProject,
        '-configuration', 'Debug',
        ...managedFlags,
        '/p:BuildInParallel=false',
      ]
    : [
        'bash', './eng/build.sh',
        '--projects', absoluteProject,
        '--configuration', 'Debug',
        ...managedFlags,
        '--msbuildMultiThreaded', 'false',
      ];
  const restoreArgs = isWindows ? ['-restore'] : ['--restore'];
  const testArgs = isWindows
    ? ['-noRestore', '-noBuild', '-test']
    : ['--no-restore', '--no-build', '--test'];

  const childEnv = {
    ...process.env,
    DOTNET_CLI_TELEMETRY_OPTOUT: '1',
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE: '1',
    DOTNET_NOLOGO: '1',
    MSBUILDDISABLENODEREUSE: '1',
  };

  const tailText = text => {
    const trimmed = (text ?? '').trim();
    return trimmed.length <= maxOutputTailChars
      ? trimmed
      : trimmed.slice(trimmed.length - maxOutputTailChars);
  };

  const phases = [];
  const runPhase = (name, commandWithArgs, extraEnv = {}) => {
    const [program, ...args] = commandWithArgs;
    const result = spawn(program, args, {
      cwd: resolvedWorktree,
      env: { ...childEnv, ...extraEnv },
    });
    const phaseRecord = {
      name,
      command: commandWithArgs,
      exitCode: result.status,
    };
    const stdoutTail = tailText(result.stdout);
    const stderrTail = tailText(result.stderr);
    if (stdoutTail) {
      phaseRecord.stdoutTail = stdoutTail;
    }
    if (stderrTail) {
      phaseRecord.stderrTail = stderrTail;
    }
    phases.push(phaseRecord);
    return result;
  };

  let bootstrapResult = runPhase('bootstrap', [...baseCommand, ...restoreArgs]);
  let generatedInputsFallback = false;

  if (capabilities.hasGenerateFilesProject && !hasGeneratedBuildInputs(resolvedWorktree)) {
    generatedInputsFallback = true;
    const dotnetDir = path.join(resolvedWorktree, '.dotnet');
    const localDotnet = path.join(dotnetDir, isWindows ? 'dotnet.exe' : 'dotnet');
    const dotnetExe = fs.existsSync(localDotnet) ? localDotnet : 'dotnet';
    const dotnetEnv = fs.existsSync(dotnetDir)
      ? {
          DOTNET_ROOT: dotnetDir,
          PATH: `${dotnetDir}${path.delimiter}${childEnv.PATH ?? ''}`,
        }
      : {};
    const generateFilesCsproj = path.join(resolvedWorktree, ...generateFilesProjectRelative.split('/'));
    const restoreGen = runPhase('generate-files-restore', [
      dotnetExe, 'msbuild', generateFilesCsproj, '-target:Restore', '/p:BuildInParallel=false',
    ], dotnetEnv);
    if (restoreGen.status === 0) {
      const buildGen = runPhase('generate-files', [
        dotnetExe, 'msbuild', generateFilesCsproj, '-target:GenerateDirectoryBuildFiles', '/p:BuildInParallel=false',
      ], dotnetEnv);
      if (buildGen.status === 0) {
        bootstrapResult = runPhase('bootstrap-retry', [...baseCommand, ...restoreArgs]);
      }
    }
  }

  verifyWorktreeHead(resolvedWorktree, options.head, spawn);

  if (bootstrapResult.status !== 0) {
    return {
      status: 'build_failed',
      exitCode: 2,
      head: options.head,
      worktree: resolvedWorktree,
      project: relativeProject,
      managedOnly: options.managedOnly,
      selectors: { classes: options.classes, methods: options.methods },
      dependencies,
      generatedInputsFallback,
      phases,
      tests: { total: 0, executed: 0, passed: 0, failed: 0, skipped: 0, resultFiles: [] },
      limitations,
    };
  }

  const resultsDir = path.join(resolvedWorktree, 'artifacts', 'TestResults', 'Debug');
  const isProjectResultFile = entry =>
    entry.endsWith('.xml') &&
    (entry === `${projectBaseName}.xml` || entry.startsWith(`${projectBaseName}_`));
  if (fs.existsSync(resultsDir)) {
    for (const entry of fs.readdirSync(resultsDir)) {
      if (isProjectResultFile(entry)) {
        fs.rmSync(path.join(resultsDir, entry), { force: true });
      }
    }
  }

  const selectorString = [
    ...options.classes.map(item => `-class ${item}`),
    ...options.methods.map(item => `-method ${item}`),
  ].join(' ');
  const testResult = runPhase('test', [...baseCommand, ...testArgs], {
    TestRunnerAdditionalArguments: selectorString,
  });

  verifyWorktreeHead(resolvedWorktree, options.head, spawn);

  const matchingFiles = [];
  if (fs.existsSync(resultsDir)) {
    for (const entry of fs.readdirSync(resultsDir).sort()) {
      if (!isProjectResultFile(entry)) {
        continue;
      }
      const fullPath = path.join(resultsDir, entry);
      if (fs.statSync(fullPath, { throwIfNoEntry: false })?.isFile()) {
        matchingFiles.push(fullPath);
      }
    }
  }

  let total = 0;
  let executed = 0;
  let passed = 0;
  let failed = 0;
  let skipped = 0;
  const allEntries = [];
  let parseValid = matchingFiles.length > 0;

  for (const filePath of matchingFiles) {
    const parsed = parseXunitResultXml(fs.readFileSync(filePath, 'utf8'));
    if (!parsed) {
      parseValid = false;
      break;
    }
    total += parsed.total;
    executed += parsed.executed;
    passed += parsed.passed;
    failed += parsed.failed;
    skipped += parsed.skipped;
    allEntries.push(...parsed.testEntries);
  }

  const selectorsMatched = parseValid &&
    executed > 0 &&
    matchesRequestedSelectors(allEntries, options.classes, options.methods);

  let status;
  let exitCode;
  if (!selectorsMatched) {
    status = 'no_tests_executed';
    exitCode = 2;
  } else if (testResult.status !== 0 || failed > 0) {
    status = 'failed';
    exitCode = 1;
  } else {
    status = 'passed';
    exitCode = 0;
  }

  return {
    status,
    exitCode,
    head: options.head,
    worktree: resolvedWorktree,
    project: relativeProject,
    managedOnly: options.managedOnly,
    selectors: { classes: options.classes, methods: options.methods },
    dependencies,
    generatedInputsFallback,
    phases,
    tests: {
      total,
      executed,
      passed,
      failed,
      skipped,
      resultFiles: matchingFiles.map(file => path.relative(resolvedWorktree, file).replaceAll('\\', '/')),
    },
    limitations,
  };
}

if (process.argv[1] && import.meta.url === pathToFileURL(path.resolve(process.argv[1])).href) {
  const receipt = runTargetedValidation(process.argv.slice(2));
  console.log(JSON.stringify(receipt));
  process.exitCode = receipt.exitCode;
}
