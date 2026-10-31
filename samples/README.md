# Packed-package smoke sample

Build the native artifacts and pack the package into `artifacts/nuget` first, then build
`samples/Smoke/Smoke.csproj`. The sample uses **PackageReference**, not project references, and R8.

It configures the core SDK with a placeholder key, creates a paywall view with a listener and adds it
to the activity. With the placeholder key RevenueCat cannot load offerings, so the run checks packaging,
R8, the JNI facade and startup (purchases-ui, Compose and the core SDK load, the facade creates the view
and listener events reach .NET), not a real paywall. To see a real paywall, launch with a public SDK key
of a test project: `adb shell am start -n <package>/<activity> -e revenuecat_api_key <key>`.
