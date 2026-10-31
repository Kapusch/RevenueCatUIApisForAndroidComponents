using Com.Revenuecat.Purchases;
using Com.Revenuecat.Purchases.Models;

namespace Kapusch.RevenueCatUI.Android;

/// <summary>
/// Mirrors RevenueCatUI's <c>PaywallListener</c>. Every member is optional and is called on the main thread.
/// </summary>
public interface IPaywallListener
{
	/// <summary><c>onPurchaseStarted</c></summary>
	void PurchaseStarted(Package package) { }

	/// <summary>
	/// <c>onPurchasePackageInitiated</c>. The purchase waits until <paramref name="resume"/> is answered;
	/// the default proceeds immediately.
	/// </summary>
	void PurchaseInitiated(Package package, PaywallResume resume) => resume.Proceed();

	/// <summary><c>onPurchaseCompleted</c></summary>
	void PurchaseCompleted(CustomerInfo customerInfo, StoreTransaction transaction) { }

	/// <summary><c>onPurchaseCancelled</c></summary>
	void PurchaseCancelled() { }

	/// <summary><c>onPurchaseError</c></summary>
	void PurchaseFailed(PurchasesError error) { }

	/// <summary><c>onRestoreStarted</c></summary>
	void RestoreStarted() { }

	/// <summary>
	/// <c>onRestoreInitiated</c>. The restore waits until <paramref name="resume"/> is answered;
	/// the default proceeds immediately.
	/// </summary>
	void RestoreInitiated(PaywallResume resume) => resume.Proceed();

	/// <summary><c>onRestoreCompleted</c></summary>
	void RestoreCompleted(CustomerInfo customerInfo) { }

	/// <summary><c>onRestoreError</c></summary>
	void RestoreFailed(PurchasesError error) { }

	/// <summary>
	/// The paywall's dismiss handler: its close button, or the end of a successful purchase. The view never
	/// closes itself; remove it here.
	/// </summary>
	void DismissRequested() { }
}
