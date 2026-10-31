# Kapusch.RevenueCatUI.Android

Thin .NET interop for RevenueCat Paywalls (purchases-ui 10.16.1) on Android API 24+.
`RevenueCatPaywall.CreateView` returns RevenueCatUI's `PaywallView` and forwards its listener to
`IPaywallListener`, including the purchase and restore hooks that wait for
`PaywallResume.Proceed()` or `Cancel()`. The view never closes itself: handle `DismissRequested`.

Requires `Kebechet.Maui.RevenueCat.Android` 10.16.1.x (brought in automatically), which ships the
matching core SDK, and Compose bindings from the Activity 1.12.4 / Lifecycle 2.10 AndroidX wave.
Configure the core SDK before showing a paywall.

See the [integration guide](https://github.com/Kapusch/RevenueCatUIApisForAndroidComponents/blob/main/Docs/Integration.md)
and the [changelog](https://github.com/Kapusch/RevenueCatUIApisForAndroidComponents/blob/main/CHANGELOG.md).
