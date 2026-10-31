# Public native SDK working agreement

Produce a reproducible `Kapusch.RevenueCatUI.Android` NuGet. Keep all documentation,
samples and artifacts generic. No application/company names, real API keys,
private diagnostics or customer data. Follow src/Native, buildTransitive, Docs
and samples conventions. Package native dependencies; consumer builds never
fetch them.

Keep the wrapper thin: mirror RevenueCatUI's own API and listener, add no purchase,
entitlement or navigation policy. Never ship the core purchases AAR: the core binding
package provides it, from the same purchases-android release as purchases-ui. Bump both
together.

Ship only the purchases-ui dependencies that have no managed .NET binding on the
consumer's AndroidX wave. Take every managed binding version from one dotnet/android-libraries
release, keep the Compose BOM in `revenuecatuiinterop/build.gradle.kts` aligned with it, and
pin shipped files in `DependencyLocks/native-artifacts.json`.

The JNI facade is `com.kapusch.revenuecatui.androidinterop.PaywallInterop`. Change a
signature by adding a new method, never by editing an existing one; `scripts/verify-package.py`
checks the managed lookups against the AAR. Verify a clean packed-package consumer and
device/runtime semantics before claiming support.

`main` is the only long-lived branch and the source of every NuGet.org release.
Use short-lived branches and pull requests. Commits follow Conventional Commits.
