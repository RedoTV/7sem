namespace Lab3;

public enum MemoryKind
{
    None,
    BufferWindow,
    Summarizing,
}

// сложный продукт, который собирает билдер; после Build() неизменяемый
public sealed class AiAgent
{
    public string Model { get; }
    public string SystemPrompt { get; }
    public double Temperature { get; }
    public int MaxOutputTokens { get; }
    public IReadOnlyList<string> Tools { get; }
    public MemoryKind Memory { get; }
    public int MemoryWindowSize { get; }
    public int MaxRetries { get; }
    public decimal DailyBudgetUsd { get; }
    public bool StreamResponses { get; }

    // конструктор внутренний: объект собирается только через билдер
    internal AiAgent(Builder b)
    {
        Model = b.Model;
        SystemPrompt = b.SystemPrompt;
        Temperature = b.Temperature;
        MaxOutputTokens = b.MaxOutputTokens;
        Tools = b.Tools.AsReadOnly();
        Memory = b.Memory;
        MemoryWindowSize = b.MemoryWindowSize;
        MaxRetries = b.MaxRetries;
        DailyBudgetUsd = b.DailyBudgetUsd;
        StreamResponses = b.StreamResponses;
    }

    public void Describe()
    {
        Console.WriteLine($"  Модель           : {Model}");
        Console.WriteLine($"  Системный промпт : {(SystemPrompt.Length > 60 ? SystemPrompt[..60] + "…" : SystemPrompt)}");
        Console.WriteLine($"  Temperature      : {Temperature:0.0#}");
        Console.WriteLine($"  MaxOutputTokens  : {MaxOutputTokens}");
        Console.WriteLine($"  Инструменты      : {(Tools.Count > 0 ? string.Join(", ", Tools) : "—")}");
        Console.WriteLine($"  Память           : {Memory}" + (Memory == MemoryKind.BufferWindow ? $" (окно {MemoryWindowSize} сообщ.)" : ""));
        Console.WriteLine($"  Ретраи           : {MaxRetries}");
        Console.WriteLine($"  Дневной бюджет   : ${DailyBudgetUsd:0.00}");
        Console.WriteLine($"  Стриминг         : {(StreamResponses ? "да" : "нет")}");
    }

    public void SimulateRequest(string userMessage)
    {
        Console.WriteLine($"\n  >>> запрос: «{userMessage}», temperature={Temperature:0.0#}");

        if (Memory != MemoryKind.None)
            Console.WriteLine($"  >>> контекст из памяти [{Memory}] подмешан");

        foreach (string tool in Tools)
            Console.WriteLine($"  >>> доступен инструмент: {tool}()");

        Console.WriteLine($"  >>> ответ{(StreamResponses ? " стримится token-by-token" : " придёт целиком")}, " +
                          $"лимит {MaxOutputTokens} токенов, до {MaxRetries} ретраев, бюджет ${DailyBudgetUsd:0.00}/день");
    }

    public sealed class Builder
    {
        internal string Model = "";
        internal string SystemPrompt = "Ты полезный ассистент.";
        internal double Temperature = 0.7;
        internal int MaxOutputTokens = 512;
        internal List<string> Tools = [];
        internal MemoryKind Memory = MemoryKind.None;
        internal int MemoryWindowSize = 10;
        internal int MaxRetries = 2;
        internal decimal DailyBudgetUsd = 5.00m;
        internal bool StreamResponses;

        public Builder(string model) => Model = model;

        public Builder WithSystemPrompt(string prompt) { SystemPrompt = prompt; return this; }
        public Builder WithTemperature(double t) { Temperature = t; return this; }
        public Builder WithMaxOutputTokens(int tokens) { MaxOutputTokens = tokens; return this; }
        public Builder WithTool(string tool) { Tools.Add(tool); return this; }
        public Builder WithMemory(MemoryKind kind, int windowSize = 10) { Memory = kind; MemoryWindowSize = windowSize; return this; }
        public Builder WithRetries(int retries) { MaxRetries = retries; return this; }
        public Builder WithDailyBudget(decimal usd) { DailyBudgetUsd = usd; return this; }
        public Builder WithStreaming() { StreamResponses = true; return this; }

        // вся валидация здесь: до Build() объекта не существует
        public AiAgent Build()
        {
            if (string.IsNullOrWhiteSpace(Model))
                throw new InvalidOperationException("Модель не задана.");

            if (Temperature is < 0.0 or > 2.0)
                throw new InvalidOperationException($"Temperature={Temperature} вне диапазона [0..2].");

            if (MaxOutputTokens is < 1 or > 128_000)
                throw new InvalidOperationException($"MaxOutputTokens={MaxOutputTokens} недопустим.");

            if (Memory == MemoryKind.BufferWindow && MemoryWindowSize < 1)
                throw new InvalidOperationException("Окно памяти должно быть >= 1.");

            if (Tools.Count != Tools.Distinct(StringComparer.OrdinalIgnoreCase).Count())
                throw new InvalidOperationException("Инструменты не должны дублироваться.");

            if (DailyBudgetUsd < 0)
                throw new InvalidOperationException("Бюджет не может быть отрицательным.");

            return new AiAgent(this);
        }
    }
}
