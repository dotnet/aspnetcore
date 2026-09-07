export const HTML = `<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>ASP.NET Core Issue Triage</title>
  <style>
    :root { color-scheme: light dark; }
    * { box-sizing: border-box; }
    body {
      margin: 0;
      background: var(--background-color-default, #fff);
      color: var(--text-color-default, #1f2328);
      font-family: var(--font-sans, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif);
      font-size: var(--text-body-medium, 14px);
      line-height: var(--leading-body-medium, 20px);
    }
    button, select {
      border: 1px solid var(--border-color-default, #d0d7de);
      border-radius: 6px;
      background: var(--background-color-default, #fff);
      color: var(--text-color-default, #1f2328);
      font: inherit;
      padding: 6px 10px;
    }
    button { cursor: pointer; font-weight: var(--font-weight-semibold, 600); }
    button:hover { background: var(--background-color-muted, #f6f8fa); }
    button:disabled { cursor: wait; opacity: .6; }
    button:focus-visible, select:focus-visible, a:focus-visible {
      outline: 2px solid var(--color-focus-outline, #0969da);
      outline-offset: 2px;
    }
    h1, h2, h3, p { margin-top: 0; }
    h1 {
      font-size: var(--text-title-large, 26px);
      line-height: var(--leading-title-large, 32px);
      margin-bottom: 3px;
    }
    h2 { font-size: 18px; line-height: 24px; margin-bottom: 8px; }
    h3 { font-size: 15px; line-height: 20px; margin-bottom: 6px; }
    code, pre {
      font-family: var(--font-mono, "SFMono-Regular", Consolas, monospace);
      font-size: var(--text-code-inline, 12px);
    }
    .shell { margin: 0 auto; max-width: 1500px; padding: 20px; }
    .header {
      align-items: flex-start;
      display: flex;
      flex-wrap: wrap;
      gap: 16px;
      justify-content: space-between;
      margin-bottom: 14px;
    }
    .toolbar { align-items: center; display: flex; flex-wrap: wrap; gap: 8px; }
    .muted { color: var(--text-color-muted, #59636e); }
    .error { color: var(--true-color-red, #cf222e); font-weight: 600; }
    .warning {
      border: 1px solid var(--true-color-red-muted, #ff8182);
      border-radius: 8px;
      margin-bottom: 12px;
      padding: 10px 12px;
    }
    .stats {
      display: grid;
      gap: 8px;
      grid-template-columns: repeat(auto-fit, minmax(145px, 1fr));
      margin: 12px 0;
    }
    .stat {
      border: 1px solid var(--border-color-default, #d0d7de);
      border-radius: 8px;
      padding: 10px 12px;
    }
    .stat strong { display: block; font-size: 20px; line-height: 26px; }
    .predicate {
      background: var(--background-color-muted, #f6f8fa);
      border-radius: 8px;
      margin-bottom: 14px;
      overflow-wrap: anywhere;
      padding: 10px 12px;
    }
    .layout {
      display: grid;
      gap: 14px;
      grid-template-columns: minmax(430px, 1.1fr) minmax(360px, .9fr);
    }
    .panel {
      border: 1px solid var(--border-color-default, #d0d7de);
      border-radius: 8px;
      min-width: 0;
      padding: 12px;
    }
    .panel-header {
      align-items: center;
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
      justify-content: space-between;
      margin-bottom: 10px;
    }
    .issue-list { display: grid; gap: 8px; }
    .issue {
      border: 1px solid var(--border-color-default, #d0d7de);
      border-left: 4px solid var(--true-color-blue, #0969da);
      border-radius: 8px;
      cursor: pointer;
      padding: 10px 12px;
      text-align: left;
      width: 100%;
    }
    .issue[aria-current="true"] {
      background: var(--background-color-muted, #f6f8fa);
      border-color: var(--true-color-blue, #0969da);
    }
    .issue-title { display: block; font-weight: 600; margin-bottom: 3px; }
    .labels { display: flex; flex-wrap: wrap; gap: 4px; margin-top: 7px; }
    .label {
      border: 1px solid var(--border-color-default, #d0d7de);
      border-radius: 999px;
      color: var(--text-color-muted, #59636e);
      font-size: 11px;
      padding: 1px 6px;
    }
    .pagination {
      align-items: center;
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
      justify-content: space-between;
      margin-top: 12px;
    }
    .detail-empty { padding: 30px 12px; text-align: center; }
    .detail-actions { display: flex; flex-wrap: wrap; gap: 8px; margin: 12px 0; }
    .primary {
      background: var(--true-color-blue, #0969da);
      border-color: var(--true-color-blue, #0969da);
      color: var(--color-white, #fff);
    }
    .primary:hover { filter: brightness(.94); }
    .report-meta {
      border-top: 1px solid var(--border-color-default, #d0d7de);
      margin-top: 14px;
      padding-top: 12px;
    }
    .report {
      background: var(--background-color-muted, #f6f8fa);
      border-radius: 8px;
      max-height: 640px;
      overflow: auto;
      padding: 12px;
      white-space: pre-wrap;
      word-break: break-word;
    }
    textarea {
      border: 1px solid var(--border-color-default, #d0d7de);
      border-radius: 6px;
      background: var(--background-color-default, #fff);
      color: var(--text-color-default, #1f2328);
      display: block;
      font: inherit;
      line-height: 1.45;
      min-height: 260px;
      padding: 8px;
      resize: vertical;
      width: 100%;
    }
    textarea:focus-visible {
      outline: 2px solid var(--color-focus-outline, #0969da);
      outline-offset: 2px;
    }
    .editor-actions, .discussion-actions, .publication-actions {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
      margin: 8px 0;
    }
    .answer, .preview {
      background: var(--background-color-muted, #f6f8fa);
      border-radius: 8px;
      margin-top: 8px;
      padding: 10px;
      white-space: pre-wrap;
      word-break: break-word;
    }
    .success { color: var(--true-color-green, #1a7f37); font-weight: 600; }
    .footer { margin-top: 14px; }
    @media (max-width: 920px) {
      .layout { grid-template-columns: 1fr; }
    }
  </style>
</head>
<body>
  <main class="shell">
    <header class="header">
      <div>
        <h1>ASP.NET Core Issue Triage</h1>
        <p class="muted">Public issue queues with local investigation drafts and human-confirmed comments.</p>
      </div>
      <div class="toolbar">
        <label for="area">Area</label>
        <select id="area"></select>
        <button id="refresh" type="button">Refresh</button>
        <label for="saved-issue">Saved issue #</label>
        <input id="saved-issue" type="number" min="1" inputmode="numeric">
        <button id="open-saved" type="button">Open saved draft</button>
      </div>
    </header>
    <div id="status" class="muted" role="status"></div>
    <div id="warnings"></div>
    <section id="stats" class="stats"></section>
    <div id="predicate" class="predicate"></div>
    <div class="layout">
      <section class="panel">
        <div class="panel-header">
          <div>
            <h2>Selected queue</h2>
            <div id="range" class="muted"></div>
          </div>
          <label>Page size
            <select id="page-size">
              <option value="25">25</option>
              <option value="50" selected>50</option>
              <option value="100">100</option>
            </select>
          </label>
        </div>
        <div id="issues" class="issue-list"></div>
        <div class="pagination">
          <button id="previous" type="button">Previous</button>
          <span id="page" class="muted"></span>
          <button id="next" type="button">Next</button>
        </div>
      </section>
      <aside class="panel">
        <div id="detail" class="detail-empty muted">Select one issue to inspect it.</div>
      </aside>
    </div>
    <p class="footer muted">Human judgment remains authoritative. Saving and discussion stay local; commenting requires an explicit preview and confirmation.</p>
  </main>
  <script>
    const state = {
      metadata: null,
      page: null,
      offset: 0,
      limit: 50,
      localDraft: null,
      localQuestion: null,
      discussion: null,
      preview: null,
      previewPending: null,
      draftSavePromise: null,
      pendingSelection: null,
      selectionGeneration: 0,
    };
    const token = new URLSearchParams(location.search).get("token") || "";

    const areaSelect = document.getElementById("area");
    const refreshButton = document.getElementById("refresh");
    const savedIssueNumber = document.getElementById("saved-issue");
    const openSavedButton = document.getElementById("open-saved");
    const pageSize = document.getElementById("page-size");
    const previousButton = document.getElementById("previous");
    const nextButton = document.getElementById("next");

    function text(element, value) {
      element.textContent = value == null ? "" : String(value);
      return element;
    }

    function node(tag, className, value) {
      const element = document.createElement(tag);
      if (className) element.className = className;
      if (value !== undefined) text(element, value);
      return element;
    }

    async function request(path, options) {
      const separator = path.includes("?") ? "&" : "?";
      const response = await fetch(path + separator + "token=" + encodeURIComponent(token), options);
      const body = await response.json();
      if (!response.ok) {
        const error = new Error(body.error || "Request failed");
        error.code = body.code;
        throw error;
      }
      return body;
    }

    function showError(error) {
      text(document.getElementById("status"), "Error: " + (error?.message || String(error)));
    }

    function currentIssue() {
      return state.metadata?.snapshot?.selectedIssue ?? null;
    }

    function currentDraftRevision() {
      return state.metadata?.snapshot?.selectedWorkspace?.draft?.revision ?? null;
    }

    function captureEditorState() {
      const element = document.activeElement;
      const issue = currentIssue();
      if (
        !element
        || !issue
        || !["draft", "question"].includes(element.id)
        || Number(element.dataset.issueNumber) !== issue.number
        || element.dataset.itemId !== issue.id
      ) {
        return null;
      }
      return {
        id: element.id,
        itemId: issue.id,
        issueNumber: issue.number,
        selectionStart: element.selectionStart,
        selectionEnd: element.selectionEnd,
        selectionDirection: element.selectionDirection,
      };
    }

    function restoreEditorState(editorState) {
      if (
        !editorState
        || currentIssue()?.id !== editorState.itemId
        || currentIssue()?.number !== editorState.issueNumber
      ) {
        return;
      }
      const element = document.getElementById(editorState.id);
      if (!element) {
        return;
      }
      element.focus({ preventScroll: true });
      const valueLength = element.value.length;
      element.setSelectionRange(
        Math.min(editorState.selectionStart ?? valueLength, valueLength),
        Math.min(editorState.selectionEnd ?? valueLength, valueLength),
        editorState.selectionDirection ?? "none",
      );
    }

    function cancelPendingDraftSave() {
      if (state.localDraft?.timer) {
        clearTimeout(state.localDraft.timer);
        state.localDraft.timer = null;
      }
    }

    function isCurrentIssueContext(context) {
      const issue = currentIssue();
      return state.selectionGeneration === context.generation
        && !state.pendingSelection
        && issue?.id === context.itemId
        && issue.number === context.issueNumber
        && currentDraftRevision() === context.revision;
    }

    function isCurrentIssueIdentity(context) {
      const issue = currentIssue();
      return state.selectionGeneration === context.generation
        && !state.pendingSelection
        && issue?.id === context.itemId
        && issue.number === context.issueNumber;
    }

    function isResponseForContext(responseState, context, revisions) {
      const issue = responseState?.snapshot?.selectedIssue;
      const revision = responseState?.snapshot?.selectedWorkspace?.draft?.revision;
      return isCurrentIssueContext(context)
        && issue?.id === context.itemId
        && issue.number === context.issueNumber
        && revisions.includes(revision);
    }

    function trackDraftSave(promise) {
      state.draftSavePromise = promise;
      promise.then(
        () => {
          if (state.draftSavePromise === promise) state.draftSavePromise = null;
        },
        () => {
          if (state.draftSavePromise === promise) state.draftSavePromise = null;
        },
      );
      return promise;
    }

    async function saveDraftContext(draftContext, editorStatus = null, reloadPage = true) {
      if (
        state.localDraft !== draftContext
        || !isCurrentIssueContext(draftContext)
      ) {
        return false;
      }
      try {
        const response = await request("/api/draft", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            content: draftContext.content,
            provenance: "human",
            issueNumber: draftContext.issueNumber,
            revision: draftContext.revision,
          }),
        });
        const responseIssue = response?.snapshot?.selectedIssue;
        const responseDraft = response?.snapshot?.selectedWorkspace?.draft;
        if (
          state.localDraft !== draftContext
          || !isCurrentIssueIdentity(draftContext)
          || responseIssue?.id !== draftContext.itemId
          || responseIssue.number !== draftContext.issueNumber
          || responseDraft?.revision !== draftContext.revision + 1
          || responseDraft.content !== draftContext.content
        ) {
          return false;
        }
        state.localDraft = null;
        state.metadata = response;
        renderMetadata();
        if (reloadPage && state.metadata.snapshot) await loadPage();
        return true;
      } catch (error) {
        if (state.localDraft === draftContext && editorStatus) {
          text(editorStatus, "Draft conflict: " + (error?.message || String(error)));
        }
        throw error;
      }
    }

    function draftElementFor(issue) {
      const draft = document.getElementById("draft");
      return draft?.dataset.itemId === issue.id
        && Number(draft.dataset.issueNumber) === issue.number
        ? draft
        : null;
    }

    async function loadState() {
      state.metadata = await request("/api/state");
      renderMetadata();
      if (state.metadata.snapshot) {
        await loadPage();
      }
    }

    async function loadPage() {
      state.page = await request(
        "/api/issues?offset=" + state.offset + "&limit=" + state.limit,
      );
      renderPage();
    }

    function renderMetadata() {
      const editorState = captureEditorState();
      const metadata = state.metadata;
      const snapshot = metadata.snapshot;
      refreshButton.disabled = metadata.refresh.phase === "refreshing";
      areaSelect.disabled = metadata.refresh.phase === "refreshing";
      const availableAreas = metadata.areaOptions || snapshot?.areaOptions || [];
      const selectedArea = snapshot?.area || metadata.requestedArea || "area-blazor";
      areaSelect.replaceChildren();
      for (const option of availableAreas) {
        const element = document.createElement("option");
        element.value = option.label;
        element.textContent = option.label + " - " + option.description;
        areaSelect.append(element);
      }
      areaSelect.value = selectedArea;
      text(
        document.getElementById("status"),
        metadata.refresh.phase === "refreshing"
          ? "Refreshing the complete " + metadata.requestedArea + " queue..."
          : metadata.refresh.error || (snapshot ? "Snapshot " + snapshot.generatedAt : "Waiting for a snapshot."),
      );

      if (!snapshot) {
        document.getElementById("stats").replaceChildren();
        document.getElementById("issues").replaceChildren();
        document.getElementById("predicate").replaceChildren();
        renderWarnings({ coverage: { complete: false, limitation: metadata.refresh.error, warnings: [] } });
        restoreEditorState(editorState);
        return;
      }

      renderWarnings(snapshot);
      renderStats(snapshot);
      text(document.getElementById("predicate"), snapshot.predicate);
      renderDetail(snapshot);
      restoreEditorState(editorState);
    }

    async function refreshQueue() {
      state.offset = 0;
      state.metadata = await request("/api/refresh", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ area: areaSelect.value }),
      });
      renderMetadata();
      if (state.metadata.snapshot) await loadPage();
    }

    function renderWarnings(snapshot) {
      const container = document.getElementById("warnings");
      container.replaceChildren();
      const warnings = [];
      if (!snapshot.coverage.complete) {
        warnings.push("INCOMPLETE: " + (snapshot.coverage.limitation || "Queue membership could not be fully established."));
      }
      warnings.push(...snapshot.coverage.warnings);
      if (state.metadata.refresh.stale) {
        warnings.push("Showing the previous complete snapshot while refresh is unresolved.");
      }
      if (state.metadata.refresh.error) {
        warnings.push("Refresh failed: " + state.metadata.refresh.error);
      }
      for (const warning of warnings) {
        container.append(node("div", "warning", warning));
      }
    }

    function renderStats(snapshot) {
      const stats = [
        ["Qualifying", snapshot.totalCount],
        ["Open with area", snapshot.coverage.sourceOpenAreaCount ?? "Unknown"],
        ["Retrieved", snapshot.coverage.retrievedOpenAreaCount],
        ["Membership unknown", snapshot.coverage.uncertainMembershipCount ?? 0],
        ["Pages", snapshot.coverage.pages],
        ["Completeness", snapshot.coverage.complete ? "Complete" : "Incomplete"],
        ["Rate remaining", snapshot.coverage.rateLimit?.remaining ?? "Not reported"],
      ];
      const container = document.getElementById("stats");
      container.replaceChildren();
      for (const [label, value] of stats) {
        const card = node("div", "stat");
        card.append(node("strong", "", value), node("span", "muted", label));
        container.append(card);
      }
    }

    function renderPage() {
      const page = state.page;
      const snapshot = state.metadata.snapshot;
      const container = document.getElementById("issues");
      container.replaceChildren();
      for (const issue of page.items) {
        const button = node("button", "issue");
        button.type = "button";
        button.dataset.itemId = issue.id;
        button.setAttribute(
          "aria-current",
          snapshot.selectedIssue?.id === issue.id ? "true" : "false",
        );
        button.append(
          node("span", "issue-title", "#" + issue.number + " - " + issue.title),
          node("span", "muted", "Opened " + issue.createdAt.slice(0, 10) + (issue.author ? " by @" + issue.author : "")),
        );
        const labels = node("span", "labels");
        for (const label of issue.labels) labels.append(node("span", "label", label));
        button.append(labels);
        button.addEventListener("click", () => selectIssue(issue.id));
        container.append(button);
      }
      if (page.items.length === 0) {
        container.append(node("p", "muted", "No issues match this queue."));
      }
      const start = page.total === 0 ? 0 : page.offset + 1;
      const end = page.offset + page.items.length;
      text(document.getElementById("range"), "Showing " + start + "-" + end + " of " + page.total + ".");
      const currentPage = page.total === 0 ? 0 : Math.floor(page.offset / page.limit) + 1;
      const totalPages = page.total === 0 ? 0 : Math.ceil(page.total / page.limit);
      text(document.getElementById("page"), "Page " + currentPage + " of " + totalPages);
      previousButton.disabled = page.offset === 0;
      nextButton.disabled = page.nextOffset === null;
    }

    function renderDetail(snapshot) {
      const container = document.getElementById("detail");
      container.replaceChildren();
      const issue = snapshot.selectedIssue;
      if (!issue) {
        container.className = "detail-empty muted";
        text(container, "Select one issue to inspect it.");
        return;
      }
      container.className = "";
      container.append(
        node("h2", "", "#" + issue.number + " - " + issue.title),
        node("p", "muted", "Opened " + issue.createdAt.slice(0, 10) + (issue.author ? " by @" + issue.author : "")),
        node("h3", "", "Why included"),
      );
      const why = node("ul");
      for (const reason of issue.whyIncluded) why.append(node("li", "", reason));
      container.append(why);

      const actions = node("div", "detail-actions");
      const open = node("a", "", "Open canonical issue");
      open.href = issue.url;
      open.target = "_blank";
      open.rel = "noopener noreferrer";
      const investigate = node("button", "primary", investigationLabel(snapshot.investigation, issue.number));
      investigate.type = "button";
      investigate.disabled = ["queued", "running"].includes(snapshot.investigation.phase);
      investigate.addEventListener("click", () => investigateIssue(issue.id));
      actions.append(open, investigate);
      container.append(actions);

      if (snapshot.investigation.issueNumber === issue.number) {
        const status = snapshot.investigation.error
          ? "Investigation error: " + snapshot.investigation.error
          : "Investigation: " + snapshot.investigation.phase;
        container.append(node("p", snapshot.investigation.error ? "error" : "muted", status));
      }

      if (snapshot.selectedWorkspace) {
        const meta = node("div", "report-meta");
        meta.append(
          node("h3", "", "Investigation"),
          node("p", "muted", "Original generated report is preserved separately. Edit the local Markdown draft below."),
        );
        const publication = snapshot.selectedWorkspace.publication;
        if (publication?.pending) {
          meta.append(
            node("p", "warning", "A GitHub comment write has an unknown outcome. Reconcile it before trying another publication."),
          );
        } else if (publication?.commentId) {
          const draftMatchesPublication = publication.publishedRevision === snapshot.selectedWorkspace.draft.revision
            && (!publication.publishedHash || publication.publishedHash === snapshot.selectedWorkspace.draft.hash);
          const receipt = node(
            "p",
            draftMatchesPublication ? "success" : "warning",
            draftMatchesPublication
              ? "Published by explicit confirmation."
              : "Published comment reflects an earlier draft; local changes are not published.",
          );
          if (publication.commentUrl) {
            const link = node("a", "", " Open comment");
            link.href = publication.commentUrl;
            link.target = "_blank";
            link.rel = "noopener noreferrer";
            receipt.append(link);
          }
          meta.append(receipt);
        }
        const draft = document.createElement("textarea");
        draft.id = "draft";
        draft.dataset.itemId = issue.id;
        draft.dataset.issueNumber = String(issue.number);
        draft.value = state.localDraft?.issueNumber === issue.number
          && state.localDraft.generation === state.selectionGeneration
          ? state.localDraft.content
          : snapshot.selectedWorkspace.draft.content;
        draft.setAttribute("aria-label", "Investigation Markdown draft");
        const editorStatus = node(
          "div",
          "muted",
          state.localDraft?.issueNumber === issue.number ? "Unsaved local changes" : "Draft saved",
        );
        const editorActions = node("div", "editor-actions");
        const undo = node("button", "", "Undo last change");
        undo.type = "button";
        undo.disabled = snapshot.selectedWorkspace.draft.revision < 2;
        undo.addEventListener("click", () => undoDraft(issue.number));
        const previewButton = node("button", "primary", "Preview comment");
        previewButton.type = "button";
        previewButton.disabled = state.previewPending?.itemId === issue.id
          && state.previewPending.generation === state.selectionGeneration;
        previewButton.addEventListener("click", () => previewPublication(issue.number, previewButton));
        editorActions.append(undo, previewButton);
        draft.addEventListener("input", () => {
          cancelPendingDraftSave();
          state.preview = null;
          const draftContext = {
            itemId: issue.id,
            issueNumber: issue.number,
            generation: state.selectionGeneration,
            revision: snapshot.selectedWorkspace.draft.revision,
            content: draft.value,
            timer: null,
          };
          state.localDraft = draftContext;
          text(editorStatus, "Saving...");
          draftContext.timer = setTimeout(() => {
            const save = trackDraftSave(saveDraftContext(draftContext, editorStatus));
            void save.catch(() => {});
          }, 400);
        });
        meta.append(draft, editorStatus, editorActions);
        const discussionHeading = node("h3", "", "Discuss");
        const question = document.createElement("textarea");
        question.id = "question";
        question.dataset.itemId = issue.id;
        question.dataset.issueNumber = String(issue.number);
        question.rows = 3;
        question.style.minHeight = "90px";
        question.value = state.localQuestion?.issueNumber === issue.number
          && state.localQuestion.generation === state.selectionGeneration
          ? state.localQuestion.content
          : "";
        question.setAttribute("aria-label", "Discussion question");
        question.addEventListener("input", () => {
          state.localQuestion = {
            itemId: issue.id,
            issueNumber: issue.number,
            generation: state.selectionGeneration,
            content: question.value,
          };
        });
        const discussionActions = node("div", "discussion-actions");
        const ask = node("button", "primary", "Ask Copilot");
        ask.type = "button";
        ask.addEventListener("click", async () => {
          const discussionContext = {
            itemId: issue.id,
            issueNumber: issue.number,
            generation: state.selectionGeneration,
            revision: snapshot.selectedWorkspace.draft.revision,
          };
          try {
            ask.disabled = true;
            const response = await request("/api/discuss", {
              method: "POST",
              headers: { "Content-Type": "application/json" },
              body: JSON.stringify({
                issueNumber: discussionContext.issueNumber,
                question: question.value,
                revision: discussionContext.revision,
              }),
            });
            if (!isCurrentIssueIdentity(discussionContext)) {
              return;
            }
            const draftBeforeResponse = draftElementFor(issue);
            const savedContent = state.metadata?.snapshot?.selectedWorkspace?.draft.content;
            const humanContent = draftBeforeResponse?.value;
            const hasUnsavedHumanEdit = Boolean(
              draftBeforeResponse
              && typeof humanContent === "string"
              && (state.localDraft?.itemId === issue.id || humanContent !== savedContent),
            );
            state.metadata = response.state;
            state.discussion = { ...response, issueNumber: discussionContext.issueNumber };
            if (response.applied && hasUnsavedHumanEdit) {
              const responseWorkspace = response.state?.snapshot?.selectedWorkspace;
              const humanDraft = {
                itemId: issue.id,
                issueNumber: issue.number,
                generation: state.selectionGeneration,
                revision: responseWorkspace?.draft.revision,
                content: humanContent,
                timer: null,
              };
              state.localDraft = humanDraft;
              state.discussion = {
                ...state.discussion,
                applied: false,
                conflict: "Copilot's proposed replacement was not applied because a newer human edit was in progress.",
              };
              renderMetadata();
              const saved = await request("/api/draft", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                  content: humanDraft.content,
                  provenance: "human",
                  issueNumber: humanDraft.issueNumber,
                  revision: humanDraft.revision,
                }),
              });
              const savedWorkspace = saved?.snapshot?.selectedWorkspace?.draft;
              if (
                !isCurrentIssueIdentity(discussionContext)
                || savedWorkspace?.content === undefined
                || savedWorkspace.content !== humanDraft.content
                || savedWorkspace.revision !== humanDraft.revision + 1
              ) {
                return;
              }
              state.metadata = saved;
              if (state.localDraft === humanDraft) {
                state.localDraft = null;
              } else {
                const newerHumanDraft = state.localDraft;
                const currentDraft = draftElementFor(issue);
                if (
                  !newerHumanDraft
                  || !isCurrentIssueIdentity(discussionContext)
                  || currentDraft?.value !== newerHumanDraft.content
                ) {
                  return;
                }
                cancelPendingDraftSave();
                const followupDraft = {
                  ...newerHumanDraft,
                  revision: savedWorkspace.revision,
                  timer: null,
                };
                state.localDraft = followupDraft;
                renderMetadata();
                const followupSave = trackDraftSave(
                  saveDraftContext(followupDraft, null, false),
                );
                void followupSave.catch(() => {});
              }
            }
            if (!response.applied || hasUnsavedHumanEdit) state.preview = null;
            state.localQuestion = null;
            renderMetadata();
          } catch (error) {
            if (isCurrentIssueContext(discussionContext)) {
              showError(error);
            }
          } finally {
            ask.disabled = false;
          }
        });
        discussionActions.append(ask);
        meta.append(discussionHeading, question, discussionActions);
        const discussionHistory = snapshot.selectedWorkspace.discussion ?? [];
        if (discussionHistory.length > 0) {
          meta.append(node("h3", "", "Discussion history"));
          for (const turn of discussionHistory) {
            const historyText = turn.kind === "turn"
              ? "Question: " + turn.question + "\\n\\nAnswer: " + turn.answer
              : (turn.content || turn.answer || turn.question || "");
            meta.append(node("div", "answer", historyText));
          }
        }
        if (state.discussion?.answer) {
          meta.append(node("div", "answer", state.discussion.answer));
          if (state.discussion.conflict) {
            meta.append(node("p", "warning", state.discussion.conflict));
          } else if (state.discussion.applied) {
            meta.append(node("p", "success", "Copilot updated the draft."));
          }
        }
        if (snapshot.selectedWorkspace.publication?.pending) {
          const pending = node("div", "warning");
          pending.append(
            node("h3", "", "Publication needs reconciliation"),
            node("p", "", "The previous GitHub write had an unknown outcome. Read GitHub before taking another action."),
          );
          const resolve = node("button", "primary", "Reconcile publication");
          resolve.type = "button";
          resolve.addEventListener("click", () => resolvePublication(issue.number, resolve));
          pending.append(resolve);
          meta.append(pending);
        }
        if (state.preview?.issueNumber === issue.number) {
          const preview = node("div", "preview");
          preview.append(
            node("h3", "", "Publish preview"),
            node("p", "", "Issue: " + state.preview.issueUrl),
            node("p", "", "GitHub account: " + state.preview.accountLogin),
            node("pre", "report", state.preview.body),
          );
          const publicationActions = node("div", "publication-actions");
          const publish = node("button", "primary", "Publish this exact comment");
          publish.type = "button";
          publish.addEventListener("click", () => publishComment(issue.number));
          publicationActions.append(publish);
          preview.append(publicationActions);
          meta.append(preview);
        }
        container.append(meta);
      } else if (snapshot.selectedReport) {
        const meta = node("div", "report-meta");
        meta.append(
          node("h3", "", "Advisory investigation"),
          node("p", "muted", "Generated " + snapshot.selectedReport.createdAt + " using " + snapshot.selectedReport.skill + ". Model: " + snapshot.selectedReport.model + "."),
          node("pre", "report", snapshot.selectedReport.content),
        );
        container.append(meta);
      } else {
        container.append(node("p", "muted", "No durable investigation report is stored for this issue. Investigate the issue first."));
      }
    }

    async function undoDraft(issueNumber) {
      const issue = currentIssue();
      const workspace = state.metadata?.snapshot?.selectedWorkspace;
      if (!issue || issue.number !== issueNumber || !workspace) {
        showError(new Error("The selected issue changed before undo could start."));
        return;
      }
      const operation = {
        itemId: issue.id,
        issueNumber,
        generation: state.selectionGeneration,
      };
      try {
        cancelPendingDraftSave();
        const draft = draftElementFor(issue);
        const pendingSave = state.draftSavePromise;
        if (
          pendingSave
          && state.localDraft?.itemId === operation.itemId
          && draft?.value === state.localDraft.content
        ) {
          if (!(await pendingSave)) {
            throw new Error("The draft changed before it could be saved; undo was blocked.");
          }
        } else if (draft && draft.value !== workspace.draft.content) {
          const draftContext = {
            ...operation,
            revision: workspace.draft.revision,
            content: draft.value,
            timer: null,
          };
          state.localDraft = draftContext;
          const save = trackDraftSave(saveDraftContext(draftContext));
          if (!(await save)) {
            throw new Error("The draft changed before it could be saved; undo was blocked.");
          }
        } else {
          state.localDraft = null;
        }
        const context = {
          ...operation,
          revision: currentDraftRevision(),
        };
        if (!isCurrentIssueContext(context)) {
          throw new Error("The selected issue or draft changed; undo was blocked.");
        }
        const response = await request("/api/undo", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ revision: context.revision }),
        });
        if (!isResponseForContext(response, context, [context.revision + 1])) {
          throw new Error("The selected issue or draft changed while undoing.");
        }
        state.localDraft = null;
        state.metadata = response;
        state.preview = null;
        renderMetadata();
        if (state.metadata.snapshot) await loadPage();
      } catch (error) {
        if (state.selectionGeneration === operation.generation) {
          showError(error);
        }
      }
    }

    async function previewPublication(issueNumber, button) {
      const issue = currentIssue();
      const workspace = state.metadata?.snapshot?.selectedWorkspace;
      if (!issue || issue.number !== issueNumber || !workspace) {
        showError(new Error("The selected issue changed before preview could start."));
        return;
      }
      const operation = {
        itemId: issue.id,
        issueNumber,
        generation: state.selectionGeneration,
      };
      state.previewPending = operation;
      if (button) button.disabled = true;
      let previewError = null;
      try {
        const draft = draftElementFor(issue);
        if (!draft) {
          throw new Error("The investigation draft is unavailable; preview is blocked.");
        }
        const previewContent = draft.value;
        const pendingSave = state.draftSavePromise;
        if (
          pendingSave
          && state.localDraft?.itemId === operation.itemId
          && state.localDraft.content === draft.value
        ) {
          if (!(await pendingSave)) {
            throw new Error("The draft changed before it could be saved; preview was blocked.");
          }
        } else if (draft.value !== workspace.draft.content) {
          text(document.getElementById("status"), "Saving the newest draft before preparing preview...");
          cancelPendingDraftSave();
          const draftContext = {
            ...operation,
            revision: workspace.draft.revision,
            content: draft.value,
            timer: null,
          };
          state.localDraft = draftContext;
          const save = trackDraftSave(saveDraftContext(draftContext, null, false));
          if (!(await save)) {
            throw new Error("The draft changed before it could be saved; preview was blocked.");
          }
        } else {
          cancelPendingDraftSave();
          state.localDraft = null;
        }
        const previewContext = {
          ...operation,
          revision: currentDraftRevision(),
        };
        if (!isCurrentIssueContext(previewContext)) {
          throw new Error("The selected issue or draft changed; preview was blocked.");
        }
        const preview = await request("/api/preview", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ revision: previewContext.revision }),
        });
        if (
          !isCurrentIssueContext(previewContext)
          || preview.issueNumber !== previewContext.issueNumber
          || draftElementFor(issue)?.value !== previewContent
          || state.localDraft
        ) {
          throw new Error("The selected issue or draft changed while preparing preview.");
        }
        state.preview = {
          ...preview,
          itemId: previewContext.itemId,
          generation: previewContext.generation,
          revision: previewContext.revision,
        };
        renderMetadata();
      } catch (error) {
        previewError = error;
      } finally {
        if (state.previewPending === operation) state.previewPending = null;
        if (button?.isConnected) {
          button.disabled = false;
        } else if (currentIssue()?.id === operation.itemId) {
          renderMetadata();
        }
        if (previewError && state.selectionGeneration === operation.generation) {
          showError(previewError);
        }
      }
    }

    async function publishComment(issueNumber) {
      const preview = state.preview;
      const issue = currentIssue();
      const workspace = state.metadata?.snapshot?.selectedWorkspace;
      const draft = issue ? draftElementFor(issue) : null;
      if (
        !preview
        || !issue
        || !workspace
        || preview.itemId !== issue.id
        || preview.issueNumber !== issueNumber
        || preview.generation !== state.selectionGeneration
        || preview.revision !== currentDraftRevision()
        || !draft
        || draft.value !== workspace.draft.content
      ) {
        showError(new Error("The draft changed after preview; publish was blocked. Prepare a new preview."));
        return;
      }
      const context = {
        itemId: issue.id,
        issueNumber,
        generation: state.selectionGeneration,
        revision: preview.revision,
      };
      try {
        const response = await request("/api/publish", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            revision: context.revision,
            confirmation: preview.confirmationToken,
          }),
        });
        if (!isResponseForContext(response.state, context, [context.revision])) {
          throw new Error("The selected issue or draft changed while publishing.");
        }
        state.metadata = response.state;
        state.preview = null;
        renderMetadata();
      } catch (error) {
        if (isCurrentIssueContext(context)) {
          showError(error);
        }
      }

      async function resolvePublication(issueNumber, button) {
        const issue = currentIssue();
        if (!issue || issue.number !== issueNumber) {
          showError(new Error("The selected issue changed before recovery could start."));
          return;
        }
        button.disabled = true;
        try {
          const response = await request("/api/resolve-publication", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: "{}",
          });
          if (currentIssue()?.number !== issueNumber) {
            return;
          }
          state.metadata = response.state;
          renderMetadata();
        } catch (error) {
          showError(error);
          button.disabled = false;
        }
      }
    }

    function investigationLabel(investigation, issueNumber) {
      if (investigation.issueNumber !== issueNumber) return "Investigate with project skill";
      if (investigation.phase === "queued") return "Investigation queued";
      if (investigation.phase === "running") return "Investigating...";
      return "Investigate with project skill";
    }

    async function selectIssue(itemId) {
      const generation = ++state.selectionGeneration;
      cancelPendingDraftSave();
      state.localDraft = null;
      state.localQuestion = null;
      state.discussion = null;
      state.preview = null;
      state.pendingSelection = { generation, itemId };
      try {
        const metadata = await request("/api/select", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ itemId }),
        });
        if (
          state.selectionGeneration !== generation
          || state.pendingSelection?.itemId !== itemId
          || metadata.snapshot?.selectedIssue?.id !== itemId
        ) {
          return;
        }
        state.pendingSelection = null;
        state.metadata = metadata;
        renderMetadata();
        renderPage();
      } catch (error) {
        if (state.selectionGeneration === generation) {
          state.pendingSelection = null;
          showError(error);
        }
      }
    }

    async function investigateIssue(itemId) {
      try {
        state.metadata = await request("/api/investigate", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ itemId }),
        });
        renderMetadata();
      } catch (error) {
        showError(error);
      }
    }

    refreshButton.addEventListener("click", async () => {
      try {
        await refreshQueue();
      } catch (error) {
        showError(error);
      }
    });

    areaSelect.addEventListener("change", async () => {
      try {
        await refreshQueue();
      } catch (error) {
        showError(error);
      }
    });
    openSavedButton.addEventListener("click", async () => {
      const issueNumber = Number(savedIssueNumber.value);
      if (!Number.isSafeInteger(issueNumber) || issueNumber < 1) {
        showError(new Error("Enter a positive saved issue number."));
        return;
      }
      try {
        state.pendingSelection = { itemId: "saved-" + issueNumber };
        const metadata = await request("/api/select-saved", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ issueNumber }),
        });
        state.selectionGeneration += 1;
        state.pendingSelection = null;
        state.metadata = metadata;
        state.page = await request("/api/issues?offset=" + state.offset + "&limit=" + state.limit);
        renderMetadata();
        renderPage();
      } catch (error) {
        state.pendingSelection = null;
        showError(error);
      }
    });

    pageSize.addEventListener("change", async () => {
      try {
        state.limit = Number(pageSize.value);
        state.offset = 0;
        await loadPage();
      } catch (error) {
        showError(error);
      }
    });
    previousButton.addEventListener("click", async () => {
      try {
        state.offset = Math.max(0, state.offset - state.limit);
        await loadPage();
      } catch (error) {
        showError(error);
      }
    });
    nextButton.addEventListener("click", async () => {
      try {
        if (state.page.nextOffset !== null) {
          state.offset = state.page.nextOffset;
          await loadPage();
        }
      } catch (error) {
        showError(error);
      }
    });

    const events = new EventSource("/events?token=" + encodeURIComponent(token));
    events.addEventListener("state", async (event) => {
      try {
        const metadata = JSON.parse(event.data);
        const incomingIssueId = metadata.snapshot?.selectedIssue?.id ?? null;
        const incomingRevision = metadata.snapshot?.selectedWorkspace?.draft?.revision ?? null;
        const currentRevision = currentDraftRevision();
        if (
          (state.pendingSelection && incomingIssueId !== state.pendingSelection.itemId)
          || (!state.pendingSelection
            && currentIssue()
            && incomingIssueId !== currentIssue().id)
          || (!state.pendingSelection
            && currentIssue()?.id === incomingIssueId
            && currentRevision !== null
            && incomingRevision !== null
            && incomingRevision < currentRevision)
        ) {
          return;
        }
        state.metadata = metadata;
        renderMetadata();
        if (state.metadata.snapshot) {
          const total = state.metadata.snapshot.totalCount;
          const maximumOffset = total === 0
            ? 0
            : Math.floor((total - 1) / state.limit) * state.limit;
          state.offset = Math.min(state.offset, maximumOffset);
          await loadPage();
        }
      } catch (error) {
        showError(error);
      }
    });

    loadState().catch(showError);
  </script>
</body>
</html>`;
