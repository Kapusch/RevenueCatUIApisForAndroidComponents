package com.kapusch.revenuecatui.androidinterop

import android.content.Context
import android.view.View
import com.revenuecat.purchases.CustomerInfo
import com.revenuecat.purchases.Offering
import com.revenuecat.purchases.Package
import com.revenuecat.purchases.PurchasesError
import com.revenuecat.purchases.models.StoreTransaction
import com.revenuecat.purchases.ui.revenuecatui.CustomVariableValue
import com.revenuecat.purchases.ui.revenuecatui.PaywallListener
import com.revenuecat.purchases.ui.revenuecatui.utils.Resumable
import com.revenuecat.purchases.ui.revenuecatui.views.PaywallView
import java.util.concurrent.ConcurrentHashMap
import java.util.concurrent.atomic.AtomicLong

/**
 * Static entry points called from .NET through JNI. Event numbers match the iOS package.
 * `events` receives (event, first, second, token); `token` is non-zero for the two events that
 * wait for [resume].
 */
object PaywallInterop {
    const val PURCHASE_STARTED = 1
    const val PURCHASE_INITIATED = 2
    const val PURCHASE_COMPLETED = 3
    const val PURCHASE_CANCELLED = 4
    const val PURCHASE_FAILED = 5
    const val RESTORE_STARTED = 6
    const val RESTORE_INITIATED = 7
    const val RESTORE_COMPLETED = 8
    const val RESTORE_FAILED = 9
    const val DISMISS_REQUESTED = 10

    private val nextToken = AtomicLong()
    private val pending = ConcurrentHashMap<Long, Resumable>()

    private fun register(resume: Resumable): Long {
        val token = nextToken.incrementAndGet()
        pending[token] = resume
        return token
    }

    /**
     * Creates a [PaywallView] for [offering] (null: the current offering). Always passes a dismiss
     * handler, so the view never closes itself and the caller decides what a close or a completed
     * purchase does. Main thread only.
     */
    @JvmStatic
    fun createPaywallView(
        context: Context,
        offering: Offering?,
        displayCloseButton: Boolean,
        events: Function4<Int, Any?, Any?, Long, Unit>,
    ): View {
        val listener = object : PaywallListener {
            override fun onPurchaseStarted(rcPackage: Package) =
                events(PURCHASE_STARTED, rcPackage, null, 0L)

            override fun onPurchasePackageInitiated(rcPackage: Package, resume: Resumable) =
                events(PURCHASE_INITIATED, rcPackage, null, register(resume))

            override fun onPurchaseCompleted(customerInfo: CustomerInfo, storeTransaction: StoreTransaction) =
                events(PURCHASE_COMPLETED, customerInfo, storeTransaction, 0L)

            override fun onPurchaseCancelled() = events(PURCHASE_CANCELLED, null, null, 0L)

            override fun onPurchaseError(error: PurchasesError) = events(PURCHASE_FAILED, error, null, 0L)

            override fun onRestoreStarted() = events(RESTORE_STARTED, null, null, 0L)

            override fun onRestoreInitiated(resume: Resumable) =
                events(RESTORE_INITIATED, null, null, register(resume))

            override fun onRestoreCompleted(customerInfo: CustomerInfo) =
                events(RESTORE_COMPLETED, customerInfo, null, 0L)

            override fun onRestoreError(error: PurchasesError) = events(RESTORE_FAILED, error, null, 0L)
        }
        return PaywallView(
            context = context,
            offering = offering,
            listener = listener,
            fontProvider = null,
            shouldDisplayDismissButton = displayCloseButton,
            dismissHandler = { events(DISMISS_REQUESTED, null, null, 0L) },
        )
    }

    /** Answers a purchase or restore initiated event once. Returns false for an unknown token. */
    @JvmStatic
    fun resume(token: Long, proceed: Boolean): Boolean {
        val resume = pending.remove(token) ?: return false
        resume.resume(proceed)
        return true
    }

    @JvmStatic
    fun setOfferingId(view: View, offeringId: String?): Boolean {
        val paywall = view as? PaywallView ?: return false
        paywall.setOfferingId(offeringId)
        return true
    }

    @JvmStatic
    fun setDisplayDismissButton(view: View, display: Boolean): Boolean {
        val paywall = view as? PaywallView ?: return false
        paywall.setDisplayDismissButton(display)
        return true
    }

    /** Replaces the custom variables with [keys] and [values] (same length). */
    @JvmStatic
    fun setCustomVariables(view: View, keys: Array<String>, values: Array<String>): Boolean {
        val paywall = view as? PaywallView ?: return false
        if (keys.size != values.size) return false
        paywall.setCustomVariables(keys.indices.associate { keys[it] to CustomVariableValue.String(values[it]) })
        return true
    }

    @JvmStatic
    fun onBackPressed(view: View): Boolean {
        val paywall = view as? PaywallView ?: return false
        paywall.onBackPressed()
        return true
    }
}
