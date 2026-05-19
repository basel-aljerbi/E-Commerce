using System.Security.Claims;
using E_Commerce.API.Data;
using E_Commerce.API.Dtos;
using E_Commerce.API.Infrastructure;
using E_Commerce.API.Models;
using E_Commerce.API.Models.Enums;
using E_Commerce.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace E_Commerce.API.Endpoints;

public static class PaymentsEndpoints
{
    public static void MapPaymentsEndpoints(this WebApplication app)
    {
        // Protected payment endpoints (require auth)
        var authorizedGroup = app.MapGroup("/payments").RequireAuthorization();

        authorizedGroup.MapPost("/create-intent", async (
            PaymentRequestDto dto,
            ECommerceContext dbContext,
            StripeService stripeService,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var userId = int.Parse(
                httpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var order = await dbContext.Orders
                .FirstOrDefaultAsync(o => o.Id == dto.OrderId && o.UserId == userId, ct);

            if (order is null)
                return ApiResult.NotFound("Order not found");

            if (order.Status == OrderStatus.Paid)
                return ApiResult.BadRequest("Order is already paid");

            if (order.Status == OrderStatus.Cancelled)
                return ApiResult.BadRequest("Order is cancelled");

            var paymentIntent = await stripeService.CreatePaymentIntentAsync(
                order.TotalAmount,
                order.Id);

            var payment = new Payment
            {
                OrderId = order.Id,
                StripePaymentIntentId = paymentIntent.Id,
                Amount = order.TotalAmount,
                Currency = "usd",
                Status = paymentIntent.Status
            };

            dbContext.Payments.Add(payment);

            if (order.Status == OrderStatus.Pending)
            {
                var previousStatus = order.Status;
                order.Status = OrderStatus.PaymentProcessing;
                order.UpdatedAt = DateTime.UtcNow;

                dbContext.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    OrderId = order.Id,
                    FromStatus = previousStatus,
                    ToStatus = OrderStatus.PaymentProcessing,
                    Reason = "Payment initiated",
                    ChangedAt = DateTime.UtcNow
                });
            }

            await dbContext.SaveChangesAsync(ct);

            return ApiResult.Success(new PaymentResponseDto(
                paymentIntent.ClientSecret,
                paymentIntent.Status,
                "Payment intent created"));
        });

        // Stripe webhook - intentionally NOT behind RequireAuthorization
        // Stripe signs requests with webhook secret; we verify the signature
        app.MapPost("/payments/webhook", async (
            HttpContext context,
            StripeService stripeService,
            ECommerceContext dbContext,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("PaymentsWebhook");
            var json = await new StreamReader(context.Request.Body)
                .ReadToEndAsync(ct);

            var signatureHeader = context.Request.Headers["Stripe-Signature"].FirstOrDefault();
            if (string.IsNullOrEmpty(signatureHeader))
            {
                return Results.BadRequest(new { error = "Missing signature header" });
            }

            try
            {
                var stripeEvent = stripeService.ConstructWebhookEvent(
                    json,
                    signatureHeader);

                switch (stripeEvent.Type)
                {
                    case EventTypes.PaymentIntentSucceeded:
                        await HandlePaymentIntentSucceeded(stripeEvent, dbContext, logger, ct);
                        break;

                    case EventTypes.PaymentIntentPaymentFailed:
                        await HandlePaymentIntentFailed(stripeEvent, dbContext, logger, ct);
                        break;
                }

                return Results.Ok(new { received = true });
            }
            catch (StripeException ex)
            {
                logger.LogWarning(ex, "Stripe webhook signature verification failed");
                return Results.BadRequest(new { error = "Invalid signature" });
            }
        }).AllowAnonymous();
    }

    private static async Task HandlePaymentIntentSucceeded(
        Event stripeEvent,
        ECommerceContext dbContext,
        ILogger logger,
        CancellationToken ct)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent is null)
        {
            logger.LogWarning("Webhook: PaymentIntentSucceeded but object is null");
            return;
        }

        if (!paymentIntent.Metadata.TryGetValue("order_id", out var orderIdStr))
        {
            logger.LogWarning("Webhook: PaymentIntent {Id} has no order_id metadata", paymentIntent.Id);
            return;
        }

        if (!int.TryParse(orderIdStr, out var orderId))
        {
            logger.LogWarning("Webhook: Invalid order_id in metadata: {OrderId}", orderIdStr);
            return;
        }

        var existingPayment = await dbContext.Payments
            .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntent.Id, ct);

        if (existingPayment is not null && existingPayment.Status == "succeeded")
        {
            logger.LogInformation("Webhook: Payment {PaymentIntentId} already processed", paymentIntent.Id);
            return;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

        try
        {
            var order = await dbContext.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId, ct);

            if (order is null)
            {
                logger.LogWarning("Webhook: Order {OrderId} not found for PaymentIntent {Id}",
                    orderId, paymentIntent.Id);
                return;
            }

            var previousStatus = order.Status;
            order.Status = OrderStatus.Paid;
            order.UpdatedAt = DateTime.UtcNow;

            dbContext.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                FromStatus = previousStatus,
                ToStatus = OrderStatus.Paid,
                Reason = "Payment succeeded",
                ChangedAt = DateTime.UtcNow
            });

            if (existingPayment is null)
            {
                dbContext.Payments.Add(new Payment
                {
                    OrderId = orderId,
                    StripePaymentIntentId = paymentIntent.Id,
                    Amount = (decimal)paymentIntent.Amount / 100,
                    Currency = paymentIntent.Currency,
                    Status = "succeeded",
                    CompletedAt = DateTime.UtcNow
                });
            }
            else
            {
                existingPayment.Status = "succeeded";
                existingPayment.CompletedAt = DateTime.UtcNow;
            }

            await dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            logger.LogInformation(
                "Webhook: Order {OrderId} marked as paid. PaymentIntent: {PaymentIntentId}",
                orderId, paymentIntent.Id);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private static async Task HandlePaymentIntentFailed(
        Event stripeEvent,
        ECommerceContext dbContext,
        ILogger logger,
        CancellationToken ct)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent is null) return;

        if (!paymentIntent.Metadata.TryGetValue("order_id", out var orderIdStr)) return;
        if (!int.TryParse(orderIdStr, out var orderId)) return;

        var existingPayment = await dbContext.Payments
            .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntent.Id, ct);

        if (existingPayment is not null)
        {
            existingPayment.Status = "failed";
            await dbContext.SaveChangesAsync(ct);
        }

        logger.LogWarning("Payment failed for PaymentIntent {PaymentIntentId}, Order {OrderId}",
            paymentIntent.Id, orderId);
    }
}
