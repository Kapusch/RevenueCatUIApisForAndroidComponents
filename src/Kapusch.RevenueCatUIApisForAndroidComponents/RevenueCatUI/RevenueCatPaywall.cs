using Android.Content;
using Android.Runtime;
using Android.Views;
using Com.Revenuecat.Purchases;
using Com.Revenuecat.Purchases.Models;
using Kotlin.Jvm.Functions;

namespace Kapusch.RevenueCatUI.Android;

/// <summary>
/// Creates RevenueCatUI paywalls (<c>PaywallView</c>) and forwards their listener calls through JNI.
/// Configure the core SDK (<c>Purchases</c>) before creating a paywall. Main thread only.
/// </summary>
public static class RevenueCatPaywall
{
	private const string InteropClass = "com/kapusch/revenuecatui/androidinterop/PaywallInterop";
	private const int PurchaseStartedEvent = 1;
	private const int PurchaseInitiatedEvent = 2;
	private const int PurchaseCompletedEvent = 3;
	private const int PurchaseCancelledEvent = 4;
	private const int PurchaseFailedEvent = 5;
	private const int RestoreStartedEvent = 6;
	private const int RestoreInitiatedEvent = 7;
	private const int RestoreCompletedEvent = 8;
	private const int RestoreFailedEvent = 9;
	private const int DismissRequestedEvent = 10;

	private static readonly IntPtr Class = JNIEnv.NewGlobalRef(JNIEnv.FindClass(InteropClass));

	/// <summary>Creates a paywall view, to add to a layout.</summary>
	/// <param name="context">An Activity context (the view hosts Compose).</param>
	/// <param name="listener">Receives the paywall events. Kept alive as long as the view.</param>
	/// <param name="offering">The offering whose paywall to show; <see langword="null"/> uses the current offering.</param>
	/// <param name="displayCloseButton">Adds the paywall's own close button.</param>
	/// <remarks>The view never closes itself: handle <see cref="IPaywallListener.DismissRequested"/>.</remarks>
	public static View CreateView(
		Context context,
		IPaywallListener listener,
		Offering? offering = null,
		bool displayCloseButton = false
	)
	{
		ArgumentNullException.ThrowIfNull(context);
		ArgumentNullException.ThrowIfNull(listener);
		// Not disposed: the paywall keeps a Java reference to it, and the GC bridge keeps the managed
		// peer alive for as long as that reference exists.
		var sink = new EventSink(listener);
		var view = JNIEnv.CallStaticObjectMethod(
			Class,
			JNIEnv.GetStaticMethodID(
				Class,
				"createPaywallView",
				"(Landroid/content/Context;Lcom/revenuecat/purchases/Offering;ZLkotlin/jvm/functions/Function4;)Landroid/view/View;"
			),
			new JValue(context),
			offering is null ? new JValue(IntPtr.Zero) : new JValue(offering),
			new JValue(displayCloseButton),
			new JValue(sink)
		);
		return Java.Lang.Object.GetObject<View>(view, JniHandleOwnership.TransferLocalRef)
			?? throw new InvalidOperationException("RevenueCatUI did not create a paywall view.");
	}

	/// <summary><c>setOfferingId</c>; <see langword="null"/> uses the current offering.</summary>
	public static bool SetOfferingId(View paywall, string? offeringId)
	{
		ArgumentNullException.ThrowIfNull(paywall);
		var id = offeringId is null ? IntPtr.Zero : JNIEnv.NewString(offeringId);
		try
		{
			return CallBoolean("setOfferingId", "(Landroid/view/View;Ljava/lang/String;)Z", new JValue(paywall), new JValue(id));
		}
		finally
		{
			JNIEnv.DeleteLocalRef(id);
		}
	}

	/// <summary><c>setDisplayDismissButton</c></summary>
	public static bool SetDisplayDismissButton(View paywall, bool display)
	{
		ArgumentNullException.ThrowIfNull(paywall);
		return CallBoolean("setDisplayDismissButton", "(Landroid/view/View;Z)Z", new JValue(paywall), new JValue(display));
	}

	/// <summary><c>setCustomVariables</c> with string values; replaces any previous variables.</summary>
	public static bool SetCustomVariables(View paywall, IReadOnlyDictionary<string, string> variables)
	{
		ArgumentNullException.ThrowIfNull(paywall);
		ArgumentNullException.ThrowIfNull(variables);
		var keys = JNIEnv.NewArray(variables.Keys.ToArray());
		var values = JNIEnv.NewArray(variables.Values.ToArray());
		try
		{
			return CallBoolean(
				"setCustomVariables",
				"(Landroid/view/View;[Ljava/lang/String;[Ljava/lang/String;)Z",
				new JValue(paywall),
				new JValue(keys),
				new JValue(values)
			);
		}
		finally
		{
			JNIEnv.DeleteLocalRef(keys);
			JNIEnv.DeleteLocalRef(values);
		}
	}

	/// <summary><c>onBackPressed</c>, for hosts that route the system back action to the paywall.</summary>
	public static bool OnBackPressed(View paywall)
	{
		ArgumentNullException.ThrowIfNull(paywall);
		return CallBoolean("onBackPressed", "(Landroid/view/View;)Z", new JValue(paywall));
	}

	internal static void Resume(long token, bool proceed) =>
		CallBoolean("resume", "(JZ)Z", new JValue(token), new JValue(proceed));

	private static bool CallBoolean(string name, string signature, params JValue[] args) =>
		JNIEnv.CallStaticBooleanMethod(Class, JNIEnv.GetStaticMethodID(Class, name, signature), args);

	/// <summary>The Kotlin <c>Function4&lt;Int, Any?, Any?, Long, Unit&gt;</c> the facade calls for every event.</summary>
	private sealed class EventSink(IPaywallListener listener) : Java.Lang.Object, IFunction4
	{
		public Java.Lang.Object? Invoke(Java.Lang.Object? p0, Java.Lang.Object? p1, Java.Lang.Object? p2, Java.Lang.Object? p3)
		{
			var token = p3 is Java.Lang.Long value ? value.LongValue() : 0L;
			var resume = token != 0 ? new PaywallResume(token) : null;
			try
			{
				var @event = p0 is Java.Lang.Integer number ? number.IntValue() : 0;
				Dispatch(@event, p1, p2, resume);
			}
			catch (Exception exception)
			{
				// A failed check must not let a purchase through.
				resume?.Cancel();
				global::Android.Util.Log.Error("RevenueCatUI", $"Paywall listener threw: {exception}");
			}
			return Kotlin.Unit.Instance;
		}

		private void Dispatch(int @event, Java.Lang.Object? first, Java.Lang.Object? second, PaywallResume? resume)
		{
			switch (@event)
			{
				case PurchaseStartedEvent:
					listener.PurchaseStarted(Cast<Package>(first));
					break;
				case PurchaseInitiatedEvent:
					listener.PurchaseInitiated(Cast<Package>(first), resume!);
					break;
				case PurchaseCompletedEvent:
					listener.PurchaseCompleted(Cast<CustomerInfo>(first), Cast<StoreTransaction>(second));
					break;
				case PurchaseCancelledEvent:
					listener.PurchaseCancelled();
					break;
				case PurchaseFailedEvent:
					listener.PurchaseFailed(Cast<PurchasesError>(first));
					break;
				case RestoreStartedEvent:
					listener.RestoreStarted();
					break;
				case RestoreInitiatedEvent:
					listener.RestoreInitiated(resume!);
					break;
				case RestoreCompletedEvent:
					listener.RestoreCompleted(Cast<CustomerInfo>(first));
					break;
				case RestoreFailedEvent:
					listener.RestoreFailed(Cast<PurchasesError>(first));
					break;
				case DismissRequestedEvent:
					listener.DismissRequested();
					break;
			}
		}

		private static T Cast<T>(Java.Lang.Object? value)
			where T : Java.Lang.Object =>
			value?.JavaCast<T>() ?? throw new InvalidOperationException($"RevenueCatUI sent no {typeof(T).Name}.");
	}
}
