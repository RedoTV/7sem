// Вкладка отчетов: поиск записей по наименованию, артикулу или номеру чека
// и отчет о продажах за период с подсчетом количества и суммы.
// Кнопки сохранения PDF стоят отдельными строками, не в ряд с полями.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Lab3.Data;
using Lab3.Logic;
using Lab3.Model;

namespace Lab3.Gui;

public class ReportsView : UserControl
{
    public ReportsView()
    {
        var layout = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto,Auto,*"),
            Margin = new Thickness(8)
        };

        // --- поиск ---
        var searchBox = new TextBox
        {
            Watermark = "наименование, артикул или номер чека",
            Width = 320,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        var searchButton = new Button { Content = "Найти" };
        var searchInfo = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        var searchGrid = SaleTable.Create();
        List<Sale> searchSales = [];
        var searchReady = false;

        searchButton.Click += (sender, e) =>
        {
            searchSales = SaleRepository.Search(searchBox.Text ?? "");
            searchGrid.ItemsSource = searchSales;
            searchReady = true;
            searchInfo.Text = $"Найдено записей: {searchSales.Count}";
        };

        var searchRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        searchRow.Children.Add(searchBox);
        searchRow.Children.Add(searchButton);
        searchRow.Children.Add(searchInfo);

        var searchPdfButton = new Button { Content = "Сохранить поиск в PDF" };
        var searchPdfInfo = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        searchPdfButton.Click += async (sender, e) =>
        {
            if (!searchReady)
            {
                searchPdfInfo.Text = "Сначала выполните поиск.";
                return;
            }

            string? path = await PickPdf(this, $"poisk-{DateTime.Now:yyyy-MM-dd}.pdf");
            if (path == null)
                return;

            PdfReportExporter.ExportSearch(path, searchSales, searchBox.Text ?? "");
            searchPdfInfo.Text = $"PDF сохранен: {Path.GetFileName(path)}";
        };

        var searchPdfRow = ActionRow(searchPdfButton, searchPdfInfo);

        // --- отчет за период ---
        var fromDate = new DatePicker { SelectedDate = DateTimeOffset.Now.AddDays(-60) };
        var toDate = new DatePicker { SelectedDate = DateTimeOffset.Now };
        var periodButton = new Button { Content = "Сформировать" };
        var periodInfo = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        var periodGrid = SaleTable.Create();
        List<Sale> periodSales = [];
        var periodReady = false;

        periodButton.Click += (sender, e) =>
        {
            periodSales = SaleRepository.GetForPeriod(
                fromDate.SelectedDate?.ToString("yyyy-MM-dd") ?? "",
                toDate.SelectedDate?.ToString("yyyy-MM-dd") ?? "");

            periodGrid.ItemsSource = periodSales;
            periodReady = true;
            periodInfo.Text = $"Продаж за период: {periodSales.Count}, сумма: {SaleRepository.Total(periodSales):F2} руб.";
        };

        var periodRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 8, 0, 0)
        };
        periodRow.Children.Add(new TextBlock { Text = "С", VerticalAlignment = VerticalAlignment.Center });
        periodRow.Children.Add(fromDate);
        periodRow.Children.Add(new TextBlock { Text = "по", VerticalAlignment = VerticalAlignment.Center });
        periodRow.Children.Add(toDate);
        periodRow.Children.Add(periodButton);
        periodRow.Children.Add(periodInfo);

        var pdfButton = new Button { Content = "Сохранить отчет в PDF" };
        var pdfInfo = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        pdfButton.Click += async (sender, e) =>
        {
            if (!periodReady)
            {
                pdfInfo.Text = "Сначала сформируйте отчет за период.";
                return;
            }

            string? path = await PickPdf(this, $"otchet-{DateTime.Now:yyyy-MM-dd}.pdf");
            if (path == null)
                return;

            PdfReportExporter.ExportPeriod(path, periodSales,
                fromDate.SelectedDate?.ToString("yyyy-MM-dd") ?? "",
                toDate.SelectedDate?.ToString("yyyy-MM-dd") ?? "");
            pdfInfo.Text = $"PDF сохранен: {Path.GetFileName(path)}";
        };

        var pdfRow = ActionRow(pdfButton, pdfInfo);

        layout.Children.Add(searchRow);
        layout.Children.Add(searchPdfRow);
        layout.Children.Add(searchGrid);
        layout.Children.Add(periodRow);
        layout.Children.Add(pdfRow);
        layout.Children.Add(periodGrid);

        Grid.SetRow(searchRow, 0);
        Grid.SetRow(searchPdfRow, 1);
        Grid.SetRow(searchGrid, 2);
        Grid.SetRow(periodRow, 3);
        Grid.SetRow(pdfRow, 4);
        Grid.SetRow(periodGrid, 5);

        Content = layout;
    }

    private static StackPanel ActionRow(Button button, TextBlock info)
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 6, 0, 6)
        };
        row.Children.Add(button);
        row.Children.Add(info);
        return row;
    }

    private static async Task<string?> PickPdf(Control owner, string suggestedName)
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(owner);
        if (topLevel == null)
            return null;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Сохранить отчет",
            SuggestedFileName = suggestedName,
            DefaultExtension = "pdf",
            FileTypeChoices = [new FilePickerFileType("PDF") { Patterns = ["*.pdf"] }]
        });

        return file?.TryGetLocalPath();
    }
}
