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

    .inbox {
      display: grid;
      gap: 12px;
      margin-top: 16px;
    }

    .workspace {
      display: grid;
      gap: 16px;
      grid-template-columns: minmax(0, 1.2fr) minmax(340px, 0.8fr);
      align-items: start;
      margin-top: 16px;
    }

    .main-column,
    .detail-column {
      display: grid;
      gap: 16px;
      min-width: 0;
      grid-template-columns: minmax(0, 1fr);
    }

    .detail-column {
      position: sticky;
      top: 16px;
      align-self: start;
      max-height: calc(100vh - 32px);
      overflow: auto;
    }

    .personal-inbox {
      border: 2px solid var(--true-color-blue-muted, #54aeff);
      border-radius: 8px;
      margin-top: 16px;
      padding: 12px;
    }

    .personal-inbox > .inbox-header {
      margin-bottom: 10px;
    }

    .personal-inbox > .muted {
      overflow-wrap: anywhere;
    }

    .personal-item {
      border: 1px solid var(--border-color-default, #d0d7de);
      border-left: 4px solid var(--true-color-blue, #0969da);
      border-radius: 8px;
      padding: 12px;
    }

    .personal-signals {
      display: flex;
      flex-wrap: wrap;
      gap: 6px;
      margin: 8px 0;
    }

    .coverage {
      font-size: 12px;
      margin-top: 8px;
    }

    .inbox-group {
      border: 1px solid var(--border-color-default, #d0d7de);
      border-radius: 8px;
      width: 100%;
      min-width: 0;
      box-sizing: border-box;
      padding: 12px;
    }

    .inbox-header {
      align-items: baseline;
      display: flex;
      flex-wrap: wrap;
      justify-content: space-between;
      gap: 12px;
      margin-bottom: 10px;
    }

    .inbox-header > h2,
    .inbox-header > span {
      min-width: 0;
    }

    .inbox-header > span {
      overflow-wrap: anywhere;
    }

    .inbox-list {
      display: grid;
      gap: 10px;
    }

    .inbox-item {
      border: 1px solid var(--border-color-default, #d0d7de);
      border-left: 4px solid var(--border-color-default, #d0d7de);
      border-radius: 8px;
      padding: 12px;
    }

    .selected-card,
    .list-row,
    .personal-row,
    .inbox-row {
      border: 1px solid var(--border-color-default, #d0d7de);
      border-radius: 8px;
      padding: 10px 12px;
    }

    .selected-card,
    .personal-inbox,
    .inbox,
    .lane,
    .ready-strip,
    .secondary,
    .discussion-verification,
    .list-row,
    .personal-row,
    .inbox-row {
      box-sizing: border-box;
      min-width: 0;
    }

    .personal-inbox,
    .inbox,
    .lane,
    .ready-strip,
    .secondary,
    .discussion-verification,
    .selected-detail {
      max-width: 100%;
      width: 100%;
    }

    .list-row,
    .personal-row,
    .inbox-row {
      align-items: flex-start;
      display: grid;
      gap: 8px;
      grid-template-columns: minmax(0, 1fr) auto;
    }

    .list-row.selected,
    .personal-row.selected,
    .inbox-row.selected {
      border-color: var(--true-color-blue, #0969da);
      box-shadow: 0 0 0 1px var(--true-color-blue-muted, #54aeff);
    }

    .row-button {
      align-items: flex-start;
      background: transparent;
      border: 0;
      color: inherit;
      cursor: pointer;
      display: grid;
      gap: 4px;
      justify-items: start;
      padding: 0;
      text-align: left;
      min-width: 0;
      width: 100%;
    }

    .row-button[aria-pressed="true"] {
      color: var(--true-color-blue, #0969da);
      font-weight: var(--font-weight-semibold, 600);
    }

    .row-button:focus-visible {
      outline-offset: 3px;
    }

    .row-title {
      font-size: 14px;
      font-weight: var(--font-weight-semibold, 600);
      overflow-wrap: anywhere;
      word-break: break-word;
    }

    .row-status {
      color: var(--text-color-default, #1f2328);
      font-size: 12px;
      font-weight: var(--font-weight-semibold, 600);
      line-height: 18px;
      overflow-wrap: anywhere;
    }

    .row-meta,
    .row-summary,
    .row-coverage {
      color: var(--text-color-muted, #59636e);
      font-size: 12px;
      line-height: 18px;
      overflow-wrap: anywhere;
    }

    .row-pills {
      display: flex;
      flex-wrap: wrap;
      gap: 4px;
    }

    .row-actions {
      align-items: center;
      display: flex;
      gap: 6px;
      justify-content: flex-end;
    }

    .inbox-evidence {
      background: var(--background-color-muted, #f6f8fa);
      border-radius: 6px;
      margin-top: 8px;
      padding: 8px;
    }

    .evidence-link {
      color: var(--color-link, #0969da);
      font-weight: var(--font-weight-semibold, 600);
      text-decoration: none;
    }

    .evidence-link:hover {
      text-decoration: underline;
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

    .discussion-verification {
      border: 1px solid var(--true-color-orange, #bc4c00);
      border-radius: 8px;
      margin-top: 16px;
      padding: 12px;
    }

    .discussion-evidence {
      background: var(--background-color-muted, #f6f8fa);
      border-radius: 6px;
      margin-top: 8px;
      padding: 8px;
    }

    .discussion-evidence > summary {
      cursor: pointer;
      font-weight: var(--font-weight-semibold, 600);
    }

    .discussion-comment {
      border-top: 1px solid var(--border-color-default, #d0d7de);
      margin-top: 8px;
      padding-top: 8px;
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

    .selected-card h3 {
      overflow-wrap: anywhere;
      word-break: break-word;
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

    .review-actions {
      align-items: flex-start;
      display: flex;
      flex-wrap: wrap;
      gap: 6px;
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

    .selected-detail {
      border: 1px solid var(--border-color-default, #d0d7de);
      border-radius: 8px;
      padding: 12px;
      scroll-margin-top: 16px;
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

      .workspace {
        grid-template-columns: 1fr;
      }

      .detail-column {
        position: static;
        max-height: none;
        overflow: visible;
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
        <div id="action-status" class="status muted" role="status" aria-live="polite"></div>
      </div>
    </header>

    <section id="scope" class="scope muted">Waiting for a complete snapshot.</section>
    <section id="warnings"></section>
    <section id="stats" class="stats" aria-label="Queue statistics"></section>
    <section class="workspace" aria-label="Attention workspace">
      <div class="main-column">
        <section id="personal-inbox" class="personal-inbox" aria-live="polite"></section>
        <section id="inbox" class="inbox" aria-live="polite"></section>
        <section id="lanes" class="lanes"></section>
        <section id="discussion-verification" class="discussion-verification"></section>
        <section id="ready" class="ready-strip"></section>
        <details id="secondary" class="secondary">
          <summary>Secondary classifications</summary>
          <div id="secondary-content" class="secondary-content"></div>
        </details>
      </div>
      <div class="detail-column">
        <section id="selected-detail" class="selected-detail" aria-live="polite"></section>
      </div>
    </section>
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
      inbox: document.getElementById("inbox"),
      personalInbox: document.getElementById("personal-inbox"),
      selectedDetail: document.getElementById("selected-detail"),
      lanes: document.getElementById("lanes"),
      preset: document.getElementById("preset"),
      ready: document.getElementById("ready"),
      discussionVerification: document.getElementById("discussion-verification"),
      refresh: document.getElementById("refresh"),
      actionStatus: document.getElementById("action-status"),
      scope: document.getElementById("scope"),
      secondary: document.getElementById("secondary"),
      secondaryContent: document.getElementById("secondary-content"),
      stats: document.getElementById("stats"),
      status: document.getElementById("status"),
      subtitle: document.getElementById("subtitle"),
      warnings: document.getElementById("warnings"),
    };
    let selectedItemKey = null;
    let lastRenderedState = null;
    let pendingReview = null;
    let actionNotice = { phase: "idle", message: "" };
    let personalInventoryOpen = false;

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

    function formatOptionalDate(value, formatter = (date) => date.toLocaleString()) {
      if (value === null || value === undefined || value === "") {
        return "unknown";
      }
      const date = new Date(value);
      return Number.isNaN(date.getTime()) ? "unknown" : formatter(date);
    }

    function selectionKeyForItem(item) {
      return Number.isInteger(item?.number) ? "item:" + item.number : null;
    }

    function selectionKeyForPersonalItem(item) {
      return Number.isInteger(item?.number) ? "personal:" + item.number : null;
    }

    function selectionKeyForInboxItem(item) {
      return Number.isInteger(item?.number) ? "inbox:" + item.number : null;
    }

    function selectItem(key) {
      selectedItemKey = key;
      render(lastRenderedState);
      requestAnimationFrame(() => {
        elements.selectedDetail?.scrollIntoView({ block: "start", inline: "nearest" });
      });
    }

    function isSelectedKey(key) {
      return key !== null && key === selectedItemKey;
    }

    function isReviewPending() {
      return pendingReview !== null;
    }

    function findQueueItemByNumber(snapshot, number) {
      const searchGroups = [
        snapshot.primary?.reviewNow ?? [],
        snapshot.primary?.needsRescue ?? [],
        snapshot.readyToMerge ?? [],
        snapshot.discussionVerification ?? [],
        ...Object.values(snapshot.secondary ?? {}),
        ...Object.values(snapshot.overflowItems ?? {}),
      ];
      for (const group of searchGroups) {
        const found = group.find((item) => item.number === number);
        if (found) {
          return found;
        }
      }
      return null;
    }

    function findInboxItemByNumber(snapshot, number) {
      const inbox = snapshot.inbox ?? {};
      const searchGroups = [
        inbox.recentCommunity?.inventory ?? [],
        inbox.community?.inventory ?? [],
        inbox.unclassified?.inventory ?? [],
      ];
      for (const group of searchGroups) {
        const found = group.find((item) => item.number === number);
        if (found) {
          return found;
        }
      }
      return null;
    }

    function findPersonalItemByNumber(snapshot, number) {
      return (snapshot.personalInbox?.items ?? []).find((item) => item.number === number) ?? null;
    }

    function renderSelectableRow({ key, title, meta = [], status = null, summary = [], pills = [], href = null, hrefLabel = "Open PR", selected = false, pending = false, onSelect }) {
      const row = element("article", "list-row" + (selected ? " selected" : ""));
      const main = element("button", "row-button", "");
      main.type = "button";
      main.setAttribute("aria-pressed", selected ? "true" : "false");
      main.disabled = pending;
      main.addEventListener("click", onSelect);
      main.append(element("span", "row-title", title));
      if (status) {
        main.append(
          element(
            "span",
            "row-status",
            status.detail ? status.label + " · " + status.detail : status.label,
          ),
        );
      }
      for (const line of meta) {
        main.append(element("span", "row-meta", line));
      }
      for (const line of summary) {
        main.append(element("span", "row-summary", line));
      }
      if (pills.length) {
        const pillRow = element("div", "row-pills");
        for (const pill of pills) {
          pillRow.append(element("span", "pill", pill));
        }
        main.append(pillRow);
      }
      row.append(main);
      const actions = element("div", "row-actions");
      if (href) {
        const open = element("a", "evidence-link", hrefLabel);
        open.href = href;
        open.target = "_blank";
        open.rel = "noreferrer noopener";
        actions.append(open);
      }
      row.append(actions);
      if (selected) {
        row.dataset.selected = "true";
      }
      return row;
    }

    function findQueueItem(snapshot, number) {
      return findQueueItemByNumber(snapshot, number);
    }

    function findInboxItem(snapshot, number) {
      return findInboxItemByNumber(snapshot, number);
    }

    function resolveSelectedItem(snapshot) {
      const selectedNumber = typeof selectedItemKey === "string"
        ? Number(selectedItemKey.split(":")[1])
        : Number.NaN;
      const queueItem = typeof selectedItemKey === "string" && selectedItemKey.startsWith("item:")
        ? findQueueItem(snapshot, selectedNumber)
        : null;
      if (queueItem) {
        return {
          type: "queue",
          item: queueItem,
          personalItem: findPersonalItemByNumber(snapshot, selectedNumber),
          inboxItem: findInboxItemByNumber(snapshot, selectedNumber),
        };
      }
      const inboxItem = typeof selectedItemKey === "string" && selectedItemKey.startsWith("inbox:")
        ? findInboxItem(snapshot, selectedNumber)
        : null;
      if (inboxItem) {
        const canonicalQueueItem = findQueueItem(snapshot, selectedNumber);
        if (canonicalQueueItem) {
          return {
            type: "queue",
            item: canonicalQueueItem,
            inboxItem,
            personalItem: findPersonalItemByNumber(snapshot, selectedNumber),
          };
        }
        return { type: "inbox", item: inboxItem };
      }
      const personalItem = typeof selectedItemKey === "string" && selectedItemKey.startsWith("personal:")
        ? findPersonalItemByNumber(snapshot, selectedNumber)
        : null;
      if (personalItem) {
        const canonicalQueueItem = findQueueItem(snapshot, selectedNumber);
        if (canonicalQueueItem) {
          return {
            type: "queue",
            item: canonicalQueueItem,
            personalItem,
            inboxItem: findInboxItemByNumber(snapshot, selectedNumber),
          };
        }
        return { type: "personal", item: personalItem };
      }

      const fallbackQueue = snapshot.primary?.reviewNow?.[0]
        ?? snapshot.primary?.needsRescue?.[0]
        ?? snapshot.readyToMerge?.[0]
        ?? snapshot.discussionVerification?.[0]
        ?? Object.values(snapshot.secondary ?? {}).flat().find((item) => item?.number)
        ?? Object.values(snapshot.overflowItems ?? {}).flat().find((item) => item?.number)
        ?? null;
      if (fallbackQueue) {
        selectedItemKey = selectionKeyForItem(fallbackQueue);
        return {
          type: "queue",
          item: fallbackQueue,
          personalItem: findPersonalItemByNumber(snapshot, fallbackQueue.number),
          inboxItem: findInboxItemByNumber(snapshot, fallbackQueue.number),
        };
      }

      const fallbackPersonal = snapshot.personalInbox?.items?.[0] ?? null;
      if (fallbackPersonal) {
        selectedItemKey = selectionKeyForPersonalItem(fallbackPersonal);
        const canonicalQueueItem = findQueueItem(snapshot, fallbackPersonal.number);
        if (canonicalQueueItem) {
          return {
            type: "queue",
            item: canonicalQueueItem,
            personalItem: fallbackPersonal,
            inboxItem: findInboxItemByNumber(snapshot, fallbackPersonal.number),
          };
        }
        return { type: "personal", item: fallbackPersonal };
      }

      const fallbackInbox = snapshot.inbox?.recentCommunity?.inventory?.[0]
        ?? snapshot.inbox?.community?.inventory?.[0]
        ?? snapshot.inbox?.unclassified?.inventory?.[0]
        ?? null;
      if (fallbackInbox) {
        selectedItemKey = selectionKeyForInboxItem(fallbackInbox);
        const canonicalQueueItem = findQueueItem(snapshot, fallbackInbox.number);
        if (canonicalQueueItem) {
          return {
            type: "queue",
            item: canonicalQueueItem,
            inboxItem: fallbackInbox,
            personalItem: findPersonalItemByNumber(snapshot, fallbackInbox.number),
          };
        }
        return { type: "inbox", item: fallbackInbox };
      }

      selectedItemKey = null;
      return null;
    }

    function render(state) {
      lastRenderedState = state;
      const snapshot = state.snapshot;
      renderStatus(state.refresh, snapshot);
      renderActionStatus();
      elements.refresh.disabled = state.refresh.phase === "refreshing";

      if (!snapshot) {
        elements.subtitle.textContent = "Loading the live PR attention snapshot...";
        elements.scope.textContent = "Waiting for a complete snapshot.";
        elements.stats.replaceChildren();
        elements.personalInbox.replaceChildren();
        elements.lanes.replaceChildren(element("div", "empty", "Loading live GitHub data..."));
        elements.selectedDetail.replaceChildren();
        elements.selectedDetail.hidden = true;
        elements.ready.hidden = true;
        elements.discussionVerification.hidden = true;
        elements.secondary.hidden = true;
        elements.warnings.replaceChildren();
        return;
      }

      const inboxAvailable = hasInboxData(snapshot);
      elements.preset.value = snapshot.options.preset;
      elements.subtitle.textContent =
        snapshot.repository + " | generated " + new Date(snapshot.generatedAt).toLocaleString();
      const excludedAuthors = snapshot.filter.excludeDigestAuthors ?? [];
      const digestExclusions = excludedAuthors.length
        ? " | digest excludes @" + excludedAuthors.join(", @")
        : "";
      elements.scope.textContent =
        snapshot.filter.description + " | " + snapshot.filter.selection
        + " | " + snapshot.filter.coverage + digestExclusions;
      elements.ready.hidden = false;
      elements.discussionVerification.hidden = false;
      elements.secondary.hidden = false;

      renderWarnings(snapshot.warnings);
      renderStats(snapshot);
      renderPersonalInbox(snapshot);
      if (inboxAvailable) {
        renderInbox(snapshot);
      } else {
        elements.inbox.replaceChildren();
      }
      renderPrimaryLanes(snapshot, inboxAvailable);
      renderDiscussionVerification(snapshot);
      renderReady(snapshot);
      renderSecondary(snapshot);
      renderSelectedDetail(snapshot);
    }

    function renderStatus(refresh, snapshot) {
      const hasSnapshot = Boolean(snapshot);
      elements.status.classList.remove("error");
      if (refresh.phase === "refreshing") {
        elements.status.textContent = hasSnapshot
          ? "Refreshing live data. Showing the previous complete snapshot"
            + freshnessSuffix(snapshot)
            + "."
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
      if (!refresh.completedAt) {
        elements.status.textContent = "";
        return;
      }
      const source = snapshot?.options?.source === "fixture" ? "fixture" : "live";
      elements.status.textContent = refresh.cached
        ? "Showing cached " + source + " data" + freshnessSuffix(snapshot) + "."
        : source[0].toUpperCase() + source.slice(1) + " data fetched"
          + freshnessSuffix(snapshot) + ".";
    }

    function renderActionStatus() {
      elements.actionStatus.classList.remove("error");
      if (actionNotice.phase === "idle") {
        elements.actionStatus.textContent = "";
        return;
      }
      if (actionNotice.phase === "error") {
        elements.actionStatus.classList.add("error");
      }
      elements.actionStatus.textContent = actionNotice.message;
    }

    function freshnessSuffix(snapshot) {
      const cache = snapshot?.cache;
      if (!cache?.loadedAt) {
        return "";
      }
      const age = formatAge(cache.ageMs);
      return " from " + new Date(cache.loadedAt).toLocaleString() + " (" + age + " old)";
    }

    function formatAge(ageMs) {
      const age = Math.max(0, Number(ageMs) || 0);
      if (age < 1000) {
        return "just now";
      }
      if (age < 60 * 1000) {
        return Math.floor(age / 1000) + "s";
      }
      if (age < 60 * 60 * 1000) {
        return Math.floor(age / (60 * 1000)) + "m";
      }
      return Math.floor(age / (60 * 60 * 1000)) + "h";
    }

    function renderWarnings(warnings) {
      elements.warnings.replaceChildren();
      for (const warning of warnings) {
        elements.warnings.append(element("div", "warning", warning));
      }
    }

    function hasInboxData(snapshot) {
      const inbox = snapshot.inbox;
      return Boolean(
        inbox
        && typeof inbox === "object"
        && (
          Object.prototype.hasOwnProperty.call(inbox, "recentCommunity")
          || Object.prototype.hasOwnProperty.call(inbox, "community")
          || Object.prototype.hasOwnProperty.call(inbox, "unclassified")
          || Object.prototype.hasOwnProperty.call(inbox, "evidence")
        ),
      );
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

    function renderPersonalInbox(snapshot) {
        const personal = snapshot.personalInbox;
        elements.personalInbox.replaceChildren();
        if (!personal) {
          elements.personalInbox.hidden = true;
          return;
        }
        elements.personalInbox.hidden = false;

        const header = element("div", "inbox-header");
        header.append(element("h2", "", "My PR inbox"));
        const identity = personal.identity ? "@" + personal.identity : "authenticated user";
        const repository = personal.scope?.repository || "dotnet/aspnetcore";
        header.append(
          element(
            "span",
            "muted",
            identity + " | All " + repository
              + " | " + (personal.activeCount ?? 0) + " actionable now of "
              + (personal.items?.length ?? 0) + " PRs",
          ),
        );
        elements.personalInbox.append(header);

        const coverage = personal.coverage ?? { overall: "unassessed" };
        const metrics = personal.metrics ?? {};
        const summary = element("p", "muted");
        summary.textContent = "Coverage: " + coverage.overall
          + " | pull requests: " + (coverage.pullRequests || "unassessed")
          + " | notifications: " + (coverage.notifications || "unassessed")
          + " | API: " + (metrics.cacheMode || "unknown")
          + " (" + (metrics.apiCalls ?? "unknown") + " call(s), "
          + (metrics.elapsedMs ?? "unknown") + " ms, "
          + (metrics.pullRequestsScanned ?? "unknown") + " personal candidate(s)).";
        elements.personalInbox.append(summary);
        if (coverage.error) {
          elements.personalInbox.append(element("div", "warning", "Personal inbox unavailable: " + coverage.error));
        }

        const items = Array.isArray(personal.items) ? personal.items : [];
        const previewItems = Array.isArray(personal.previewItems)
          ? personal.previewItems
          : items.filter((item) => item.hasActionablePersonalSignal).slice(0, 5);
        if (previewItems.length) {
          const preview = element("div", "inbox-list");
          for (const item of previewItems) {
            preview.append(renderPersonalListRow(snapshot, item));
          }
          elements.personalInbox.append(preview);
        }
        else {
          elements.personalInbox.append(
            element(
              "div",
              "empty",
              coverage.overall === "unavailable"
                ? "No personal cards could be assessed."
                : "No active personal signals were found.",
            ),
          );
        }

        const inventory = element("details", "secondary");
        inventory.open = personalInventoryOpen;
        inventory.addEventListener("toggle", () => {
          personalInventoryOpen = inventory.open;
        });
        inventory.append(
          element(
            "summary",
            "",
            "View full personal inventory (" + items.length + " total)",
          ),
        );
        const list = element("div", "inbox-list");
        for (const item of items) {
          list.append(renderPersonalListRow(snapshot, item));
        }
        inventory.append(list);
        elements.personalInbox.append(inventory);
      }

    function renderPersonalListRow(snapshot, item) {
      const queueItem = findQueueItem(snapshot, Number(item.number));
      const key = queueItem ? selectionKeyForItem(queueItem) : selectionKeyForPersonalItem(item);
      const selected = isSelectedKey(key);
      const row = renderSelectableRow({
        key,
        title: "#" + item.number + " " + item.title,
        status: item.actionStatus,
        meta: ["@" + item.author + (item.authorIsBot ? " | bot-authored" : "")],
        pills: [
          ...(item.directRequests?.length ? ["Direct review request"] : []),
          ...(item.teamRequests?.length ? ["Team request"] : []),
          ...(item.notificationSignal?.present ? ["Notification"] : []),
          ...(item.replyEvidence?.status === "evidenced" ? ["Reply evidence"] : []),
          ...(item.changedSinceOwnReview?.status === "yes" ? ["Changed since own review"] : []),
        ],
        href: item.url,
        selected,
        pending: false,
        onSelect: () => selectItem(key),
      });
      row.className = "personal-row" + (selected ? " selected" : "");
      return row;
    }

    function renderPersonalItem(item, selectionKey = null) {
        const card = element("article", "personal-item");
        card.append(element("h3", "", "#" + item.number + " " + item.title));
        card.append(
          element(
            "div",
            "muted",
            "@" + item.author
              + (item.authorIsBot ? " | bot-authored" : "")
              + " | updated " + formatOptionalDate(item.updatedAt),
          ),
        );
        if (item.actionStatus) {
          card.append(
            element(
              "div",
              "muted",
              item.actionStatus.label + " | " + item.actionStatus.detail,
            ),
          );
        }
        if (item.queue) {
          card.append(
            element(
              "div",
              "muted",
              "Queue: " + item.queue.bucket + " | next actor: " + item.queue.nextActor,
            ),
          );
        }

        const signals = element("div", "personal-signals");
        if (item.directRequests?.length) {
          signals.append(element("span", "pill", "Direct review request"));
        }
        if (item.teamRequests?.length) {
          signals.append(element("span", "pill", "Team request: " + item.teamRequests.map((request) => request.slug).join(", ")));
        }
        if (item.otherDirectRequests?.length) {
          signals.append(element("span", "pill", "Other direct request: " + item.otherDirectRequests.map((request) => "@" + request.login).join(", ")));
        }
        if (item.notificationSignal?.present) {
          signals.append(element("span", "pill", "Notification: " + item.notificationSignal.reasons.join(", ")));
        }
        if (item.changedSinceOwnReview?.status === "yes") {
          signals.append(element("span", "pill", "Changed since own review"));
        }
        if (item.replyEvidence?.status === "evidenced") {
          signals.append(element("span", "pill", "Reply in participated thread"));
        }
        if (item.participation?.mentions?.length) {
          signals.append(element("span", "pill", "Mentioned (" + item.participation.mentions.length + ")"));
        }
        if (item.participation?.participatedOrMentioned) {
          signals.append(element("span", "pill", "Participated or mentioned"));
        }
        card.append(signals);

        const evidence = element("div", "inbox-evidence");
        evidence.append(
          element(
            "div",
            "muted",
            "Coverage: " + (item.coverage?.overall || "unassessed")
              + " | review requests: " + (item.coverage?.reviewRequests || "unassessed")
              + " | review history: " + (item.coverage?.reviewHistory || "unassessed")
              + " | threads: " + (item.coverage?.threads || "unassessed")
              + " | notifications: " + (item.coverage?.notifications || "unassessed"),
          ),
        );
        const changedStatus = item.changedSinceOwnReview?.status || "unassessed";
        evidence.append(element("div", "muted", "Changed since own review: " + changedStatus + "."));
        if (item.replyEvidence?.replies?.length) {
          evidence.append(
            element(
              "div",
              "muted",
              "Evidenced replies: " + item.replyEvidence.replies
                .map((reply) => "@" + reply.author)
                .join(", "),
            ),
          );
        }
        if (item.eligibility && !item.eligibility.eligibleForCanvasAction) {
          evidence.append(
            element(
              "div",
              "muted",
              "Personal signal only; no canvas action granted (" + item.eligibility.reason + ").",
            ),
          );
        }
        if (item.signals?.length) {
          const evidenceLinks = element("div", "muted");
          evidenceLinks.append(document.createTextNode("Evidence: "));
          item.signals.forEach((signal, index) => {
            if (index > 0) {
              evidenceLinks.append(document.createTextNode(" | "));
            }
            const link = element("a", "evidence-link", signal.kind);
            link.href = signal.evidenceUrl || item.url;
            link.target = "_blank";
            link.rel = "noreferrer noopener";
            evidenceLinks.append(link);
          });
          card.append(evidenceLinks);
        }
        card.append(evidence);

        const link = element("a", "evidence-link", "Open PR");
        link.href = item.url;
        link.target = "_blank";
        link.rel = "noreferrer noopener";
        card.append(link);
        return card;
    }

    function renderInboxItem(item) {
      const card = element("article", "inbox-item");
      card.append(element("h3", "", "#" + item.number + " " + item.title));
      const metadata = [];
      if (item.provenance) {
        metadata.push(item.provenance === "community" ? "Community contribution" : "Unclassified contribution");
      }
      if (item.bucket) {
        metadata.push(item.bucket);
      }
      if (item.nextActor) {
        metadata.push("Next actor: " + item.nextActor);
      }
      if (item.createdAt) {
        metadata.push("Opened " + formatOptionalDate(item.createdAt, (date) => date.toLocaleDateString()));
      }
      if (metadata.length) {
        card.append(element("div", "muted", metadata.join(" | ")));
      }

      if (Array.isArray(item.reasonCodes) && item.reasonCodes.length) {
        const pills = element("div", "pills");
        for (const code of item.reasonCodes.slice(0, 5)) {
          pills.append(element("span", "pill", code));
        }
        if (pills.children.length) {
          card.append(pills);
        }
      }

      card.append(renderInboxEvidence(item));

      const actions = element("div", "actions");
      const link = element("a", "evidence-link", "Open PR");
      link.href = item.url || "#";
      link.target = "_blank";
      link.rel = "noreferrer noopener";
      actions.append(link);
      card.append(actions);
      return card;
    }

    function renderInbox(snapshot) {
      const inbox = snapshot.inbox ?? {};
      const recent = inbox.recentCommunity ?? { count: 0, newest: null, inventory: [] };
      const community = inbox.community ?? { count: 0, inventory: [] };
      const unclassified = inbox.unclassified ?? { count: 0, inventory: [] };
      const verificationNumbers = new Set(
        (snapshot.discussionVerification ?? []).map((item) => item.number),
      );
      const worthItems = snapshot.primary.reviewNow
        .filter((item) => !verificationNumbers.has(item.number))
        .slice(0, 5);

      elements.inbox.replaceChildren();

      const worth = element("div", "inbox-group");
      worth.append(
        element(
          "div",
          "inbox-header",
          ""),
      );
      worth.firstChild.append(element("h2", "", "Worth reviewing now"));
      worth.firstChild.append(element("span", "muted", String(worthItems.length) + " visible"));
      const worthList = element("div", "inbox-list");
      if (!worthItems.length) {
        worthList.append(element("div", "empty", "No reviewable candidates are currently visible."));
      } else {
        for (const item of worthItems) {
          worthList.append(renderQueueListRow(snapshot, item, "Review now"));
        }
      }
      worth.append(worthList);
      elements.inbox.append(worth);

      elements.inbox.append(
        renderInboxDetails({
          snapshot,
          title: "Recently opened community PRs",
          summary: recent.count
            ? "Within the last " + (inbox.recentCommunityWindowDays ?? 7) + " days • " + recent.count + " total • newest #" + recent.newest
            : "Within the last " + (inbox.recentCommunityWindowDays ?? 7) + " days • no recent community PRs",
          items: recent.inventory ?? [],
        }),
      );
      elements.inbox.append(
        renderInboxDetails({
          snapshot,
          title: "Community attention",
          summary: community.count
            ? String(community.count) + " total community items"
            : "No community items",
          items: community.inventory ?? [],
          previewNeedsRescueFirst: true,
        }),
      );
      elements.inbox.append(
        renderInboxDetails({
          snapshot,
          title: "Unclassified",
          summary: unclassified.count
            ? String(unclassified.count) + " scoped but unlabelled items"
            : "No unclassified items",
          items: unclassified.inventory ?? [],
        }),
      );

      const evidence = inbox.evidence ?? {};
      const coverage = element("p", "muted");
      coverage.textContent = "Inbox evidence: "
        + (evidence.coverage || "not-collected")
        + " | recorded " + (evidence.recordedResponseCount ?? 0)
        + " | unknown " + (evidence.unknownResponseCount ?? 0)
        + " | no-response " + (evidence.noResponseCount ?? 0)
        + ".";
      elements.inbox.append(coverage);
    }

    function renderInboxDetails({
      snapshot,
      title,
      summary,
      items,
      previewLimit = 5,
      previewNeedsRescueFirst = false,
    }) {
      const group = element("div", "inbox-group");
      const header = element("div", "inbox-header");
      header.append(element("h2", "", title));
      header.append(element("span", "muted", summary));
      group.append(header);

      const ordered = previewNeedsRescueFirst ? orderNeedsRescueFirst(items) : [...items];
      const previewItems = ordered.slice(0, previewLimit);
      const previewList = element("div", "inbox-list");
      if (!previewItems.length) {
        previewList.append(element("div", "muted", "None"));
      } else {
        for (const item of previewItems) {
          previewList.append(renderInboxListRow(snapshot, item));
        }
      }
      group.append(previewList);

      if (items.length > previewLimit) {
        const details = element("details", "secondary");
        details.append(element("summary", "", "View the full inventory (" + items.length + " total)"));
        const fullList = element("div", "inbox-list");
        for (const item of ordered) {
          fullList.append(renderInboxListRow(snapshot, item));
        }
        details.append(fullList);
        group.append(details);
      }

      return group;
    }

    function orderNeedsRescueFirst(items) {
      const rescue = [];
      const others = [];
      for (const item of items) {
        if (item.bucket === "NeedsRescue") {
          rescue.push(item);
        } else {
          others.push(item);
        }
      }
      return rescue.concat(others);
    }

    function renderInboxListRow(snapshot, item) {
      const queueItem = findQueueItem(snapshot, Number(item.number));
      const key = selectionKeyForInboxItem(item);
      const title = "#" + item.number + " " + item.title;
      const meta = [
        item.provenance === "community" ? "Community contribution" : "Unclassified contribution",
        item.bucket || "Inbox",
        item.nextActor ? "Next actor: " + item.nextActor : null,
      ].filter(Boolean);
      const summary = [];
      if (item.createdAt) {
        summary.push("Opened " + formatOptionalDate(item.createdAt, (date) => date.toLocaleDateString()));
      }
      if (Array.isArray(item.reasonCodes) && item.reasonCodes.length) {
        summary.push("Reasons: " + item.reasonCodes.slice(0, 3).join(", "));
      }
      const selected = isSelectedKey(key);
      const row = renderSelectableRow({
        key,
        title,
        meta,
        summary: summary.slice(0, 1),
        pills: queueItem?.reasons?.map((reason) => reason.label).slice(0, 3) ?? [],
        href: item.url || "#",
        selected,
        pending: false,
        onSelect: () => selectItem(key),
      });
      row.className = "inbox-row" + (selected ? " selected" : "");
      return row;
    }

    function renderInboxEvidence(item) {
      const evidence = element("div", "inbox-evidence");
      const responseEvidence = item.responseEvidence ?? {};
      const status = responseEvidence.status ?? "unknown";
      const note = responseEvidence.recordedNonAuthorHumanResponse
        ? "Recorded non-author human response"
        : responseEvidence.complete
          ? "Complete evidence"
          : "Bounded evidence";
      evidence.append(element("div", "muted", "Evidence status: " + status + " | " + note));
      const canonical = responseEvidence.canonicalEvidenceUrl || item.url || "#";
      const link = element("a", "evidence-link", "Canonical evidence");
      link.href = canonical;
      link.target = "_blank";
      link.rel = "noreferrer noopener";
      evidence.append(link);
      if (status === "unknown") {
        evidence.append(
          element(
            "div",
            "muted",
            "Evidence is incomplete or ambiguous; no-response is not claimed.",
          ),
        );
      }
      return evidence;
    }

    function renderPrimaryLanes(snapshot, inboxAvailable = hasInboxData(snapshot)) {
      const lanes = [
        {
          bucket: "NeedsRescue",
          items: snapshot.primary.needsRescue,
          overflow: snapshot.overflow.needsRescue,
        },
      ];
      if (!inboxAvailable) {
        lanes.unshift({
          bucket: "ReviewNow",
          items: snapshot.primary.reviewNow,
          overflow: snapshot.overflow.reviewNow,
        });
      }
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
            section.append(renderQueueListRow(snapshot, item, metadata.label));
          }
        }
        elements.lanes.append(section);
      }
    }

    function renderQueueListRow(snapshot, item, labelPrefix) {
      const key = selectionKeyForItem(item);
      const selected = isSelectedKey(key);
      const row = renderSelectableRow({
        key,
        title: "#" + item.number + " " + item.title,
        meta: ["@" + item.author, labelPrefix, "Next actor: " + item.nextActor],
        summary: ["Open " + item.ageDays + "d | idle " + item.idleDays + "d"],
        pills: item.reasons.slice(0, 2).map((metadata) => metadata.label),
        href: item.url,
        selected,
        pending: false,
        onSelect: () => selectItem(key),
      });
      row.className = "list-row " + item.bucket + (selected ? " selected" : "");
      return row;
    }

    function renderQueueDetailCard(snapshot, item, supplemental = {}) {
      const card = element("article", "selected-card " + item.bucket);
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

      if (item.discussion) {
        const assessment = element("details", "discussion-evidence");
        const state = item.discussion.display.label;
        assessment.append(
          element(
            "summary",
            "",
            state + " | "
              + item.discussion.threads.unresolvedCount + " unresolved thread(s)"
              + (item.discussion.complete ? "" : " | incomplete"),
          ),
        );
        for (const signal of item.discussion.signals) {
          assessment.append(element("p", "", signal.label + ": " + signal.description));
        }
        for (const comment of item.discussion.comments) {
          const evidence = element("div", "discussion-comment");
          evidence.append(
            element(
              "strong",
              "",
              "@" + comment.author + " | " + comment.actor + " | " + comment.kindDisplay.label,
            ),
          );
          evidence.append(element("div", "muted", formatOptionalDate(comment.createdAt)));
          evidence.append(element("p", "", comment.excerpt || "(No text returned.)"));
          assessment.append(evidence);
        }
        card.append(assessment);
      }

      for (const blocker of item.blockers) {
        card.append(element("p", "blocker", blocker));
      }

      const actions = element("div", "actions");
      actions.append(actionButton(item, "open", "Open PR", false));
      actions.append(renderReviewActions(item, selectionKeyForItem(item)));
      if (item.bucket === "NeedsRescue") {
        actions.append(actionButton(item, "investigate-rescue", "Investigate rescue", true));
      }
      card.append(actions);
      const supplementalSections = renderSupplementalEvidenceSections(supplemental);
      if (supplementalSections) {
        card.append(supplementalSections);
      }
      return card;
    }

    function renderSupplementalEvidenceSections({ personalItem = null, inboxItem = null } = {}) {
      if (!personalItem && !inboxItem) {
        return null;
      }
      const group = element("div", "supplemental-evidence");
      if (personalItem) {
        const personal = element("details", "discussion-evidence");
        personal.append(element("summary", "", "Personal evidence"));
        personal.append(renderPersonalItem(personalItem));
        group.append(personal);
      }
      if (inboxItem) {
        const inbox = element("details", "discussion-evidence");
        inbox.append(element("summary", "", "Inbox evidence"));
        inbox.append(renderInboxItem(inboxItem));
        group.append(inbox);
      }
      return group;
    }

    function renderDiscussionVerification(snapshot) {
      const items = snapshot.discussionVerification;
      elements.discussionVerification.replaceChildren();
      const title = snapshot.display.discussion?.states?.["verification-needed"]?.label
        || "Verify discussion";
      elements.discussionVerification.append(element("h2", "", title));
      elements.discussionVerification.append(
        element(
          "p",
          "muted",
          "These remain deterministic Review now classifications, but recent discussion needs a human disposition check before starting ordinary code review.",
        ),
      );
      if (!items.length) {
        elements.discussionVerification.append(
          element("div", "empty", "No selected candidates require discussion verification."),
        );
        return;
      }
      for (const item of items) {
        elements.discussionVerification.append(renderQueueListRow(snapshot, item, "Verify discussion"));
      }
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
          items.append(renderQueueListRow(snapshot, item, "Ready to merge"));
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
            const key = selectionKeyForItem(item);
            section.append(
              renderSelectableRow({
                key,
                title: "#" + item.number + " " + item.title,
                meta: [metadata.label],
                summary: ["Open " + item.ageDays + "d | idle " + item.idleDays + "d"],
                href: item.url,
                selected: isSelectedKey(key),
                pending: false,
                onSelect: () => selectItem(key),
              }),
            );
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

    function renderSelectedDetail(snapshot) {
      elements.selectedDetail.replaceChildren();
      const selected = resolveSelectedItem(snapshot);
      if (!selected) {
        elements.selectedDetail.hidden = true;
        return;
      }
      elements.selectedDetail.hidden = false;

      elements.selectedDetail.append(element("h2", "", "Selected item"));
      if (selected.type === "queue") {
        elements.selectedDetail.append(renderQueueDetailCard(snapshot, selected.item, {
          personalItem: selected.personalItem ?? null,
          inboxItem: selected.inboxItem ?? null,
        }));
        return;
      }

      if (selected.type === "inbox") {
        elements.selectedDetail.append(renderInboxItem(selected.item));
        return;
      }

      elements.selectedDetail.append(renderPersonalItem(selected.item));
    }

    function renderReviewActions(item, selectionKey) {
      const group = element("div", "review-actions");
      group.append(actionButton(item, "review", "Review in new session", true, "new-session", selectionKey));
      group.append(actionButton(item, "review", "Review in this session", false, "this-session", selectionKey));
      return group;
    }

    function actionButton(item, kind, label, primary, destination, selectionKey = null) {
      const button = element("button", primary ? "primary-action" : "", label);
      button.type = "button";
      button.disabled = kind === "review" && isReviewPending();
      button.addEventListener("click", () => runAction(button, item.id, kind, destination, selectionKey));
      return button;
    }

    async function runAction(button, itemId, kind, destination, selectionKey = null) {
      if (kind === "review" && pendingReview) {
        return;
      }
      const token = crypto.randomUUID();
      if (kind === "review") {
        pendingReview = { token, selectionKey, kind, destination, itemId };
        actionNotice = {
          phase: "pending",
          message: actionPendingText(kind, destination),
        };
        render(lastRenderedState);
      }
      button.disabled = true;
      try {
        const payload = { itemId: itemId, kind: kind };
        if (destination) {
          payload.destination = destination;
        }
        const response = await fetch("/api/action", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(payload),
        });
        const body = await response.json();
        if (!response.ok) {
          throw new Error(
            body.code === "action_revalidation_failed"
              ? "Action withheld: " + (body.error || "the pull request changed")
              : body.error || "Action failed",
          );
        }
        actionNotice = {
          phase: "success",
          message: body.message || actionQueuedText(kind, destination),
        };
        render(lastRenderedState);
      } catch (error) {
        actionNotice = {
          phase: "error",
          message: error.message,
        };
        render(lastRenderedState);
      } finally {
        if (!pendingReview || pendingReview.token === token) {
          pendingReview = null;
          render(lastRenderedState);
        }
        button.disabled = false;
      }
    }

    function actionPendingText(kind, destination) {
      if (kind === "review") {
        return destination === "this-session"
          ? "Queuing review in this session..."
          : "Queuing review in a new session...";
      }
      return kind === "open" ? "Opening pull request..." : "Sending read-only work to a new session...";
    }

    function actionQueuedText(kind, destination) {
      if (kind === "review") {
        return destination === "this-session"
          ? "Review in this session queued."
          : "Review in new session queued.";
      }
      return kind === "open" ? "Pull request opened." : "Read-only session request queued.";
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
