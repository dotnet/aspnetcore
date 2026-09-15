import { mkdir, readFile, rename, rm, writeFile } from "node:fs/promises";
import { dirname, isAbsolute, join, relative, resolve, sep } from "node:path";
import { randomUUID } from "node:crypto";

import { normalizeArea } from "./taxonomy.mjs";

export function createAreaStore(workspacePath, repositoryRoot) {
  if (!workspacePath) {
    return { readArea: async () => null, writeArea: async (area) => normalizeArea(area) };
  }
  const distance = relative(resolve(repositoryRoot), resolve(workspacePath));
  if (!isAbsolute(workspacePath) || !isAbsolute(repositoryRoot)
    || (distance !== ".." && !distance.startsWith(`..${sep}`) && !isAbsolute(distance))) {
    throw new Error("Session artifacts must be outside the repository checkout.");
  }
  const path = join(workspacePath, "files", "aspnetcore-issue-triage", "preferences.json");
  return {
    async readArea() {
      try {
        const settings = JSON.parse(await readFile(path, "utf8"));
        if (!settings || settings.schemaVersion !== "1.0.0" || typeof settings.area !== "string") {
          throw Object.assign(new Error("Area preferences have an invalid shape."), { code: "preferences_invalid" });
        }
        return normalizeArea(settings.area);
      } catch (error) {
        if (error.code === "ENOENT") {
          return null;
        }
        if (error instanceof SyntaxError) {
          throw Object.assign(new Error("Area preferences contain invalid JSON."), { code: "preferences_invalid" });
        }
        throw error;
      }
    },
    async writeArea(area, isCurrent = () => true) {
      const normalized = normalizeArea(area);
      const temporary = `${path}.${randomUUID()}.tmp`;
      await mkdir(dirname(path), { recursive: true });
      await writeFile(temporary, `${JSON.stringify({ schemaVersion: "1.0.0", area: normalized })}\n`);
      if (!isCurrent()) {
        await rm(temporary, { force: true });
        throw Object.assign(new Error("The area write belongs to a closed canvas instance."), { code: "artifact_write_cancelled" });
      }
      await rename(temporary, path);
      return normalized;
    },
  };
}
