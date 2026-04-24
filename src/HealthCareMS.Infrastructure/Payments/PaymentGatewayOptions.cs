namespace HealthCareMS.Infrastructure.Payments;

public sealed class PaymentGatewayOptions
{
    public const string SectionName = "Payments";

    public string Currency { get; init; } = "PKR";

    public GatewayOptions JazzCash { get; init; } = new()
    {
        DisplayName = "JazzCash",
        CheckoutBaseUrl = "https://sandbox.jazzcash.healthcarems.local/checkout",
        WebhookSecret = "jazzcash-sandbox-secret"
    };

    public GatewayOptions EasyPaisa { get; init; } = new()
    {
        DisplayName = "EasyPaisa",
        CheckoutBaseUrl = "https://sandbox.easypaisa.healthcarems.local/checkout",
        WebhookSecret = "easypaisa-sandbox-secret"
    };

    public GatewayOptions Stripe { get; init; } = new()
    {
        DisplayName = "Stripe",
        CheckoutBaseUrl = "https://sandbox.stripe.healthcarems.local/checkout",
        WebhookSecret = "stripe-sandbox-secret"
    };

    public sealed class GatewayOptions
    {
        public bool Enabled { get; init; } = true;

        public string DisplayName { get; init; } = string.Empty;

        public string CheckoutBaseUrl { get; init; } = string.Empty;

        public string WebhookSecret { get; init; } = string.Empty;
    }
}
