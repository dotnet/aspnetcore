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
        <p class="muted">Complete, read-only area queues with one-issue advisory investigation.</p>
      </div>
      <div class="toolbar">
        <label for="area">Area</label>
        <select id="area"></select>
        <button id="refresh" type="button">Refresh</button>
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
    <p class="footer muted">Human judgment remains authoritative. This canvas never labels, assigns, comments, closes, milestones, or changes projects.</p>
  </main>
  <script>
    const state = {
      metadata: null,
      page: null,
      offset: 0,
      limit: 50,
    };
    const token = new URLSearchParams(location.search).get("token") || "";

    const areaSelect = document.getElementById("area");
    const refreshButton = document.getElementById("refresh");
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
        return;
      }

      renderWarnings(snapshot);
      renderStats(snapshot);
      text(document.getElementById("predicate"), snapshot.predicate);
      renderDetail(snapshot);
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

      if (snapshot.selectedReport) {
        const meta = node("div", "report-meta");
        meta.append(
          node("h3", "", "Advisory investigation"),
          node("p", "muted", "Generated " + snapshot.selectedReport.createdAt + " using " + snapshot.selectedReport.skill + ". Model: " + snapshot.selectedReport.model + "."),
          node("pre", "report", snapshot.selectedReport.content),
        );
        container.append(meta);
      } else {
        container.append(node("p", "muted", "No durable investigation report is stored for this issue."));
      }
    }

    function investigationLabel(investigation, issueNumber) {
      if (investigation.issueNumber !== issueNumber) return "Investigate with project skill";
      if (investigation.phase === "queued") return "Investigation queued";
      if (investigation.phase === "running") return "Investigating...";
      return "Investigate with project skill";
    }

    async function selectIssue(itemId) {
      try {
        state.metadata = await request("/api/select", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ itemId }),
        });
        renderMetadata();
        renderPage();
      } catch (error) {
        text(document.getElementById("status"), error.message);
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
        text(document.getElementById("status"), error.message);
      }
    }

    refreshButton.addEventListener("click", async () => {
      try {
        state.offset = 0;
        state.metadata = await request("/api/refresh", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ area: areaSelect.value }),
        });
        renderMetadata();
        if (state.metadata.snapshot) await loadPage();
      } catch (error) {
        text(document.getElementById("status"), error.message);
      }
    });

    pageSize.addEventListener("change", async () => {
      state.limit = Number(pageSize.value);
      state.offset = 0;
      await loadPage();
    });
    previousButton.addEventListener("click", async () => {
      state.offset = Math.max(0, state.offset - state.limit);
      await loadPage();
    });
    nextButton.addEventListener("click", async () => {
      if (state.page.nextOffset !== null) {
        state.offset = state.page.nextOffset;
        await loadPage();
      }
    });

    const events = new EventSource("/events?token=" + encodeURIComponent(token));
    events.addEventListener("state", async (event) => {
      state.metadata = JSON.parse(event.data);
      renderMetadata();
      if (state.metadata.snapshot) {
        const total = state.metadata.snapshot.totalCount;
        const maximumOffset = total === 0
          ? 0
          : Math.floor((total - 1) / state.limit) * state.limit;
        state.offset = Math.min(state.offset, maximumOffset);
        await loadPage();
      }
    });

    loadState().catch((error) => text(document.getElementById("status"), error.message));
  </script>
</body>
</html>`;
