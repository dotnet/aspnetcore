// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace BlazorComponentReadiness.Tests;

public sealed class EvidenceLedgerTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();
    private static readonly string FixtureRoot = Path.Combine(
        RepositoryRoot,
        "eng",
        "tools",
        "BlazorComponentReadiness.Tests",
        "Fixtures",
        "EvidenceKnownAnswers");

    [Fact]
    public void IndependentlyDerivedKnownAnswersMatchCanonicalImplementation()
    {
        // The checked-in values are produced by the adjacent Python implementation, not by the
        // canonical C# writer under test.
        var derivation = File.ReadAllText(
            Path.Combine(FixtureRoot, "derive-known-answers.py"),
            Encoding.UTF8);
        Assert.Contains("hashlib.sha256", derivation);
        Assert.Contains("\"escaping_assessment\"", derivation);
        Assert.DoesNotContain("CanonicalEvidenceJson", derivation);
        var assessmentBytes = ReadCanonicalFixture("assessment.json");
        var repositoryBytes = ReadCanonicalFixture("repository-ledger.json");
        var componentBytes = ReadCanonicalFixture("component-ledger.json");
        var bundleBytes = ReadCanonicalFixture("bundle.json");
        var manifestBytes = ReadCanonicalFixture("manifest.json");
        using var valuesDocument = JsonDocument.Parse(
            ReadCanonicalFixture("values.json"));
        var values = valuesDocument.RootElement;

        var assessment = CanonicalEvidenceJson.ParseAssessment(assessmentBytes);
        var repositoryLedger =
            CanonicalEvidenceJson.ParseSourceLedger(repositoryBytes);
        var componentLedger =
            CanonicalEvidenceJson.ParseSourceLedger(componentBytes);
        var bundle = CanonicalEvidenceJson.ParseBundle(bundleBytes);
        var manifest = BuildKnownAnswerManifest();

        Assert.Equal(
            assessmentBytes,
            CanonicalEvidenceJson.SerializeAssessment(assessment));
        Assert.Equal(
            repositoryBytes,
            CanonicalEvidenceJson.SerializeSourceLedger(repositoryLedger));
        Assert.Equal(
            componentBytes,
            CanonicalEvidenceJson.SerializeSourceLedger(componentLedger));
        Assert.Equal(bundleBytes, CanonicalEvidenceJson.SerializeBundle(bundle));
        Assert.Equal(
            manifestBytes,
            CanonicalEvidenceJson.SerializeValidationInputManifest(manifest));
        Assert.Equal(
            values.GetProperty("assessment_sha256").GetString(),
            CanonicalEvidenceJson.ComputeAssessmentSha256(assessment));
        Assert.Equal(
            values.GetProperty("repository_ledger_sha256").GetString(),
            CanonicalEvidenceJson.ComputeSourceLedgerSha256(repositoryLedger));
        Assert.Equal(
            values.GetProperty("component_ledger_sha256").GetString(),
            CanonicalEvidenceJson.ComputeSourceLedgerSha256(componentLedger));
        Assert.Equal(
            values.GetProperty("bundle_sha256").GetString(),
            CanonicalEvidenceJson.ComputeBundleSha256(bundle));
        Assert.Equal(
            values.GetProperty("validation_inputs_sha256").GetString(),
            CanonicalEvidenceJson.ComputeValidationInputsSha256(manifest));
        Assert.Equal(
            values.GetProperty("repository_record_ids")
                .EnumerateArray()
                .Select(element => element.GetString()),
            repositoryLedger.Records.Select(record => record.StableId));
        Assert.Equal(
            values.GetProperty("component_record_id").GetString(),
            Assert.Single(componentLedger.Records).StableId);
    }

    [Fact]
    public void RepositoryAndComponentBuildsMatchKnownAnswerBytes()
    {
        var repositoryKnownAnswer =
            CanonicalEvidenceJson.ParseSourceLedger(
                ReadCanonicalFixture("repository-ledger.json"));
        var componentKnownAnswer =
            CanonicalEvidenceJson.ParseSourceLedger(
                ReadCanonicalFixture("component-ledger.json"));
        var repositoryBuilt = EvidenceLedgerBuilder.BuildRepositoryLedger(
            repositoryKnownAnswer.RepositorySubject!,
            repositoryKnownAnswer.Records.Select(ToDraft));
        var componentBuilt = EvidenceLedgerBuilder.BuildComponentLedger(
            componentKnownAnswer.ComponentSubject!,
            componentKnownAnswer.Records.Select(ToDraft));

        Assert.Equal(
            ReadCanonicalFixture("repository-ledger.json"),
            CanonicalEvidenceJson.SerializeSourceLedger(repositoryBuilt));
        Assert.Equal(
            ReadCanonicalFixture("component-ledger.json"),
            CanonicalEvidenceJson.SerializeSourceLedger(componentBuilt));
    }

    [Fact]
    public void CanonicalJsonUsesLiteralUtf8AndMinimalRequiredEscaping()
    {
        var bytes = ReadCanonicalFixture("escaping-assessment.json");
        var assessment = CanonicalEvidenceJson.ParseAssessment(bytes);

        Assert.Equal("Trée \"A\"", assessment.ComponentId);
        Assert.Equal(bytes, CanonicalEvidenceJson.SerializeAssessment(assessment));
        Assert.Contains(
            "Trée \\\"A\\\"",
            Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public void BundleSelectsSubsetFromLargerEmbeddedLedger()
    {
        var assessment = KnownAssessment();
        var repositoryLedger = KnownRepositoryLedger();
        var componentLedger = KnownComponentLedger();
        string[] selected =
        [
            repositoryLedger.Records[0].StableId,
            repositoryLedger.Records[2].StableId,
            componentLedger.Records[0].StableId,
        ];

        var bundle = EvidenceLedgerBuilder.BuildBundle(
            assessment,
            [repositoryLedger, componentLedger],
            selected);

        Assert.Equal(3, repositoryLedger.Records.Count);
        Assert.Equal(3, bundle.Selection.Count);
        Assert.DoesNotContain(
            bundle.Selection,
            selection => string.Equals(
                selection.EvidenceId,
                repositoryLedger.Records[1].StableId,
                StringComparison.Ordinal));
        Assert.Equal(
            ReadCanonicalFixture("bundle.json"),
            CanonicalEvidenceJson.SerializeBundle(bundle));
    }

    [Fact]
    public void IdenticalObservationDeduplicatesAndChangedTimestampChangesIdentity()
    {
        var subject = KnownRepositorySubject();
        var original = RepositoryDraft(
            "Repository license is MIT.",
            "2026-08-16T20:00:00Z");
        var changed = RepositoryDraft(
            "Repository license is MIT.",
            "2026-08-16T20:00:01Z");

        var deduplicated = EvidenceLedgerBuilder.BuildRepositoryLedger(
            subject,
            [original, original]);
        var changedLedger = EvidenceLedgerBuilder.BuildRepositoryLedger(
            subject,
            [original, changed]);

        Assert.Single(deduplicated.Records);
        Assert.Equal(2, changedLedger.Records.Count);
        Assert.Equal(
            2,
            changedLedger.Records
                .Select(record => record.StableId)
                .Distinct(StringComparer.Ordinal)
                .Count());
    }

    [Fact]
    public void DifferentPreimagesWithOneDigestFailClosed()
    {
        var exception = Assert.Throws<InvalidDataException>(() =>
            EvidenceLedgerBuilder.BuildRepositoryLedger(
                KnownRepositorySubject(),
                [
                    RepositoryDraft(
                        "Repository license is MIT.",
                        "2026-08-16T20:00:00Z"),
                    RepositoryDraft(
                        "Repository license is Apache-2.0.",
                        "2026-08-16T20:00:00Z"),
                ],
                new ConstantHasher()));

        Assert.Contains("EVID003", exception.Message);
        Assert.Contains("collision", exception.Message);
    }

    [Fact]
    public void ForgedSourceLedgerDigestFails()
    {
        var bundle = KnownBundle();
        var source = bundle.SourceLedgers[0] with
        {
            SourceLedgerSha256 = new string('0', 64),
        };
        var forged = bundle with
        {
            SourceLedgers = [source, .. bundle.SourceLedgers.Skip(1)],
        };

        var exception = Assert.Throws<InvalidDataException>(() =>
            EvidenceLedgerValidator.ValidateBundle(forged));

        Assert.Contains("EVID009", exception.Message);
    }

    [Fact]
    public void NonmemberSelectionFails()
    {
        var bundle = KnownBundle();
        var selection = bundle.Selection[0] with
        {
            EvidenceId = "EV1-" + new string('f', 64),
        };
        var forged = bundle with
        {
            Selection = [selection, .. bundle.Selection.Skip(1)],
        };

        var exception = Assert.Throws<InvalidDataException>(() =>
            EvidenceLedgerValidator.ValidateBundle(forged));

        Assert.Contains("EVID008", exception.Message);
        Assert.Contains("resolves to 0", exception.Message);
    }

    [Fact]
    public void DuplicateAndNoncontiguousSelectionsFail()
    {
        var bundle = KnownBundle();
        var duplicate = bundle with
        {
            Selection =
            [
                bundle.Selection[0],
                bundle.Selection[0] with
                {
                    DisplayOrder = 2,
                },
            ],
        };
        var noncontiguous = bundle with
        {
            Selection =
            [
                bundle.Selection[0] with
                {
                    DisplayOrder = 2,
                },
            ],
        };

        Assert.Contains(
            "duplicate",
            Assert.Throws<InvalidDataException>(() =>
                EvidenceLedgerValidator.ValidateBundle(duplicate)).Message,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            "contiguous",
            Assert.Throws<InvalidDataException>(() =>
                EvidenceLedgerValidator.ValidateBundle(noncontiguous)).Message);
    }

    [Fact]
    public void ComponentEvidenceCannotCrossControls()
    {
        var radio = KnownAssessment() with
        {
            ComponentId = "Radio",
        };

        var exception = Assert.Throws<InvalidDataException>(() =>
            EvidenceLedgerBuilder.BuildBundle(
                radio,
                [KnownComponentLedger()],
                [KnownComponentLedger().Records[0].StableId]));

        Assert.Contains("EVID007", exception.Message);
    }

    [Fact]
    public void SourceOnlyRepositoryEvidenceReusesOnlyWithinExactComponent()
    {
        var assessment = KnownAssessment() with
        {
            Artifact = new ArtifactIdentity("source-only", null),
        };
        var ledger = EvidenceLedgerBuilder.BuildRepositoryLedger(
            new RepositoryLedgerSubject(
                assessment.Repository,
                assessment.Artifact,
                assessment.ComponentId),
            [
                RepositoryDraft(
                    "Repository license is MIT.",
                    "2026-08-16T20:00:00Z"),
            ]);

        var sameControl = EvidenceLedgerBuilder.BuildBundle(
            assessment,
            [ledger],
            [ledger.Records[0].StableId]);
        var differentControl = assessment with
        {
            ComponentId = "Radio",
        };
        var radioLedger = EvidenceLedgerBuilder.BuildRepositoryLedger(
            new RepositoryLedgerSubject(
                differentControl.Repository,
                differentControl.Artifact,
                differentControl.ComponentId),
            [
                RepositoryDraft(
                    "Repository license is MIT.",
                    "2026-08-16T20:00:00Z"),
            ]);

        Assert.Single(sameControl.Selection);
        Assert.NotEqual(
            ledger.Records[0].StableId,
            radioLedger.Records[0].StableId);
        var exception = Assert.Throws<InvalidDataException>(() =>
            EvidenceLedgerBuilder.BuildBundle(
                differentControl,
                [ledger],
                [ledger.Records[0].StableId]));
        Assert.Contains("EVID007", exception.Message);
        Assert.Contains("component_id", exception.Message);
    }

    [Fact]
    public void ReleasedPackageRepositoryEvidenceReusesAcrossControls()
    {
        var radio = KnownAssessment() with
        {
            ComponentId = "Radio",
        };
        var ledger = KnownRepositoryLedger();

        var bundle = EvidenceLedgerBuilder.BuildBundle(
            radio,
            [ledger],
            [ledger.Records[0].StableId]);

        Assert.Equal("Radio", bundle.Assessment.ComponentId);
        Assert.Single(bundle.Selection);
    }

    [Fact]
    public void RepositorySubjectComponentBindingMatchesArtifactMode()
    {
        var released = KnownRepositorySubject() with
        {
            ComponentId = "Tree",
        };
        var sourceOnly = new RepositoryLedgerSubject(
            KnownAssessment().Repository,
            new ArtifactIdentity("source-only", null),
            ComponentId: null);

        Assert.Contains(
            "released-package",
            Assert.Throws<InvalidDataException>(() =>
                EvidenceLedgerBuilder.BuildRepositoryLedger(
                    released,
                    [
                        RepositoryDraft(
                            "Repository license is MIT.",
                            "2026-08-16T20:00:00Z"),
                    ])).Message);
        Assert.Contains(
            "source-only",
            Assert.Throws<InvalidDataException>(() =>
                EvidenceLedgerBuilder.BuildRepositoryLedger(
                    sourceOnly,
                    [
                        RepositoryDraft(
                            "Repository license is MIT.",
                            "2026-08-16T20:00:00Z"),
                    ])).Message);
    }

    [Theory]
    [InlineData("source-only", null)]
    [InlineData("released-package", "different")]
    public void RepositoryEvidenceRequiresExactArtifactIdentity(
        string mode,
        string? packageId)
    {
        var assessment = KnownAssessment();
        var artifact = mode == "source-only"
            ? new ArtifactIdentity("source-only", null)
            : assessment.Artifact with
            {
                Package = assessment.Artifact.Package! with
                {
                    PackageId = packageId!,
                },
            };
        assessment = assessment with
        {
            Artifact = artifact,
        };

        var exception = Assert.Throws<InvalidDataException>(() =>
            EvidenceLedgerBuilder.BuildBundle(
                assessment,
                [KnownRepositoryLedger()],
                [KnownRepositoryLedger().Records[0].StableId]));

        Assert.Contains("EVID006", exception.Message);
    }

    [Fact]
    public void RepositoryEvidenceRejectsEveryExactIdentityMismatch()
    {
        var original = KnownAssessment();
        var package = original.Artifact.Package!;
        var mismatches = new[]
        {
            original with
            {
                Repository = original.Repository with
                {
                    Commit = new string('2', 40),
                },
            },
            original with
            {
                Artifact = original.Artifact with
                {
                    Package = package with
                    {
                        PackageId = "other.package",
                    },
                },
            },
            original with
            {
                Artifact = original.Artifact with
                {
                    Package = package with
                    {
                        Version = "1.2.4",
                    },
                },
            },
            original with
            {
                Artifact = original.Artifact with
                {
                    Package = package with
                    {
                        NupkgDigest = new Sha256Digest(
                            "sha256",
                            new string('b', 64)),
                    },
                },
            },
        };

        foreach (var mismatch in mismatches)
        {
            Assert.Contains(
                "EVID006",
                Assert.Throws<InvalidDataException>(() =>
                    EvidenceLedgerBuilder.BuildBundle(
                        mismatch,
                        [KnownRepositoryLedger()],
                        [KnownRepositoryLedger().Records[0].StableId])).Message);
        }
    }

    [Fact]
    public void LedgerKindsEnforceRubricApplicability()
    {
        var componentDraft = ComponentDraft();
        var repositoryDraft = RepositoryDraft(
            "Repository license is MIT.",
            "2026-08-16T20:00:00Z");

        Assert.Contains(
            "EVID007",
            Assert.Throws<InvalidDataException>(() =>
                EvidenceLedgerBuilder.BuildRepositoryLedger(
                    KnownRepositorySubject(),
                    [componentDraft])).Message);
        Assert.Contains(
            "EVID007",
            Assert.Throws<InvalidDataException>(() =>
                EvidenceLedgerBuilder.BuildComponentLedger(
                    KnownAssessment(),
                    [repositoryDraft])).Message);
    }

    [Fact]
    public void CommitmentOnlyIsTheOnlyRetentionToken()
    {
        var draft = RepositoryDraft(
            "Repository license is MIT.",
            "2026-08-16T20:00:00Z");
        draft = draft with
        {
            Provenance = draft.Provenance with
            {
                Retention = "bundled",
            },
        };

        var exception = Assert.Throws<InvalidDataException>(() =>
            EvidenceLedgerBuilder.BuildRepositoryLedger(
                KnownRepositorySubject(),
                [draft]));

        Assert.Contains("commitment-only", exception.Message);
    }

    [Theory]
    [InlineData("repository-path", "../LICENSE")]
    [InlineData("repository-path", "/" + "Users/reviewer/LICENSE")]
    [InlineData("command-probe", "/tmp/probe")]
    [InlineData("repository-path", "src\\file.cs")]
    [InlineData("repository-path", "src|file.cs")]
    [InlineData("repository-path", "src`file.cs")]
    [InlineData("public-https", "https://user:secret@example.com/path")]
    [InlineData("public-https", "https://example.com/path?token=secret")]
    [InlineData("public-https", "https://example.com/a/../b")]
    [InlineData("public-https", "https://example.com/a//b")]
    [InlineData("public-https", "https://example.com/%61")]
    [InlineData("public-https", "https://localhost./path")]
    [InlineData("public-https", "https://example.local./path")]
    [InlineData("public-https", "https://example.com./path")]
    [InlineData("public-https", "file:///tmp/evidence")]
    [InlineData("command-probe", "probe: bad|label")]
    [InlineData("command-probe", "probe:\nlabel")]
    public void ForbiddenLocatorLiteralsFail(string kind, string locator)
    {
        var draft = RepositoryDraft(
            "Repository license is MIT.",
            "2026-08-16T20:00:00Z");
        draft = draft with
        {
            Provenance = draft.Provenance with
            {
                Kind = kind,
                Locator = locator,
            },
        };

        Assert.Contains(
            "EVID005",
            Assert.Throws<InvalidDataException>(() =>
                EvidenceLedgerBuilder.BuildRepositoryLedger(
                    KnownRepositorySubject(),
                    [draft])).Message);
    }

    [Theory]
    [InlineData("public-https", "HTTPS://EXAMPLE.COM/evidence", "https://example.com/evidence")]
    [InlineData("public-https", "HTTPS://EXAMPLE.COM/Ärea/File", "https://example.com/Ärea/File")]
    [InlineData(
        "public-https",
        "HTTPS://EXAMPLE.COM/a-._~!$&'()*+,;=:@/b",
        "https://example.com/a-._~!$&'()*+,;=:@/b")]
    [InlineData("repository-path", "src/Widget.razor", "src/Widget.razor")]
    [InlineData("command-probe", "probe: Tree expansion", "probe: Tree expansion")]
    public void LocatorKindsHaveCanonicalForms(
        string kind,
        string locator,
        string expected)
    {
        var draft = RepositoryDraft(
            "Repository license is MIT.",
            "2026-08-16T20:00:00Z");
        draft = draft with
        {
            Provenance = draft.Provenance with
            {
                Kind = kind,
                Locator = locator,
            },
        };

        var ledger = EvidenceLedgerBuilder.BuildRepositoryLedger(
            KnownRepositorySubject(),
            [draft]);

        Assert.Equal(expected, ledger.Records[0].Provenance.Locator);
    }

    [Theory]
    [InlineData("2026-08-16T20:00:00.000Z")]
    [InlineData("2026-08-16T20:00:00-05:00")]
    [InlineData("2026-08-16T20:00:00z")]
    [InlineData("2026-08-16T20:00:60Z")]
    public void NoncanonicalTimestampsFail(string timestamp)
    {
        Assert.Contains(
            "captured_at_utc",
            Assert.Throws<InvalidDataException>(() =>
                EvidenceLedgerBuilder.BuildRepositoryLedger(
                    KnownRepositorySubject(),
                    [RepositoryDraft("Repository license is MIT.", timestamp)]))
                .Message);
    }

    [Theory]
    [InlineData("\u200b- Hidden Markdown list claim.")]
    [InlineData("Tree\u202eevil")]
    [InlineData("Tree\ufeff")]
    public void UnicodeFormatCharactersCannotSpoofEvidenceIdentity(string value)
    {
        var draft = RepositoryDraft(
            "Repository license is MIT.",
            "2026-08-16T20:00:00Z") with
        {
            Claim = value,
        };

        Assert.Contains(
            "EVID005",
            Assert.Throws<InvalidDataException>(() =>
                EvidenceLedgerBuilder.BuildRepositoryLedger(
                    KnownRepositorySubject(),
                    [draft])).Message);
        Assert.Contains(
            "EVID005",
            Assert.Throws<InvalidDataException>(() =>
                EvidenceIdentity.NormalizeAssessment(
                    KnownAssessment() with
                    {
                        ComponentId = value,
                    })).Message);
    }

    [Theory]
    [InlineData("Tree\u200cControl")]
    [InlineData("Tree\u200dControl")]
    public void ZwnjAndZwjRemainValidIdentityText(string value)
    {
        var assessment = EvidenceIdentity.NormalizeAssessment(
            KnownAssessment() with
            {
                ComponentId = value,
            });
        var draft = RepositoryDraft(
            $"Component {value} remains joined.",
            "2026-08-16T20:00:00Z");
        var ledger = EvidenceLedgerBuilder.BuildRepositoryLedger(
            KnownRepositorySubject(),
            [draft]);

        Assert.Equal(value, assessment.ComponentId);
        Assert.Contains(value, ledger.Records[0].Claim);
    }

    [Fact]
    public void RepositoryUriCanonicalizationIsHostSpecific()
    {
        var github = EvidenceIdentity.NormalizeRepositorySubject(
            KnownRepositorySubject() with
            {
                Repository = new RepositoryIdentity(
                    "https://GitHub.com/Owner/Repo.git",
                    new string('1', 40)),
            });
        var nonGitHub = EvidenceIdentity.NormalizeRepositorySubject(
            KnownRepositorySubject() with
            {
                Repository = new RepositoryIdentity(
                    "https://git.example.com/Org/Repo.git",
                    new string('1', 40)),
            });

        Assert.Equal("https://github.com/owner/repo", github.Repository.RepositoryUri);
        Assert.Equal(
            "https://git.example.com/Org/Repo.git",
            nonGitHub.Repository.RepositoryUri);
    }

    [Fact]
    public void RepositoryUriRejectsRootLabelDotAliases()
    {
        foreach (var uri in new[]
        {
            "https://github.com./Owner/Repo.git",
            "https://git.example.com./Org/Repo.git",
        })
        {
            Assert.Contains(
                "root-label dot",
                Assert.Throws<InvalidDataException>(() =>
                    EvidenceIdentity.NormalizeRepositorySubject(
                        KnownRepositorySubject() with
                        {
                            Repository = new RepositoryIdentity(
                                uri,
                                new string('1', 40)),
                        })).Message);
        }
    }

    [Theory]
    [InlineData("https://github.com/owner/a/../repo")]
    [InlineData("https://github.com/../../repo")]
    [InlineData("https://github.com/owner//repo")]
    [InlineData("https://github.com/owner/%72epo")]
    [InlineData("https://git.example.com/Org/a/../Repo")]
    public void RepositoryUriRejectsRawAliasesBeforeUriNormalization(string uri)
    {
        Assert.Contains(
            "EVID005",
            Assert.Throws<InvalidDataException>(() =>
                EvidenceIdentity.NormalizeRepositorySubject(
                    KnownRepositorySubject() with
                    {
                        Repository = new RepositoryIdentity(
                            uri,
                            new string('1', 40)),
                    })).Message);
    }

    [Fact]
    public void RepositoryUriPreservesLiteralNfcPathAndCase()
    {
        var normalized = EvidenceIdentity.NormalizeRepositorySubject(
            KnownRepositorySubject() with
            {
                Repository = new RepositoryIdentity(
                    "HTTPS://GIT.EXAMPLE.COM/Org/Résumé.git",
                    new string('1', 40)),
            });

        Assert.Equal(
            "https://git.example.com/Org/Résumé.git",
            normalized.Repository.RepositoryUri);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("\u00a0")]
    [InlineData("\u2003")]
    [InlineData("\"")]
    [InlineData("<")]
    [InlineData(">")]
    [InlineData("[")]
    [InlineData("]")]
    [InlineData("^")]
    [InlineData("{")]
    [InlineData("}")]
    public void HttpsPathsRejectCharactersOutsidePcharAndLiteralNfc(string invalid)
    {
        var repositoryUri = $"https://git.example.com/Org/a{invalid}b";
        Assert.Contains(
            "EVID005",
            Assert.Throws<InvalidDataException>(() =>
                EvidenceIdentity.NormalizeRepositorySubject(
                    KnownRepositorySubject() with
                    {
                        Repository = new RepositoryIdentity(
                            repositoryUri,
                            new string('1', 40)),
                    })).Message);

        var draft = RepositoryDraft(
            "Repository license is MIT.",
            "2026-08-16T20:00:00Z");
        draft = draft with
        {
            Provenance = draft.Provenance with
            {
                Kind = "public-https",
                Locator = $"https://example.com/a{invalid}b",
            },
        };
        Assert.Contains(
            "EVID005",
            Assert.Throws<InvalidDataException>(() =>
                EvidenceLedgerBuilder.BuildRepositoryLedger(
                    KnownRepositorySubject(),
                    [draft])).Message);
    }

    [Fact]
    public void RepositoryRelativePosixPathsKeepInternalSpaces()
    {
        var draft = RepositoryDraft(
            "Repository license is MIT.",
            "2026-08-16T20:00:00Z");
        draft = draft with
        {
            Provenance = draft.Provenance with
            {
                Locator = "docs/release notes.txt",
            },
        };

        var ledger = EvidenceLedgerBuilder.BuildRepositoryLedger(
            KnownRepositorySubject(),
            [draft]);

        Assert.Equal(
            "docs/release notes.txt",
            ledger.Records[0].Provenance.Locator);
    }

    [Fact]
    public void ExactNupkgNuspecSuppliesCanonicalPackageIdentity()
    {
        var nupkg = Convert.FromBase64String(
            Encoding.ASCII.GetString(ReadCanonicalFixture("nupkg.base64")));
        using var valuesDocument = JsonDocument.Parse(
            ReadCanonicalFixture("values.json"));

        var identity = EvidenceIdentity.ReadPackageIdentity(nupkg);

        Assert.Equal("widget.blazor", identity.PackageId);
        Assert.Equal("1.2.3-beta+Build_7", identity.Version);
        Assert.Equal("sha256", identity.NupkgDigest.Algorithm);
        Assert.Equal(
            valuesDocument.RootElement
                .GetProperty("nupkg_sha256")
                .GetString(),
            identity.NupkgDigest.Value);
    }

    [Fact]
    public void PackageIdentityUsesOneSnapshotWhenSourceMutatesOnSecondRewind()
    {
        var initial = CreateNupkg(
            "<package><metadata><id>First.Package</id><version>1.0.0</version>" +
            "</metadata></package>");
        var replacement = CreateNupkg(
            "<package><metadata><id>Second.Package</id><version>2.0.0</version>" +
            "</metadata></package>");
        using var stream = new MutatingOnSecondRewindStream(initial, replacement);

        var identity = EvidenceIdentity.ReadPackageIdentity(stream);

        Assert.Equal("first.package", identity.PackageId);
        Assert.Equal("1.0.0", identity.Version);
        Assert.Equal(
            Convert.ToHexStringLower(
                System.Security.Cryptography.SHA256.HashData(initial)),
            identity.NupkgDigest.Value);
    }

    [Fact]
    public void PackageSnapshotRejectsShortReadAfterLengthCheck()
    {
        var nupkg = CreateNupkg(CanonicalNuspec());
        using var stream = new MisreportedLengthStream(
            nupkg,
            nupkg.LongLength + 1);

        var exception = Assert.Throws<InvalidDataException>(() =>
            EvidenceIdentity.ReadPackageIdentity(stream));

        Assert.Contains(
            "nupkg input changed while it was read",
            exception.Message);
    }

    [Fact]
    public void PackageSnapshotRejectsGrowthAfterLengthCheck()
    {
        var nupkg = CreateNupkg(CanonicalNuspec());
        using var stream = new MisreportedLengthStream(
            nupkg,
            nupkg.LongLength - 1);

        var exception = Assert.Throws<InvalidDataException>(() =>
            EvidenceIdentity.ReadPackageIdentity(stream));

        Assert.Contains(
            "nupkg input grew while it was read",
            exception.Message);
    }

    [Theory]
    [InlineData("1.2.3 beta")]
    [InlineData("[1.0,2.0)")]
    [InlineData("1.2.3/evil")]
    public void UnsafeNuspecVersionTokensFail(string version)
    {
        var nupkg = CreateNupkg(
            $"<package><metadata><id>Widget.Blazor</id><version>{version}</version>" +
            "</metadata></package>");

        var exception = Assert.Throws<InvalidDataException>(() =>
            EvidenceIdentity.ReadPackageIdentity(nupkg));

        Assert.Contains("version", exception.Message);
    }

    [Fact]
    public void NuspecDtdIsProhibited()
    {
        var nupkg = CreateNupkg(
            "<!DOCTYPE package [<!ENTITY xxe SYSTEM \"file:///etc/passwd\">]>" +
            "<package><metadata><id>&xxe;</id><version>1.0.0</version>" +
            "</metadata></package>");

        var exception = Assert.Throws<InvalidDataException>(() =>
            EvidenceIdentity.ReadPackageIdentity(nupkg));

        Assert.Contains("invalid or unsafe nuspec XML", exception.Message);
    }

    [Fact]
    public void NuspecReadStopsAtConfiguredByteLimit()
    {
        using var stream = new MemoryStream(new byte[9]);

        var exception = Assert.Throws<InvalidDataException>(() =>
            EvidenceIdentity.ReadBounded(stream, maximumBytes: 8));

        Assert.Contains("exceeds 8 bytes", exception.Message);
        Assert.Equal(9, stream.Position);
    }

    [Fact]
    public void DuplicateNuspecEntriesFail()
    {
        var nupkg = CreateNupkg(
            ("First.nuspec", CanonicalNuspec()),
            ("Second.nuspec", CanonicalNuspec()));

        var exception = Assert.Throws<InvalidDataException>(() =>
            EvidenceIdentity.ReadPackageIdentity(nupkg));

        Assert.Contains("one nuspec; found 2", exception.Message);
    }

    [Fact]
    public void DuplicateMetadataFails()
    {
        var nupkg = CreateNupkg(
            "<package><metadata><id>Widget.Blazor</id><version>1.0.0</version>" +
            "</metadata><metadata><id>Other</id><version>2.0.0</version>" +
            "</metadata></package>");

        var exception = Assert.Throws<InvalidDataException>(() =>
            EvidenceIdentity.ReadPackageIdentity(nupkg));

        Assert.Contains("exactly one metadata", exception.Message);
    }

    [Theory]
    [InlineData(
        "<id>Widget.Blazor</id><id>Other</id><version>1.0.0</version>",
        "id")]
    [InlineData(
        "<id>Widget.Blazor</id><version>1.0.0</version><version>2.0.0</version>",
        "version")]
    public void DuplicateNuspecIdentityFieldsFail(string metadata, string field)
    {
        var nupkg = CreateNupkg(
            $"<package><metadata>{metadata}</metadata></package>");

        var exception = Assert.Throws<InvalidDataException>(() =>
            EvidenceIdentity.ReadPackageIdentity(nupkg));

        Assert.Contains($"exactly one {field}", exception.Message);
    }

    [Fact]
    public void SupersessionIsImmutableAcyclicAndNotCoselectable()
    {
        var originalDraft = RepositoryDraft(
            "Repository license is MIT.",
            "2026-08-16T20:00:00Z");
        var originalLedger = EvidenceLedgerBuilder.BuildRepositoryLedger(
            KnownRepositorySubject(),
            [originalDraft]);
        var original = Assert.Single(originalLedger.Records);
        var successorDraft = RepositoryDraft(
            "Repository license is MIT.",
            "2026-08-17T20:00:00Z") with
        {
            Supersedes = [original.StableId],
        };
        var ledger = EvidenceLedgerBuilder.BuildRepositoryLedger(
            KnownRepositorySubject(),
            [originalDraft, successorDraft]);
        var successor = Assert.Single(
            ledger.Records,
            record => record.Supersedes.Count > 0);

        var exception = Assert.Throws<InvalidDataException>(() =>
            EvidenceLedgerBuilder.BuildBundle(
                KnownAssessment(),
                [ledger],
                [original.StableId, successor.StableId]));

        Assert.Contains("EVID010", exception.Message);
        Assert.Contains("superseded", exception.Message);
    }

    [Fact]
    public void MissingSupersessionPredecessorFails()
    {
        var draft = RepositoryDraft(
            "Repository license is MIT.",
            "2026-08-16T20:00:00Z") with
        {
            Supersedes = ["EV1-" + new string('f', 64)],
        };

        var exception = Assert.Throws<InvalidDataException>(() =>
            EvidenceLedgerBuilder.BuildRepositoryLedger(
                KnownRepositorySubject(),
                [draft]));

        Assert.Contains("EVID010", exception.Message);
        Assert.Contains("missing", exception.Message);
    }

    [Theory]
    [InlineData("2026-08-16T19:59:59Z")]
    [InlineData("2026-08-16T20:00:00Z")]
    public void SupersessionChronologyRemainsReviewerJudgment(string timestamp)
    {
        var originalDraft = RepositoryDraft(
            "Repository license is MIT.",
            "2026-08-16T20:00:00Z");
        var original = Assert.Single(
            EvidenceLedgerBuilder.BuildRepositoryLedger(
                KnownRepositorySubject(),
                [originalDraft]).Records);
        var successor = RepositoryDraft(
            "Repository license is MIT.",
            timestamp) with
        {
            Provenance = originalDraft.Provenance with
            {
                ContentDigest = new Sha256Digest("sha256", new string('c', 64)),
                CapturedAtUtc = timestamp,
            },
            Supersedes = [original.StableId],
        };

        var ledger = EvidenceLedgerBuilder.BuildRepositoryLedger(
            KnownRepositorySubject(),
            [originalDraft, successor]);

        Assert.Equal(2, ledger.Records.Count);
        Assert.Single(ledger.Records, record => record.Supersedes.Count == 1);
    }

    [Fact]
    public void NoncanonicalPersistedJsonFails()
    {
        var canonical = ReadCanonicalFixture("repository-ledger.json");
        var noncanonical = Encoding.UTF8.GetBytes(
            Encoding.UTF8.GetString(canonical) + "\n");

        var exception = Assert.Throws<InvalidDataException>(() =>
            CanonicalEvidenceJson.ParseSourceLedger(noncanonical));

        Assert.Contains("EVID001", exception.Message);
        Assert.Contains("not canonical", exception.Message);
    }

    [Fact]
    public void UnknownAndDuplicateJsonPropertiesFail()
    {
        var canonical = Encoding.UTF8.GetString(
            ReadCanonicalFixture("assessment.json"));
        var unknown = Encoding.UTF8.GetBytes(
            canonical.Replace(
                "\"component_id\":\"Tree\"",
                "\"component_id\":\"Tree\",\"unknown\":true",
                StringComparison.Ordinal));
        var duplicate = Encoding.UTF8.GetBytes(
            canonical.Replace(
                "\"component_id\":\"Tree\"",
                "\"component_id\":\"Tree\",\"component_id\":\"Tree\"",
                StringComparison.Ordinal));

        Assert.Contains(
            "EVID001",
            Assert.Throws<InvalidDataException>(() =>
                CanonicalEvidenceJson.ParseAssessment(unknown)).Message);
        Assert.Contains(
            "EVID001",
            Assert.Throws<InvalidDataException>(() =>
                CanonicalEvidenceJson.ParseAssessment(duplicate)).Message);
    }

    [Fact]
    public void MalformedUtf8AndBomFail()
    {
        var canonical = ReadCanonicalFixture("assessment.json");
        var malformed = canonical.ToArray();
        malformed[malformed.Length / 2] = 0xff;
        var bom = new byte[canonical.Length + 3];
        bom[0] = 0xef;
        bom[1] = 0xbb;
        bom[2] = 0xbf;
        canonical.CopyTo(bom.AsSpan(3));

        Assert.Contains(
            "EVID001",
            Assert.Throws<InvalidDataException>(() =>
                CanonicalEvidenceJson.ParseAssessment(malformed)).Message);
        Assert.Contains(
            "EVID001",
            Assert.Throws<InvalidDataException>(() =>
                CanonicalEvidenceJson.ParseAssessment(bom)).Message);
    }

    [Fact]
    public void AlternateJsonEscapingFailsCanonicalParsing()
    {
        var canonical = Encoding.UTF8.GetString(
            ReadCanonicalFixture("assessment.json"));
        var noncanonical = Encoding.UTF8.GetBytes(
            canonical.Replace("Tree", "\\u0054ree", StringComparison.Ordinal));

        var exception = Assert.Throws<InvalidDataException>(() =>
            CanonicalEvidenceJson.ParseAssessment(noncanonical));

        Assert.Contains("not canonical", exception.Message);
    }

    [Fact]
    public void OverlappingSourceLedgersAreRejectedAsAmbiguous()
    {
        var known = KnownRepositoryLedger();
        var first = EvidenceLedgerBuilder.BuildRepositoryLedger(
            known.RepositorySubject!,
            [ToDraft(known.Records[0])]);
        var second = EvidenceLedgerBuilder.BuildRepositoryLedger(
            known.RepositorySubject!,
            [ToDraft(known.Records[0]), ToDraft(known.Records[1])]);

        var exception = Assert.Throws<InvalidDataException>(() =>
            EvidenceLedgerBuilder.BuildBundle(
                KnownAssessment(),
                [first, second],
                [known.Records[0].StableId]));

        Assert.Contains("EVID008", exception.Message);
        Assert.Contains("resolves to 2", exception.Message);
    }

    [Fact]
    public void ProgramRoutesExposeOnlyCompleteStep2Commands()
    {
        var content = File.ReadAllText(
            Path.Combine(
                RepositoryRoot,
                "eng",
                "tools",
                "BlazorComponentReadiness",
                "Program.cs"),
            Encoding.UTF8);

        Assert.Contains(
            "<scorecard|tracker|ledger|receipt|revision|validate-skill> [options]",
            content);
        Assert.Contains("\"scorecard\" =>", content);
        Assert.Contains("\"tracker\" =>", content);
        Assert.Contains("\"validate-skill\" =>", content);
        Assert.Contains("\"ledger\" =>", content);
        Assert.Contains("\"receipt\" =>", content);
        Assert.Contains("\"revision\" =>", content);
    }

    private static ExactAssessmentIdentity KnownAssessment()
    {
        return CanonicalEvidenceJson.ParseAssessment(
            ReadCanonicalFixture("assessment.json"));
    }

    private static RepositoryLedgerSubject KnownRepositorySubject()
    {
        return KnownRepositoryLedger().RepositorySubject!;
    }

    private static EvidenceSourceLedger KnownRepositoryLedger()
    {
        return CanonicalEvidenceJson.ParseSourceLedger(
            ReadCanonicalFixture("repository-ledger.json"));
    }

    private static EvidenceSourceLedger KnownComponentLedger()
    {
        return CanonicalEvidenceJson.ParseSourceLedger(
            ReadCanonicalFixture("component-ledger.json"));
    }

    private static EvidenceBundle KnownBundle()
    {
        return CanonicalEvidenceJson.ParseBundle(
            ReadCanonicalFixture("bundle.json"));
    }

    private static EvidenceRecordDraft RepositoryDraft(
        string claim,
        string timestamp)
    {
        return new EvidenceRecordDraft(
            claim,
            new EvidenceApplicability("repository-wide", null),
            new EvidenceProvenance(
                "repository-path",
                "LICENSE",
                "Read exact repository file.",
                timestamp,
                new Sha256Digest("sha256", new string('b', 64)),
                "commitment-only"),
            []);
    }

    private static EvidenceRecordDraft ComponentDraft()
    {
        return new EvidenceRecordDraft(
            "Tree expands selected nodes.",
            new EvidenceApplicability("component-specific", "Tree"),
            new EvidenceProvenance(
                "command-probe",
                "probe: Tree expansion",
                "Run deterministic browser probe.",
                "2026-08-16T20:03:00Z",
                new Sha256Digest("sha256", new string('e', 64)),
                "commitment-only"),
            []);
    }

    private static EvidenceRecordDraft ToDraft(EvidenceRecord record)
    {
        return new EvidenceRecordDraft(
            record.Claim,
            record.Applicability,
            record.Provenance,
            record.Supersedes);
    }

    private static ValidationInputManifest BuildKnownAnswerManifest()
    {
        return new ValidationInputManifest(
            1,
            [
                new ValidationInput(
                    "references/checklist.md",
                    new Sha256Digest("sha256", new string('f', 64))),
                new ValidationInput(
                    "references/overlays/scaffolder.md",
                    new Sha256Digest("sha256", new string('9', 64))),
            ]);
    }

    private static byte[] ReadCanonicalFixture(string name)
    {
        var bytes = File.ReadAllBytes(Path.Combine(FixtureRoot, name));
        Assert.NotEmpty(bytes);
        Assert.Equal((byte)'\n', bytes[^1]);
        return bytes[..^1];
    }

    private static byte[] CreateNupkg(string nuspec)
    {
        return CreateNupkg(("Widget.Blazor.nuspec", nuspec));
    }

    private static byte[] CreateNupkg(params (string Name, string Content)[] entries)
    {
        using var memory = new MemoryStream();
        using (var archive = new ZipArchive(
            memory,
            ZipArchiveMode.Create,
            leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                var entry = archive.CreateEntry(
                    name,
                    CompressionLevel.NoCompression);
                using var stream = entry.Open();
                stream.Write(Encoding.UTF8.GetBytes(content));
            }
        }

        return memory.ToArray();
    }

    private static string CanonicalNuspec()
    {
        return "<package><metadata><id>Widget.Blazor</id><version>1.0.0</version>" +
            "</metadata></package>";
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "activate.sh")) &&
                Directory.Exists(Path.Combine(
                    directory.FullName,
                    ".github",
                    "skills",
                    "blazor-component-readiness")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find the repository root.");
    }

    private sealed class ConstantHasher : IEvidenceHasher
    {
        public byte[] Hash(ReadOnlySpan<byte> content)
        {
            return new byte[32];
        }
    }

    private sealed class MutatingOnSecondRewindStream : Stream
    {
        private readonly byte[] _replacement;
        private MemoryStream _current;
        private int _rewindCount;

        internal MutatingOnSecondRewindStream(
            byte[] initial,
            byte[] replacement)
        {
            _current = new MemoryStream(initial, writable: false);
            _replacement = replacement;
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => _current.Length;

        public override long Position
        {
            get => _current.Position;
            set
            {
                ReplaceOnSecondRewind(value);
                _current.Position = value;
            }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _current.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            var target = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => checked(_current.Position + offset),
                SeekOrigin.End => checked(_current.Length + offset),
                _ => throw new ArgumentOutOfRangeException(nameof(origin)),
            };
            ReplaceOnSecondRewind(target);
            _current.Position = target;

            return target;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _current.Dispose();
            }

            base.Dispose(disposing);
        }

        private void ReplaceOnSecondRewind(long target)
        {
            if (target == 0 && ++_rewindCount == 2)
            {
                _current.Dispose();
                _current = new MemoryStream(_replacement, writable: false);
            }
        }
    }

    private sealed class MisreportedLengthStream : MemoryStream
    {
        private readonly long _reportedLength;

        internal MisreportedLengthStream(
            byte[] bytes,
            long reportedLength)
            : base(bytes, writable: false)
        {
            _reportedLength = reportedLength;
        }

        public override long Length => _reportedLength;
    }
}
