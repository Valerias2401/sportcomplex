using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Npgsql;
using Pulse.Core;

namespace Pulse.User;

public partial class TrainerScheduleView : UserControl
{
    private readonly MainWindow? _mainWindow;
    private readonly SessionData _session;

    public TrainerScheduleView()
    {
        InitializeComponent();
        _session = new SessionData();
    }

    public TrainerScheduleView(MainWindow mainWindow, SessionData session)
    {
        InitializeComponent();

        _mainWindow = mainWindow;
        _session = session;

        LoadSchedule();
    }

    private async void LoadSchedule()
    {
        SchedulePanel.Children.Clear();

        try
        {
            await using var connection = Database.GetConnection();
            await connection.OpenAsync();

            const string sql = """
                SELECT
                    r.id_raspisanie,
                    r.date_training,
                    r.time_start,
                    r.time_end,
                    r.max_people,
                    u.name_uslugi,
                    u.type_uslugi,
                    u.duration_minutes,
                    COALESCE(r.price_training, u.price_uslugi) AS price_training,
                    COALESCE(COUNT(z.id_zapisi), 0) AS busy_places
                FROM raspisanie r
                JOIN uslugi u ON u.id_uslugi = r.id_uslugi
                LEFT JOIN zapisi z ON z.id_raspisanie = r.id_raspisanie
                              AND z.status_zapisi = 'active'
                WHERE r.id_trainer = @id_trainer
                  AND r.date_training >= CURRENT_DATE
                GROUP BY
                    r.id_raspisanie,
                    r.date_training,
                    r.time_start,
                    r.time_end,
                    r.max_people,
                    u.name_uslugi,
                    u.type_uslugi,
                    u.duration_minutes,
                    r.price_training,
                    u.price_uslugi
                ORDER BY r.date_training, r.time_start
                """;

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id_trainer", _session.UserId);

            await using var reader = await command.ExecuteReaderAsync();

            bool hasRows = false;

            while (await reader.ReadAsync())
            {
                hasRows = true;

                string name = reader["name_uslugi"]?.ToString() ?? "Занятие";
                string type = reader["type_uslugi"]?.ToString() ?? "";
                DateTime date = Convert.ToDateTime(reader["date_training"]);
                TimeSpan start = (TimeSpan)reader["time_start"];
                TimeSpan end = (TimeSpan)reader["time_end"];
                int duration = Convert.ToInt32(reader["duration_minutes"]);
                decimal price = Convert.ToDecimal(reader["price_training"]);
                int maxPeople = Convert.ToInt32(reader["max_people"]);
                int busyPlaces = Convert.ToInt32(reader["busy_places"]);

                AddScheduleCard(name, type, date, start, end, duration, price, maxPeople, busyPlaces);
            }

            if (!hasRows)
                AddEmptyCard();
        }
        catch (Exception ex)
        {
            AddErrorCard(ex.Message);
        }
    }

    private void AddScheduleCard(
        string name,
        string type,
        DateTime date,
        TimeSpan start,
        TimeSpan end,
        int duration,
        decimal price,
        int maxPeople,
        int busyPlaces)
    {
        int freePlaces = maxPeople - busyPlaces;

        if (freePlaces < 0)
            freePlaces = 0;

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
            Text = name,
            Foreground = Brush.Parse("#141414"),
            FontSize = 30,
            FontWeight = FontWeight.Bold,
            TextWrapping = TextWrapping.Wrap
        });

        left.Children.Add(new TextBlock
        {
            Text = GetTypeName(type),
            Foreground = Brush.Parse("#E32626"),
            FontSize = 22,
            FontWeight = FontWeight.Bold,
            TextWrapping = TextWrapping.Wrap
        });

        left.Children.Add(new TextBlock
        {
            Text = $"Дата: {date:dd.MM.yyyy}",
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
            Text = $"Длительность: {duration} минут",
            Foreground = Brush.Parse("#6F6F6F"),
            FontSize = 21
        });

        left.Children.Add(new TextBlock
        {
            Text = $"Цена занятия: {price:0.00} ₽",
            Foreground = Brush.Parse("#141414"),
            FontSize = 21,
            FontWeight = FontWeight.Bold
        });

        Grid.SetColumn(left, 0);
        grid.Children.Add(left);

        var right = new Border
        {
            Background = Brush.Parse("#F7F7F7"),
            BorderBrush = Brush.Parse("#E0E0E0"),
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
                        Text = "Записано",
                        Foreground = Brush.Parse("#6F6F6F"),
                        FontSize = 20,
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = $"{busyPlaces} из {maxPeople}",
                        Foreground = Brush.Parse("#141414"),
                        FontSize = 34,
                        FontWeight = FontWeight.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = $"Свободно: {freePlaces}",
                        Foreground = freePlaces > 0 ? Brush.Parse("#27AE60") : Brush.Parse("#E32626"),
                        FontSize = 20,
                        FontWeight = FontWeight.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center
                    }
                }
            }
        };

        Grid.SetColumn(right, 1);
        grid.Children.Add(right);

        card.Child = grid;
        SchedulePanel.Children.Add(card);
    }

    private void AddEmptyCard()
    {
        SchedulePanel.Children.Add(new Border
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
                        Text = "Занятий пока нет",
                        Foreground = Brush.Parse("#141414"),
                        FontSize = 28,
                        FontWeight = FontWeight.Bold
                    },
                    new TextBlock
                    {
                        Text = "Администратор сможет поставить занятия после добавления свободного времени тренером.",
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
        SchedulePanel.Children.Add(new Border
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
                        Text = "Ошибка загрузки расписания",
                        Foreground = Brush.Parse("#E32626"),
                        FontSize = 28,
                        FontWeight = FontWeight.Bold
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

    private void OpenHomePage(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenTrainerHome();
    }

    private void OpenClientsPage(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenTrainerClients();
    }

    private void OpenAvailableTimePage(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenTrainerAvailableTime();
    }

    private void OpenProfilePage(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenTrainerProfile();
    }
}