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

## SMTP configuration

The API reads SMTP credentials from the `Smtp` configuration section or from environment variables. The following keys are supported:

| Setting | Environment variable | Description |
| --- | --- | --- |
| `Smtp:Host` | `SMTP_HOST` | SMTP server host name. |
| `Smtp:Port` | `SMTP_PORT` | SMTP server port (defaults to `587`). |
| `Smtp:UserName` | `SMTP_USERNAME` | Account user name used to authenticate with the SMTP server. |
| `Smtp:Password` | `SMTP_PASSWORD` | Account password or application-specific password. |
| `Smtp:FromEmail` | `EMAIL_FROM` | Sender email address displayed on outgoing messages. |
| `Smtp:FromName` | `EMAIL_FROM_NAME` | Optional sender display name. |
| `Smtp:UseSsl` | `SMTP_USE_SSL` | When `true`, enforces SSL from the start of the connection. |
| `Smtp:UseStartTls` | `SMTP_USE_STARTTLS` | When `true`, upgrades the connection using STARTTLS. |

During development, store sensitive SMTP values with [.NET user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets#secret-manager) (`dotnet user-secrets`) instead of committing them to `appsettings*.json`. In production, configure the equivalent environment variables through your hosting provider. If you use Gmail, generate an application password and supply it via `SMTP_PASSWORD` (or `Smtp:Password`) instead of your main account password.

> ⚠️ Do not commit real API keys, SMTP credentials, or other secrets to the repository. Configuration files such as `appsettings.json` must not contain sensitive values.

## Order email notifications

Two different emails are sent when a checkout is confirmed: one to the customer and one to the shop owner. The owner address can be configured via the `OrderEmails` section or with the `ORDER_EMAIL_OWNER` environment variable. When the value is missing the service falls back to `Smtp:FromEmail`.

| Setting | Environment variable | Description |
| --- | --- | --- |
| `OrderEmails:OwnerEmail` | `ORDER_EMAIL_OWNER` | Address that receives the internal copy of the order confirmation. |

## How to test E2E (local)

1. **Run the API**
   ```bash
   dotnet run --project EtalDeJeux.Api
   ```
   The API listens on <http://localhost:5188> and <https://localhost:7131> by default.

2. **Start the Stripe CLI forwarder**
   ```bash
   stripe listen --forward-to https://localhost:7131/api/webhooks/stripe
   ```
   The CLI prints a webhook signing secret (`whsec_…`). Copy this value and export it for the API:
   ```bash
   export STRIPE_WEBHOOK_SECRET="whsec_xxx"
   ```

3. **Trigger a checkout session**
   - Use the API to create a session. Replace the sample SKU identifiers with real ones from your catalog (for example, via `GET /api/catalog`).
     ```bash
     curl -k -X POST https://localhost:7131/api/checkout/session \
       -H "Content-Type: application/json" \
       -d '{
         "items": [
           { "skuId": "11111111-1111-1111-1111-111111111111", "qty": 1 },
           { "skuId": "22222222-2222-2222-2222-222222222222", "qty": 2 }
         ],
         "email": "player@example.com",
         "successUrl": "http://localhost:5173/success?session_id={CHECKOUT_SESSION_ID}",
         "cancelUrl": "http://localhost:5173/cancel",
         "reservationId": "33333333-3333-3333-3333-333333333333",
         "orderId": "44444444-4444-4444-4444-444444444444"
       }'
     ```
   - Complete the payment with the test card `4242 4242 4242 4242`, any future expiration date, and any CVC.

4. **Observe the outputs**
   - **Stripe CLI**: should display the forwarded `checkout.session.completed` event and confirm a `200` response from the API webhook endpoint.
   - **API logs**: should include entries that the webhook was received and processed without errors.

### Troubleshooting

- **Account id guard**: If checkout session creation fails, confirm the configured `Stripe:ExpectedAccountId`/`STRIPE_ACCOUNT` matches the account for your API keys. When they differ, the API returns `409 Conflict` with “Mauvais compte de clés Stripe”.
- **Diagnostic endpoint**: Verify the API is healthy by calling `curl -k https://localhost:7131/api/checkout/config` and confirming it returns `200 OK` with the expected JSON payload.
