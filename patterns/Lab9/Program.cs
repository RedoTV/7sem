using Lab9;

Console.WriteLine("=== 1. Хаб моделей создаётся мгновенно, ничего не грузится ===");
var hub = new ModelHub();
Console.WriteLine($"   Модель загружена? {hub.GptModel.IsCreated}\n");

Console.WriteLine("=== 2. Первый запрос — модель грузится (один раз) ===");
string answer = hub.GptModel.Get().Infer("Что такое паттерн Singleton?");
Console.WriteLine($"   Ответ модели: {answer}");
Console.WriteLine($"   Модель загружена? {hub.GptModel.IsCreated}\n");

Console.WriteLine("=== 3. Второй и третий запросы — грузиться незачем ===");
hub.GptModel.Get().Infer("А чем он плох?");
Console.WriteLine($"   Загрузок модели за всё время: {NeuralModel.LoadCount}\n");

Console.WriteLine("=== 4. Многопоточный тест: 8 потоков одновременно ===");
var embeddings = new MyLazy<EmbeddingIndex>(() => new EmbeddingIndex());

var barrier = new Barrier(8); // все стартуют одновременно
var tasks = Enumerable.Range(1, 8).Select(_ => Task.Run(() =>
{
    barrier.SignalAndWait();
    embeddings.Get().Search("запрос-заглушка");
})).ToArray();

Task.WaitAll(tasks);
Console.WriteLine($"   Индекс собран? {embeddings.IsCreated}");
Console.WriteLine($"   Сборок индекса: {EmbeddingIndex.BuildCount} (двойная проверка дала 1)");
Console.WriteLine($"   Поисковых вызовов: {EmbeddingIndex.SearchCount}");

// «дорогие» объекты, которые ленимся создавать
public sealed class NeuralModel
{
    public static int LoadCount;

    public NeuralModel()
    {
        Interlocked.Increment(ref LoadCount);
        Console.WriteLine("      [МОДЕЛЬ] загружаю веса с диска… (это долго!)");
        Thread.Sleep(600);
        Console.WriteLine("      [МОДЕЛЬ] веса в памяти, готова к инференсу");
    }

    public string Infer(string prompt)
    {
        Console.WriteLine($"      [МОДЕЛЬ] инференс: «{prompt}»");
        return $"думаю над «{prompt}»… думаю… 42!";
    }
}

public sealed class EmbeddingIndex
{
    public static int BuildCount;
    public static int SearchCount;

    public EmbeddingIndex()
    {
        Interlocked.Increment(ref BuildCount);
        Console.WriteLine("      [ИНДЕКС] строю эмбеддинги 1 000 000 документов…");
        Thread.Sleep(300);
        Console.WriteLine("      [ИНДЕКС] готов");
    }

    public void Search(string query) => Interlocked.Increment(ref SearchCount);
}

public sealed class ModelHub
{
    public MyLazy<NeuralModel> GptModel { get; } = new(() => new NeuralModel());
}
