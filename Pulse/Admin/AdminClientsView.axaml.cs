using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Npgsql;
using Pulse.Core;

namespace Pulse.User;

public partial class AdminClientsView : UserControl
{
    private readonly MainWindow? _mainWindow;
    private readonly SessionData _session;

    public AdminClientsView()
    {
        InitializeComponent();
        _session = new SessionData();
    }

    public AdminClientsView(MainWindow mainWindow, SessionData session)
    {
        InitializeComponent();

        _mainWindow = mainWindow;
        _session = session;

        LoadClients();
    }

    private async void LoadClients()
    {
        ClientsPanel.Children.Clear();

        try
        {
            await using var connection = Database.GetConnection();
            await connection.OpenAsync();

            const string sql = """
                SELECT
                    z.id_zapisi,
                    z.status_zapisi,
                    z.date_zapisi,
                    client.full_name AS client_name,
                    client.email AS client_email,
                    client.phone AS client_phone,
                    trainer.full_name AS trainer_name,
                    u.name_uslugi,
                    u.type_uslugi,
                    r.date_training,
                    r.time_start,
                    r.time_end
                FROM zapisi z
                JOIN users client ON client.id_user = z.id_user
                JOIN raspisanie r ON r.id_raspisanie = z.id_raspisanie
                JOIN uslugi u ON u.id_uslugi = r.id_uslugi
                LEFT JOIN users trainer ON trainer.id_user = r.id_trainer
                ORDER BY r.date_training DESC, r.time_start DESC, client.full_name
                """;

            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            bool hasRows = false;

            while (await reader.ReadAsync())
            {
                hasRows = true;

                string clientName = reader["client_name"]?.ToString() ?? "Клиент";
                string clientEmail = reader["client_email"]?.ToString() ?? "";
                string clientPhone = reader["client_phone"]?.ToString() ?? "Не указан";
                string trainerName = reader["trainer_name"]?.ToString() ?? "Тренер не указан";
                string serviceName = reader["name_uslugi"]?.ToString() ?? "Занятие";
                string type = reader["type_uslugi"]?.ToString() ?? "";
                string status = reader["status_zapisi"]?.ToString() ?? "";
                DateTime date = Convert.ToDateTime(reader["date_training"]);
                TimeSpan start = (TimeSpan)reader["time_start"];
                TimeSpan end = (TimeSpan)reader["time_end"];
                DateTime zapisDate = Convert.ToDateTime(reader["date_zapisi"]);

                AddClientCard(
                    clientName,
                    clientEmail,
                    clientPhone,
                    trainerName,
                    serviceName,
                    type,
                    date,
                    start,
                    end,
                    zapisDate,
                    status
                );
            }

            if (!hasRows)
                AddEmptyCard();
        }
        catch (Exception ex)
        {
            AddErrorCard(ex.Message);
        }
    }

    private void AddClientCard(
        string clientName,
        string clientEmail,
        string clientPhone,
        string trainerName,
        string serviceName,
        string type,
        DateTime date,
        TimeSpan start,
        TimeSpan end,
        DateTime zapisDate,
        string status)
    {
        IBrush statusColor = status switch
        {
            "active" => Brush.Parse("#27AE60"),
            "cancelled" => Brush.Parse("#E32626"),
            "used" => Brush.Parse("#8A8A8A"),
            _ => Brush.Parse("#6F6F6F")
        };

        var card = new Border
        {
            Background = Brushes.White,
            BorderBrush = Brush.Parse("#E0E0E0"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(26),
            Padding = new Thickness(26)
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,240"),
            ColumnSpacing = 22
        };

        var left = new StackPanel
        {
            Spacing = 8
        };

        left.Children.Add(new TextBlock
        {
            Text = clientName,
            Foreground = Brush.Parse("#141414"),
            FontSize = 30,
            FontWeight = FontWeight.Bold,
            TextWrapping = TextWrapping.Wrap
        });

        left.Children.Add(new TextBlock
        {
            Text = $"Телефон: {clientPhone}",
            Foreground = Brush.Parse("#6F6F6F"),
            FontSize = 21,
            TextWrapping = TextWrapping.Wrap
        });

        left.Children.Add(new TextBlock
        {
            Text = $"Email: {clientEmail}",
            Foreground = Brush.Parse("#6F6F6F"),
            FontSize = 21,
            TextWrapping = TextWrapping.Wrap
        });

        left.Children.Add(new Border
        {
            Height = 1,
            Background = Brush.Parse("#E0E0E0"),
            Margin = new Thickness(0, 8)
        });

        left.Children.Add(new TextBlock
        {
            Text = $"Занятие: {serviceName}",
            Foreground = Brush.Parse("#141414"),
            FontSize = 22,
            FontWeight = FontWeight.Bold,
            TextWrapping = TextWrapping.Wrap
        });

        left.Children.Add(new TextBlock
        {
            Text = GetTypeName(type),
            Foreground = Brush.Parse("#E32626"),
            FontSize = 21,
            FontWeight = FontWeight.Bold,
            TextWrapping = TextWrapping.Wrap
        });

        left.Children.Add(new TextBlock
        {
            Text = $"Тренер: {trainerName}",
            Foreground = Brush.Parse("#6F6F6F"),
            FontSize = 21,
            TextWrapping = TextWrapping.Wrap
        });

        left.Children.Add(new TextBlock
        {
            Text = $"Дата занятия: {date:dd.MM.yyyy}",
            Foreground = Brush.Parse("#6F6F6F"),
            FontSize = 21
        });

        left.Children.Add(new TextBlock
        {
            Text = $"Время: {start:hh\\:mm} — {end:hh\\:mm}",
            Foreground = Brush.Parse("#6F6F6F"),
            FontSize = 21
        });

        left.Children.Add(new TextBlock
        {
            Text = $"Дата записи: {zapisDate:dd.MM.yyyy}",
            Foreground = Brush.Parse("#6F6F6F"),
            FontSize = 21
        });

        Grid.SetColumn(left, 0);
        grid.Children.Add(left);

        var right = new Border
        {
            Background = status switch
            {
                "active" => Brush.Parse("#EAF8EF"),
                "cancelled" => Brush.Parse("#FFECEC"),
                "used" => Brush.Parse("#F0F0F0"),
                _ => Brush.Parse("#F7F7F7")
            },
            BorderBrush = statusColor,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(22),
            Padding = new Thickness(20),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Статус",
                        Foreground = Brush.Parse("#6F6F6F"),
                        FontSize = 20,
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = GetStatusName(status),
                        Foreground = statusColor,
                        FontSize = 28,
                        FontWeight = FontWeight.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        TextWrapping = TextWrapping.Wrap
                    }
                }
            }
        };

        Grid.SetColumn(right, 1);
        grid.Children.Add(right);

        card.Child = grid;
        ClientsPanel.Children.Add(card);
    }

    private void AddEmptyCard()
    {
        ClientsPanel.Children.Add(new Border
        {
            Background = Brushes.White,
            BorderBrush = Brush.Parse("#E0E0E0"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(26),
            Padding = new Thickness(26),
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Записей клиентов пока нет",
                        Foreground = Brush.Parse("#141414"),
                        FontSize = 28,
                        FontWeight = FontWeight.Bold,
                        TextWrapping = TextWrapping.Wrap
                    },
                    new TextBlock
                    {
                        Text = "После записи клиентов на занятия данные появятся на этой странице.",
                        Foreground = Brush.Parse("#6F6F6F"),
                        FontSize = 20,
                        TextWrapping = TextWrapping.Wrap
                    }
                }
            }
        });
    }

    private void AddErrorCard(string message)
    {
        ClientsPanel.Children.Add(new Border
        {
            Background = Brush.Parse("#FFF1F1"),
            BorderBrush = Brush.Parse("#E32626"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(26),
            Padding = new Thickness(26),
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Ошибка загрузки клиентов",
                        Foreground = Brush.Parse("#E32626"),
                        FontSize = 28,
                        FontWeight = FontWeight.Bold,
                        TextWrapping = TextWrapping.Wrap
                    },
                    new TextBlock
                    {
                        Text = message,
                        Foreground = Brush.Parse("#6F6F6F"),
                        FontSize = 20,
                        TextWrapping = TextWrapping.Wrap
                    }
                }
            }
        });
    }

    private string GetTypeName(string type)
    {
        return type switch
        {
            "group" => "Групповое занятие",
            "personal" => "Персональная тренировка",
            "recovery" => "Восстановление",
            _ => "Занятие"
        };
    }

    private string GetStatusName(string status)
    {
        return status switch
        {
            "active" => "Активно",
            "cancelled" => "Отменено",
            "used" => "Истёк",
            _ => status
        };
    }

    private void OpenHomePage(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenAdminHome();
    }

    private void OpenSchedulePage(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenAdminSchedule();
    }

    private void OpenTrainerTimesPage(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenAdminTrainerTimes();
    }

    private void OpenProfilePage(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenAdminProfile();
    }
}