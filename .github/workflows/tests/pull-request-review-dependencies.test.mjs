import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import test from 'node:test';
import { prepareValidationDependencies } from '../../skills/review-pull-request/scripts/prepare-validation-dependencies.mjs';
import {
  parseValidationArgs,
  runTargetedValidation,
} from '../../skills/review-pull-request/scripts/run-targeted-validation.mjs';

const modulePath = 'src/submodules/MessagePack-CSharp';
const repository = 'https://github.com/aspnet/MessagePack-CSharp.git';

function fixture(action, { link = true } = {}) {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), 'review-dependency-test-'));
  const worktree = path.join(root, 'worktree');
  const dependency = path.join(root, 'dependency');
  const git = (directory, ...args) => execFileSync('git', [
    '-C', directory, '-c', 'core.hooksPath=', '-c', 'core.autocrlf=false', '-c', 'user.name=Fixture',
    '-c', 'user.email=fixture@example.invalid', ...args,
  ], { encoding: 'utf8' }).trim();
  try {
    for (const directory of [worktree, dependency]) {
      fs.mkdirSync(directory);
      git(directory, 'init', '--quiet');
    }
    fs.writeFileSync(path.join(dependency, 'source.txt'), 'frozen source\n');
    git(dependency, 'add', '.');
    git(dependency, 'commit', '--quiet', '-m', 'Fixture dependency');
    const dependencyHead = git(dependency, 'rev-parse', 'HEAD');
    fs.writeFileSync(path.join(worktree, 'control.txt'), 'unchanged\n');
    fs.writeFileSync(path.join(worktree, '.gitmodules'),
      `[submodule "${modulePath}"]\npath = ${modulePath}\nurl = https://untrusted.invalid/must-not-fetch\n`);
    git(worktree, 'add', '.');
    if (link) {
      git(worktree, 'update-index', '--add', '--cacheinfo', `160000,${dependencyHead},${modulePath}`);
    }
    git(worktree, 'commit', '--quiet', '-m', 'Fixture target');
    if (link) {
      fs.mkdirSync(path.join(worktree, modulePath), { recursive: true });
    }
    const head = git(worktree, 'rev-parse', 'HEAD');
    let fetches = 0;
    const fetch = (program, args, options) => {
      fetches++;
      assert.equal(program, 'git');
      assert.equal(args.at(-2), repository);
      assert.equal(args.at(-1), dependencyHead);
      assert.ok(args.includes('--no-recurse-submodules'));
      assert.equal(options.env.GIT_TERMINAL_PROMPT, '0');
      assert.equal(options.env.GIT_CONFIG_NOSYSTEM, '1');
      assert.deepEqual(Object.keys(options.env).filter(name =>
        /TOKEN|SECRET|PASSWORD|CREDENTIAL|API_KEY|GIT_CONFIG_COUNT/i.test(name)), []);
      return execFileSync(program, [...args.slice(0, -2), dependency, dependencyHead], options);
    };
    action({ root, worktree, dependency, head, dependencyHead, git, fetch, fetches: () => fetches });
  } finally {
    fs.rmSync(root, { recursive: true, force: true });
  }
}

test('MaterializesOnlyTheFrozenAllowlistedGitlinkAndPreservesTheTarget', () => fixture(f => {
  const before = f.git(f.worktree, 'status', '--porcelain');
  const result = prepareValidationDependencies(f.worktree, f.head, { fetch: f.fetch });
  assert.deepEqual(result, [{ path: modulePath, repository, commit: f.dependencyHead }]);
  assert.equal(f.fetches(), 1);
  assert.equal(f.git(path.join(f.worktree, modulePath), 'rev-parse', 'HEAD'), f.dependencyHead);
  assert.equal(fs.readFileSync(path.join(f.worktree, modulePath, 'source.txt'), 'utf8'), 'frozen source\n');
  assert.equal(f.git(f.worktree, 'rev-parse', 'HEAD'), f.head);
  assert.equal(f.git(f.worktree, 'status', '--porcelain'), before);
}));

test('LeavesTargetsWithoutTheGitlinkUnchanged', () => fixture(f => {
  assert.deepEqual(prepareValidationDependencies(f.worktree, f.head, { fetch: f.fetch }), []);
  assert.equal(f.fetches(), 0);
  assert.equal(fs.existsSync(path.join(f.worktree, modulePath)), false);
  assert.equal(f.git(f.worktree, 'status', '--porcelain'), '');
}, { link: false }));

test('RejectsADifferentFrozenHeadBeforeFetching', () => fixture(f => {
  assert.throws(() => prepareValidationDependencies(f.worktree, 'a'.repeat(40), { fetch: f.fetch }),
    /differs from the frozen head/);
  assert.equal(f.fetches(), 0);
}));

test('RejectsMalformedFrozenHeads', () => {
  for (const head of ['main', 'a'.repeat(39), 'A'.repeat(40), 'a'.repeat(40) + ';']) {
    assert.throws(() => prepareValidationDependencies('unused', head), /full frozen validation head/);
  }
});

test('RejectsARegularDirectoryAtTheGitlinkPath', () => fixture(f => {
  const destination = path.join(f.worktree, modulePath);
  fs.mkdirSync(destination, { recursive: true });
  fs.writeFileSync(path.join(destination, 'source.txt'), 'not a gitlink\n');
  f.git(f.worktree, 'update-index', '--force-remove', modulePath);
  f.git(f.worktree, 'add', '.');
  f.git(f.worktree, 'commit', '--quiet', '-m', 'Replace gitlink with regular source');
  assert.throws(() => prepareValidationDependencies(f.worktree,
    f.git(f.worktree, 'rev-parse', 'HEAD'), { fetch: f.fetch }), /frozen gitlink/);
  assert.equal(f.fetches(), 0);
}));

test('PreservesAnAlreadyPopulatedDependencyDirectory', () => fixture(f => {
  const destination = path.join(f.worktree, modulePath);
  fs.mkdirSync(destination, { recursive: true });
  fs.writeFileSync(path.join(destination, 'keep.txt'), 'user content\n');
  assert.throws(() => prepareValidationDependencies(f.worktree, f.head, { fetch: f.fetch }), /must be empty/);
  assert.equal(fs.readFileSync(path.join(destination, 'keep.txt'), 'utf8'), 'user content\n');
  assert.equal(f.fetches(), 0);
}));

test('RejectsASymlinkedParentBeforeFetching', () => fixture(f => {
  const outside = path.join(f.root, 'outside');
  fs.mkdirSync(outside);
  fs.rmdirSync(path.join(f.worktree, modulePath));
  fs.rmdirSync(path.join(f.worktree, 'src', 'submodules'));
  fs.symlinkSync(outside, path.join(f.worktree, 'src', 'submodules'),
    process.platform === 'win32' ? 'junction' : 'dir');
  assert.throws(() => prepareValidationDependencies(f.worktree, f.head, { fetch: f.fetch }), /symlink/);
  assert.deepEqual(fs.readdirSync(outside), []);
  assert.equal(f.fetches(), 0);
}));

test('AcceptsAlreadyMaterializedCleanDependencyWithoutRefetching', () => fixture(f => {
  const first = prepareValidationDependencies(f.worktree, f.head, { fetch: f.fetch });
  assert.equal(f.fetches(), 1);
  const second = prepareValidationDependencies(f.worktree, f.head, {
    fetch: () => { throw new Error('must not refetch an already-materialized dependency'); },
  });
  assert.deepEqual(second, first);
  assert.equal(f.fetches(), 1);
}));

test('RejectsAlreadyMaterializedDependencyWhenDirtyOrWrongCommit', () => fixture(f => {
  prepareValidationDependencies(f.worktree, f.head, { fetch: f.fetch });
  const destination = path.join(f.worktree, modulePath);
  fs.writeFileSync(path.join(destination, 'untracked.txt'), 'dirty\n');
  assert.throws(() => prepareValidationDependencies(f.worktree, f.head, { fetch: f.fetch }),
    /must be empty or match the clean frozen gitlink/);
  fs.rmSync(path.join(destination, 'untracked.txt'));
  fs.writeFileSync(path.join(destination, 'source.txt'), 'wrong commit\n');
  f.git(destination, 'add', '.');
  f.git(destination, 'commit', '--quiet', '-m', 'Advance dependency commit');
  assert.throws(() => prepareValidationDependencies(f.worktree, f.head, { fetch: f.fetch }),
    /must be empty or match the clean frozen gitlink/);
}));

test('PropagatesDependencyAcquisitionFailureAndLeavesDestinationCleanForRetry', () => fixture(f => {
  assert.throws(() => prepareValidationDependencies(f.worktree, f.head, {
    fetch: () => { throw new Error('fixture acquisition failed'); },
  }), /fixture acquisition failed/);
  assert.deepEqual(fs.readdirSync(path.join(f.worktree, modulePath)), []);
  assert.equal(f.git(f.worktree, 'rev-parse', 'HEAD'), f.head);
  const retried = prepareValidationDependencies(f.worktree, f.head, { fetch: f.fetch });
  assert.equal(retried.length, 1);
}));

test('AdapterLoadsTheTrustedHelperAndRetainsSourceReviewOnPreparationFailure', () => {
  const root = path.resolve(import.meta.dirname, '../../..');
  for (const name of ['pull-request-review.md', 'pull-request-review.lock.yml']) {
    let source = fs.readFileSync(path.join(root, '.github', 'workflows', name), 'utf8');
    if (name.endsWith('.lock.yml')) {
      const step = source.split(/\r?\n/).find(line =>
        line.trimStart().startsWith('run: "') && line.includes('prepare-validation-dependencies.mjs'));
      assert.ok(step, 'The compiled preparation step must contain the trusted dependency helper.');
      source = JSON.parse(step.trim().slice('run: '.length));
    }
    assert.match(source, /for filename in prepare-review\.cs prepare-validation-dependencies\.mjs run-targeted-validation\.mjs/);
    assert.match(source, /cp "\$producer_dir\/prepare-validation-dependencies\.mjs"/);
    assert.match(source, /node "\$producer_dir\/prepare-validation-dependencies\.mjs" "\$worktree" "\$REVIEW_HEAD" &&/);
    assert.match(source, /optional validation worktree could not be prepared; source review will continue/);
  }
});

test('ParseValidationArgsValidatesRequiredFlagsAndSelectors', () => {
  const head = 'b'.repeat(40);
  const parsed = parseValidationArgs([
    '--worktree', '/tmp/gh-aw/review-worktree',
    '--head', head,
    '--project', 'src/Components/Components/test/Microsoft.AspNetCore.Components.Tests.csproj',
    '--class', 'Microsoft.AspNetCore.Components.RenderTree.RenderBatchWriterTest',
    '--method', 'Microsoft.AspNetCore.Components.RenderTree.RenderBatchWriterTest.WritesEmptyBatch',
    '--managed-only',
  ]);
  assert.deepEqual(parsed, {
    worktree: '/tmp/gh-aw/review-worktree',
    head,
    project: 'src/Components/Components/test/Microsoft.AspNetCore.Components.Tests.csproj',
    classes: ['Microsoft.AspNetCore.Components.RenderTree.RenderBatchWriterTest'],
    methods: ['Microsoft.AspNetCore.Components.RenderTree.RenderBatchWriterTest.WritesEmptyBatch'],
    managedOnly: true,
  });
  assert.throws(() => parseValidationArgs(['--worktree', '/tmp/w', '--head', head, '--project', 'a.csproj']),
    /Specify at least one --class or --method test selector/);
  assert.throws(() => parseValidationArgs([
    '--worktree', '/tmp/w', '--head', head, '--project', 'a.csproj', '--class', 'Bad Class;rm',
  ]), /Invalid test selector/);
  assert.throws(() => parseValidationArgs([
    '--worktree', '/tmp/w', '--head', head, '--project', 'a.csproj', '--class', 'Foo`id`Bar',
  ]), /Invalid test selector/);
  assert.throws(() => parseValidationArgs([
    '--worktree', '/tmp/w', '--head', head, '--project', 'a.csproj', '--class', 'Foo.Test', '--unknown',
  ]), /Unsupported argument/);
});

function validationFixture(action) {
  fixture(f => {
    const projectRel = 'src/Components/Components/test/Microsoft.AspNetCore.Components.Tests.csproj';
    const projectFull = path.join(f.worktree, projectRel);
    fs.mkdirSync(path.dirname(projectFull), { recursive: true });
    fs.writeFileSync(projectFull, '<Project />\n');
    const componentsTargets = path.join(
      f.worktree,
      'src/Components/Directory.Build.targets'
    );
    fs.mkdirSync(path.dirname(componentsTargets), { recursive: true });
    fs.writeFileSync(componentsTargets, '<Project><PropertyGroup><IncludeComponentsWebAssets>true</IncludeComponentsWebAssets></PropertyGroup></Project>\n');
    const generateFilesProd = path.join(
      f.worktree,
      'eng/tools/GenerateFiles/GenerateFiles.csproj'
    );
    fs.mkdirSync(path.dirname(generateFilesProd), { recursive: true });
    fs.writeFileSync(generateFilesProd, '<Project />\n');
    const genDir = path.join(f.worktree, 'artifacts', 'bin', 'GenerateFiles');
    fs.mkdirSync(genDir, { recursive: true });
    fs.writeFileSync(path.join(genDir, 'Directory.Build.props'), '<Project />\n');
    fs.writeFileSync(path.join(genDir, 'Directory.Build.targets'), '<Project />\n');
    f.git(f.worktree, 'add', '.');
    f.git(f.worktree, 'commit', '--quiet', '-m', 'Add fixture test project');
    const head = f.git(f.worktree, 'rev-parse', 'HEAD');
    prepareValidationDependencies(f.worktree, head, { fetch: f.fetch });
    action({ ...f, head, projectRel, projectFull, genDir });
  });
}

function writeXunitXml(worktree, projectBaseName, content) {
  const resultsDir = path.join(worktree, 'artifacts', 'TestResults', 'Debug');
  fs.mkdirSync(resultsDir, { recursive: true });
  const xmlPath = path.join(resultsDir, `${projectBaseName}_net11.0_x64.xml`);
  fs.writeFileSync(xmlPath, content);
  return xmlPath;
}

test('RunTargetedValidationBuildsAndVerifiesExecutedXunitSelectors', () => validationFixture(f => {
  const calls = [];
  const spawn = (program, args, options) => {
    if (program === 'git') {
      const stdout = execFileSync(program, args, { ...options, encoding: 'utf8' });
      return { status: 0, stdout, stderr: '' };
    }
    calls.push({ program, args, env: options.env });
    if (args.includes('--test')) {
      writeXunitXml(
        f.worktree,
        'Microsoft.AspNetCore.Components.Tests',
        `<assemblies><assembly total="8" passed="8" failed="0" skipped="0">` +
          `<collection><test type="Microsoft.AspNetCore.Components.RenderTree.RenderBatchWriterTest" ` +
          `name="Microsoft.AspNetCore.Components.RenderTree.RenderBatchWriterTest.WritesEmptyBatch" result="Pass" />` +
          `</collection></assembly></assemblies>\n`
      );
    }
    return { status: 0, stdout: 'Build succeeded.\n', stderr: '' };
  };

  const receipt = runTargetedValidation({
    worktree: f.worktree,
    head: f.head,
    project: f.projectRel,
    classes: ['Microsoft.AspNetCore.Components.RenderTree.RenderBatchWriterTest'],
    methods: [],
    managedOnly: true,
  }, { platform: 'linux', spawn });

  assert.equal(receipt.status, 'passed');
  assert.equal(receipt.exitCode, 0);
  assert.equal(receipt.managedOnly, true);
  assert.equal(receipt.phases.length, 2);
  assert.deepEqual(calls[0].args, [
    './eng/build.sh',
    '--projects',
    fs.realpathSync(f.projectFull),
    '--configuration',
    'Debug',
    '-p:BuildNodeJS=false',
    '-p:IncludeComponentsWebAssets=false',
    '--msbuildMultiThreaded',
    'false',
    '--restore',
  ]);
  assert.deepEqual(calls[1].args, [
    './eng/build.sh',
    '--projects',
    fs.realpathSync(f.projectFull),
    '--configuration',
    'Debug',
    '-p:BuildNodeJS=false',
    '-p:IncludeComponentsWebAssets=false',
    '--msbuildMultiThreaded',
    'false',
    '--no-restore',
    '--no-build',
    '--test',
  ]);
  assert.equal(
    calls[1].env.TestRunnerAdditionalArguments,
    '-class Microsoft.AspNetCore.Components.RenderTree.RenderBatchWriterTest'
  );
  assert.deepEqual(receipt.tests, {
    total: 8,
    passed: 8,
    failed: 0,
    skipped: 0,
    executed: 8,
    resultFiles: [
      'artifacts/TestResults/Debug/Microsoft.AspNetCore.Components.Tests_net11.0_x64.xml',
    ],
  });
}));

test('RunTargetedValidationRunsGenerateFilesFallbackWhenGeneratedPropsAreMissing', () => validationFixture(f => {
  fs.rmSync(f.genDir, { recursive: true, force: true });
  const phases = [];
  const spawn = (program, args, options) => {
    if (program === 'git') {
      const stdout = execFileSync(program, args, { ...options, encoding: 'utf8' });
      return { status: 0, stdout, stderr: '' };
    }
    phases.push({ program, args });
    if (args.some(arg => arg.includes('GenerateDirectoryBuildFiles'))) {
      fs.mkdirSync(f.genDir, { recursive: true });
      fs.writeFileSync(path.join(f.genDir, 'Directory.Build.props'), '<Project />\n');
      fs.writeFileSync(path.join(f.genDir, 'Directory.Build.targets'), '<Project />\n');
    }
    if (args.includes('--test')) {
      writeXunitXml(
        f.worktree,
        'Microsoft.AspNetCore.Components.Tests',
        `<assemblies><assembly total="1" passed="1" failed="0" skipped="0">` +
          `<collection><test type="MyNamespace.MyTest" name="MyNamespace.MyTest.Fact1" result="Pass" />` +
          `</collection></assembly></assemblies>\n`
      );
    }
    return { status: 0, stdout: 'ok\n', stderr: '' };
  };

  const receipt = runTargetedValidation({
    worktree: f.worktree,
    head: f.head,
    project: f.projectRel,
    classes: ['MyNamespace.MyTest'],
    methods: [],
    managedOnly: false,
  }, { platform: 'linux', spawn });

  assert.equal(receipt.status, 'passed');
  assert.equal(receipt.generatedInputsFallback, true);
  assert.deepEqual(receipt.phases.map(cmd => cmd.name), [
    'bootstrap',
    'generate-files-restore',
    'generate-files',
    'bootstrap-retry',
    'test',
  ]);
}));

test('RunTargetedValidationFailsClosedWhenZeroTestsExecuteOrSelectorMissesOrStaleXmlExists', () => validationFixture(f => {
  // Pre-seed a stale XML file from an earlier run to ensure it is purged before the test phase.
  writeXunitXml(
    f.worktree,
    'Microsoft.AspNetCore.Components.Tests',
    `<assemblies><assembly total="5" passed="5" failed="0" skipped="0">` +
      `<collection><test type="MyNamespace.MyTest" name="MyNamespace.MyTest.StaleFact" result="Pass" />` +
      `</collection></assembly></assemblies>\n`
  );

  let mode = 'no-xml';
  const spawn = (program, args, options) => {
    if (program === 'git') {
      const stdout = execFileSync(program, args, { ...options, encoding: 'utf8' });
      return { status: 0, stdout, stderr: '' };
    }
    if (args.includes('--test')) {
      if (mode === 'zero-executed') {
        writeXunitXml(
          f.worktree,
          'Microsoft.AspNetCore.Components.Tests',
          `<assemblies><assembly total="0" passed="0" failed="0" skipped="0" /></assemblies>\n`
        );
      } else if (mode === 'wrong-selector') {
        writeXunitXml(
          f.worktree,
          'Microsoft.AspNetCore.Components.Tests',
          `<assemblies><assembly total="1" passed="1" failed="0" skipped="0">` +
            `<collection><test type="OtherNamespace.OtherTest" name="OtherNamespace.OtherTest.Fact1" result="Pass" />` +
            `</collection></assembly></assemblies>\n`
        );
      } else if (mode === 'misleading-display-name') {
        writeXunitXml(
          f.worktree,
          'Microsoft.AspNetCore.Components.Tests',
          `<assemblies><assembly total="1" passed="1" failed="0" skipped="0">` +
            `<collection><test type="OtherNamespace.OtherTest" name="MyNamespace.MyTest.FakeDisplayName" result="Pass" />` +
            `</collection></assembly></assemblies>\n`
        );
      } else if (mode === 'skipped-requested-selector') {
        writeXunitXml(
          f.worktree,
          'Microsoft.AspNetCore.Components.Tests',
          `<assemblies><assembly total="2" passed="1" failed="0" skipped="1">` +
            `<collection>` +
            `<test type="MyNamespace.MyTest" method="Fact1" name="MyNamespace.MyTest.Fact1" result="Pass" />` +
            `<test type="MyNamespace.MyTest" method="Fact2" name="MyNamespace.MyTest.Fact2" result="Skip" />` +
            `</collection></assembly></assemblies>\n`
        );
      }
    }
    return { status: 0, stdout: 'Exit 0 without matching tests\n', stderr: '' };
  };

  for (const currentMode of [
    'no-xml',
    'zero-executed',
    'wrong-selector',
    'misleading-display-name',
    'skipped-requested-selector',
  ]) {
    mode = currentMode;
    const receipt = runTargetedValidation({
      worktree: f.worktree,
      head: f.head,
      project: f.projectRel,
      classes: ['MyNamespace.MyTest'],
      methods: currentMode === 'skipped-requested-selector'
        ? ['MyNamespace.MyTest.Fact1', 'MyNamespace.MyTest.Fact2']
        : [],
      managedOnly: true,
    }, { platform: 'linux', spawn });
    assert.equal(receipt.status, 'no_tests_executed', `Expected no_tests_executed for mode ${currentMode}`);
    assert.equal(receipt.exitCode, 2);
  }
}));

test('RunTargetedValidationRejectsProjectEscapeAndHeadMismatch', () => validationFixture(f => {
  assert.throws(() => runTargetedValidation({
    worktree: f.worktree,
    head: f.head,
    project: '../outside.csproj',
    classes: ['MyNamespace.MyTest'],
  }), /Project path must stay within the validation worktree/);

  assert.throws(() => runTargetedValidation({
    worktree: f.worktree,
    head: 'c'.repeat(40),
    project: f.projectRel,
    classes: ['MyNamespace.MyTest'],
  }), /differs from the frozen head/);
}));

test('RunTargetedValidationHandlesWindowsCommandsBuildFailuresAndFailingTests', () => validationFixture(f => {
  // 1. Build failure stops before test phase.
  let testPhaseRan = false;
  const buildFailedReceipt = runTargetedValidation({
    worktree: f.worktree,
    head: f.head,
    project: f.projectRel,
    classes: ['MyNamespace.MyTest'],
    methods: [],
    managedOnly: true,
  }, {
    platform: 'win32',
    spawn: (program, args, options) => {
      if (program === 'git') {
        const stdout = execFileSync(program, args, { ...options, encoding: 'utf8' });
        return { status: 0, stdout, stderr: '' };
      }
      if (args.includes('-test')) {
        testPhaseRan = true;
      }
      return { status: 1, stdout: '', stderr: 'Build failed.\n' };
    },
  });
  assert.equal(buildFailedReceipt.status, 'build_failed');
  assert.equal(buildFailedReceipt.exitCode, 2);
  assert.equal(testPhaseRan, false);
  assert.equal(buildFailedReceipt.phases[0].stderrTail, 'Build failed.');
  assert.deepEqual(buildFailedReceipt.phases[0].command, [
    'cmd.exe',
    '/d',
    '/s',
    '/c',
    'eng\\build.cmd',
    '-projects',
    fs.realpathSync(f.projectFull),
    '-configuration',
    'Debug',
    '/p:BuildNodeJS=false',
    '/p:IncludeComponentsWebAssets=false',
    '/p:BuildInParallel=false',
    '-restore',
  ]);

  // 2. Failing xUnit test returns status: 'failed' and exitCode: 1.
  const failedTestReceipt = runTargetedValidation({
    worktree: f.worktree,
    head: f.head,
    project: f.projectRel,
    classes: ['MyNamespace.MyTest'],
    methods: ['MyNamespace.MyTest.FailingFact'],
    managedOnly: true,
  }, {
    platform: 'win32',
    spawn: (program, args, options) => {
      if (program === 'git') {
        const stdout = execFileSync(program, args, { ...options, encoding: 'utf8' });
        return { status: 0, stdout, stderr: '' };
      }
      if (args.includes('-test')) {
        writeXunitXml(
          f.worktree,
          'Microsoft.AspNetCore.Components.Tests',
          `<assemblies><assembly total="1" passed="0" failed="1" skipped="0">` +
            `<collection><test type="MyNamespace.MyTest" name="MyNamespace.MyTest.FailingFact" result="Fail" />` +
            `</collection></assembly></assemblies>\n`
        );
        return { status: 1, stdout: '1 failed\n', stderr: '' };
      }
      return { status: 0, stdout: 'Build succeeded.\n', stderr: '' };
    },
  });
  assert.equal(failedTestReceipt.status, 'failed');
  assert.equal(failedTestReceipt.exitCode, 1);
  assert.equal(failedTestReceipt.tests.failed, 1);
  assert.equal(failedTestReceipt.phases[1].stdoutTail, '1 failed');
  assert.deepEqual(failedTestReceipt.phases[1].command, [
    'cmd.exe',
    '/d',
    '/s',
    '/c',
    'eng\\build.cmd',
    '-projects',
    fs.realpathSync(f.projectFull),
    '-configuration',
    'Debug',
    '/p:BuildNodeJS=false',
    '/p:IncludeComponentsWebAssets=false',
    '/p:BuildInParallel=false',
    '-noRestore',
    '-noBuild',
    '-test',
  ]);
}));
