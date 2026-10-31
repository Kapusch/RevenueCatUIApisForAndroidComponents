# Integration

## Install

```xml
<PackageReference Include="Kapusch.RevenueCatUI.Android" Version="1.0.0-rc.1" />
```

The package brings `Kebechet.Maui.RevenueCat.Android` in the matching version range and the Compose
bindings. Configure the core SDK (`Purchases`) as usual before showing a paywall. The paywall uses the
offering's paywall as published in the RevenueCat dashboard; when none is published, RevenueCatUI
shows its default paywall. The app needs Android API 24 or later.

## Show a paywall

```csharp
using Com.Revenuecat.Purchases;
using Com.Revenuecat.Purchases.Models;
using Kapusch.RevenueCatUI.Android;

sealed class PaywallEvents : IPaywallListener
{
	public void PurchaseInitiated(Package package, PaywallResume resume)
	{
		// Optional gate before the store sheet, for example a sign-in check.
		resume.Proceed();
	}

	public void PurchaseCompleted(CustomerInfo customerInfo, StoreTransaction transaction)
	{
		// Entitlement handling stays in the app.
	}

	public void DismissRequested()
	{
		// Close button or successful purchase: remove the view.
	}
}

var paywall = RevenueCatPaywall.CreateView(activity, new PaywallEvents(), offering: null, displayCloseButton: true);
container.AddView(paywall);
```

`CreateView` returns RevenueCatUI's `PaywallView`, a regular Android `View` that hosts Compose.
Create it with an Activity context on the main thread; every listener call arrives on the main thread.

## Behavior

- The view never closes itself. `DismissRequested` fires for the close button and after a
  successful purchase; the app decides what happens next.
- `PurchaseInitiated` and `RestoreInitiated` hold the operation until `Proceed()` or `Cancel()` is
  called. The defaults proceed immediately. An exception thrown by either handler cancels the
  operation.
- Route the system back action to `RevenueCatPaywall.OnBackPressed` when the paywall should handle it
  (for example to close a sheet it opened).
- Paywall language follows the core SDK's preferred UI locale
  (`Purchases.SharedInstance.OverridePreferredUILocale`, or the device language).
