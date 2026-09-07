function initializeCanvas() {
  const token = new URLSearchParams(location.search).get("token") ?? "";
  const element = (id) => document.getElementById(id);
  const text = (id, value) => { element(id).textContent = value ?? ""; };
  const state = {
    meta: null,
    page: null,
    offset: 0,
    limit: 50,
    pageRequest: 0,
    actionRequest: 0,
    pendingAction: null,
    pendingLaunch: null,
    optionsLoaded: false,
  };

  async function request(path, body) {
    const url = new URL(path, location.href);
    url.searchParams.set("token", token);
    const response = await fetch(url, body === undefined ? {} : {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
    const result = await response.json();
    if (!response.ok) {
      if (result.state) {
        acceptState(result.state);
      }
      throw new Error(result.error ?? "Request failed.");
    }
    return result;
  }

  function acceptState(next) {
    if (state.meta && next.revision <= state.meta.revision) {
      return;
    }
    const previousSnapshot = state.meta?.snapshot?.id;
    state.meta = next;
    if (previousSnapshot !== next.snapshot?.id) {
      state.offset = 0;
      state.page = null;
      state.pageRequest++;
      if (next.snapshot) {
        void loadPage();
      }
    }
    render();
  }

  async function loadPage() {
    const ticket = ++state.pageRequest;
    const snapshotId = state.meta?.snapshot?.id;
    const offset = state.offset;
    const limit = state.limit;
    renderPage();
    try {
      const page = await request("/api/issues?offset=" + offset + "&limit=" + limit);
      if (ticket !== state.pageRequest || snapshotId !== state.meta?.snapshot?.id
        || page.snapshotId !== snapshotId) {
        return;
      }
      state.page = page;
      renderPage();
    } catch (error) {
      if (ticket === state.pageRequest) {
        text("error", error.message);
      }
    }
  }

  async function action(path, body, launch = false) {
    if (state.pendingAction || (launch && state.pendingLaunch)) {
      return;
    }
    const ticket = ++state.actionRequest;
    if (launch) {
      state.pendingLaunch = ticket;
    } else {
      state.pendingAction = ticket;
    }
    text("error", "");
    render();
    try {
      acceptState(await request(path, body));
    } catch (error) {
      if (ticket === state.actionRequest) {
        text("error", launch
          ? error.message + " Inspect chat before deliberately resending if delivery is uncertain."
          : error.message);
      }
    } finally {
      if (state.pendingAction === ticket) {
        state.pendingAction = null;
      }
      if (state.pendingLaunch === ticket) {
        state.pendingLaunch = null;
      }
      render();
    }
  }

  function render() {
    const meta = state.meta;
    if (!meta) {
      return;
    }
    if (!state.optionsLoaded) {
      for (const option of meta.areaOptions) {
        const node = document.createElement("option");
        node.value = option.label;
        node.textContent = option.label + " - " + option.description;
        element("area").append(node);
      }
      state.optionsLoaded = true;
    }
    const snapshot = meta.snapshot;
    const refreshing = meta.refresh.phase === "refreshing";
    if (!state.pendingAction) {
      element("area").value = snapshot?.area ?? meta.requestedArea;
    }
    element("area").disabled = refreshing || state.pendingAction !== null;
    element("refresh").disabled = refreshing || state.pendingAction !== null;
    text("status", refreshing ? "Refreshing public queue..."
      : meta.refresh.error ? "Refresh failed: " + meta.refresh.error
        : snapshot ? "Snapshot " + snapshot.generatedAt : "No snapshot available.");
    text("stale", meta.refresh.stale ? "Showing the previous snapshot; it may be stale." : "");
    const coverage = snapshot?.coverage;
    const count = (value) => value ?? "unknown";
    text("coverage", coverage
      ? (coverage.complete ? "Complete" : "INCOMPLETE") + ": "
        + count(coverage.sourceOpenAreaCount) + " source issues; "
        + count(coverage.retrievedOpenAreaCount) + " retrieved; "
        + snapshot.totalCount + " qualifying; "
        + count(coverage.uncertainMembershipCount) + " uncertain; "
        + count(coverage.pages) + " GitHub pages."
      : "");
    text("predicate", snapshot?.predicate);
    text("limitation", coverage?.limitation);
    text("warnings", (coverage?.warnings ?? []).join("\n"));
    text("rate", coverage?.rateLimit
      ? "Rate limit: " + count(coverage.rateLimit.remaining) + " remaining; resets "
        + coverage.rateLimit.resetAt + "."
      : "Rate-limit information unavailable.");
    renderPage();
    renderDetail();
  }

  function renderPage() {
    const page = state.page;
    const current = page && page.snapshotId === state.meta?.snapshot?.id
      && page.offset === state.offset && page.limit === state.limit;
    const list = element("issues");
    list.replaceChildren();
    element("previous").disabled = !current || page.offset === 0;
    element("next").disabled = !current || page.nextOffset === null;
    if (!current) {
      text("range", state.meta?.snapshot ? "Loading page..." : "");
      return;
    }
    text("range", page.total === 0 ? "0 issues" : (page.offset + 1) + "-"
      + (page.offset + page.items.length) + " of " + page.total);
    for (const issue of page.items) {
      const button = document.createElement("button");
      button.className = "issue";
      button.textContent = "#" + issue.number + " - " + issue.title;
      button.disabled = state.pendingAction !== null || state.meta.refresh.phase === "refreshing";
      button.setAttribute("aria-pressed", String(state.meta.snapshot.selectedIssue?.id === issue.id));
      button.onclick = () => { void action("/api/select", { itemId: issue.id }); };
      list.append(button);
    }
  }

  function renderDetail() {
    const issue = state.meta?.snapshot?.selectedIssue;
    const handoff = state.meta?.snapshot?.handoff;
    const link = element("issue-link");
    link.hidden = !issue;
    element("investigate").hidden = !issue;
    if (!issue) {
      text("why", "Select one issue to request research.");
      text("labels", "");
      text("handoff", "");
      return;
    }
    link.href = issue.url;
    link.textContent = "#" + issue.number + " - " + issue.title;
    text("why", "Why included: " + issue.whyIncluded.join("; "));
    text("labels", issue.labels.join(", "));
    element("investigate").disabled = state.pendingLaunch !== null
      || state.pendingAction !== null || state.meta.refresh.phase === "refreshing"
      || handoff.phase === "sending";
    element("investigate").textContent = ["sent", "unknown"].includes(handoff.phase)
      ? "Send request again" : "Investigate in issue session";
    text("handoff", handoff.phase === "sent"
      ? "Request sent to chat." + (handoff.queued === true ? " Queued behind the current task." : "")
        + " Open the issue session from chat; results stay there."
      : handoff.phase === "unknown"
        ? "Delivery unconfirmed. Inspect chat before deliberately resending."
          + (handoff.error ? " " + handoff.error : "")
        : handoff.phase === "failed" ? "Request failed: " + handoff.error
          : handoff.phase === "sending" ? "Sending request to chat..."
            : handoff.phase === "cancelled" ? "Request cancelled before dispatch." : "");
  }

  element("investigate").onclick = () => {
    const issue = state.meta?.snapshot?.selectedIssue;
    if (issue) {
      void action("/api/investigate", { itemId: issue.id }, true);
    }
  };
  const refresh = () => { void action("/api/refresh", { area: element("area").value }); };
  element("area").onchange = refresh;
  element("refresh").onclick = refresh;
  element("previous").onclick = () => {
    state.offset = Math.max(0, state.offset - state.limit);
    void loadPage();
  };
  element("next").onclick = () => {
    if (state.page?.nextOffset !== null) {
      state.offset = state.page.nextOffset;
      void loadPage();
    }
  };
  element("page-size").onchange = () => {
    state.limit = Number(element("page-size").value);
    state.offset = 0;
    void loadPage();
  };
  const events = new EventSource("/events?token=" + encodeURIComponent(token));
  events.onmessage = (event) => {
    try {
      acceptState(JSON.parse(event.data));
      text("connection", "");
    } catch (error) {
      text("error", "Invalid state update: " + error.message);
    }
  };
  events.onerror = () => text("connection", "Connection interrupted; waiting to reconnect. No requests will be resent automatically.");
  void request("/api/state").then(acceptState).catch((error) => text("error", error.message));
}

export const HTML = `<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>ASP.NET Core Issue Triage</title>
  <style>
    body { font: 14px var(--font-sans, sans-serif); margin: 20px;
      color: var(--text-color-default, #1f2328); background: var(--background-color-default, #fff); }
    button, select { padding: 6px; margin: 3px; }
    a { color: var(--fgColor-accent, #0969da); }
    .grid { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
    .issue { display: block; width: 100%; text-align: left; margin: 6px 0; }
    .muted { color: var(--text-color-muted, #59636e); }
    #warnings, #predicate { white-space: pre-wrap; overflow-wrap: anywhere; }
    #error { color: var(--true-color-red, #cf222e); }
    @media (max-width: 800px) { .grid { grid-template-columns: 1fr; } }
  </style>
</head>
<body>
  <h1>ASP.NET Core Issue Triage</h1>
  <p class="muted">Public issue queues. Research opens in a normal issue session, using its settings and permissions.</p>
  <label>Area <select id="area"></select></label><button id="refresh">Refresh</button>
  <p id="status" role="status"></p><p id="connection" role="status"></p>
  <p id="error" role="alert"></p><p id="stale"></p>
  <p id="coverage"></p><p id="rate"></p><p id="limitation"></p><p id="warnings"></p>
  <p id="predicate" class="muted"></p>
  <div class="grid">
    <section>
      <h2>Selected queue</h2>
      <label>Page size <select id="page-size"><option>25</option><option selected>50</option><option>100</option></select></label>
      <p id="range" role="status"></p><div id="issues"></div>
      <button id="previous" disabled>Previous</button><button id="next" disabled>Next</button>
    </section>
    <aside id="detail">
      <h2>Issue</h2>
      <a id="issue-link" target="_blank" rel="noopener noreferrer" hidden></a>
      <p id="labels" class="muted"></p><p id="why">Select one issue to request research.</p>
      <button id="investigate" hidden>Investigate in issue session</button>
      <p id="handoff" role="status"></p>
      <p class="muted">Queue membership is not a severity or disposition decision. Research requires the investigate-issue skill in the issue session. Findings and follow-up stay in that session, not this canvas.</p>
    </aside>
  </div>
  <script>${initializeCanvas.toString()}; initializeCanvas();</script>
</body>
</html>`;
