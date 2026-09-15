namespace Lab11;

// считает стоимость диалога по тарифам
public sealed class TokenCostVisitor : IChatElementVisitor
{
    private const decimal InputUsdPer1K = 0.0025m;
    private const decimal OutputUsdPer1K = 0.0100m;
    private const decimal ImageUsdPerMp = 0.0030m;

    public int TotalInputTokens { get; private set; }
    public int TotalOutputTokens { get; private set; }
    public decimal TotalUsd { get; private set; }

    public void Visit(UserMessage e)
    {
        int tokens = e.Text.Length / 2;
        TotalInputTokens += tokens;
        AddCost(tokens * InputUsdPer1K / 1000m, $"реплика пользователя ({tokens} ток.)");
    }

    public void Visit(AssistantMessage e)
    {
        TotalInputTokens += e.InputTokens;
        TotalOutputTokens += e.OutputTokens;
        AddCost((e.InputTokens * InputUsdPer1K + e.OutputTokens * OutputUsdPer1K) / 1000m,
                $"ответ {e.Model} (in {e.InputTokens} / out {e.OutputTokens})");
    }

    public void Visit(ToolCall e)
    {
        TotalInputTokens += e.Tokens;
        AddCost(e.Tokens * InputUsdPer1K / 1000m, $"вызов {e.ToolName}() ({e.Tokens} ток.)");
    }

    public void Visit(ImageAttachment e)
    {
        TotalInputTokens += e.Tokens;
        AddCost(e.Megapixels * ImageUsdPerMp, $"картинка {e.FileName} ({e.Width}x{e.Height})");
    }

    private void AddCost(decimal usd, string what)
    {
        TotalUsd += usd;
        Console.WriteLine($"   {what,-52} +${usd:0.000000}");
    }

    public void PrintReport() =>
        Console.WriteLine($"""

            --- Отчёт о стоимости ---
            Входные токены : {TotalInputTokens}
            Выходные токены: {TotalOutputTokens}
            ИТОГО          : ${TotalUsd:0.000000}
            """);
}

// рендерит диалог в читаемую расшифровку
public sealed class TranscriptRendererVisitor : IChatElementVisitor
{
    public void Visit(UserMessage e) =>
        Console.WriteLine($"  [Пользователь] {e.Text}");

    public void Visit(AssistantMessage e) =>
        Console.WriteLine($"  [{e.Model}    ] {e.Text}");

    public void Visit(ToolCall e) =>
        Console.WriteLine($"  [{e.ToolName}  ] вызов: {e.Arguments}" +
                          $"\n  {"",12}-> результат: {e.ResultSummary}");

    public void Visit(ImageAttachment e) =>
        Console.WriteLine($"  [Изображение  ] {e.FileName} ({e.Width}x{e.Height})");
}
