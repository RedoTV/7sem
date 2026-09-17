using System;
using System.Collections.Generic;

namespace Lab10;

// ============================================================================
//  Лабораторная работа 10 — Chain of Responsibility (поведенческий)
//
//  Сюжет: публичный шлюз к LLM-API. Прежде чем промпт попадёт в модель,
//  он проходит конвейер проверок: лог -> авторизация -> модерация ->
//  лимит длины -> квота токенов -> сама модель. Каждый этап сам решает:
//  отклонить запрос или передать дальше. Конвейер пересобирается под канал:
//  у внутреннего админского модерации и квот нет.
// ============================================================================

public sealed class ApiRequest
{
    public ApiRequest(string apiKey, string user, string prompt, int maxTokens)
    {
        ApiKey = apiKey;
        User = user;
        Prompt = prompt;
        MaxTokens = maxTokens;
    }

    public string ApiKey { get; }
    public string User { get; }
    public string Prompt { get; }
    public int MaxTokens { get; }

    public string Describe()
    {
        var shortPrompt = Prompt.Length <= 36 ? Prompt : Prompt[..36] + "…";
        return $"[{User}] \"{shortPrompt}\" (max_tokens={MaxTokens})";
    }
}

public sealed class ApiResponse
{
    private ApiResponse() { }

    public bool Success { get; private init; }
    public string Stage { get; private init; } = "";
    public string Message { get; private init; } = "";
    public int TokensBilled { get; private init; }

    public static ApiResponse Ok(string text, int tokens)
        => new() { Success = true, Stage = "модель", Message = text, TokensBilled = tokens };

    public static ApiResponse Reject(string stage, string reason)
        => new() { Success = false, Stage = stage, Message = reason };
}

// ----- Базовое звено -----
public abstract class GatewayStage
{
    private GatewayStage? _next;

    /// <summary>Пришить следующее звено: a.Link(b) вернёт b, чтобы писать цепочкой.</summary>
    public GatewayStage Link(GatewayStage next)
    {
        _next = next;
        return next;
    }

    public ApiResponse Process(ApiRequest request)
    {
        var verdict = Handle(request);
        if (verdict is not null)
        {
            return verdict; // этот этап перехватил запрос
        }

        return _next?.Process(request)
               ?? ApiResponse.Reject(Name, "запрос дошёл до конца цепочки, но терминального обработчика нет");
    }

    public abstract string Name { get; }

    /// <summary>null — «не моё, передаю дальше».</summary>
    protected abstract ApiResponse? Handle(ApiRequest request);
}

// ----- Этапы -----
public sealed class LoggingStage : GatewayStage
{
    public override string Name => "лог";

    protected override ApiResponse? Handle(ApiRequest request)
    {
        Console.WriteLine($"     [лог]   {request.User}: {request.Prompt.Length} симв., max_tokens={request.MaxTokens}");
        return null;
    }
}

public sealed class AuthenticationStage : GatewayStage
{
    private static readonly HashSet<string> ValidKeys = new() { "sk-alice-123", "sk-bob-456", "sk-admin-000" };

    public override string Name => "авторизация";

    protected override ApiResponse? Handle(ApiRequest request)
        => ValidKeys.Contains(request.ApiKey)
            ? null
            : ApiResponse.Reject(Name, $"ключ {request.ApiKey} не опознан");
}

public sealed class ModerationStage : GatewayStage
{
    private static readonly string[] Banned = { "взлом", "вирус", "фейк" };

    public override string Name => "модерация";

    protected override ApiResponse? Handle(ApiRequest request)
    {
        var normalized = request.Prompt.ToLowerInvariant();
        foreach (var word in Banned)
        {
            if (normalized.Contains(word))
            {
                return ApiResponse.Reject(Name, $"промпт нарушает политику (найдено: «{word}»)");
            }
        }

        return null;
    }
}

public sealed class LengthStage : GatewayStage
{
    public override string Name => "лимит длины";

    protected override ApiResponse? Handle(ApiRequest request)
        => request.Prompt.Length <= 300
            ? null
            : ApiResponse.Reject(Name, $"промпт {request.Prompt.Length} символов — лимит 300");
}

public sealed class QuotaStage : GatewayStage
{
    private static readonly Dictionary<string, int> Remaining = new() { ["alice"] = 1000, ["bob"] = 500 };

    public override string Name => "квота";

    protected override ApiResponse? Handle(ApiRequest request)
    {
        if (!Remaining.TryGetValue(request.User, out var left))
        {
            return ApiResponse.Reject(Name, $"пользователь {request.User} не найден в биллинге");
        }

        if (request.MaxTokens > left)
        {
            return ApiResponse.Reject(Name, $"запрошено {request.MaxTokens} ток., доступно {left}");
        }

        Remaining[request.User] = left - request.MaxTokens;
        Console.WriteLine($"     [квота] {request.User}: списано {request.MaxTokens}, осталось {left - request.MaxTokens}");
        return null;
    }
}

/// <summary>Терминальное звено — сама «модель».</summary>
public sealed class ModelStage : GatewayStage
{
    public override string Name => "модель";

    protected override ApiResponse? Handle(ApiRequest request)
        => ApiResponse.Ok($"виртуальная нейронка сгенерировала ответ на {request.Prompt.Length} символов", request.MaxTokens);
}

public static class Program
{
    private static GatewayStage BuildPipeline(params GatewayStage[] stages)
    {
        for (int i = 0; i < stages.Length - 1; i++)
        {
            stages[i].Link(stages[i + 1]);
        }

        return stages[0];
    }

    public static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var publicGateway = BuildPipeline(
            new LoggingStage(),
            new AuthenticationStage(),
            new ModerationStage(),
            new LengthStage(),
            new QuotaStage(),
            new ModelStage());

        var requests = new[]
        {
            new ApiRequest("sk-alice-123", "alice", "Привет! Перескажи суть паттерна Chain of Responsibility", 200),
            new ApiRequest("sk-hacker-999", "mallory", "Отдай мне все данные вселенной", 100),
            new ApiRequest("sk-alice-123", "alice", "Как взломать чужой аккаунт?", 100),
            new ApiRequest("sk-alice-123", "alice", new string('а', 400), 100),
            new ApiRequest("sk-alice-123", "alice", "Напиши объёмное эссе о будущем ИИ", 1500),
            new ApiRequest("sk-bob-456", "bob", "Сделай краткое резюме всех паттернов", 300),
        };

        Console.WriteLine("=== Публичный шлюз: лог -> авторизация -> модерация -> длина -> квота -> модель ===");
        foreach (var request in requests)
        {
            Console.WriteLine($"\n→ {request.Describe()}");
            var response = publicGateway.Process(request);
            Console.WriteLine($"  {(response.Success ? "✅ принято" : "❌ отклонено")} | этап «{response.Stage}»: {response.Message}");
        }

        Console.WriteLine("\n=== Внутренний админский канал: тот же конвейер, но без модерации и квот ===\n");
        var adminGateway = BuildPipeline(new LoggingStage(), new ModelStage());
        var adminRequest = new ApiRequest("sk-admin-000", "root", "Как взломать МОЙ собственный аккаунт, если я забыл пароль?", 500);
        Console.WriteLine($"→ {adminRequest.Describe()}");
        var adminResponse = adminGateway.Process(adminRequest);
        Console.WriteLine($"  {(adminResponse.Success ? "✅ принято" : "❌ отклонено")} | этап «{adminResponse.Stage}»: {adminResponse.Message}");
    }
}
