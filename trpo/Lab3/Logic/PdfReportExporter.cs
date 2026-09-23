// Формирование PDF-отчетов. Печать в базу не пишет: получает уже
// выбранные записи и сохраняет документ по указанному пути.

using Lab3.Data;
using Lab3.Model;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Lab3.Logic;

public static class PdfReportExporter
{
    private static bool _licenseConfigured;

    public static void ExportPeriod(string path, IReadOnlyList<Sale> sales, string from, string to)
    {
        EnsureLicense();

        Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(28);
            page.DefaultTextStyle(style => style.FontFamily("DejaVu Sans").FontSize(9));

            page.Header().Column(column =>
            {
                column.Item().Text("Отчет о продажах за период").FontSize(16).Bold();
                column.Item().PaddingTop(2).Text($"Период: {FormatDate(from)} — {FormatDate(to)}");
                column.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Medium);
            });

            page.Content().PaddingTop(10).Column(column =>
            {
                if (sales.Count == 0)
                {
                    column.Item().Text("За выбранный период продаж нет.");
                    return;
                }

                column.Item().Element(container => SalesTable(container, sales));
                column.Item().PaddingTop(10).AlignRight()
                    .Text($"Записей: {sales.Count}     Количество: {sales.Sum(sale => sale.Quantity)}     Итого: {SaleRepository.Total(sales.ToList()):F2} руб.")
                    .Bold();
            });

            PageFooter(page);
        })).GeneratePdf(path);
    }

    public static void ExportSearch(string path, IReadOnlyList<Sale> sales, string query)
    {
        EnsureLicense();

        Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(28);
            page.DefaultTextStyle(style => style.FontFamily("DejaVu Sans").FontSize(9));

            page.Header().Column(column =>
            {
                column.Item().Text("Результаты поиска").FontSize(16).Bold();
                column.Item().PaddingTop(2).Text(string.IsNullOrWhiteSpace(query)
                    ? "Запрос: все записи"
                    : $"Запрос: {query.Trim()}");
                column.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Medium);
            });

            page.Content().PaddingTop(10).Column(column =>
            {
                if (sales.Count == 0)
                {
                    column.Item().Text("По запросу ничего не найдено.");
                    return;
                }

                column.Item().Element(container => SalesTable(container, sales));
                column.Item().PaddingTop(10).AlignRight()
                    .Text($"Найдено: {sales.Count}     Сумма: {SaleRepository.Total(sales.ToList()):F2} руб.")
                    .Bold();
            });

            PageFooter(page);
        })).GeneratePdf(path);
    }

    private static void SalesTable(IContainer container, IReadOnlyList<Sale> sales)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(28);
                columns.ConstantColumn(52);
                columns.ConstantColumn(48);
                columns.ConstantColumn(58);
                columns.RelativeColumn(3);
                columns.RelativeColumn(1.4f);
                columns.ConstantColumn(62);
                columns.ConstantColumn(48);
                columns.ConstantColumn(72);
                columns.ConstantColumn(78);
            });

            table.Header(header =>
            {
                HeaderCell(header.Cell(), "№");
                HeaderCell(header.Cell(), "Магазин");
                HeaderCell(header.Cell(), "Секция");
                HeaderCell(header.Cell(), "Чек");
                HeaderCell(header.Cell(), "Наименование");
                HeaderCell(header.Cell(), "Артикул");
                HeaderCell(header.Cell(), "Цена");
                HeaderCell(header.Cell(), "Кол-во");
                HeaderCell(header.Cell(), "Сумма");
                HeaderCell(header.Cell(), "Дата");
            });

            int row = 0;
            foreach (Sale sale in sales)
            {
                string background = row % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                BodyCell(table.Cell(), sale.Id.ToString(), background);
                BodyCell(table.Cell(), sale.Shop.ToString(), background);
                BodyCell(table.Cell(), sale.Section.ToString(), background);
                BodyCell(table.Cell(), sale.Receipt.ToString(), background);
                BodyCell(table.Cell(), sale.Product, background);
                BodyCell(table.Cell(), sale.Article, background);
                BodyCell(table.Cell(), sale.Price.ToString("F2"), background, right: true);
                BodyCell(table.Cell(), sale.Quantity.ToString(), background, right: true);
                BodyCell(table.Cell(), (sale.Price * sale.Quantity).ToString("F2"), background, right: true);
                BodyCell(table.Cell(), FormatDate(sale.Date), background);
                row++;
            }
        });
    }

    private static void PageFooter(PageDescriptor page)
    {
        page.Footer().Column(column =>
        {
            column.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
            column.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text($"Сформирован: {DateTime.Now:dd.MM.yyyy HH:mm}").FontSize(8);
                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.DefaultTextStyle(style => style.FontSize(8));
                    text.Span("Страница ");
                    text.CurrentPageNumber();
                    text.Span(" из ");
                    text.TotalPages();
                });
            });
        });
    }

    private static void HeaderCell(IContainer container, string text)
    {
        container.Background(Colors.Grey.Lighten2).Border(0.5f).BorderColor(Colors.Grey.Medium)
            .Padding(3).AlignCenter().Text(text).Bold().FontSize(8);
    }

    private static void BodyCell(IContainer container, string text, string background, bool right = false)
    {
        IContainer cell = container.Background(background)
            .BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
            .Padding(3);

        if (right)
            cell.AlignRight().Text(text);
        else
            cell.Text(text);
    }

    private static string FormatDate(string date)
    {
        return DateTime.TryParseExact(date, "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out DateTime parsed)
            ? parsed.ToString("dd.MM.yyyy")
            : date;
    }

    private static void EnsureLicense()
    {
        if (_licenseConfigured)
            return;

        QuestPDF.Settings.License = LicenseType.Community;
        _licenseConfigured = true;
    }
}
