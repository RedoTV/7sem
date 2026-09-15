namespace Lab5;

public sealed record LlmRequest(string Model, string Prompt, double Temperature = 0.7);

public sealed record LlmResponse(string Model, string Text, int TokensUsed);

public interface ILlmService
{
    LlmResponse Complete(LlmRequest request);
}

// «удалённый» сервис: каждый вызов — задержка и деньги за токены
public sealed class RemoteLlmService : ILlmService
{
    private static int _networkCalls;
    public static int NetworkCalls => Volatile.Read(ref _networkCalls);

    public LlmResponse Complete(LlmRequest request)
    {
        int call = Interlocked.Increment(ref _networkCalls);
        int latencyMs = Random.Shared.Next(700, 1400);

        Console.WriteLine($"      [СЕТЬ #{call}] запрос к {request.Model}, задержка ~{latencyMs} мс…");
        Thread.Sleep(latencyMs);

        return new LlmResponse(request.Model,
            $"«{request.Prompt}» — вот развёрнутый ответ от {request.Model}.",
            30 + request.Prompt.Length / 2);
    }
}
