export const REPOSITORY = "dotnet/aspnetcore";
export const DEFAULT_AREA = "area-blazor";

// Kept in the same order as the repository's public issue-triage area taxonomy.
export const AREA_OPTIONS = [
  ["area-auth", "Authentication and authorization"],
  ["area-blazor", "Blazor and Razor Components"],
  ["area-commandlinetools", "Command-line tools and template infrastructure"],
  ["area-dataprotection", "Data Protection"],
  ["area-grpc", "ASP.NET Core gRPC integration"],
  ["area-healthchecks", "Health checks"],
  ["area-hosting", "Hosting"],
  ["area-identity", "ASP.NET Core Identity"],
  ["area-infrastructure", "Build, CI, packaging, and test infrastructure"],
  ["area-middleware", "Request-pipeline middleware"],
  ["area-minimal", "Minimal APIs and runtime OpenAPI"],
  ["area-mvc", "MVC controllers and model binding"],
  ["area-networking", "Servers, HTTP, transports, and connection management"],
  ["area-perf", "Performance"],
  ["area-routing", "Endpoint routing and URL matching"],
  ["area-security", "Antiforgery and ASP.NET Core security hardening"],
  ["area-signalr", "SignalR"],
  ["area-ui-rendering", "Razor Pages, Views, and Razor rendering"],
  ["area-unified-build", "Unified build and VMR integration"],
].map(([label, description]) => ({ label, description }));

const AREA_LABELS = new Set(AREA_OPTIONS.map((option) => option.label));

export const EXCLUDED_LABELS = [
  "Needs: Author Feedback",
  ":heavy_check_mark: Resolution: Answered",
  ":heavy_check_mark: Resolution: Duplicate",
];

export function normalizeArea(area, fallback = DEFAULT_AREA) {
  const candidate = typeof area === "string" ? area : fallback;
  if (!AREA_LABELS.has(candidate)) {
    throw taxonomyError("invalid_area", "area must be a supported public area label");
  }
  return candidate;
}

export function isSupportedArea(area) {
  return AREA_LABELS.has(area);
}

function taxonomyError(code, message) {
  const error = new Error(message);
  error.code = code;
  return error;
}
