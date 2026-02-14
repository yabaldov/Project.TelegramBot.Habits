using System.Text;
using System.Text.Json;
using HabiBot.Infrastructure.Models.Telegram;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HabiBot.Infrastructure.Services;

/// <summary>
/// Реализация клиента для работы с Telegram Bot API через HttpClient
/// </summary>
public class TelegramApiClient : ITelegramApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TelegramApiClient> _logger;
    private readonly TelegramBotOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    public TelegramApiClient(
        HttpClient httpClient,
        IOptions<TelegramBotOptions> options,
        ILogger<TelegramApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;

        // Настройка JSON сериализации
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        // Настройка базового URL для Telegram API
        _httpClient.BaseAddress = new Uri($"https://api.telegram.org/bot{_options.BotToken}/");
    }

    public async Task<Message> SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Отправка сообщения в чат {ChatId}: {Text}", request.ChatId, request.Text);

        var response = await PostAsync<Message>("sendMessage", request, cancellationToken);
        
        _logger.LogInformation("Сообщение успешно отправлено в чат {ChatId}", request.ChatId);
        
        return response;
    }

    public async Task<Update[]> GetUpdatesAsync(GetUpdatesRequest? request = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Получение обновлений с offset {Offset}", request?.Offset);

        var updates = await PostAsync<Update[]>("getUpdates", request ?? new GetUpdatesRequest(), cancellationToken);
        
        _logger.LogDebug("Получено {Count} обновлений", updates.Length);
        
        return updates;
    }

    public async Task<TelegramUser> GetMeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Получение информации о боте");

        var botInfo = await GetAsync<TelegramUser>("getMe", cancellationToken);
        
        _logger.LogInformation("Информация о боте получена: {Username}", botInfo.Username);
        
        return botInfo;
    }

    private async Task<T> PostAsync<T>(string method, object request, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var httpResponse = await _httpClient.PostAsync(method, content, cancellationToken);
        var responseBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

        if (!httpResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Ошибка при вызове метода {Method}: {StatusCode} - {Response}", 
                method, httpResponse.StatusCode, responseBody);
            throw new HttpRequestException($"Telegram API error: {httpResponse.StatusCode}");
        }

        var telegramResponse = JsonSerializer.Deserialize<TelegramResponse<T>>(responseBody, _jsonOptions);

        if (telegramResponse == null || !telegramResponse.Ok || telegramResponse.Result == null)
        {
            _logger.LogError("Некорректный ответ от Telegram API: {Response}", responseBody);
            throw new InvalidOperationException($"Invalid Telegram API response: {telegramResponse?.Description}");
        }

        return telegramResponse.Result;
    }

    private async Task<T> GetAsync<T>(string method, CancellationToken cancellationToken)
    {
        var httpResponse = await _httpClient.GetAsync(method, cancellationToken);
        var responseBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

        if (!httpResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Ошибка при вызове метода {Method}: {StatusCode} - {Response}", 
                method, httpResponse.StatusCode, responseBody);
            throw new HttpRequestException($"Telegram API error: {httpResponse.StatusCode}");
        }

        var telegramResponse = JsonSerializer.Deserialize<TelegramResponse<T>>(responseBody, _jsonOptions);

        if (telegramResponse == null || !telegramResponse.Ok || telegramResponse.Result == null)
        {
            _logger.LogError("Некорректный ответ от Telegram API: {Response}", responseBody);
            throw new InvalidOperationException($"Invalid Telegram API response: {telegramResponse?.Description}");
        }

        return telegramResponse.Result;
    }
}

/// <summary>
/// Настройки Telegram бота
/// </summary>
public class TelegramBotOptions
{
    public const string SectionName = "TelegramBot";

    /// <summary>
    /// Токен бота
    /// </summary>
    public string BotToken { get; set; } = string.Empty;
}
