namespace LoanManagement.Business.ExternalServices;

/// Dış servis konfigürasyonu kök bölümü (appsettings.json: "ExternalServices").
public sealed class ExternalServicesOptions
{
    public const string SectionName = "ExternalServices";

    public PaymentGatewayOptions PaymentGateway { get; set; } = new();
    public CreditBureauOptions CreditBureau { get; set; } = new();
}

public sealed class PaymentGatewayOptions
{
    public string ProviderName { get; set; } = "Stripe Sandbox";
    public string BaseUrl { get; set; } = "https://api.stripe.com/v1/";
    public string ApiKey { get; set; } = "sk_test_placeholder";
    public string Currency { get; set; } = "TRY";
    public int TimeoutSeconds { get; set; } = 10;
    public bool UseSandboxHandler { get; set; } = true;
    public int RetryCount { get; set; } = 2;
}

public sealed class CreditBureauOptions
{
    public string ProviderName { get; set; } = "Findeks Mock";
    public string BaseUrl { get; set; } = "https://findeks-mock.example.com/api/v1/";
    public string ApiKey { get; set; } = "fk_test_placeholder";
    public int TimeoutSeconds { get; set; } = 8;
    public bool UseSandboxHandler { get; set; } = true;
    public int RetryCount { get; set; } = 2;
}
