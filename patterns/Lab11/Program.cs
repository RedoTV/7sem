using Lab11;

List<IChatElement> chat =
[
    new UserMessage("Найди в коде все вызовы client.PostAsync и посчитай их"),
    new AssistantMessage("gpt-4o", "Сейчас посмотрю через поиск по коду.", 180, 24),
    new ToolCall("search_code", "{\"pattern\": \"client.PostAsync\"}", "найдено 7 совпадений в 3 файлах", 450),
    new AssistantMessage("gpt-4o", "Вызовов PostAsync: 7. Из них 5 без таймаута — риск зависания.", 620, 95),
    new ImageAttachment("architecture.png", 1920, 1080, 1440),
    new UserMessage("А теперь объясни, что происходит на диаграмме"),
    new AssistantMessage("gpt-4o", "Диаграмма показывает поток авторизации через OAuth2…", 2100, 380),
];

Console.WriteLine("=== Визитор 1: расшифровка диалога ===");
var renderer = new TranscriptRendererVisitor();
foreach (IChatElement element in chat)
    element.Accept(renderer);

Console.WriteLine("\n=== Визитор 2: стоимость по тарифам ===");
var cost = new TokenCostVisitor();
foreach (IChatElement element in chat)
    element.Accept(cost);
cost.PrintReport();

Console.WriteLine("\n=== Визитор 3: экспорт в JSON (иерархию не меняли) ===");
var json = new JsonExportVisitor();
foreach (IChatElement element in chat)
    element.Accept(json);
Console.WriteLine(json.ToJson());

// третий визитор — добавлен без правки классов элементов
public sealed class JsonExportVisitor : IChatElementVisitor
{
    private readonly List<string> _lines = [];

    public void Visit(UserMessage e) => Add("user", e.Text, e.Text.Length / 2);

    public void Visit(AssistantMessage e) => Add(e.Model, e.Text, e.InputTokens + e.OutputTokens);

    public void Visit(ToolCall e) => Add("tool", $"{e.ToolName}({e.Arguments})", e.Tokens);

    public void Visit(ImageAttachment e) => Add("image", e.FileName, e.Tokens);

    private void Add(string role, string content, int tokens) =>
        _lines.Add($"    {{ \"role\": \"{role}\", \"tokens\": {tokens}, \"content\": \"{content}\" }}");

    public string ToJson() => "[\n" + string.Join(",\n", _lines) + "\n]";
}
