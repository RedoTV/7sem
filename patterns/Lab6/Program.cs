using System;
using System.Collections.Generic;

namespace Lab6;

public sealed record ChatRequest(string Prompt);

public sealed record ChatResponse(string Text, int Tokens, bool FromCache);

public interface ILlmClient
{
    ChatResponse Complete(ChatRequest request);
}

public sealed class TransientLlmException : Exception 
{
    public TransientLlmException(string message) : base(message) { }
}

public sealed class FakeLlmClient : ILlmClient
{
    private int _calls;

    public ChatResponse Complete(ChatRequest request)
    {
        if (++_calls % 3 == 0) 
        {
            throw new TransientLlmException("сетевой сбой шлюза");
        }

        return new ChatResponse($"виртуальный ответ на \"{request.Prompt}\"",
            Tokens: 10 + request.Prompt.Length / 3, FromCache: false);
    }
}

public sealed class LoggingDecorator : ILlmClient
{
    private readonly ILlmClient _inner;

    public LoggingDecorator(ILlmClient inner) => _inner = inner;

    public ChatResponse Complete(ChatRequest request)
    {
        Console.WriteLine($"    [log] -> \"{request.Prompt}\"");
        var response = _inner.Complete(request);
        Console.WriteLine($"    [log] <- {response.Tokens} ток.");
        return response;
    }
}

public sealed class RetryDecorator : ILlmClient
{
    private readonly ILlmClient _inner;
    private readonly int _maxAttempts;

    public RetryDecorator(ILlmClient inner, int maxAttempts)
    {
        _inner = inner;
        _maxAttempts = maxAttempts;
    }

    public ChatResponse Complete(ChatRequest request)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                return _inner.Complete(request);
            }
            catch (TransientLlmException ex) when (attempt < _maxAttempts)
            {
                Console.WriteLine($"    [retry] попытка {attempt} не удалась: {ex.Message}");
            }
        }
    }
}

public sealed class CacheDecorator : ILlmClient
{
    private readonly ILlmClient _inner;
    private readonly Dictionary<string, ChatResponse> _cache = new();

    public CacheDecorator(ILlmClient inner) => _inner = inner;

    public ChatResponse Complete(ChatRequest request)
    {
        if (_cache.TryGetValue(request.Prompt, out var cached))
        {
            return cached with { FromCache = true };
        }

        var response = _inner.Complete(request);
        _cache[request.Prompt] = response;
        return response;
    }
}

public static class Program
{
    public static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        ILlmClient client = new CacheDecorator(
            new RetryDecorator(
                new LoggingDecorator(
                    new FakeLlmClient()),
                maxAttempts: 3));

        Console.WriteLine("Цепочка: Cache -> Retry(3) -> Logging -> FakeLlm\n");

        Ask(client, "Привет, кто ты?");
        Ask(client, "Привет, кто ты?");
        Ask(client, "Расскажи про Decorator");
    }

    private static void Ask(ILlmClient client, string prompt)
    {
        var response = client.Complete(new ChatRequest(prompt));
        var source = response.FromCache ? "из кэша" : "от модели";
        Console.WriteLine($"— \"{prompt}\" -> OK [{source}] {response.Text}\n");
    }
}
