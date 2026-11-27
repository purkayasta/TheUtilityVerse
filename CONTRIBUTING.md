# Contributing to The UtilityVerse

Discuss a proposed change in an issue first. Use the repository README to find the
package guide, then read the applicable `AGENTS.md` instructions before editing.
[Package guides](README.md#packages) · [Repository instructions](AGENTS.md)

## Branch and pull request

1. Fork the repository and create a branch from `development`.
2. Keep the change focused and add or update regression tests for changed behavior.
3. Run the validation workflow below.
4. Open a pull request against `development`, link the issue, and report validation results.

Push contributions to your feature branch. Describe consumer-visible behavior
changes and update the affected README and NuGet package documentation.

## Validate a change

Use .NET SDK **11.0.100** with compatible patch roll-forward, as selected by
`global.json`. Run commands from the repository root.

```sh
./scripts/build.sh
bash scripts/verify-packages.sh
```

On Windows, use `scripts/build.ps1` for the full build workflow. The build script
removes `bin` and `obj` under source and tests, formats modified and untracked C#
files, restores, builds, and tests. Include scoped formatting changes in the commit.
For an individual formatting pass, use `dotnet format UtilityVerse.slnx --include <paths>`.

Package verification builds Release packages and exercises fresh consumers on
.NET 8 and .NET 11. Copy changes must also follow the project's test-first workflow
and keep diagnostics, supported-shape documentation, and runtime behavior aligned.
[Copy instructions](src/UtilityVerse.Copy/AGENTS.md)

## Publish to NuGet.org with Trusted Publishing

The [`publish.yml`](.github/workflows/publish.yml) workflow automatically publishes
unpublished versions of the three packages after a push to `master`, including a merged pull request.
It also supports manual retries. It runs Release validation, fails on known
dependency vulnerabilities or unavailable audit data, and verifies fresh package
consumers before obtaining a temporary credential with `NuGet/login@v1`.
The publishing job requests `id-token: write`; no long-lived NuGet API key is needed.
It checks each built version on NuGet.org and publishes only missing versions.
If nothing is new, it skips login, publishing, and GitHub Release creation.
After the selected uploads succeed, it creates a GitHub Release at the published
commit with generated notes and only the published `.nupkg` assets. GitHub's built-in token
uses `contents: write` for this step. An existing release is left unchanged on
retries. NuGet acceptance precedes its asynchronous validation and search indexing;
a successful upload does not mean the package is immediately searchable.

Configure these account settings before the first publish:

1. Create a GitHub environment named `release`. Restrict deployment branches to
   `master`. Required reviewers are optional; enabling them pauses automatic
   publishing until an environment reviewer approves the job.
2. Add environment secret `NUGET_USER` containing your NuGet.org profile username,
   not your email address.
3. At [NuGet.org Trusted Publishing](https://www.nuget.org/account/trustedpublishing),
   add a policy owned by the account or organization that owns these packages:

   | Field | Value |
   | --- | --- |
   | Repository owner | `purkayasta` |
   | Repository | `TheUtilityVerse` |
   | Workflow file | `publish.yml` (filename only) |
   | Environment | `release` |
   | Package pattern | `UtilityVerse*` |

   Allow new versions of the three existing packages. Shared is bundled inside
   UtilityVerse and must not be published separately. The workflow uses explicit
   filenames for the three packages rather than publishing the pattern.

Before enabling the first publish, confirm the policy is configured and merge the
reviewed workflow through `development` into `master`. That merge triggers
publishing, so configure the account settings first. Bump the version of each
package you want to release; changing source alone does not publish an existing
version again. Shared changes require a UtilityVerse version bump. Package paths,
release tags, and titles are derived from the verified package files. Keep the
explicit consumer versions in `scripts/verify-packages.sh` aligned with version bumps.

Keep any existing API keys until a Trusted Publishing release succeeds. See the
[official Trusted Publishing guide](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing).

## Manual command-line publishing fallback

Run the release checks from the repository root before publishing:

```sh
bash scripts/build.sh Release
dotnet package list --project UtilityVerse.slnx --vulnerable --include-transitive
bash scripts/verify-packages.sh
```

Review the audit output, package manifests, and migration notes in
[`breaking-changes.md`](src/UtilityVerse/breaking-changes.md). A clean dependency
audit reports known advisories; it does not prove application code is secure.

The current release files are:

| Package | Version |
| --- | --- |
| UtilityVerse | 0.7.0 |
| UtilityVerse.Copy | 0.6.0 |
| UtilityVerse.ASPNET | 0.6.0 |

Shared's DLL and XML documentation are bundled inside UtilityVerse for every
target framework. There is no separate Shared package or NuGet dependency.

Create a short-lived API key on [NuGet.org](https://www.nuget.org/account/apikeys),
scoped to these package IDs and push permissions. Enter it locally; do not paste
it into chat, source files, or a command saved in shell history. In macOS zsh:

```zsh
read -rs 'NUGET_API_KEY?NuGet API key: '
printf '\n'
dotnet nuget push artifacts/packages/UtilityVerse.0.7.0.nupkg --api-key "$NUGET_API_KEY" --source https://api.nuget.org/v3/index.json
dotnet nuget push artifacts/packages/UtilityVerse.Copy.0.6.0.nupkg --api-key "$NUGET_API_KEY" --source https://api.nuget.org/v3/index.json
dotnet nuget push artifacts/packages/UtilityVerse.ASPNET.0.6.0.nupkg --api-key "$NUGET_API_KEY" --source https://api.nuget.org/v3/index.json
unset NUGET_API_KEY
```

Use explicit filenames: the output folder can contain older packages, including
UtilityVerse 1.1.0. NuGet.org versions cannot be overwritten. Check existing
versions before pushing; do not hide an unexpected conflict with `--skip-duplicate`.
The CLI also pushes an adjacent matching `.snupkg` when one exists.

See Microsoft's [publishing guide](https://learn.microsoft.com/en-us/nuget/nuget-org/publish-a-package)
and [dependency auditing guide](https://learn.microsoft.com/en-us/nuget/concepts/auditing-packages).
