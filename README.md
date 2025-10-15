# Étale de Jeux API

## Stripe configuration

The API can read Stripe credentials from either environment variables or the `Stripe` section of `appsettings*.json`.

| Setting | Environment variable | Description |
| --- | --- | --- |
| `Stripe:PublishableKey` | `STRIPE_PUBLISHABLE` | Front-end publishable key used for initializing Stripe.js. |
| `Stripe:SecretKey` | `STRIPE_SECRET` | Secret key used to call Stripe APIs. The prefix (`sk_test`, `sk_live`, etc.) determines the detected mode. |
| `Stripe:WebhookSecret` | `STRIPE_WEBHOOK_SECRET` | Secret that secures incoming webhook events. |
| `Stripe:ExpectedAccountId` | `STRIPE_ACCOUNT` | Optional. When provided, the API verifies that the configured keys belong to the expected Stripe account before creating checkout sessions. |

For local development you can copy `appsettings.Development.json` and populate the Stripe section with keys from your dashboard. If you want to exercise the account verification logic, set `Stripe:ExpectedAccountId` (or `STRIPE_ACCOUNT`) to the account id displayed in the Stripe dashboard (e.g. `acct_1234`). When the secret key belongs to a different account, checkout session creation will return a `409 Conflict` with the message “Mauvais compte de clés Stripe”.

## How to test E2E (local)

1. **Run the API**
   ```bash
   dotnet run --project EtalDeJeux.Api
   ```
   The API listens on <http://localhost:5106> by default.

2. **Start the Stripe CLI forwarder**
   ```bash
   stripe listen --forward-to localhost:5106/stripe/webhook
   ```
   The CLI prints a webhook signing secret (`whsec_…`). Copy this value and export it for the API:
   ```bash
   export STRIPE_WEBHOOK_SECRET="whsec_xxx"
   ```

3. **Trigger a checkout session**
   - Use the API to create a session:
     ```bash
     curl -X POST http://localhost:5106/stripe/checkout
     ```
   - Complete the payment with the test card `4242 4242 4242 4242`, any future expiration date, and any CVC.

4. **Observe the outputs**
   - **Stripe CLI**: should display the forwarded `checkout.session.completed` event and confirm a `200` response from the API webhook endpoint.
   - **API logs**: should include entries that the webhook was received and processed without errors.

### Troubleshooting

- **Account id guard**: If checkout session creation fails, confirm the configured `Stripe:ExpectedAccountId`/`STRIPE_ACCOUNT` matches the account for your API keys. When they differ, the API returns `409 Conflict` with “Mauvais compte de clés Stripe”.
- **Diagnostic endpoint**: Verify the API is healthy by calling `curl http://localhost:5106/diagnostics` and confirming it returns `200 OK` with the expected JSON payload.
