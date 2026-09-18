using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using HardwareTracker.Data;
using HardwareTracker.Services;
using HardwareTracker.ViewModels;
using HardwareTracker.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HardwareTracker;

/// <summary>
/// Root application lifetime manager orchestrating dependency injection, EF Core migrations, and window composition.
/// </summary>
public partial class App : Application
{
    public IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Suppress default Avalonia DataAnnotations validator to avoid dual validation errors with CommunityToolkit's ObservableValidator.
        DisableAvaloniaDataAnnotationValidation();

        var collection = new ServiceCollection();

        collection.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlite("Data Source=hardware_tracker.db"));

        collection.AddSingleton<IExportService, ExportService>();
        collection.AddSingleton<IAssetService, AssetService>();
        collection.AddSingleton<IDialogService, DialogService>();
        collection.AddTransient<MainWindowViewModel>();
        collection.AddTransient<MainWindow>();

        Services = collection.BuildServiceProvider();

        // Apply pending database schema migrations before UI presentation to ensure SQLite persistence readiness.
        using (var scope = Services.CreateScope())
        {
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            using var context = factory.CreateDbContext();
            context.Database.Migrate();
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Gracefully dispose DI container singletons upon application termination.
            desktop.Exit += (_, _) =>
            {
                if (Services is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            };

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.DataContext = Services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Removes Avalonia's built-in DataAnnotations validator plugin to prevent duplicate validation triggers
    /// when ViewModels inherit from CommunityToolkit.Mvvm's ObservableValidator.
    /// </summary>
    private static void DisableAvaloniaDataAnnotationValidation()
    {
        var dataAnnotationPlugins = BindingPlugins.DataValidators
            .OfType<DataAnnotationsValidationPlugin>()
            .ToArray();

        foreach (var plugin in dataAnnotationPlugins)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}