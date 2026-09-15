using Lab3;

Console.WriteLine("=== 1. Ручная сборка: код-ревьюер ===");
var reviewer = new AiAgent.Builder("gpt-4o")
    .WithSystemPrompt("Ты строгий ревьюер C#-кода. Указывай на утечки, NRE и нарушения SOLID.")
    .WithTemperature(0.2)
    .WithMaxOutputTokens(1024)
    .WithTool("read_file")
    .WithTool("search_code")
    .WithMemory(MemoryKind.BufferWindow, windowSize: 20)
    .WithRetries(3)
    .WithDailyBudget(2.50m)
    .Build();

reviewer.Describe();
reviewer.SimulateRequest("Проверь PR #42, там рефакторинг репозитория");

Console.WriteLine("\n=== 2. Сборка через заготовки (Director) ===");

var cheap = AgentPresets.CheapSummarizer();
Console.WriteLine("Пресет CheapSummarizer:");
cheap.Describe();

var creative = AgentPresets.Storyteller();
Console.WriteLine("\nПресет Storyteller:");
creative.Describe();
creative.SimulateRequest("Придумай сказку про лямбда-выражения");

Console.WriteLine("\n=== 3. Валидация в Build() ===");
try
{
    _ = new AiAgent.Builder("gpt-4o")
        .WithTemperature(4.2)
        .Build();
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Отказано в сборке: {ex.Message}");
}

// Director: готовые рецепты сборки через тот же билдер
public static class AgentPresets
{
    public static AiAgent CheapSummarizer() =>
        new AiAgent.Builder("gpt-4o-mini")
            .WithSystemPrompt("Сжимай текст до выжимки в 5 пунктов, без воды.")
            .WithTemperature(0.1)
            .WithMaxOutputTokens(256)
            .WithMemory(MemoryKind.Summarizing)
            .WithDailyBudget(0.50m)
            .Build();

    public static AiAgent Storyteller() =>
        new AiAgent.Builder("gpt-4o")
            .WithSystemPrompt("Ты писатель-фантаст. Пиши образно, с интригой в первой фразе.")
            .WithTemperature(1.4)
            .WithMaxOutputTokens(2048)
            .WithTool("image_gen")
            .WithMemory(MemoryKind.BufferWindow, 50)
            .WithRetries(1)
            .WithDailyBudget(10.00m)
            .WithStreaming()
            .Build();
}
