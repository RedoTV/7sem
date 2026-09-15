namespace Lab5;

// прокси: тот же интерфейс, что у сервиса, но сначала смотрит в кеш
public sealed class CachingLlmProxy : ILlmService
{
    private sealed record CacheEntry(LlmResponse Response, DateTime ExpiresAt)
    {
        public bool IsFresh => DateTime.UtcNow < ExpiresAt;
    }

    private readonly ILlmService _inner;
    private readonly TimeSpan _ttl;
    private readonly Dictionary<string, CacheEntry> _cache = new();

    public int Hits { get; private set; }
    public int Misses { get; private set; }
    public int TokensSaved { get; private set; }

    public CachingLlmProxy(ILlmService inner, TimeSpan? ttl = null)
    {
        _inner = inner;
        _ttl = ttl ?? TimeSpan.FromSeconds(30);
    }

    public LlmResponse Complete(LlmRequest request)
    {
        string key = CacheKey(request);

        if (_cache.TryGetValue(key, out var entry) && entry.IsFresh)
        {
            Hits++;
            TokensSaved += entry.Response.TokensUsed;
            Console.WriteLine($"      [ПРОКСИ] HIT «{key}» — ответ из кеша, сеть не трогаем");
            return entry.Response;
        }

        if (entry is not null)
            _cache.Remove(key); // запись просрочена

        Misses++;
        Console.WriteLine($"      [ПРОКСИ] MISS «{key}» — ходим в облако");

        LlmResponse response = _inner.Complete(request);
        _cache[key] = new CacheEntry(response, DateTime.UtcNow + _ttl);
        return response;
    }

    // ключ кеша: тот же вопрос с другой temperature даёт другой ключ
    private static string CacheKey(LlmRequest request)
    {
        var hc = new HashCode();
        hc.Add(request.Model);
        hc.Add(request.Prompt);
        hc.Add(request.Temperature);
        return hc.ToHashCode().ToString("X8");
    }

    public void PrintStats() =>
        Console.WriteLine($"[ПРОКСИ] Статистика: попаданий {Hits}, промахов {Misses}, сэкономлено {TokensSaved} токенов");
}
