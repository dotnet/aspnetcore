import copy
import json
import subprocess
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from prepare_context import build_context, compute_signals, github_api, patch_status, prepare_context, source_preflight


def pull_request():
    return {
        "number": 42,
        "html_url": "https://github.com/dotnet/aspnetcore/pull/42",
        "title": "Add a streaming option",
        "body": "A new public option. Fixes #41\n\n" + "Detailed authored explanation.\n" * 300,
        "user": {"login": "contributor", "type": "User"},
        "labels": [{"name": "area-blazor"}],
        "milestone": {"number": 12, "title": "12.0-preview1"},
        "state": "closed",
        "merged": True,
        "merged_at": "2026-10-01T12:00:00Z",
        "merge_commit_sha": "abc123",
        "base": {"ref": "main", "sha": "base123", "repo": {"full_name": "dotnet/aspnetcore"}},
        "head": {"ref": "feature", "sha": "head123", "repo": {"full_name": "contributor/aspnetcore"}},
        "changed_files": 1,
    }


def changed_file(filename, old="old", new="new"):
    return {
        "filename": filename,
        "status": "modified",
        "additions": 1,
        "deletions": 1,
        "changes": 2,
        "sha": "file123",
        "patch": f"@@ -10 +10 @@\n-{old}\n+{new}",
    }


class PrepareContextTests(unittest.TestCase):
    def test_shipping_evidence_has_real_paths_and_line_numbers(self):
        cases = (
            ("src/Components/Web/src/PublicAPI.Unshipped.txt",
             "Microsoft.AspNetCore.Components.Web.Foo.Bar() -> void", "public_api"),
            ("src/Http/Http.Extensions/ref/Microsoft.AspNetCore.Http.Extensions.cs",
             "public bool BufferResponse { get; set; }", "public_api"),
            ("src/ProjectTemplates/Web.ProjectTemplates/content/WebApi-CSharp/Program.cs",
             "builder.Services.AddOpenApi();", "project_template"),
            ("src/Middleware/HttpLogging/src/HttpLoggingOptions.cs",
             "public int ResponseBodyLogLimit { get; set; } = 32768;", "defaults_configuration"),
            ("src/Framework/AspNetCoreAnalyzers/src/Analyzers/DiagnosticDescriptors.cs",
             'new DiagnosticDescriptor("ASP0001", title, message, category, DiagnosticSeverity.Warning, true);',
             "analyzers_diagnostics"),
            ("src/Components/Web/src/Microsoft.AspNetCore.Components.Web.csproj",
             "<TargetFramework>net12.0</TargetFramework>", "target_framework"),
        )
        for filename, line, kind in cases:
            with self.subTest(filename=filename):
                result = compute_signals(pull_request(), [changed_file(filename, new=line)])
                evidence = next(item for item in result["evidence"] if item["kind"] == kind)
                self.assertEqual("review_evidence", result["recommendation"])
                self.assertEqual(filename, evidence["file"])
                lines = evidence.get("lines", [evidence])
                self.assertTrue(any(item["line"] == 10 and item["text"] == line for item in lines))
                self.assertNotIn("docs_required", result.values())

    def test_test_build_and_internal_only_changes_have_no_signal(self):
        for filename in (
            "src/Components/Web/test/PublicAPI.Unshipped.txt",
            "src/ProjectTemplates/test/TemplateTests.cs",
            "src/Http/Http.Extensions/testassets/Options.cs",
            "src/Middleware/HttpLogging/test/HttpLoggingOptionsTests.cs",
            ".github/workflows/build.yml",
            "eng/Versions.props",
            "src/Http/Http/src/Internal/BufferHelper.cs",
        ):
            with self.subTest(filename=filename):
                result = compute_signals(pull_request(), [changed_file(filename)])
                self.assertEqual("no_signal", result["recommendation"])
                self.assertEqual([], result["evidence"])

    def test_comment_only_api_changes_are_candidates_not_confirmed_requirements(self):
        result = compute_signals(pull_request(), [
            changed_file("src/Components/Web/src/PublicAPI.Unshipped.txt", "// old comment", "// new comment"),
        ])
        self.assertEqual([], result["evidence"][0]["lines"])
        self.assertEqual("candidate_requires_review", result["evidence"][0]["basis"])
        self.assertEqual(
            {"public_api": 70, "project_template": 70, "defaults_configuration": 75, "explicit_breaking": 80},
            result["confidence_floors_when_confirmed"],
        )

    def test_explicit_evidence_preserves_authored_text_and_field_lines(self):
        pr = pull_request()
        pr["body"] = "Details\n## User-facing behavior\nBreaking change: remove the old overload."
        pr["labels"] = [{"name": "breaking-change"}]
        result = compute_signals(pr, [])
        self.assertIn(
            {"kind": "explicit_user_facing", "field": "body", "line": 2, "text": "## User-facing behavior"},
            result["evidence"],
        )
        self.assertTrue(any(item["kind"] == "explicit_breaking" for item in result["evidence"]))
        self.assertFalse(result["file_list_complete"])

    def test_missing_and_truncated_patches_are_not_no_docs_evidence(self):
        for value, expected in ((None, "missing"), ("@@ -10 +10 @@\n-old", "incomplete")):
            with self.subTest(patch=value):
                file = changed_file("src/Middleware/HttpLogging/src/HttpLoggingOptions.cs")
                file["patch"] = value
                result = compute_signals(pull_request(), [file])
                self.assertEqual(expected, patch_status(file))
                self.assertEqual([{"file": file["filename"], "patch_status": expected}], result["missing_patches"])
                self.assertEqual("review_evidence", result["recommendation"])

    def test_hunk_context_truncation_and_complete_multiple_hunks(self):
        file = changed_file("src/Http/Http/src/Foo.cs")
        file["patch"] = "@@ -10,3 +10,3 @@\n context\n-old\n+new"
        self.assertEqual("incomplete", patch_status(file))
        file.update(
            additions=2, deletions=2,
            patch="@@ -10,2 +10,2 @@\n context\n-old\n+new\n@@ -30 +30 @@\n-before\n+after\n\\ No newline at end of file",
        )
        self.assertEqual("available", patch_status(file))

    def test_shipping_configuration_calls_and_root_tfms_are_candidates(self):
        for filename, line, kind in (
            ("src/Hosting/Hosting/src/WebHost.cs", 'configuration.GetValue<bool>("UseNewBehavior")',
             "defaults_configuration"),
            ("src/Components/Components/src/NewFeature.cs", "public sealed class NewFeature", "public_api"),
            ("Directory.Build.props", "<TargetFramework>net12.0</TargetFramework>", "target_framework"),
        ):
            with self.subTest(filename=filename):
                result = compute_signals(pull_request(), [changed_file(filename, new=line)])
                self.assertTrue(any(evidence["kind"] == kind for evidence in result["evidence"]))

    def test_renamed_api_is_detected_by_previous_filename(self):
        file = changed_file("src/Http/Http/src/ApiBackup.txt")
        file["previous_filename"] = "src/Http/Http/src/PublicAPI.Shipped.txt"
        file["status"] = "renamed"
        result = compute_signals(pull_request(), [file])
        self.assertEqual("public_api", result["evidence"][0]["kind"])

    def test_compact_context_preserves_metadata_without_embedding_patches(self):
        pr = pull_request()
        file = changed_file("src/Components/Web/src/PublicAPI.Unshipped.txt")
        file["previous_filename"] = "src/Components/Web/src/OldAPI.txt"
        context = build_context(pr, [file])
        for key in ("number", "html_url", "title", "body", "merged", "merged_at", "merge_commit_sha"):
            self.assertEqual(pr[key], context[key])
        self.assertEqual(pr["user"], context["author"])
        self.assertEqual(pr["milestone"], context["milestone"])
        self.assertEqual(["area-blazor"], context["labels"])
        self.assertEqual("main", context["base"]["ref"])
        self.assertEqual("head123", context["head"]["sha"])
        self.assertEqual("available", context["changed_files"][0]["patch_status"])
        self.assertEqual(file["previous_filename"], context["changed_files"][0]["previous_filename"])
        self.assertNotIn("patch", context["changed_files"][0])

    def test_eligible_preparation_reuses_metadata_and_keeps_targeted_patches(self):
        pr = pull_request()
        files = [changed_file("src/Components/Web/src/PublicAPI.Unshipped.txt")]
        endpoints = []

        def api(endpoint):
            endpoints.append(endpoint)
            return files if "/files?" in endpoint else pr

        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            gate = prepare_context("dotnet/aspnetcore", "42", root, api)
            self.assertEqual("eligible", gate["status"])
            self.assertEqual(pr, json.loads((root / "source-pr.json").read_text()))
            self.assertEqual(files, json.loads((root / "files.json").read_text()))
            self.assertEqual(pr["body"], json.loads((root / "pr.json").read_text())["body"])
        self.assertEqual([
            "/repos/dotnet/aspnetcore/pulls/42",
            "/repos/dotnet/aspnetcore/pulls/42/files?per_page=100",
        ], endpoints)

    def test_ineligible_requests_do_not_fetch_patches_or_prepare_version_inputs(self):
        for pr, expected in (
            ({**pull_request(), "merged": False}, "ineligible"),
            ({**pull_request(), "base": {**pull_request()["base"], "ref": "release/10.0"}}, "ineligible"),
            ({**pull_request(), "title": "CVE update"}, "restricted"),
            (None, "invalid"),
        ):
            with self.subTest(status=expected, pr=pr):
                endpoints = []

                def api(endpoint):
                    endpoints.append(endpoint)
                    if "/files?" in endpoint:
                        self.fail("Ineligible/restricted source fetched implementation patches")
                    return pr

                with tempfile.TemporaryDirectory() as directory:
                    root = Path(directory)
                    result = prepare_context("dotnet/aspnetcore", "42", root, api)
                    self.assertEqual(expected, result["status"])
                    self.assertEqual(
                        {"source-preflight.json", "existing-draft.json"},
                        {file.name for file in root.iterdir()},
                    )
                    self.assertNotIn("CVE", (root / "source-preflight.json").read_text())
                self.assertEqual(["/repos/dotnet/aspnetcore/pulls/42"], endpoints)

    def test_invalid_input_does_not_make_api_requests(self):
        for repository, number in (("other/repo", "42"), ("dotnet/aspnetcore", "0"), ("dotnet/aspnetcore", "bad")):
            with self.subTest(repository=repository, number=number):
                with tempfile.TemporaryDirectory() as directory:
                    result = prepare_context(
                        repository, number, Path(directory),
                        lambda endpoint: self.fail("Invalid input made an API request"),
                    )
                self.assertEqual("invalid", result["status"])

    def test_product_area_and_general_hardening_metadata_remain_eligible(self):
        pr = pull_request()
        pr.update(title="Harden authentication validation", labels=[{"name": "area-security"}])
        self.assertEqual("eligible", source_preflight("dotnet/aspnetcore", "42", pr)["status"])

    def test_restricted_gate_precedes_ineligible_branch(self):
        pr = pull_request()
        pr.update(title="Advisory update", merged=False)
        pr["base"]["ref"] = "release/10.0"
        self.assertEqual("restricted", source_preflight("dotnet/aspnetcore", "42", pr)["status"])

    def test_api_pagination_and_failures_are_explicit(self):
        file = changed_file("src/Http/Http/src/PublicAPI.Unshipped.txt")
        with patch("prepare_context.subprocess.run") as run:
            run.return_value = subprocess.CompletedProcess([], 0, json.dumps([[file], [file]]), "")
            self.assertEqual([file, file], github_api("/repos/dotnet/aspnetcore/pulls/42/files?per_page=100"))
            self.assertIn("--paginate", run.call_args.args[0])
            self.assertIn("--slurp", run.call_args.args[0])
            run.return_value = subprocess.CompletedProcess([], 1, "", "HTTP 404")
            self.assertIsNone(github_api("/repos/dotnet/aspnetcore/pulls/42"))
            with self.assertRaisesRegex(RuntimeError, "HTTP 404"):
                github_api("/repos/dotnet/aspnetcore/pulls/42/files?per_page=100")
            run.return_value = subprocess.CompletedProcess([], 1, "", "HTTP 403")
            with self.assertRaisesRegex(RuntimeError, "HTTP 403"):
                github_api("/repos/dotnet/aspnetcore/pulls/42")

    def test_mismatched_source_metadata_fails(self):
        pr = copy.deepcopy(pull_request())
        pr["number"] = 43
        with self.assertRaisesRegex(ValueError, "does not match"):
            source_preflight("dotnet/aspnetcore", "42", pr)


if __name__ == "__main__":
    unittest.main()
