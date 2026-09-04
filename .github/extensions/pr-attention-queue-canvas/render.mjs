export const HTML = `<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>PR Attention Queue</title>
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
    a {
      font: inherit;
    }

    button {
      border: 1px solid var(--border-color-default, #d0d7de);
      border-radius: 6px;
      background: var(--background-color-default, #ffffff);
      color: var(--text-color-default, #1f2328);
      cursor: pointer;
      padding: 6px 10px;
    }

    button:hover {
      background: var(--background-color-muted, #f6f8fa);
    }

    button:focus-visible,
    a:focus-visible {
      outline: 2px solid var(--color-focus-outline, #0969da);
      outline-offset: 2px;
    }

    button:disabled {
      cursor: wait;
      opacity: 0.6;
    }

    .shell {
      margin: 0 auto;
      max-width: 1500px;
      padding: 20px;
    }

    .header {
      align-items: flex-start;
      display: flex;
      gap: 16px;
      justify-content: space-between;
      margin-bottom: 16px;
    }

    h1 {
      font-size: var(--text-title-large, 26px);
      line-height: var(--leading-title-large, 32px);
      margin: 0 0 4px;
    }

    h2,
    h3,
    p {
      margin-top: 0;
    }

    .muted {
      color: var(--text-color-muted, #59636e);
    }

    .toolbar {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
      justify-content: flex-end;
    }

    .source {
      align-items: center;
      border: 1px solid var(--border-color-default, #d0d7de);
      border-radius: 999px;
      display: inline-flex;
      font-weight: var(--font-weight-semibold, 600);
      padding: 5px 9px;
    }

    .status {
      min-height: 20px;
      text-align: right;
    }

    .scope,
    .warning {
      border: 1px solid var(--border-color-default, #d0d7de);
      border-radius: 8px;
      margin-bottom: 16px;
      padding: 12px 14px;
    }

    .warning {
      border-color: var(--true-color-red-muted, #ff8182);
    }

    .stats {
      display: grid;
      gap: 10px;
      grid-template-columns: repeat(auto-fit, minmax(135px, 1fr));
      margin-bottom: 18px;
    }

    .stat {
      border: 1px solid var(--border-color-default, #d0d7de);
      border-radius: 8px;
      padding: 12px;
    }

    .stat strong {
      display: block;
      font-size: 22px;
      line-height: 26px;
    }

    .columns {
      display: grid;
      gap: 14px;
      grid-template-columns: repeat(3, minmax(280px, 1fr));
    }

    .lane {
      min-width: 0;
    }

    .lane-header {
      align-items: baseline;
      display: flex;
      justify-content: space-between;
    }

    .lane-header h2 {
      font-size: 18px;
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

    .card h3 {
      font-size: 15px;
      line-height: 20px;
      margin-bottom: 5px;
    }

    .card a {
      color: var(--text-color-default, #1f2328);
      text-decoration: none;
    }

    .card a:hover {
      text-decoration: underline;
    }

    .actor {
      background: var(--background-color-muted, #f6f8fa);
      border-radius: 6px;
      font-weight: var(--font-weight-semibold, 600);
      margin: 9px 0;
      padding: 7px 8px;
    }

    .pills {
      display: flex;
      flex-wrap: wrap;
      gap: 5px;
    }

    .pill {
      border: 1px solid var(--border-color-default, #d0d7de);
      border-radius: 999px;
      color: var(--text-color-muted, #59636e);
      font-family: var(--font-mono, "SFMono-Regular", Consolas, monospace);
      font-size: var(--text-code-inline, 12px);
      padding: 2px 6px;
    }

    .blocker {
      color: var(--true-color-red, #cf222e);
      margin: 8px 0 0;
    }

    .bucket-counts {
      display: grid;
      gap: 8px;
      grid-template-columns: repeat(auto-fit, minmax(145px, 1fr));
      margin-top: 18px;
    }

    .bucket-count {
      background: var(--background-color-muted, #f6f8fa);
      border-radius: 6px;
      display: flex;
      justify-content: space-between;
      padding: 8px 10px;
    }

    .empty {
      border: 1px dashed var(--border-color-default, #d0d7de);
      border-radius: 8px;
      color: var(--text-color-muted, #59636e);
      padding: 16px;
      text-align: center;
    }

    .error {
      color: var(--true-color-red, #cf222e);
      font-weight: var(--font-weight-semibold, 600);
    }

    footer {
      border-top: 1px solid var(--border-color-default, #d0d7de);
      color: var(--text-color-muted, #59636e);
      margin-top: 20px;
      padding-top: 12px;
    }

    @media (max-width: 980px) {
      .columns {
        grid-template-columns: 1fr;
      }

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
    }
  </style>
</head>
<body>
  <main class="shell">
    <header class="header">
      <div>
        <h1>PR Attention Queue</h1>
        <div id="subtitle" class="muted">Loading the deterministic queue...</div>
      </div>
      <div>
        <div class="toolbar">
          <span id="source" class="source">Loading</span>
          <button id="fixture-refresh" type="button">Refresh fixture</button>
          <button id="live-refresh" type="button">Refresh live (~40s)</button>
        </div>
        <div id="status" class="status muted" role="status" aria-live="polite"></div>
      </div>
    </header>

    <section id="scope" class="scope"></section>
    <section id="warnings"></section>
    <section id="stats" class="stats" aria-label="Queue statistics"></section>
    <section id="lanes" class="columns"></section>
    <section>
      <h2>Full classification</h2>
      <div id="bucket-counts" class="bucket-counts"></div>
    </section>
    <footer>
      The PowerShell skill owns classification and ordering. This canvas displays its next actor,
      reason codes, blockers, caps, and overflow without adding an AI score.
    </footer>
  </main>
  <script>
    const lanes = [
      { bucket: "ReviewNow", title: "Review now", actor: "Human reviewer" },
      { bucket: "NeedsRescue", title: "Needs rescue", actor: "Maintainer / triager" },
      { bucket: "ReadyToMerge", title: "Ready to merge", actor: "Merger" },
    ];
    const allBuckets = [
      "ReviewNow",
      "NeedsRescue",
      "ReadyToMerge",
      "WaitingOnAuthor",
      "WaitingOnCI",
      "DesignDecision",
      "Draft",
      "Excluded",
    ];
    const buttons = [
      document.getElementById("fixture-refresh"),
      document.getElementById("live-refresh"),
    ];

    function element(name, className, text) {
      const node = document.createElement(name);
      if (className) node.className = className;
      if (text !== undefined) node.textContent = text;
      return node;
    }

    function render(state) {
      const queue = state.queue;
      document.getElementById("source").textContent =
        state.options.source === "live" ? "Live GitHub data" : "Offline fixture";
      document.getElementById("subtitle").textContent =
        queue.repository + " | generated " + new Date(queue.generatedAt).toLocaleString();
      document.getElementById("scope").textContent =
        queue.filter.description + " - " + queue.filter.selection;

      renderWarnings(queue.warnings);
      renderStats(queue);
      renderLanes(queue);
      renderBucketCounts(queue.census.byBucket);
    }

    function renderWarnings(warnings) {
      const root = document.getElementById("warnings");
      root.replaceChildren();
      for (const warning of warnings) {
        root.append(element("div", "warning", warning));
      }
    }

    function renderStats(queue) {
      const values = [
        ["Open PRs", queue.census.openPullRequests],
        ["Matched scope", queue.census.matched],
        ["Review now", queue.census.byBucket.ReviewNow],
        ["Needs rescue", queue.census.byBucket.NeedsRescue],
        ["Digest shown", queue.items.filter((item) => item.shownInDigest).length],
        ["Incidental paths excluded", queue.census.incidentalPathExcluded],
      ];
      const root = document.getElementById("stats");
      root.replaceChildren();
      for (const pair of values) {
        const card = element("div", "stat");
        card.append(element("strong", "", String(pair[1])));
        card.append(element("span", "muted", pair[0]));
        root.append(card);
      }
    }

    function renderLanes(queue) {
      const root = document.getElementById("lanes");
      root.replaceChildren();

      for (const lane of lanes) {
        const section = element("section", "lane");
        const header = element("div", "lane-header");
        header.append(element("h2", "", lane.title));
        const overflow = queue.overflow[lane.bucket.charAt(0).toLowerCase() + lane.bucket.slice(1)] || 0;
        header.append(element("span", "muted", overflow ? overflow + " more" : lane.actor));
        section.append(header);

        const items = queue.items.filter(
          (item) => item.bucket === lane.bucket && item.shownInDigest,
        );
        if (!items.length) {
          section.append(element("div", "empty", "No pull requests in this digest lane."));
        } else {
          for (const item of items) section.append(renderCard(item));
        }
        root.append(section);
      }
    }

    function renderCard(item) {
      const card = element("article", "card " + item.bucket);
      const heading = element("h3");
      const link = element("a", "", "#" + item.number + " " + item.title);
      link.href = item.url;
      link.target = "_blank";
      link.rel = "noreferrer";
      heading.append(link);
      card.append(heading);
      card.append(
        element(
          "div",
          "muted",
          "@" + item.author + " | open " + item.ageDays + "d | idle " + item.idleDays + "d",
        ),
      );
      card.append(element("div", "actor", "Next actor: " + item.nextActor));

      const pills = element("div", "pills");
      for (const reason of item.reasonCodes) pills.append(element("span", "pill", reason));
      card.append(pills);

      for (const blocker of item.blockers) {
        card.append(element("p", "blocker", blocker));
      }
      return card;
    }

    function renderBucketCounts(counts) {
      const root = document.getElementById("bucket-counts");
      root.replaceChildren();
      for (const bucket of allBuckets) {
        const row = element("div", "bucket-count");
        row.append(element("span", "", bucket));
        row.append(element("strong", "", String(counts[bucket] || 0)));
        root.append(row);
      }
    }

    async function load() {
      const response = await fetch("/api/state", { cache: "no-store" });
      if (!response.ok) throw new Error("Unable to load queue state");
      render(await response.json());
    }

    async function refresh(source) {
      const status = document.getElementById("status");
      buttons.forEach((button) => { button.disabled = true; });
      status.classList.remove("error");
      status.textContent = source === "live" ? "Querying GitHub..." : "Refreshing fixture...";
      try {
        const response = await fetch("/api/refresh", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ source }),
        });
        const body = await response.json();
        if (!response.ok) throw new Error(body.error || "Refresh failed");
        render(body);
        status.textContent = "Updated " + new Date(body.queue.generatedAt).toLocaleTimeString();
      } catch (error) {
        status.classList.add("error");
        status.textContent = error.message;
      } finally {
        buttons.forEach((button) => { button.disabled = false; });
      }
    }

    document.getElementById("fixture-refresh").addEventListener("click", () => refresh("fixture"));
    document.getElementById("live-refresh").addEventListener("click", () => refresh("live"));
    const events = new EventSource("/events");
    events.addEventListener("state", (event) => render(JSON.parse(event.data)));
    load().catch((error) => {
      const status = document.getElementById("status");
      status.classList.add("error");
      status.textContent = error.message;
    });
  </script>
</body>
</html>`;
