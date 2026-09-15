// Уровень логики: проверка данных формы. Отдельный модуль, чтобы
// интерфейс и уровень данных не занимались проверкой ввода.

using System.Globalization;
using Lab3.Model;

namespace Lab3.Logic;

public static class SaleValidator
{
    // Проверяет введенные строки и заполняет sale.
    // Возвращает текст ошибки или null, если данные верны.
    public static string? Parse(string shopText, string sectionText, string receiptText,
        string productText, string articleText, string priceText, string quantityText,
        string date, out Sale sale)
    {
        sale = new Sale { Date = date };

        if (!int.TryParse(shopText, out int shop) || shop <= 0)
            return "Номер магазина - целое положительное число";
        sale.Shop = shop;

        if (!int.TryParse(sectionText, out int section) || section <= 0)
            return "Номер секции - целое положительное число";
        sale.Section = section;

        if (!int.TryParse(receiptText, out int receipt) || receipt <= 0)
            return "Номер чека - целое положительное число";
        sale.Receipt = receipt;

        if (string.IsNullOrWhiteSpace(productText) || string.IsNullOrWhiteSpace(articleText))
            return "Заполните наименование и артикул";
        sale.Product = productText.Trim();
        sale.Article = articleText.Trim();

        // цену допускается вводить и с точкой, и с запятой
        if (!double.TryParse(priceText.Replace(',', '.'),
                NumberStyles.Any, CultureInfo.InvariantCulture, out double price) || price < 0)
            return "Цена - неотрицательное число";
        sale.Price = price;

        if (!int.TryParse(quantityText, out int quantity) || quantity <= 0)
            return "Количество - целое положительное число";
        sale.Quantity = quantity;

        return null;
    }
}
