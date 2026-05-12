using System.Net;
using System.Text.Json;
using LoanManagement.Business.ExternalServices;

namespace LoanManagement.API.Middleware;

/// Global Exception Handler Middleware
/// AMAÇ:
/// - Tüm unhandled exception'ları yakala
/// - Validasyon hataları için 400 Bad Request döndür
/// - Operasyon hatalarını uygun HTTP status code'larına map et
/// - Bankacılık sektörü standartlarına uygun error response format'ı kullan

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    
    /// Request pipeline'da exception handle et
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception occurred: {ExceptionMessage}", exception.Message);
            await HandleExceptionAsync(context, exception);
        }
    }

    /// Exception'ı uygun HTTP response'a çevir
    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        // Exception türüne göre uygun status code ve mesaj belirle
        var response = exception switch
        {
            // Dış ödeme sağlayıcısı reddi → 402 Payment Required (Stripe-uyumlu).
            PaymentDeclinedException declinedException =>
                new ExceptionResponse
                {
                    StatusCode = HttpStatusCode.PaymentRequired,
                    Message = declinedException.Message,
                    Details = new
                    {
                        status = declinedException.Status.ToString(),
                        declineCode = declinedException.DeclineCode,
                        providerName = declinedException.ProviderName,
                    },
                },

            // Validasyon hatası
            FluentValidation.ValidationException validationException =>
                new ExceptionResponse
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Message = "Giriş verisi validasyonu başarısız.",
                    Details = validationException.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToList()
                        )
                },

            // Argüman hatası (null, out of range vb.)
            ArgumentException or ArgumentNullException =>
                new ExceptionResponse
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Message = "Geçersiz argüman değeri.",
                    Details = new { error = exception.Message }
                },

            // İş kuralı ihlali (InvalidOperationException)
            InvalidOperationException =>
                new ExceptionResponse
                {
                    StatusCode = HttpStatusCode.Conflict,
                    Message = "İş kuralı ihlali: " + exception.Message,
                    Details = new { error = exception.Message }
                },

            // Kaynak bulunamadı
            KeyNotFoundException =>
                new ExceptionResponse
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Message = "İstenen kaynak bulunamadı.",
                    Details = new { error = exception.Message }
                },

            // Yetkisiz erişim (ör. başka müşterinin taksi ödemesi)
            UnauthorizedAccessException =>
                new ExceptionResponse
                {
                    StatusCode = HttpStatusCode.Forbidden,
                    Message = exception.Message,
                    Details = new { error = exception.Message }
                },

            // Varsayılan: İç Sunucu Hatası
            _ => new ExceptionResponse
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Message = "Sunucu taraflı bir hata oluştu. Lütfen daha sonra tekrar deneyin.",
                // Production'da detaylı hata bilgisi döndürme
                Details = new { error = "Internal Server Error" }
            }
        };

        context.Response.StatusCode = (int)response.StatusCode;
        return context.Response.WriteAsJsonAsync(response);
    }
}


public class ExceptionResponse
{
    public HttpStatusCode StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Details { get; set; }
}
