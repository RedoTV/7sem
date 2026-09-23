// Lab9: Lazy — универсальный потокобезопасный контейнер MyLazy<T>.
// Значение создаётся фабрикой ровно один раз — при первом обращении.

// ---------- 1. Базовый сценарий: дорогой объект ----------
Console.WriteLine("=== 1. Базовый сценарий ===");
var model = new MyLazy<NeuralModel>(() => new NeuralModel());

Console.WriteLine("Значение создано? " + model.IsValueCreated);
Console.WriteLine("ToString(): " + model);

NeuralModel first = model.Value;   // здесь происходит загрузка
NeuralModel second = model.Value;  // тот же объект, загрузки больше нет

Console.WriteLine("Это один и тот же объект? " + ReferenceEquals(first, second));
Console.WriteLine("Загрузок модели: " + NeuralModel.LoadCount);
Console.WriteLine();

// ---------- 2. Готовое значение можно передать сразу ----------
Console.WriteLine("=== 2. Уже готовое значение ===");
var ready = new MyLazy<string>("просто строка");
Console.WriteLine("Создано сразу? " + ready.IsValueCreated + ", ToString(): " + ready);
Console.WriteLine();

// ---------- 3. Исключение в фабрике не кешируется ----------
Console.WriteLine("=== 3. Ошибка фабрики ===");
var flaky = new MyLazy<NeuralModel>(() => throw new Exception("файл весов повреждён"));

try
{
    flaky.Get();
}
catch (Exception ex)
{
    Console.WriteLine("Поймали: " + ex.Message);
}
Console.WriteLine("После ошибки создано? " + flaky.IsValueCreated + " (можно повторить попытку)");
Console.WriteLine();

// ---------- 4. Гонка потоков: 8 потоков зовут Get() одновременно ----------
Console.WriteLine("=== 4. Потокобезопасность ===");
var index = new MyLazy<EmbeddingIndex>(() => new EmbeddingIndex());

Barrier start = new Barrier(8);
List<Task> tasks = new List<Task>();
for (int i = 0; i < 8; i++)
{
    tasks.Add(Task.Run(() =>
    {
        start.SignalAndWait(); // все потоки стартуют одновременно
        index.Get();
    }));
}
Task.WaitAll(tasks.ToArray());

Console.WriteLine("Сборок индекса на 8 потоков: " + EmbeddingIndex.BuildCount); // ровно 1

// ================== обобщённый ленивый контейнер ==================

// Double-checked locking: быстрая дорога без блокировки,
// под локом — повторная проверка, чтобы значение создалось ровно один раз.
// Если фабрика бросает исключение — значение не кешируется, следующий Get() пробует снова.
public class MyLazy<T>
{
    private readonly Func<T> _factory = default!;
    private readonly object _sync = new object();
    private T _value = default!;
    private bool _created;

    public MyLazy(Func<T> factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public MyLazy(T precreatedValue)
    {
        _value = precreatedValue;
        _created = true;
    }

    public bool IsValueCreated
    {
        get { lock (_sync) return _created; }
    }

    // синоним Get(), как у System.Lazy<T>.Value
    public T Value
    {
        get { return Get(); }
    }

    public T Get()
    {
        if (Volatile.Read(ref _created))
            return _value;

        lock (_sync)
        {
            if (!_created)
            {
                _value = _factory();
                _created = true;
            }
            return _value;
        }
    }

    public override string ToString()
    {
        return IsValueCreated ? Convert.ToString(_value) : "<значение ещё не создано>";
    }
}

// ================== «дорогие» объекты для демо ==================

public class NeuralModel
{
    public static int LoadCount;

    public NeuralModel()
    {
        int n = Interlocked.Increment(ref LoadCount);
        Console.WriteLine("  [модель] загрузка весов №" + n + "...");
        Thread.Sleep(300);
        Console.WriteLine("  [модель] готова");
    }
}

public class EmbeddingIndex
{
    public static int BuildCount;

    public EmbeddingIndex()
    {
        Interlocked.Increment(ref BuildCount);
        Thread.Sleep(200); // тяжёлое построение индекса
    }
}
