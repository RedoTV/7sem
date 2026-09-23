using System;
using System.Collections.Generic;

namespace Lab6;

public sealed record ChatResponse(string Text, int Tokens, bool FromCache);

public interface ILlmClient
{
    ChatResponse Complete(string prompt);
}

public sealed class TransientLlmException : Exception
{
    public TransientLlmException(string message) : base(message) { }
}

public sealed class FakeLlmClient : ILlmClient
{
    private int _calls;

    public ChatResponse Complete(string prompt)
    {
        if (++_calls % 2 == 0)
        {
            throw new TransientLlmException("сетевой сбой шлюза");
        }

        return new ChatResponse($"виртуальный ответ на \"{prompt}\"",
            Tokens: 10 + prompt.Length / 3, FromCache: false);
    }
}

public sealed class LoggingDecorator : ILlmClient
{
    private readonly ILlmClient _inner;

    public LoggingDecorator(ILlmClient inner) => _inner = inner;

    public ChatResponse Complete(string prompt)
    {
        Console.WriteLine($"    [log] -> \"{prompt}\"");
        var response = _inner.Complete(prompt);
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

    public ChatResponse Complete(string prompt)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                return _inner.Complete(prompt);
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

    public ChatResponse Complete(string prompt)
    {
        if (_cache.TryGetValue(prompt, out var cached))
        {
            return cached with { FromCache = true };
        }

        var response = _inner.Complete(prompt);
        _cache[prompt] = response;
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
        var response = client.Complete(prompt);
        var source = response.FromCache ? "из кэша" : "от модели";
        Console.WriteLine($"— \"{prompt}\" -> OK [{source}] {response.Text}\n");
    }
}
