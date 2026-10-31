using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Activity;
using Com.Revenuecat.Purchases;
using Com.Revenuecat.Purchases.Models;
using Kapusch.RevenueCatUI.Android;

[Activity(Label = "RevenueCatUI smoke", MainLauncher = true)]
public class MainActivity : ComponentActivity
{
	protected override void OnCreate(Bundle? savedInstanceState)
	{
		base.OnCreate(savedInstanceState);

		// A placeholder key keeps the sample free of real credentials; offerings then fail to load.
		// Pass a public SDK key of a test project with: adb shell am start -e revenuecat_api_key <key> ...
		var apiKey = Intent?.GetStringExtra("revenuecat_api_key") ?? "goog_placeholder";
		using (var builder = new PurchasesConfiguration.Builder(ApplicationContext!, apiKey))
		using (var configuration = builder.Build())
			Purchases.Configure(configuration);

		var root = new FrameLayout(this);
		SetContentView(root);
		var paywall = RevenueCatPaywall.CreateView(this, new SmokeListener(root), displayCloseButton: true);
		root.AddView(paywall, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));
		Android.Util.Log.Info("RevenueCatUISmoke", "REVENUECATUI_NATIVE_SMOKE_OK " + paywall.Class.Name);
	}
}

sealed class SmokeListener(FrameLayout root) : IPaywallListener
{
	public void PurchaseInitiated(Package package, PaywallResume resume)
	{
		Android.Util.Log.Info("RevenueCatUISmoke", "REVENUECATUI_EVENT purchase_initiated " + package.Identifier);
		resume.Proceed();
	}

	public void PurchaseCompleted(CustomerInfo customerInfo, StoreTransaction transaction) =>
		Android.Util.Log.Info("RevenueCatUISmoke", "REVENUECATUI_EVENT purchase_completed " + transaction.OrderId);

	public void PurchaseFailed(PurchasesError error) =>
		Android.Util.Log.Info("RevenueCatUISmoke", "REVENUECATUI_EVENT purchase_failed " + error.Code);

	public void DismissRequested()
	{
		Android.Util.Log.Info("RevenueCatUISmoke", "REVENUECATUI_EVENT dismiss_requested");
		root.RemoveAllViews();
	}
}
