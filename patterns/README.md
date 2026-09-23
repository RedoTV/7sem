# Лабораторные работы по паттернам проектирования

Тематика примеров — нейросети и LLM-сервисы. Нечётные лабы: 1, 3, 5, 9, 11 — на C#, лаба 7 — на C++.

| Лаба | Паттерн | Пример | Язык |
|------|---------|--------|------|
| Lab1  | Factory  | фабрика моделей: облачная / локальная / заглушка | C# |
| Lab3  | Builder  | пошаговая сборка конфигурации ИИ-агента | C# |
| Lab5  | Proxy    | protection- и caching-прокси над удалённым LLM (TTL, статистика) | C# |
| Lab7  | Observer | обобщённый каркас Subject<T>/Observer<T> (шаблоны C++), обучение нейросети + биржевой фид | C++ |
| Lab9  | Lazy     | потокобезопасный MyLazy<T>: double-checked locking, гонка 8 потоков | C# |
| Lab11 | Visitor  | обобщённый IVisitable<T> + три операции: расшифровка, стоимость, JSON | C# |

Каждая лаба — одна папка с одним файлом кода (`Program.cs` / `main.cpp`).

## Запуск

C# (нужен .NET SDK):
```bash
dotnet run --project Lab1
dotnet run --project Lab3
dotnet run --project Lab5
dotnet run --project Lab9
dotnet run --project Lab11
```

C++ (нужен g++):
```bash
cd Lab7
g++ -std=c++17 -o lab7 main.cpp
./lab7
```
