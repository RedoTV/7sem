namespace Lab1;

// клиент просит модель по имени провайдера и не знает конкретных классов
public static class ChatModelFactory
{
    private static readonly Dictionary<string, Func<IChatModel>> Registry = new(StringComparer.OrdinalIgnoreCase)
    {
        ["cloud"] = () => new CloudGptModel(),
        ["local"] = () => new LocalLlamaModel(),
        ["mock"]  = () => new MockEchoModel(),
    };

    public static IChatModel Create(string provider)
    {
        if (!Registry.TryGetValue(provider, out var creator))
            throw new ArgumentException(
                $"Неизвестный провайдер «{provider}». Доступны: {string.Join(", ", Registry.Keys)}");

        return creator();
    }

    // новый провайдер добавляется без правки фабрики
    public static void Register(string provider, Func<IChatModel> creator) => Registry[provider] = creator;
}
