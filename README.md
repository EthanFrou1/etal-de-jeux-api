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
