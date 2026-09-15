namespace Lab1;

public sealed record ChatRequest(string Prompt, int MaxTokens = 256);

public sealed record ChatReply(string Model, string Text, int TokensUsed, decimal CostUsd);

public interface IChatModel
{
    string Name { get; }
    ChatReply Complete(ChatRequest request);
}

// платная облачная модель: задержка, деньги за токены
public sealed class CloudGptModel : IChatModel
{
    private const decimal PricePer1K = 0.0025m;

    public string Name => "cloud-gpt-4o";

    public ChatReply Complete(ChatRequest request)
    {
        Thread.Sleep(Random.Shared.Next(300, 700));

        int tokens = 24 + request.Prompt.Length / 2;
        string text = $"Облачная модель развёрнуто и качественно отвечает на: «{request.Prompt}»";
        return new ChatReply(Name, text, tokens, Math.Round(tokens / 1000m * PricePer1K, 6));
    }
}

// локальная модель на своей видеокарте: бесплатная
public sealed class LocalLlamaModel : IChatModel
{
    public string Name => "local-llama-3-8b";

    public ChatReply Complete(ChatRequest request)
    {
        Thread.Sleep(Random.Shared.Next(100, 250));

        int tokens = 16 + request.Prompt.Length / 3;
        string text = $"Локальная модель отвечает коротко: «{request.Prompt}» — примерно так.";
        return new ChatReply(Name, text, tokens, 0m);
    }
}

// заглушка для тестов
public sealed class MockEchoModel : IChatModel
{
    public string Name => "mock-echo";

    public ChatReply Complete(ChatRequest request) =>
        new(Name, $"[MOCK] {request.Prompt}", request.Prompt.Length, 0m);
}
