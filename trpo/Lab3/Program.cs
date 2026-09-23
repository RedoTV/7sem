// Лабораторная работа №3, вариант 7.
// БД «Реализованный товар»: магазин, секция, чек, товар, артикул,
// цена, количество, дата продажи. Файл sales.db, при первом запуске
// в нем 8 записей.
//
// Один класс - один файл:
//   Sale            запись
//   Database        файл базы и выполнение SQL
//   SaleRepository  добавление, изменение, удаление, поиск, период
//   SaleValidator   проверка полей формы
//   MainWindow      список и кнопки
//   EditWindow      форма записи, в базу сама не пишет
//   ReportsView     поиск и отчет за период
//   SaleTable       общая таблица для списка и отчетов

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
