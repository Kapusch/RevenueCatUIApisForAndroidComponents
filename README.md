# RevenueCatUIApisForAndroidComponents

Thin .NET 10 Android interop for [RevenueCat Paywalls](https://www.revenuecat.com/docs/tools/paywalls)
(purchases-ui). NuGet ID: **Kapusch.RevenueCatUI.Android**.

Paywalls are designed in the RevenueCat dashboard and rendered natively by purchases-ui (Jetpack
Compose). This package exposes RevenueCatUI's `PaywallView` and its `PaywallListener` to .NET,
nothing more: it creates the view, forwards every listener call (including the purchase and restore
hooks that wait for an answer) and supports offering, close button and custom variable updates.
Purchases are performed by the RevenueCat SDK; entitlement, navigation and analytics decisions stay
in the app.

| Package | Ships | Depends on |
|---|---|---|
| `Kapusch.RevenueCatUI.Android` | `purchases-ui` 10.16.1, a JNI facade AAR and the purchases-ui dependencies without a managed binding | [`Kebechet.Maui.RevenueCat.Android`](https://www.nuget.org/packages/Kebechet.Maui.RevenueCat.Android) 10.16.1.x (core SDK) and `Xamarin.AndroidX.Compose.*` 1.10.4 bindings |

The managed Compose and AndroidX bindings come from a single dotnet/android-libraries release
(Activity 1.12.4, Lifecycle 2.10, Kotlin 2.3.10), so the package adds Compose without moving an
app's AndroidX wave. Shipped native files are pinned by SHA-256.

See [integration](Docs/Integration.md), [source builds](Docs/SourceMode.md),
[releases](Docs/Release.md), [samples](samples/README.md), [changelog](CHANGELOG.md) and
[third-party licenses](THIRD_PARTY_NOTICES.md).

## Build

```sh
bash src/Kapusch.RevenueCatUIApisForAndroidComponents/Native/Android/build.sh
dotnet pack src/Kapusch.RevenueCatUIApisForAndroidComponents/Kapusch.RevenueCatUIApisForAndroidComponents.csproj -c Release -o artifacts/nuget
```

The native build bootstraps a checksum-verified Gradle and resolves the pinned Maven artifacts.
Consumer builds do not download native SDKs.

## CI

| Trigger | Result |
|---|---|
| Pull request, push to `main` | Build, verify and pack (`ci.yml`) |
| Manual `publish` run without a version | Preview package on GitHub Packages |
| `vX.Y.Z` / `vX.Y.Z-rc.N` tag reachable from `main` | Package on NuGet.org |

Required GitHub secret: `NUGET_USER` (NuGet.org Trusted Publishing).
