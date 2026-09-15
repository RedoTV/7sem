namespace Lab9;

// ленивая инициализация: объект создаётся при первом Get(), потокобезопасно
public sealed class MyLazy<T> where T : class
{
    private readonly Func<T> _factory;
    private readonly object _gate = new();
    private T? _value;
    private bool _created;

    public MyLazy(Func<T> factory) =>
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));

    public bool IsCreated
    {
        get { lock (_gate) return _created; }
    }

    public T Get()
    {
        if (Volatile.Read(ref _created))
            return _value!;

        lock (_gate)
        {
            if (!_created) // вторая проверка уже под блокировкой
            {
                Console.WriteLine($"      [LAZY] первый запрос -> вызываем фабрику {typeof(T).Name}…");
                _value = _factory();
                _created = true;
            }

            return _value!;
        }
    }
}
