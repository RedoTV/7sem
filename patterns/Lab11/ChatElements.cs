namespace Lab11;

// иерархия элементов закрыта для изменения: новые операции добавляются визиторами
public interface IChatElement
{
    void Accept(IChatElementVisitor visitor);
}

public sealed class UserMessage(string text) : IChatElement
{
    public string Text { get; } = text;

    public void Accept(IChatElementVisitor visitor) => visitor.Visit(this);
}

public sealed class AssistantMessage(
    string model,
    string text,
    int inputTokens,
    int outputTokens) : IChatElement
{
    public string Model { get; } = model;
    public string Text { get; } = text;
    public int InputTokens { get; } = inputTokens;
    public int OutputTokens { get; } = outputTokens;

    public void Accept(IChatElementVisitor visitor) => visitor.Visit(this);
}

public sealed class ToolCall(
    string toolName,
    string arguments,
    string resultSummary,
    int tokens) : IChatElement
{
    public string ToolName { get; } = toolName;
    public string Arguments { get; } = arguments;
    public string ResultSummary { get; } = resultSummary;
    public int Tokens { get; } = tokens;

    public void Accept(IChatElementVisitor visitor) => visitor.Visit(this);
}

public sealed class ImageAttachment(
    string fileName,
    int width,
    int height,
    int tokens) : IChatElement
{
    public string FileName { get; } = fileName;
    public int Width { get; } = width;
    public int Height { get; } = height;
    public int Tokens { get; } = tokens;

    public long Megapixels => (long)Width * Height / 1_000_000;

    public void Accept(IChatElementVisitor visitor) => visitor.Visit(this);
}

public interface IChatElementVisitor
{
    void Visit(UserMessage element);
    void Visit(AssistantMessage element);
    void Visit(ToolCall element);
    void Visit(ImageAttachment element);
}
