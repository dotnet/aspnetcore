import assert from "node:assert/strict";
import test from "node:test";

import {
  ASSISTANCE_DISCLOSURE,
  CANVAS_CREATED_COMMENT_KIND,
  createPublicationPreview,
  createPublicationService,
  hashPublicationBody,
  validatePendingAttempt,
  validatePublicationCitations,
  validatePublicationBody,
} from "./publication.mjs";

const issue = {
  repository: "dotnet/aspnetcore",
  number: 123,
  url: "https://github.com/dotnet/aspnetcore/issues/123",
  state: "open",
  public: true,
  isPullRequest: false,
  updatedAt: "issue-revision-1",
};
const account = { login: "maintainer", id: 42, type: "User" };
const body = [
  "# Issue investigation: dotnet/aspnetcore#123 — Example",
  "",
  ASSISTANCE_DISCLOSURE,
  "",
  "**Classification:** Research",
  "**Classification reason:** Public evidence is sufficient.",
  "**Preliminary assessment:** Likely product bug",
  "**Disposition:** Non-binding; maintainers own final disposition.",
  "**Source:** main at aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
  "**Retrieval:** Public GitHub",
  "**Reproduction role:** Not required for the current assessment",
  "",
  "## Conclusion",
  "",
  "The evidence supports the reported behavior.",
  "",
  "## Recommended next action",
  "",
  "**One action:** Maintainers should review the linked evidence.",
].join("\n");
const updatedBody = body.replace(
  "The evidence supports the reported behavior.",
  "The evidence strongly supports the reported behavior.",
);

function adapterFixture(overrides = {}) {
  const calls = [];
  const comment = {
    id: 987,
    repository: issue.repository,
    issueNumber: issue.number,
    author: account.login,
    authorId: account.id,
    html_url: "https://github.com/dotnet/aspnetcore/issues/123#issuecomment-987",
    body,
    updatedAt: "comment-revision-1",
  };
  return {
    calls,
    issue: { ...issue },
    account: { ...account },
    comment,
    async readIssue(number) {
      calls.push(["readIssue", number]);
      return this.issue;
    },
    async readAccount() {
      calls.push(["readAccount"]);
      return this.account;
    },
    async readComment(query) {
      calls.push(["readComment", query]);
      return this.comment;
    },
    async createComment(input) {
      calls.push(["createComment", input]);
      return {
        id: 987,
        repository: issue.repository,
        issueNumber: issue.number,
        author: account.login,
        authorId: account.id,
        html_url: "https://github.com/dotnet/aspnetcore/issues/123#issuecomment-987",
        body: input.body,
        updatedAt: "comment-revision-1",
      };
    },
    async updateComment(input) {
      calls.push(["updateComment", input]);
      return {
        id: input.commentId,
        repository: issue.repository,
        issueNumber: issue.number,
        author: account.login,
        authorId: account.id,
        html_url: "https://github.com/dotnet/aspnetcore/issues/123#issuecomment-987",
        body: input.body,
        updatedAt: "comment-revision-2",
      };
    },
    ...overrides,
  };
}

function preview(overrides = {}) {
  return createPublicationPreview({
    issue,
    account,
    approvedBody: body,
    draftRevision: 1,
    createConfirmationToken: () => "human-confirmation-token",
    ...overrides,
  });
}

test("preview is pure and create publishes the exact approved Markdown body", async () => {
  const adapter = adapterFixture();
  const service = createPublicationService(adapter, {
    createAttemptId: () => "attempt-1",
  });
  const createdPreview = preview();

  assert.equal(adapter.calls.length, 0);
  const result = await service.publish(createdPreview, createdPreview.confirmationToken);

  assert.equal(result.status, "published");
  assert.equal(result.operation, "create");
  assert.deepEqual(adapter.calls.find(([name]) => name === "createComment"), [
    "createComment",
    {
      repository: "dotnet/aspnetcore",
      issueNumber: 123,
      body,
    },
  ]);
  assert.equal(result.recordedComment.kind, CANVAS_CREATED_COMMENT_KIND);
  assert.equal(result.recordedComment.bodyHash, hashPublicationBody(body));
});

test("update publishes only a recorded canvas-created comment", async () => {
  const adapter = adapterFixture();
  const recordedComment = {
    kind: CANVAS_CREATED_COMMENT_KIND,
    id: 987,
    repository: "dotnet/aspnetcore",
    issueNumber: 123,
    accountLogin: "maintainer",
    accountId: account.id,
    authorId: account.id,
    bodyHash: hashPublicationBody(body),
    body,
    htmlUrl: "https://github.com/dotnet/aspnetcore/issues/123#issuecomment-987",
    issueRevision: issue.updatedAt,
  };
  const service = createPublicationService(adapter);
  const updatePreview = preview({ approvedBody: updatedBody, recordedComment });

  const result = await service.publish(updatePreview, updatePreview.confirmationToken);

  assert.equal(result.operation, "update");
  assert.deepEqual(adapter.calls.find(([name]) => name === "updateComment"), [
    "updateComment",
    {
      repository: "dotnet/aspnetcore",
      issueNumber: 123,
      commentId: 987,
      body: updatedBody,
    },
  ]);
  assert.equal(adapter.calls.some(([name]) => name === "createComment"), false);
});

test("confirmation binds token, account, issue revision, and exact body hash", async () => {
  const createdPreview = preview();
  await assert.rejects(
    createPublicationService(adapterFixture()).publish(createdPreview, "wrong-token"),
    (error) => error.code === "confirmation_required",
  );
  const staleAdapter = adapterFixture({
    issue: { ...issue, updatedAt: "issue-revision-2" },
  });
  await assert.rejects(
    createPublicationService(staleAdapter).publish(createdPreview, createdPreview.confirmationToken),
    (error) => error.code === "stale_issue_revision",
  );
  const changedAccountAdapter = adapterFixture({
    account: { ...account, login: "different-maintainer" },
  });
  await assert.rejects(
    createPublicationService(changedAccountAdapter).publish(
      createdPreview,
      createdPreview.confirmationToken,
    ),
    (error) => error.code === "account_changed",
  );
  assert.throws(
    () => validatePublicationBody(`${body}\nconfidential`),
    (error) => error.code === "publication_confidential_marker",
  );
});

test("stale, external, and deleted recorded comments fail closed", async () => {
  const recordedComment = {
    kind: CANVAS_CREATED_COMMENT_KIND,
    id: 987,
    repository: "dotnet/aspnetcore",
    issueNumber: 123,
    accountLogin: "maintainer",
    accountId: account.id,
    authorId: account.id,
    bodyHash: hashPublicationBody(body),
    body,
    htmlUrl: "https://github.com/dotnet/aspnetcore/issues/123#issuecomment-987",
    issueRevision: issue.updatedAt,
  };
  for (const [name, comment, code] of [
    ["stale", { id: 987, repository: issue.repository, issueNumber: 123, author: "maintainer", authorId: account.id, html_url: "https://github.com/dotnet/aspnetcore/issues/123#issuecomment-987", body: `${body}\nchanged` }, "recorded_comment_stale"],
    ["external", { id: 987, repository: issue.repository, issueNumber: 123, author: "someone-else", authorId: 99, html_url: "https://github.com/dotnet/aspnetcore/issues/123#issuecomment-987", body }, "external_comment"],
    ["deleted", null, "recorded_comment_deleted"],
  ]) {
    const adapter = adapterFixture({ comment });
    const service = createPublicationService(adapter);
    const updatePreview = preview({ recordedComment });
    await assert.rejects(
      service.publish(updatePreview, updatePreview.confirmationToken),
      (error) => error.code === code,
      name,
    );
    assert.equal(adapter.calls.some(([call]) => call === "updateComment"), false);
  }
});

test("arbitrary comment IDs are rejected without a canvas-created binding", () => {
  assert.throws(
    () => preview({ recordedComment: { id: 987 } }),
    (error) => error.code === "recorded_comment_invalid",
  );
  assert.throws(
    () => preview({ commentId: 987 }),
    (error) => error.code === "recorded_comment_invalid",
  );
});

test("double-clicks serialize and do not create duplicate comments", async () => {
  let release;
  const gate = new Promise((resolve) => {
    release = resolve;
  });
  let creates = 0;
  const adapter = adapterFixture({
    async createComment(input) {
      creates += 1;
      await gate;
      return {
        id: 987,
        repository: issue.repository,
        issueNumber: issue.number,
        author: account.login,
        authorId: account.id,
        html_url: "https://github.com/dotnet/aspnetcore/issues/123#issuecomment-987",
        body: input.body,
      };
    },
  });
  const service = createPublicationService(adapter);
  const createdPreview = preview();
  const first = service.publish(createdPreview, createdPreview.confirmationToken);
  const second = service.publish(createdPreview, createdPreview.confirmationToken);
  await new Promise((resolve) => setImmediate(resolve));
  assert.equal(creates, 1);
  release();
  const [firstResult, secondResult] = await Promise.all([first, second]);
  assert.equal(firstResult.status, "published");
  assert.strictEqual(secondResult, firstResult);
});

test("ambiguous writes stay pending and are never retried automatically", async () => {
  let writes = 0;
  const adapter = adapterFixture({
    async createComment() {
      writes += 1;
      const error = new Error("network timeout");
      error.ambiguous = true;
      throw error;
    },
  });
  const service = createPublicationService(adapter, {
    createAttemptId: () => "ambiguous-attempt",
  });
  const createdPreview = preview();
  const result = await service.publish(createdPreview, createdPreview.confirmationToken);

  assert.deepEqual(result, {
    status: "unknown",
    retryAllowed: false,
    issueNumber: 123,
    operation: "create",
    attemptId: "ambiguous-attempt",
    message: "The write outcome is unknown. Resolve the pending attempt by reading GitHub; do not retry.",
    pendingIssueKey: "dotnet/aspnetcore#123",
  });
  await assert.rejects(
    service.publish(createdPreview, createdPreview.confirmationToken),
    (error) => error.code === "publication_pending",
  );
  assert.equal(writes, 1);
});

test("malformed create acknowledgement remains pending and never sends a second POST", async () => {
  let writes = 0;
  const adapter = adapterFixture({
    async createComment() {
      writes += 1;
      return { id: 987 };
    },
  });
  const service = createPublicationService(adapter, {
    createAttemptId: () => "malformed-create-attempt",
  });
  const createdPreview = preview();
  const result = await service.publish(createdPreview, createdPreview.confirmationToken);

  assert.equal(result.status, "unknown");
  assert.equal(result.retryAllowed, false);
  await assert.rejects(
    service.publish(createdPreview, createdPreview.confirmationToken),
    (error) => error.code === "publication_pending",
  );
  assert.equal(writes, 1);
});

test("optional durable attempt hook records pending and terminal publication state", async () => {
  const attempts = [];
  const adapter = adapterFixture();
  const service = createPublicationService(adapter, {
    createAttemptId: () => "durable-attempt",
    recordAttempt: async (record) => attempts.push(record),
  });
  const createdPreview = preview();

  await service.publish(createdPreview, createdPreview.confirmationToken);

  assert.deepEqual(attempts.map((record) => record.status), ["pending", "published"]);
  assert.equal(attempts[0].attemptId, "durable-attempt");
  assert.equal(attempts[0].bodyHash, createdPreview.bodyHash);
  assert.equal(attempts[1].recordedComment.html_url, adapter.comment.html_url);
});

test("receipt persistence failure is visible without retrying a published write", async () => {
  let writes = 0;
  const adapter = adapterFixture({
    async createComment(input) {
      writes += 1;
      return {
        id: 987,
        repository: issue.repository,
        issueNumber: issue.number,
        author: account.login,
        authorId: account.id,
        html_url: "https://github.com/dotnet/aspnetcore/issues/123#issuecomment-987",
        body: input.body,
        updatedAt: "comment-revision-1",
      };
    },
  });
  const service = createPublicationService(adapter, {
    recordAttempt: async (record) => {
      if (record.status === "published") {
        throw new Error("artifact store unavailable");
      }
    },
  });
  const createdPreview = preview();
  const result = await service.publish(createdPreview, createdPreview.confirmationToken);

  assert.equal(result.status, "published");
  assert.equal(result.persistenceWarning.code, "publication_attempt_persist_failed");
  assert.match(result.persistenceWarning.message, /local receipt could not be saved/);
  assert.equal(writes, 1);
  const replay = await service.publish(createdPreview, createdPreview.confirmationToken);
  assert.equal(replay.status, "published");
  assert.equal(writes, 1);
});

test("durable unknown attempts can be restored and resolved without another POST", async () => {
  const durable = [];
  let writes = 0;
  const adapter = adapterFixture({
    async createComment() {
      writes += 1;
      return { id: 987 };
    },
  });
  const firstService = createPublicationService(adapter, {
    createAttemptId: () => "restored-attempt",
    recordAttempt: async (record) => durable.push(record),
  });
  const createdPreview = preview();
  const unknown = await firstService.publish(createdPreview, createdPreview.confirmationToken);
  assert.equal(unknown.status, "unknown");
  const unknownRecord = durable.find((record) => record.status === "unknown");
  assert.ok(unknownRecord);

  const secondService = createPublicationService(adapter, {
    initialAttempts: [unknownRecord],
  });
  await assert.rejects(
    secondService.publish(createdPreview, createdPreview.confirmationToken),
    (error) => error.code === "publication_pending",
  );
  const recovered = await secondService.resolvePending(createdPreview);

  assert.equal(recovered.status, "published");
  assert.equal(writes, 1);
  assert.equal(secondService.getPendingAttempt(123), null);
  assert.deepEqual(validatePendingAttempt(unknownRecord).preview, createdPreview);
});

test("initial durable attempts are validated before the service can publish", () => {
  assert.throws(
    () => createPublicationService(adapterFixture(), {
      initialAttempts: [{ status: "unknown", attemptId: "invalid" }],
    }),
    (error) => error.code === "publication_attempt_invalid",
  );
  assert.throws(
    () => createPublicationService(adapterFixture(), {
      initialAttempts: {},
    }),
    (error) => error.code === "publication_attempts_invalid",
  );
});

test("a successful PATCH with lost acknowledgement recovers the pending new body", async () => {
  const adapter = adapterFixture();
  const recordedComment = {
    kind: CANVAS_CREATED_COMMENT_KIND,
    id: 987,
    repository: "dotnet/aspnetcore",
    issueNumber: 123,
    accountLogin: "maintainer",
    accountId: account.id,
    authorId: account.id,
    bodyHash: hashPublicationBody(body),
    body,
    htmlUrl: "https://github.com/dotnet/aspnetcore/issues/123#issuecomment-987",
    issueRevision: issue.updatedAt,
  };
  const updatePreview = preview({ approvedBody: updatedBody, recordedComment });
  const originalUpdate = adapter.updateComment;
  adapter.updateComment = async (input) => {
    const response = await originalUpdate(input);
    adapter.comment = {
      ...adapter.comment,
      body: updatedBody,
      updatedAt: "comment-revision-2",
    };
    adapter.issue = { ...adapter.issue, updatedAt: "issue-revision-after-patch" };
    const error = new Error("PATCH acknowledgement lost");
    error.ambiguous = true;
    throw error;
  };
  const service = createPublicationService(adapter, {
    createAttemptId: () => "lost-patch-attempt",
  });

  const unknown = await service.publish(updatePreview, updatePreview.confirmationToken);
  const recovered = await service.resolvePending(updatePreview);

  assert.equal(unknown.status, "unknown");
  assert.equal(recovered.status, "published");
  assert.equal(recovered.recovered, true);
  assert.equal(recovered.body, updatedBody);
  assert.equal(recovered.bodyHash, hashPublicationBody(updatedBody));
  assert.equal(adapter.calls.filter(([name]) => name === "updateComment").length, 1);
});

test("an ambiguous create can only be recovered by a read, never by another write", async () => {
  let writes = 0;
  const adapter = adapterFixture({
    async createComment() {
      writes += 1;
      const error = new Error("request timed out");
      error.ambiguous = true;
      throw error;
    },
  });
  const service = createPublicationService(adapter, {
    createAttemptId: () => "recoverable-attempt",
  });
  const createdPreview = preview();
  await service.publish(createdPreview, createdPreview.confirmationToken);

  const recovered = await service.resolvePending(createdPreview);

  assert.equal(recovered.status, "published");
  assert.equal(recovered.recovered, true);
  assert.equal(writes, 1);
  assert.equal(adapter.calls.some(([name]) => name === "updateComment"), false);
});

test("unresolved stop paths cannot be changed into publishable classifications", () => {
  assert.throws(
    () => createPublicationPreview({
      issue,
      account,
      report: { content: body, classification: "Do not publish" },
      createConfirmationToken: () => "token",
    }),
    (error) => error.code === "publication_classification_mismatch",
  );
  assert.throws(
    () => validatePublicationBody(body.replace("Research", "Do not publish")),
    (error) => error.code === "publication_stop_path",
  );
  assert.throws(
    () => validatePublicationBody(`${body}\nlocal diagnostic artifact`),
    (error) => error.code === "publication_confidential_marker",
  );
});

test("public issue and account flags are explicit and bound to numeric /user identity", () => {
  assert.throws(
    () => createPublicationPreview({
      issue: { ...issue, public: undefined },
      account,
      approvedBody: body,
      createConfirmationToken: () => "token",
    }),
    (error) => error.code === "canonical_issue_invalid",
  );
  assert.throws(
    () => createPublicationPreview({
      issue: { ...issue, isPullRequest: undefined },
      account,
      approvedBody: body,
      createConfirmationToken: () => "token",
    }),
    (error) => error.code === "canonical_issue_invalid",
  );
  assert.throws(
    () => createPublicationPreview({
      issue,
      account: { ...account, id: "42" },
      approvedBody: body,
      createConfirmationToken: () => "token",
    }),
    (error) => error.code === "human_account_required",
  );
});

test("publication citations are bounded to public ASP.NET Core evidence", () => {
  assert.deepEqual(
    validatePublicationCitations(
      "[issue](https://github.com/dotnet/aspnetcore/issues/123)",
    ),
    ["https://github.com/dotnet/aspnetcore/issues/123"],
  );
  assert.throws(
    () => validatePublicationCitations("[local](file:///tmp/diagnostic.txt)"),
    (error) => error.code === "publication_citation_invalid",
  );
  assert.throws(
    () => validatePublicationCitations(
      "[arbitrary](https://github.com/octocat/Hello-World/issues/1)",
    ),
    (error) => error.code === "publication_citation_invalid",
  );
  assert.throws(
    () => validatePublicationCitations(
      "[private](https://github.com/dotnet/private-aspnetcore/issues/1)",
    ),
    (error) => error.code === "publication_citation_invalid",
  );
  assert.throws(
    () => validatePublicationCitations("[insecure](http://github.com/dotnet/aspnetcore/issues/123)"),
    (error) => error.code === "publication_citation_invalid",
  );
  assert.throws(
    () => validatePublicationCitations(
      `[too-many](${`https://github.com/dotnet/aspnetcore/issues/${"1".repeat(2_100)}`})`,
    ),
    (error) => error.code === "publication_citation_invalid",
  );
  assert.throws(
    () => validatePublicationBody(`${body}\n[local](file:///tmp/diagnostic.txt)`),
    (error) => error.code === "publication_citation_invalid",
  );
  for (const citation of [
    "file:///tmp/local-report.txt",
    "https://github.com/octocat/Hello-World/issues/1",
  ]) {
    assert.throws(
      () => createPublicationPreview({
        issue,
        account,
        approvedBody: `${body}\n[synthetic citation](${citation})`,
        createConfirmationToken: () => "human-confirmation-token",
      }),
      (error) => error.code === "publication_citation_invalid",
      citation,
    );
  }
});

test("publication rejects local files and GitHub repositories without established public scope", () => {
  assert.throws(
    () => validatePublicationBody(`${body}\nEvidence: file:///tmp/local-report.txt`),
    (error) => error.code === "publication_citation_invalid",
  );
  assert.throws(
    () => validatePublicationBody(`${body}\nEvidence: https://github.com/private/repository/issues/1`),
    (error) => error.code === "publication_citation_invalid",
  );
  assert.doesNotThrow(
    () => validatePublicationBody(`${body}\nEvidence: https://github.com/dotnet/aspnetcore/issues/123`),
  );
});
