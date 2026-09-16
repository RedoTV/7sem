// Lab9: Lazy — объект создаётся не сразу, а при первом обращении Get()

LazyHolder holder = new LazyHolder();
Console.WriteLine("Объект создан? " + holder.IsCreated); // false — ничего не создавали

Console.WriteLine(holder.GetValue()); // первый вызов -> создаём
Console.WriteLine(holder.GetValue()); // второй -> тот же объект

Console.WriteLine("Объект создан? " + holder.IsCreated); // true

// универсальный ленивый класс
public class MyLazy<T> where T : class
{
    private Func<T> factory;
    private T value;
    private bool created;

    public MyLazy(Func<T> factory)
    {
        this.factory = factory;
    }

    public bool IsCreated
    {
        get { return created; }
    }

    public T Get()
    {
        if (!created)
        {
            Console.WriteLine("(первый вызов — создаём объект)");
            value = factory();
            created = true;
        }
        return value;
    }
}

// «дорогой» объект — имитация загрузки нейросети
public class NeuralModel
{
    public NeuralModel()
    {
        Console.WriteLine("Загружаем веса модели... (долго)");
        Thread.Sleep(500);
    }

    public string Infer(string prompt)
    {
        return "Ответ модели на: " + prompt;
    }
}

// хранит ленивую модель, ничего не создаёт до первого Get()
public class LazyHolder
{
    public MyLazy<NeuralModel> lazy = new MyLazy<NeuralModel>(CreateModel);

    public bool IsCreated
    {
        get { return lazy.IsCreated; }
    }

    public string GetValue()
    {
        NeuralModel model = lazy.Get();
        return model.Infer("привет");
    }

    private static NeuralModel CreateModel()
    {
        return new NeuralModel();
    }
}
