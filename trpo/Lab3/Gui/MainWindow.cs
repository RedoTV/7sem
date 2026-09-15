// Главное окно: вкладка «Записи» с таблицей и кнопками добавления,
// изменения и удаления записей, вкладка «Отчеты» вынесена в ReportsView.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Lab3.Data;
using Lab3.Model;

namespace Lab3.Gui;

public class MainWindow : Window
{
    private readonly DataGrid _recordsGrid = SaleTable.Create();
    private readonly TextBlock _recordsInfo = new()
    {
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(12, 0, 0, 0)
    };

    public MainWindow()
    {
        Title = "БД «Реализованный товар»";
        Width = 1040;
        Height = 640;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        var recordsTab = new Grid { RowDefinitions = new RowDefinitions("Auto,*"), Margin = new Thickness(8) };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 0, 0, 8)
        };

        var addButton = new Button { Content = "Добавить" };
        addButton.Click += OnAdd;
        var editButton = new Button { Content = "Изменить" };
        editButton.Click += OnEdit;
        var deleteButton = new Button { Content = "Удалить" };
        deleteButton.Click += OnDelete;

        buttons.Children.Add(addButton);
        buttons.Children.Add(editButton);
        buttons.Children.Add(deleteButton);
        buttons.Children.Add(_recordsInfo);

        Grid.SetRow(buttons, 0);
        Grid.SetRow(_recordsGrid, 1);
        recordsTab.Children.Add(buttons);
        recordsTab.Children.Add(_recordsGrid);

        var tabs = new TabControl();
        tabs.Items.Add(new TabItem { Header = "Записи", Content = recordsTab });
        tabs.Items.Add(new TabItem { Header = "Отчеты", Content = new ReportsView() });

        Content = tabs;
        RefreshRecords();
    }

    private void RefreshRecords()
    {
        _recordsGrid.ItemsSource = SaleRepository.GetAll();
    }

    private void OnAdd(object? sender, RoutedEventArgs e)
    {
        OpenEditor(null);
    }

    private void OnEdit(object? sender, RoutedEventArgs e)
    {
        if (_recordsGrid.SelectedItem is not Sale selected)
        {
            ShowInfo("Выберите запись для изменения.");
            return;
        }

        OpenEditor(selected);
    }

    private void OnDelete(object? sender, RoutedEventArgs e)
    {
        if (_recordsGrid.SelectedItem is not Sale selected)
        {
            ShowInfo("Выберите запись для удаления.");
            return;
        }

        SaleRepository.Delete(selected.Id);
        RefreshRecords();
        ShowInfo($"Запись №{selected.Id} удалена.");
    }

    private async void OpenEditor(Sale? sale)
    {
        await new EditWindow(sale).ShowDialog<Sale>(this);
        RefreshRecords();
    }

    private void ShowInfo(string text)
    {
        _recordsInfo.Text = text;
    }
}
