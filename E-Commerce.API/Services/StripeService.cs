using E_Commerce.API.Models;
using E_Commerce.API.Options;
using Microsoft.Extensions.Options;
using Stripe;

namespace E_Commerce.API.Services;

public class StripeService
{
    private readonly StripeOptions _stripeOptions;

    public StripeService(IOptions<StripeOptions> stripeOptions)
    {
        _stripeOptions = stripeOptions.Value;
    }

    public async Task<PaymentIntent> CreatePaymentIntentAsync(
        decimal amount,
        int orderId,
        string currency = "usd")
    {
        var options = new PaymentIntentCreateOptions
        {
            Amount = (long)(amount * 100),
            Currency = currency,
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true
            },
            Metadata = new Dictionary<string, string>
            {
                { "order_id", orderId.ToString() }
            }
        };

        var service = new PaymentIntentService();
        return await service.CreateAsync(options);
    }

    public Event ConstructWebhookEvent(string json, string signatureHeader)
    {
        return EventUtility.ConstructEvent(json, signatureHeader, _stripeOptions.WebhookSecret);
    }
}
