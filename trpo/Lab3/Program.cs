// Лабораторная работа №3, вариант 7. Модульное проектирование.
// БД «Реализованный товар»: номер магазина, номер секции, номер чека,
// наименование товара, артикул, цена, количество, дата продажи.
// Хранение - SQLite (файл sales.db), при первом запуске база создается
// и заполняется восемью записями.
//
// Программа разделена на слои, один класс - один файл:
//   Lab3.Model  - Sale            модель записи;
//   Lab3.Data   - Database        соединение SQLite и выполнение SQL;
//   Lab3.Data   - SaleRepository  операции над записями (CRUD, поиск, период);
//   Lab3.Logic  - SaleValidator   проверка данных формы;
//   Lab3.Gui    - MainWindow      главное окно (записи и вкладки);
//   Lab3.Gui    - ReportsView     вкладка отчетов (поиск, период);
//   Lab3.Gui    - SaleTable       построение таблицы записей;
//   Lab3.Gui    - EditWindow      форма добавления и редактирования.
// Модули обмениваются только данными через параметры и возвращаемые
// значения (сцепление по данным).

using Avalonia;

namespace Lab3;

public static class Program
{
    public static void Main(string[] args)
    {
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .StartWithClassicDesktopLifetime(args);
    }
}
