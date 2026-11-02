import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import test from 'node:test';
import { prepareValidationDependencies } from '../../skills/review-pull-request/scripts/prepare-validation-dependencies.mjs';

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

test('PropagatesDependencyAcquisitionFailure', () => fixture(f => {
  assert.throws(() => prepareValidationDependencies(f.worktree, f.head, {
    fetch: () => { throw new Error('fixture acquisition failed'); },
  }), /fixture acquisition failed/);
  assert.equal(f.git(f.worktree, 'rev-parse', 'HEAD'), f.head);
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
    assert.match(source, /for filename in prepare-review\.cs prepare-validation-dependencies\.mjs/);
    assert.match(source, /node "\$producer_dir\/prepare-validation-dependencies\.mjs" "\$worktree" "\$REVIEW_HEAD" &&/);
    assert.match(source, /optional validation worktree could not be prepared; source review will continue/);
  }
});
