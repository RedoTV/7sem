// Lab5: Proxy — обёртка с тем же интерфейсом, что и настоящий сервис.
// Покрыты два канонических вида прокси:
//   1) protection proxy — контролирует доступ (проверка API-ключа);
//   2) caching proxy    — кеширует ответы (TTL, потокобезопасность, статистика).

using System.Collections.Concurrent;
using System.Diagnostics;

ILlmService remote = new RemoteLlmService();

// ---------- 1. Protection proxy: доступ только с валидным ключом ----------
Console.WriteLine("=== 1. Protection proxy ===");
ILlmService guarded = new ProtectionProxy(remote, validKey: "secret-123");

try
{
    guarded.Complete(new LlmRequest("gpt-6-sol", "привет", ApiKey: "wrong-key"));
}
catch (UnauthorizedAccessException ex)
{
    Console.WriteLine("Отказано: " + ex.Message);
}

LlmResponse allowed = guarded.Complete(new LlmRequest("gpt-6-sol", "привет", ApiKey: "secret-123"));
Console.WriteLine("Пропущен дальше: " + allowed.Text + "\n");

// ---------- 2. Caching proxy: кеш с TTL и статистикой ----------
Console.WriteLine("=== 2. Caching proxy ===");
var cached = new CachingProxy(remote, ttl: TimeSpan.FromSeconds(1));

var sw = Stopwatch.StartNew();
cached.Complete(new LlmRequest("gpt-6-sol", "Что такое градиентный спуск?"));
Console.WriteLine("   1-й запрос: " + sw.ElapsedMilliseconds + " мс (сеть)");

sw.Restart();
cached.Complete(new LlmRequest("gpt-6-sol", "Что такое градиентный спуск?"));
Console.WriteLine("   2-й запрос: " + sw.ElapsedMilliseconds + " мс (кеш)");

cached.Complete(new LlmRequest("gpt-6-sol", "Что такое backpropagation?"));

Thread.Sleep(1500); // TTL истёк — тот же вопрос снова уходит в сеть
sw.Restart();
cached.Complete(new LlmRequest("gpt-6-sol", "Что такое градиентный спуск?"));
Console.WriteLine("   после TTL:  " + sw.ElapsedMilliseconds + " мс (кеш протух)");

cached.PrintStats();
Console.WriteLine("Реальных вызовов сервиса: " + ((RemoteLlmService)remote).RemoteCalls);

// ================== контракт (Subject) ==================

public interface ILlmService
{
    LlmResponse Complete(LlmRequest request);
}

// record сравнивается по значению всех полей — поэтому годится как ключ кеша
public sealed record LlmRequest(string Model, string Prompt, string ApiKey = "", double Temperature = 0.7);

public sealed record LlmResponse(string Model, string Text, int TokensIn, int TokensOut, decimal CostUsd);

// ================== RealSubject ==================

// настоящий удалённый сервис: дорогой вызов (задержка + деньги)
public sealed class RemoteLlmService : ILlmService
{
    public int RemoteCalls { get; private set; }

    public LlmResponse Complete(LlmRequest request)
    {
        RemoteCalls++;
        Thread.Sleep(Random.Shared.Next(600, 1200));

        int tokensIn = 20 + request.Prompt.Length / 2;
        int tokensOut = 40 + request.Prompt.Length;
        decimal cost = (tokensIn * 0.0025m + tokensOut * 0.01m) / 1000m;

        return new LlmResponse(request.Model,
            "Развёрнутый ответ на «" + request.Prompt + "»",
            tokensIn, tokensOut, cost);
    }
}

// ================== Proxy #1: protection ==================

// пускает запрос дальше только с правильным ключом
public sealed class ProtectionProxy : ILlmService
{
    private readonly ILlmService _target;
    private readonly string _validKey;

    public ProtectionProxy(ILlmService target, string validKey)
    {
        _target = target;
        _validKey = validKey;
    }

    public LlmResponse Complete(LlmRequest request)
    {
        if (request.ApiKey != _validKey)
            throw new UnauthorizedAccessException("Неверный API-ключ: «" + request.ApiKey + "»");

        Console.WriteLine("[protection] ключ верный — пропускаем к сервису");
        return _target.Complete(request);
    }
}

// ================== Proxy #2: caching ==================

public sealed class CachingProxy : ILlmService
{
    private sealed class Entry
    {
        public Entry(LlmResponse response, DateTime expiresAt)
        {
            Response = response;
            ExpiresAt = expiresAt;
        }

        public LlmResponse Response { get; }
        public DateTime ExpiresAt { get; }

        public bool IsFresh
        {
            get { return DateTime.UtcNow < ExpiresAt; }
        }
    }

    private readonly ILlmService _target;
    private readonly TimeSpan _ttl;

    // thread-safe словарь; ключ — LlmRequest (равенство по значению, т.к. record)
    private readonly ConcurrentDictionary<LlmRequest, Entry> _cache = new ConcurrentDictionary<LlmRequest, Entry>();

    public int Hits { get; private set; }
    public int Misses { get; private set; }
    public int TokensSaved { get; private set; }

    public CachingProxy(ILlmService target, TimeSpan? ttl = null)
    {
        _target = target;
        _ttl = ttl ?? TimeSpan.FromSeconds(60);
    }

    public LlmResponse Complete(LlmRequest request)
    {
        if (_cache.TryGetValue(request, out Entry? entry) && entry is not null && entry.IsFresh)
        {
            Hits++;
            TokensSaved += entry.Response.TokensIn + entry.Response.TokensOut;
            Console.WriteLine("[cache] HIT — ответ из кеша");
            return entry.Response;
        }

        Misses++;
        Console.WriteLine("[cache] MISS — идём к настоящему сервису");

        LlmResponse response = _target.Complete(request);
        _cache[request] = new Entry(response, DateTime.UtcNow + _ttl);
        return response;
    }

    public void PrintStats()
    {
        Console.WriteLine("[cache] hit=" + Hits + ", miss=" + Misses + ", сэкономлено токенов: " + TokensSaved);
    }
}
