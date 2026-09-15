// Приложение. Порядок запуска: открыть базу (Database), заполнить ее
// при первом запуске (SaleRepository), показать главное окно (MainWindow).

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Lab3.Data;
using Lab3.Gui;

namespace Lab3;

public class App : Application
{
    public override void Initialize()
    {
        RequestedThemeVariant = ThemeVariant.Light;

        Styles.Add(new FluentTheme());
        Styles.Add(new StyleInclude(new Uri("avares://Lab3/"))
        {
            Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml")
        });
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Database.Open();
        SaleRepository.SeedIfEmpty();
        new MainWindow().Show();
    }
}
