export const HTML = `<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>ASP.NET Core Team App</title>
  <style>
    :root {
      color-scheme: light dark;
    }

    * {
      box-sizing: border-box;
    }

    body {
      margin: 0;
      background: var(--background-color-default, #ffffff);
      color: var(--text-color-default, #1f2328);
      font-family: var(--font-sans, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif);
      font-size: var(--text-body-medium, 14px);
      line-height: var(--leading-body-medium, 20px);
    }

    button,
    select {
      border: 1px solid var(--border-color-default, #d0d7de);
      border-radius: 6px;
      background: var(--background-color-default, #ffffff);
      color: var(--text-color-default, #1f2328);
      font: inherit;
      padding: 6px 10px;
    }

    button {
      cursor: pointer;
      font-weight: var(--font-weight-semibold, 600);
    }

    button:hover {
      background: var(--background-color-muted, #f6f8fa);
    }

    button:disabled {
      cursor: wait;
      opacity: 0.6;
    }

    button:focus-visible,
    select:focus-visible,
    summary:focus-visible {
      outline: 2px solid var(--color-focus-outline, #0969da);
      outline-offset: 2px;
    }

    h1,
    h2,
    h3,
    p {
      margin-top: 0;
    }

    h1 {
      font-size: var(--text-title-large, 26px);
      line-height: var(--leading-title-large, 32px);
      margin-bottom: 3px;
    }

    h2 {
      font-size: 18px;
      line-height: 24px;
      margin-bottom: 4px;
    }

    h3 {
      font-size: 15px;
      line-height: 20px;
      margin-bottom: 5px;
    }

    .shell {
      margin: 0 auto;
      max-width: 1420px;
      padding: 20px;
    }

    .header {
      align-items: flex-start;
      display: flex;
      gap: 16px;
      justify-content: space-between;
      margin-bottom: 16px;
    }

    .toolbar {
      align-items: center;
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
      justify-content: flex-end;
    }

    .muted {
      color: var(--text-color-muted, #59636e);
    }

    .status {
      margin-top: 6px;
      min-height: 20px;
      text-align: right;
    }

    .error {
      color: var(--true-color-red, #cf222e);
      font-weight: var(--font-weight-semibold, 600);
    }

    .scope,
    .warning,
    .ready-strip,
    .secondary {
      border: 1px solid var(--border-color-default, #d0d7de);
      border-radius: 8px;
    }

    .scope {
      margin-bottom: 14px;
      padding: 11px 13px;
    }

    .warning {
      border-color: var(--true-color-red-muted, #ff8182);
      margin-bottom: 10px;
      padding: 10px 12px;
    }

    .stats {
      display: grid;
      gap: 8px;
      grid-template-columns: repeat(auto-fit, minmax(125px, 1fr));
      margin: 14px 0 18px;
    }

    .stat {
      border: 1px solid var(--border-color-default, #d0d7de);
      border-radius: 8px;
      padding: 10px 12px;
    }

    .stat strong {
      display: block;
      font-size: 20px;
      line-height: 25px;
    }

    .lanes {
      display: grid;
      gap: 16px;
      grid-template-columns: repeat(2, minmax(300px, 1fr));
    }

    .lane-header {
      align-items: baseline;
      display: flex;
      gap: 12px;
      justify-content: space-between;
      margin-bottom: 10px;
    }

    .card {
      border: 1px solid var(--border-color-default, #d0d7de);
      border-left: 4px solid var(--border-color-default, #d0d7de);
      border-radius: 8px;
      margin-bottom: 10px;
      padding: 12px;
    }

    .ReviewNow {
      border-left-color: var(--true-color-blue, #0969da);
    }

    .NeedsRescue {
      border-left-color: var(--true-color-red, #cf222e);
    }

    .ReadyToMerge {
      border-left-color: #1a7f37;
    }

    .actor {
      background: var(--background-color-muted, #f6f8fa);
      border-radius: 6px;
      font-weight: var(--font-weight-semibold, 600);
      margin: 8px 0;
      padding: 6px 8px;
    }

    .pills,
    .actions,
    .ready-items {
      display: flex;
      flex-wrap: wrap;
      gap: 6px;
    }

    .pill {
      border: 1px solid var(--border-color-default, #d0d7de);
      border-radius: 999px;
      color: var(--text-color-muted, #59636e);
      font-size: 12px;
      padding: 2px 7px;
    }

    .actions {
      margin-top: 10px;
    }

    .primary-action {
      background: var(--true-color-blue, #0969da);
      border-color: var(--true-color-blue, #0969da);
      color: var(--color-white, #ffffff);
    }

    .blocker {
      color: var(--true-color-red, #cf222e);
      margin: 8px 0 0;
    }

    .empty {
      border: 1px dashed var(--border-color-default, #d0d7de);
      border-radius: 8px;
      color: var(--text-color-muted, #59636e);
      padding: 16px;
      text-align: center;
    }

    .ready-strip {
      margin-top: 16px;
      padding: 12px;
    }

    .ready-item {
      align-items: center;
      background: var(--background-color-muted, #f6f8fa);
      border-radius: 6px;
      display: flex;
      gap: 8px;
      max-width: 100%;
      padding: 6px 8px;
    }

    .ready-item span {
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .secondary {
      margin-top: 16px;
      overflow: hidden;
    }

    .secondary > summary {
      cursor: pointer;
      font-weight: var(--font-weight-semibold, 600);
      padding: 12px;
    }

    .secondary-content {
      border-top: 1px solid var(--border-color-default, #d0d7de);
      display: grid;
      gap: 12px;
      grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
      padding: 12px;
    }

    .secondary-group {
      min-width: 0;
    }

    .secondary-item {
      align-items: center;
      border-top: 1px solid var(--border-color-default, #d0d7de);
      display: flex;
      gap: 8px;
      justify-content: space-between;
      padding: 7px 0;
    }

    .secondary-item span {
      min-width: 0;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    footer {
      border-top: 1px solid var(--border-color-default, #d0d7de);
      margin-top: 18px;
      padding-top: 12px;
    }

    @media (max-width: 880px) {
      .header {
        display: block;
      }

      .toolbar {
        justify-content: flex-start;
        margin-top: 12px;
      }

      .status {
        text-align: left;
      }

      .lanes {
        grid-template-columns: 1fr;
      }
    }
  </style>
</head>
<body>
  <main class="shell">
    <header class="header">
      <div>
        <h1>ASP.NET Core Team App</h1>
        <div id="subtitle" class="muted">Loading the live PR attention snapshot...</div>
      </div>
      <div>
        <div class="toolbar">
          <label for="preset">Scope</label>
          <select id="preset">
            <option value="blazor">Blazor</option>
            <option value="all-repo">All ASP.NET Core</option>
          </select>
          <button id="refresh" type="button">Refresh live</button>
        </div>
        <div id="status" class="status muted" role="status" aria-live="polite"></div>
      </div>
    </header>

    <section id="scope" class="scope muted">Waiting for a complete snapshot.</section>
    <section id="warnings"></section>
    <section id="stats" class="stats" aria-label="Queue statistics"></section>
    <section id="lanes" class="lanes"></section>
    <section id="ready" class="ready-strip"></section>
    <details id="secondary" class="secondary">
      <summary>Secondary classifications</summary>
      <div id="secondary-content" class="secondary-content"></div>
    </details>
    <footer class="muted">
      The deterministic PR Attention skill owns scope, classification, ordering, caps, and next
      actor. This app does not add an AI score or mutate GitHub.
    </footer>
  </main>
  <script>
    const secondaryBuckets = [
      "WaitingOnAuthor",
      "WaitingOnCI",
      "DesignDecision",
      "Draft",
      "Excluded",
    ];
    const elements = {
      lanes: document.getElementById("lanes"),
      preset: document.getElementById("preset"),
      ready: document.getElementById("ready"),
      refresh: document.getElementById("refresh"),
      scope: document.getElementById("scope"),
      secondary: document.getElementById("secondary"),
      secondaryContent: document.getElementById("secondary-content"),
      stats: document.getElementById("stats"),
      status: document.getElementById("status"),
      subtitle: document.getElementById("subtitle"),
      warnings: document.getElementById("warnings"),
    };

    function element(name, className, text) {
      const node = document.createElement(name);
      if (className) {
        node.className = className;
      }
      if (text !== undefined) {
        node.textContent = text;
      }
      return node;
    }

    function render(state) {
      const snapshot = state.snapshot;
      renderStatus(state.refresh, Boolean(snapshot));
      elements.refresh.disabled = state.refresh.phase === "refreshing";

      if (!snapshot) {
        elements.subtitle.textContent = "Loading the live PR attention snapshot...";
        elements.scope.textContent = "Waiting for a complete snapshot.";
        elements.stats.replaceChildren();
        elements.lanes.replaceChildren(element("div", "empty", "Loading live GitHub data..."));
        elements.ready.hidden = true;
        elements.secondary.hidden = true;
        elements.warnings.replaceChildren();
        return;
      }

      elements.preset.value = snapshot.options.preset;
      elements.subtitle.textContent =
        snapshot.repository + " | generated " + new Date(snapshot.generatedAt).toLocaleString();
      elements.scope.textContent =
        snapshot.filter.description + " | " + snapshot.filter.selection
        + " | " + snapshot.filter.coverage;
      elements.ready.hidden = false;
      elements.secondary.hidden = false;

      renderWarnings(snapshot.warnings);
      renderStats(snapshot);
      renderPrimaryLanes(snapshot);
      renderReady(snapshot);
      renderSecondary(snapshot);
    }

    function renderStatus(refresh, hasSnapshot) {
      elements.status.classList.remove("error");
      if (refresh.phase === "refreshing") {
        elements.status.textContent = hasSnapshot
          ? "Refreshing live data. Showing the previous complete snapshot."
          : "Querying live GitHub data...";
        return;
      }
      if (refresh.phase === "error") {
        elements.status.classList.add("error");
        elements.status.textContent = hasSnapshot
          ? "Refresh failed. Showing the previous complete snapshot: " + refresh.error
          : "Unable to load the queue: " + refresh.error;
        return;
      }
      elements.status.textContent = refresh.completedAt
        ? "Updated " + new Date(refresh.completedAt).toLocaleTimeString()
        : "";
    }

    function renderWarnings(warnings) {
      elements.warnings.replaceChildren();
      for (const warning of warnings) {
        elements.warnings.append(element("div", "warning", warning));
      }
    }

    function renderStats(snapshot) {
      const counts = snapshot.census.byBucket;
      const values = [
        ["Open PRs", snapshot.census.openPullRequests],
        ["Matched scope", snapshot.census.matched],
        [snapshot.display.buckets.ReviewNow.label, counts.ReviewNow],
        [snapshot.display.buckets.NeedsRescue.label, counts.NeedsRescue],
        [snapshot.display.buckets.ReadyToMerge.label, counts.ReadyToMerge],
        ["Incidental paths excluded", snapshot.census.incidentalPathExcluded],
      ];
      elements.stats.replaceChildren();
      for (const value of values) {
        const card = element("div", "stat");
        card.append(element("strong", "", String(value[1])));
        card.append(element("span", "muted", value[0]));
        elements.stats.append(card);
      }
    }

    function renderPrimaryLanes(snapshot) {
      const lanes = [
        {
          bucket: "ReviewNow",
          items: snapshot.primary.reviewNow,
          overflow: snapshot.overflow.reviewNow,
        },
        {
          bucket: "NeedsRescue",
          items: snapshot.primary.needsRescue,
          overflow: snapshot.overflow.needsRescue,
        },
      ];
      elements.lanes.replaceChildren();
      for (const lane of lanes) {
        const metadata = snapshot.display.buckets[lane.bucket];
        const section = element("section", "lane");
        const header = element("div", "lane-header");
        const title = element("div");
        title.append(element("h2", "", metadata.label));
        title.append(element("div", "muted", metadata.description));
        header.append(title);
        header.append(element("span", "muted", lane.overflow ? lane.overflow + " more" : ""));
        section.append(header);
        if (!lane.items.length) {
          section.append(element("div", "empty", "No pull requests in this lane."));
        } else {
          for (const item of lane.items) {
            section.append(renderCard(snapshot, item));
          }
        }
        elements.lanes.append(section);
      }
    }

    function renderCard(snapshot, item) {
      const card = element("article", "card " + item.bucket);
      card.append(element("h3", "", "#" + item.number + " " + item.title));
      card.append(
        element(
          "div",
          "muted",
          "@" + item.author + " | open " + item.ageDays + "d | idle " + item.idleDays + "d",
        ),
      );
      card.append(element("div", "actor", "Next actor: " + item.nextActor));

      const pills = element("div", "pills");
      for (const metadata of item.reasons) {
        const pill = element("span", "pill", metadata.label);
        pill.title = metadata.description;
        pills.append(pill);
      }
      card.append(pills);

      for (const blocker of item.blockers) {
        card.append(element("p", "blocker", blocker));
      }

      const actions = element("div", "actions");
      actions.append(actionButton(item, "open", "Open PR", false));
      if (item.bucket === "ReviewNow") {
        actions.append(actionButton(item, "review", "Review", true));
      } else if (item.bucket === "NeedsRescue") {
        actions.append(actionButton(item, "investigate-rescue", "Investigate rescue", true));
      }
      card.append(actions);
      return card;
    }

    function renderReady(snapshot) {
      const metadata = snapshot.display.buckets.ReadyToMerge;
      elements.ready.replaceChildren();
      elements.ready.append(element("h2", "", metadata.label));
      elements.ready.append(element("p", "muted", metadata.description));
      const items = element("div", "ready-items");
      if (!snapshot.readyToMerge.length) {
        items.append(element("span", "muted", "No pull requests are ready to merge."));
      } else {
        for (const item of snapshot.readyToMerge) {
          const row = element("div", "ready-item ReadyToMerge");
          row.append(element("span", "", "#" + item.number + " " + item.title));
          row.append(actionButton(item, "open", "Open PR", false));
          items.append(row);
        }
      }
      elements.ready.append(items);
    }

    function renderSecondary(snapshot) {
      elements.secondaryContent.replaceChildren();
      for (const bucket of secondaryBuckets) {
        const metadata = snapshot.display.buckets[bucket];
        const allItems = snapshot.secondary[bucket] || [];
        const shownItems = allItems.slice(0, 12);
        const section = element("section", "secondary-group");
        section.append(element("h3", "", metadata.label + " (" + allItems.length + ")"));
        section.append(element("p", "muted", metadata.description));
        if (!shownItems.length) {
          section.append(element("div", "muted", "None"));
        } else {
          for (const item of shownItems) {
            const row = element("div", "secondary-item");
            row.append(element("span", "", "#" + item.number + " " + item.title));
            row.append(actionButton(item, "open", "Open", false));
            section.append(row);
          }
          if (allItems.length > shownItems.length) {
            section.append(
              element("div", "muted", String(allItems.length - shownItems.length) + " more not shown"),
            );
          }
        }
        elements.secondaryContent.append(section);
      }
    }

    function actionButton(item, kind, label, primary) {
      const button = element("button", primary ? "primary-action" : "", label);
      button.type = "button";
      button.addEventListener("click", () => runAction(button, item.id, kind));
      return button;
    }

    async function runAction(button, itemId, kind) {
      button.disabled = true;
      elements.status.classList.remove("error");
      elements.status.textContent =
        kind === "open" ? "Opening pull request..." : "Sending read-only work to a new session...";
      try {
        const response = await fetch("/api/action", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ itemId: itemId, kind: kind }),
        });
        const body = await response.json();
        if (!response.ok) {
          throw new Error(body.error || "Action failed");
        }
        elements.status.textContent =
          kind === "open" ? "Pull request opened." : "Read-only session request queued.";
      } catch (error) {
        elements.status.classList.add("error");
        elements.status.textContent = error.message;
      } finally {
        button.disabled = false;
      }
    }

    async function refresh() {
      elements.refresh.disabled = true;
      elements.status.classList.remove("error");
      elements.status.textContent = "Querying live GitHub data...";
      try {
        const response = await fetch("/api/refresh", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ preset: elements.preset.value }),
        });
        const body = await response.json();
        render(body.state || body);
        if (!response.ok) {
          throw new Error(body.error || "Refresh failed");
        }
      } catch (error) {
        elements.status.classList.add("error");
        elements.status.textContent = error.message;
        elements.refresh.disabled = false;
      }
    }

    elements.refresh.addEventListener("click", refresh);
    const events = new EventSource("/events");
    events.addEventListener("state", (event) => render(JSON.parse(event.data)));
    fetch("/api/state", { cache: "no-store" })
      .then((response) => {
        if (!response.ok) {
          throw new Error("Unable to load canvas state");
        }
        return response.json();
      })
      .then(render)
      .catch((error) => {
        elements.status.classList.add("error");
        elements.status.textContent = error.message;
      });
  </script>
</body>
</html>`;
