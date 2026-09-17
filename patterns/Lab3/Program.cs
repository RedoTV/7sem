// Lab3: Builder — собираем сложный объект по частям,
// вместо конструктора с кучей параметров

Agent agent = new Agent.Builder()
    .SetModel("gpt-5.6 Sol")
    .SetSystemPrompt("Ты полезный ассистент")
    .SetTemperature(0.5)
    .SetMaxTokens(1024)
    .Build();

agent.Print();

// не все шаги обязательны — берутся значения по умолчанию
Agent simple = new Agent.Builder()
    .SetModel("gpt-5.6 Terra")
    .Build();

Console.WriteLine();
simple.Print();

public class Agent
{
    public string Model;
    public string SystemPrompt;
    public double Temperature;
    public int MaxTokens;
    public bool Streaming;

    public Agent(string model, string systemPrompt, double temperature, int maxTokens, bool streaming)
    {
        Model = model;
        SystemPrompt = systemPrompt;
        Temperature = temperature;
        MaxTokens = maxTokens;
        Streaming = streaming;
    }

    public void Print()
    {
        Console.WriteLine("Модель:      " + Model);
        Console.WriteLine("Промпт:      " + SystemPrompt);
        Console.WriteLine("Temperature: " + Temperature);
        Console.WriteLine("MaxTokens:   " + MaxTokens);
        Console.WriteLine("Стриминг:    " + (Streaming ? "да" : "нет"));
    }

    public class Builder
    {
        private string model;
        private string systemPrompt = "Ты полезный ассистент";
        private double temperature = 0.7;
        private int maxTokens = 512;
        private bool streaming;

        public Builder SetModel(string m) { model = m; return this; }
        public Builder SetSystemPrompt(string p) { systemPrompt = p; return this; }
        public Builder SetTemperature(double t) { temperature = t; return this; }
        public Builder SetMaxTokens(int tokens) { maxTokens = tokens; return this; }
        public Builder SetStreaming() { streaming = true; return this; }

        public Agent Build()
        {
            if (model == null)
                throw new Exception("Модель не задана");

            return new Agent(model, systemPrompt, temperature, maxTokens, streaming);
        }
    }
}
