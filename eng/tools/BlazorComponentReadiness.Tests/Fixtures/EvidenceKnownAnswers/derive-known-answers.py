#!/usr/bin/env python3
"""Independent known-answer derivation; does not call the C# implementation."""

import base64
import hashlib
import io
import json
import zipfile


def canonical(value):
    return json.dumps(value, ensure_ascii=False, separators=(",", ":"))


def digest(character):
    return {"algorithm": "sha256", "value": character * 64}


def evidence_id(kind, repository_subject, component_subject, payload):
    envelope = {
        "ledger_kind": kind,
        "repository_subject": repository_subject,
        "component_subject": component_subject,
    }
    preimage = (
        b"blazor-component-readiness/evidence-record/v1\0"
        + canonical(envelope).encode()
        + b"\0"
        + canonical(payload).encode()
    )
    return "EV1-" + hashlib.sha256(preimage).hexdigest()


repository = {
    "repository_uri": "https://github.com/owner/repo",
    "commit": "1" * 40,
}
artifact = {
    "mode": "released-package",
    "package": {
        "package_id": "widget.blazor",
        "version": "1.2.3",
        "nupkg_sha256": digest("a"),
    },
}
assessment = {
    "repository": repository,
    "artifact": artifact,
    "component_id": "Tree",
}
escaping_assessment = {
    "repository": repository,
    "artifact": artifact,
    "component_id": 'Trée "A"',
}
repository_subject = {
    "repository": repository,
    "artifact": artifact,
    "component_id": None,
}

repository_records = []
for claim, locator, method, timestamp, content_character in [
    (
        "Repository license is MIT.",
        "LICENSE",
        "Read exact repository file.",
        "2026-08-16T20:00:00Z",
        "b",
    ),
    (
        "Package contains repository metadata.",
        "Widget.Blazor.nuspec",
        "Inspect exact package nuspec.",
        "2026-08-16T20:01:00Z",
        "c",
    ),
    (
        "Security policy defines private reporting.",
        "SECURITY.md",
        "Read exact repository file.",
        "2026-08-16T20:02:00Z",
        "d",
    ),
]:
    payload = {
        "claim": claim,
        "applicability": {
            "scope": "repository-wide",
            "component_id": None,
        },
        "provenance": {
            "kind": "repository-path",
            "locator": locator,
            "method": method,
            "captured_at_utc": timestamp,
            "content_sha256": digest(content_character),
            "retention": "commitment-only",
        },
        "supersedes": [],
    }
    repository_records.append(
        {
            "stable_id": evidence_id(
                "repository",
                repository_subject,
                None,
                payload,
            ),
            **payload,
        }
    )
repository_records.sort(key=lambda record: record["stable_id"])
repository_ledger = {
    "schema_version": 1,
    "ledger_kind": "repository",
    "repository_subject": repository_subject,
    "component_subject": None,
    "records": repository_records,
}

component_payload = {
    "claim": "Tree expands selected nodes.",
    "applicability": {
        "scope": "component-specific",
        "component_id": "Tree",
    },
    "provenance": {
        "kind": "command-probe",
        "locator": "probe: Tree expansion",
        "method": "Run deterministic browser probe.",
        "captured_at_utc": "2026-08-16T20:03:00Z",
        "content_sha256": digest("e"),
        "retention": "commitment-only",
    },
    "supersedes": [],
}
component_id = evidence_id("component", None, assessment, component_payload)
component_ledger = {
    "schema_version": 1,
    "ledger_kind": "component",
    "repository_subject": None,
    "component_subject": assessment,
    "records": [{"stable_id": component_id, **component_payload}],
}

repository_json = canonical(repository_ledger)
component_json = canonical(component_ledger)
repository_sha256 = hashlib.sha256(repository_json.encode()).hexdigest()
component_sha256 = hashlib.sha256(component_json.encode()).hexdigest()
sources = sorted(
    [
        {
            "source_ledger_sha256": repository_sha256,
            "ledger": repository_ledger,
        },
        {
            "source_ledger_sha256": component_sha256,
            "ledger": component_ledger,
        },
    ],
    key=lambda source: source["source_ledger_sha256"],
)
selected = [
    repository_records[0]["stable_id"],
    repository_records[2]["stable_id"],
    component_id,
]
source_by_id = {
    record["stable_id"]: repository_sha256 for record in repository_records
}
source_by_id[component_id] = component_sha256
bundle = {
    "schema_version": 1,
    "assessment": assessment,
    "source_ledgers": sources,
    "selection": [
        {
            "display_order": index + 1,
            "source_ledger_sha256": source_by_id[identifier],
            "evidence_id": identifier,
        }
        for index, identifier in enumerate(selected)
    ],
}
manifest = {
    "schema_version": 1,
    "files": [
        {
            "path": "references/checklist.md",
            "sha256": digest("f"),
        },
        {
            "path": "references/overlays/scaffolder.md",
            "sha256": digest("9"),
        },
    ],
}

nupkg_stream = io.BytesIO()
with zipfile.ZipFile(nupkg_stream, "w", compression=zipfile.ZIP_STORED) as package:
    entry = zipfile.ZipInfo("Widget.Blazor.nuspec", (2020, 1, 1, 0, 0, 0))
    entry.external_attr = 0o100644 << 16
    package.writestr(
        entry,
        b'<?xml version="1.0"?><package><metadata>'
        b"<id> Widget&#46;Blazor </id>"
        b"<version> 1.2.3-beta+Build_7 </version>"
        b"</metadata></package>",
    )
nupkg = nupkg_stream.getvalue()

assessment_json = canonical(assessment)
manifest_json = canonical(manifest)
bundle_json = canonical(bundle)
result = {
    "assessment": assessment_json,
    "escaping_assessment": canonical(escaping_assessment),
    "repository_ledger": repository_json,
    "component_ledger": component_json,
    "bundle": bundle_json,
    "manifest": manifest_json,
    "values": canonical(
        {
            "assessment_sha256": hashlib.sha256(
                b"blazor-component-readiness/assessment/v1\0"
                + assessment_json.encode()
            ).hexdigest(),
            "repository_ledger_sha256": repository_sha256,
            "component_ledger_sha256": component_sha256,
            "bundle_sha256": hashlib.sha256(bundle_json.encode()).hexdigest(),
            "validation_inputs_sha256": hashlib.sha256(
                b"blazor-component-readiness/validation-inputs/v1\0"
                + manifest_json.encode()
            ).hexdigest(),
            "repository_record_ids": [
                record["stable_id"] for record in repository_records
            ],
            "component_record_id": component_id,
            "nupkg_sha256": hashlib.sha256(nupkg).hexdigest(),
        }
    ),
    "nupkg_base64": base64.b64encode(nupkg).decode(),
    "legacy_schema2_receipt": json.dumps(
        {
            "canonical_row_count": 2,
            "checklist_sha256": "f249491a9e6097d128713b10db1a98560bc187177a33cec003d3de1e29b569bc",
            "limitation": "Structural validation does not establish factual evidence or classification quality.",
            "mode": "targeted",
            "report_filename": "legacy-report.md",
            "report_sha256": hashlib.sha256(b"legacy report").hexdigest(),
            "rubric_identity": "blazor-component-readiness/1.3.0+sha256:f249491a9e6097d128713b10db1a98560bc187177a33cec003d3de1e29b569bc",
            "rubric_version": "1.3.0",
            "schema_version": 2,
            "scope_schema_version": 1,
            "selected_ids": ["LP-01", "LP-02"],
            "selected_overlays": [],
            "structural_validation": "passed",
            "valid_row_count": 2,
            "validated_at_utc": "2026-08-13T18:00:00Z",
        },
        indent=2,
        sort_keys=True,
    ).replace("+", "\\u002B"),
}
print(json.dumps(result, ensure_ascii=False, indent=2, sort_keys=True))
