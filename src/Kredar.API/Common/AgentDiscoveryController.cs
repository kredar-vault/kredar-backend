using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kredar.API.Common;

[ApiController]
[AllowAnonymous]
public class AgentDiscoveryController : ControllerBase
{
    [HttpGet("/.well-known/llms.txt")]
    [Produces("text/plain")]
    public ContentResult Llms() => Content(LlmsTxt, "text/plain");

    private const string LlmsTxt = """
        # Kredar

        Dedicated Virtual Accounts Engine — issue a persistent NUBAN per customer, auto-reconcile
        inbound transfers, and push enriched webhook events. Built on Nomba.

        ## Base URL
        - Production:  https://api.kredar.xyz
        - Staging:     https://api.staging.kredar.xyz  (sandbox sim, zero real money)

        ## Auth
        1. Register at /api/v1/auth/register (email + password).
        2. Verify email by clicking the link sent to your inbox.
        3. Login at /api/v1/auth/login — a 6-digit OTP is sent to your email.
        4. Verify OTP at /api/v1/auth/login/verify -> { token, refreshToken }.
        5. Send `Authorization: Bearer <token>` on every /api/v1 call.

        ## Core flow
        - POST /api/v1/customers                        create a customer record
        - POST /api/v1/dedicated-accounts               provision a NUBAN for a customer
        - GET  /api/v1/dedicated-accounts/{id}          balance + payment state
        - GET  /api/v1/transactions                     reconciled inflows
        - POST /api/v1/webhook-endpoints                subscribe to deposit.reconciled events

        ## Build + verify with zero money (sandbox)
        - POST /api/v1/sandbox/simulate/deposit {accountReference, amountNaira}
          Drives a real reconciliation — create an account, simulate a deposit, watch it reconcile.

        ## Live Checkout (payment links)
        - POST /api/v1/checkout/sessions {accountReference} -> { token, snapshotUrl, streamUrl }
        - GET  /api/v1/checkout/{token}                     anonymous payment status snapshot
        - GET  /api/v1/checkout/{token}/stream              SSE stream of status changes

        ## Payouts
        - POST /api/v1/transfers/bank/lookup {accountNumber, bankCode}   verify recipient
        - POST /api/v1/transfers/bank {merchantTxRef, amount, ...}       initiate payout
        - GET  /api/v1/transfers                                          list all payouts

        ## Differentiators
        - Settlement Splits: PUT /api/v1/settings/splits
        - Escrow:            POST /api/v1/settlements/{ref}/hold|release
        - Money Rules:       GET/POST/DELETE /api/v1/rules
        - Sub-Merchants:     GET/POST /api/v1/sub-merchants
        - API Keys:          GET/POST/DELETE /api/v1/api-keys

        ## Conventions
        - All money is decimal naira (₦). Never kobo.
        - Payouts are idempotent on merchantTxRef.
        - Errors: JSON { isSuccess: false, message: "..." }.

        ## Full contract
        OpenAPI: /swagger/v1/swagger.json
        """;
}
