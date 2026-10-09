using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Npgsql;
using Pulse.Core;

namespace Pulse.User;

public partial class UserMyTrainingsView : UserControl
{
    private readonly MainWindow? _mainWindow;
    private readonly SessionData _session;

    public UserMyTrainingsView()
    {
        InitializeComponent();
        _session = new SessionData();
    }

    public UserMyTrainingsView(MainWindow mainWindow, SessionData session)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
        _session = session;
        LoadTrainings();
    }

    private async void LoadTrainings()
    {
        TrainingsPanel.Children.Clear();

        try
        {
            await UpdatePastTrainingsAsync();

            await using var connection = Database.GetConnection();
            await connection.OpenAsync();

            const string sql = """
                SELECT 
                    z.id_zapisi,
                    z.status_zapisi,
                    z.date_zapisi,
                    u.name_uslugi,
                    u.type_uslugi,
                    u.duration_minutes,
                    u.price_uslugi,
                    r.date_training,
                    r.time_start,
                    r.time_end,
                    trainer.full_name AS trainer_name
                FROM zapisi z
                JOIN raspisanie r ON r.id_raspisanie = z.id_raspisanie
                JOIN uslugi u ON u.id_uslugi = r.id_uslugi
                LEFT JOIN users trainer ON trainer.id_user = r.id_trainer
                WHERE z.id_user = @id_user
                ORDER BY r.date_training DESC, r.time_start DESC
                """;

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id_user", _session.UserId);

            await using var reader = await command.ExecuteReaderAsync();

            bool hasRows = false;

            while (await reader.ReadAsync())
            {
                hasRows = true;

                int zapisId = Convert.ToInt32(reader["id_zapisi"]);
                string name = reader["name_uslugi"]?.ToString() ?? "Занятие";
                string type = reader["type_uslugi"]?.ToString() ?? "";
                string trainer = reader["trainer_name"]?.ToString() ?? "Тренер не указан";
                string status = reader["status_zapisi"]?.ToString() ?? "";
                int duration = Convert.ToInt32(reader["duration_minutes"]);
                decimal price = Convert.ToDecimal(reader["price_uslugi"]);
                DateTime trainingDate = Convert.ToDateTime(reader["date_training"]);
                TimeSpan start = (TimeSpan)reader["time_start"];
                TimeSpan end = (TimeSpan)reader["time_end"];
                DateTime zapisDate = Convert.ToDateTime(reader["date_zapisi"]);

                AddTrainingCard(
                    zapisId,
                    name,
                    type,
                    trainer,
                    duration,
                    price,
                    trainingDate,
                    start,
                    end,
                    zapisDate,
                    status
                );
            }

            if (!hasRows)
            {
                AddEmptyCard();
            }
        }
        catch (Exception ex)
        {
            AddErrorCard(ex.Message);
        }
    }

    private async System.Threading.Tasks.Task UpdatePastTrainingsAsync()
    {
        await using var connection = Database.GetConnection();
        await connection.OpenAsync();

        const string sql = """
            UPDATE zapisi z
            SET status_zapisi = 'used'
            FROM raspisanie r
            WHERE z.id_raspisanie = r.id_raspisanie
              AND z.status_zapisi = 'active'
              AND (r.date_training + r.time_end) < NOW()
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private void AddTrainingCard(
        int zapisId,
        string name,
        string type,
        string trainer,
        int duration,
        decimal price,
        DateTime trainingDate,
        TimeSpan start,
        TimeSpan end,
        DateTime zapisDate,
        string status)
    {
        bool isActive = status == "active";
        bool isCancelled = status == "cancelled";
        bool isUsed = status == "used";

        bool canCancel = isActive && trainingDate.Date > DateTime.Today;

        IBrush statusColor = isActive
            ? Brush.Parse("#27AE60")
            : isCancelled
                ? Brush.Parse("#E32626")
                : Brush.Parse("#8A8A8A");

        var card = new Border
        {
            Background = Brushes.White,
            BorderBrush = Brush.Parse("#E0E0E0"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(26),
            Padding = new Thickness(26)
        };

        var root = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,220"),
            ColumnSpacing = 22
        };

        var panel = new StackPanel
        {
            Spacing = 8
        };

        panel.Children.Add(new TextBlock
        {
            Text = name,
            Foreground = Brush.Parse("#141414"),
            FontSize = 28,
            FontWeight = FontWeight.Bold,
            TextWrapping = TextWrapping.Wrap
        });

        panel.Children.Add(new TextBlock
        {
            Text = GetTypeName(type),
            Foreground = Brush.Parse("#E32626"),
            FontSize = 21,
            FontWeight = FontWeight.Bold,
            TextWrapping = TextWrapping.Wrap
        });

        panel.Children.Add(new TextBlock
        {
            Text = $"Тренер: {trainer}",
            Foreground = Brush.Parse("#6F6F6F"),
            FontSize = 20,
            TextWrapping = TextWrapping.Wrap
        });

        panel.Children.Add(new TextBlock
        {
            Text = $"Дата занятия: {trainingDate:dd.MM.yyyy}",
            Foreground = Brush.Parse("#6F6F6F"),
            FontSize = 20
        });

        panel.Children.Add(new TextBlock
        {
            Text = $"Время: {start:hh\\:mm} — {end:hh\\:mm}",
            Foreground = Brush.Parse("#6F6F6F"),
            FontSize = 20
        });

        panel.Children.Add(new TextBlock
        {
            Text = $"Длительность: {duration} минут",
            Foreground = Brush.Parse("#6F6F6F"),
            FontSize = 20
        });

        panel.Children.Add(new TextBlock
        {
            Text = $"Стоимость: {price:0.00} ₽",
            Foreground = Brush.Parse("#141414"),
            FontSize = 21,
            FontWeight = FontWeight.Bold
        });

        panel.Children.Add(new TextBlock
        {
            Text = $"Дата записи: {zapisDate:dd.MM.yyyy}",
            Foreground = Brush.Parse("#6F6F6F"),
            FontSize = 20
        });

        panel.Children.Add(new TextBlock
        {
            Text = $"Статус: {GetStatusName(status)}",
            Foreground = statusColor,
            FontSize = 22,
            FontWeight = FontWeight.Bold
        });

        if (isActive && !canCancel)
        {
            panel.Children.Add(new TextBlock
            {
                Text = "Отмена недоступна в день занятия",
                Foreground = Brush.Parse("#8A8A8A"),
                FontSize = 18,
                TextWrapping = TextWrapping.Wrap
            });
        }

        Grid.SetColumn(panel, 0);
        root.Children.Add(panel);

        var rightPanel = new StackPanel
        {
            Spacing = 12,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        var statusBox = new Border
        {
            Background = isActive
                ? Brush.Parse("#EAF8EF")
                : isCancelled
                    ? Brush.Parse("#FFECEC")
                    : Brush.Parse("#F0F0F0"),
            BorderBrush = statusColor,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(20),
            Padding = new Thickness(14),
            Child = new TextBlock
            {
                Text = GetStatusName(status),
                Foreground = statusColor,
                FontSize = 22,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            }
        };

        rightPanel.Children.Add(statusBox);

        if (canCancel)
        {
            var cancelButton = new Button
            {
                Content = "ОТМЕНИТЬ",
                Height = 54,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Background = Brushes.White,
                Foreground = Brush.Parse("#E32626"),
                BorderBrush = Brush.Parse("#E32626"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(18),
                FontSize = 18,
                FontWeight = FontWeight.Bold,
                Tag = new CancelInfo
                {
                    ZapisId = zapisId,
                    Price = price
                }
            };

            cancelButton.Click += CancelTraining;
            rightPanel.Children.Add(cancelButton);
        }

        Grid.SetColumn(rightPanel, 1);
        root.Children.Add(rightPanel);

        card.Child = root;
        TrainingsPanel.Children.Add(card);
    }

    private async void CancelTraining(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not CancelInfo info)
            return;

        try
        {
            await using var connection = Database.GetConnection();
            await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync();

            const string checkSql = """
                SELECT r.date_training
                FROM zapisi z
                JOIN raspisanie r ON r.id_raspisanie = z.id_raspisanie
                WHERE z.id_zapisi = @id_zapisi
                  AND z.id_user = @id_user
                  AND z.status_zapisi = 'active'
                LIMIT 1
                """;

            await using var checkCommand = new NpgsqlCommand(checkSql, connection, transaction);
            checkCommand.Parameters.AddWithValue("@id_zapisi", info.ZapisId);
            checkCommand.Parameters.AddWithValue("@id_user", _session.UserId);

            var dateObject = await checkCommand.ExecuteScalarAsync();

            if (dateObject == null)
            {
                await transaction.RollbackAsync();
                AddErrorCard("Запись не найдена или уже не активна.");
                return;
            }

            DateTime trainingDate = Convert.ToDateTime(dateObject);

            if (trainingDate.Date <= DateTime.Today)
            {
                await transaction.RollbackAsync();
                AddErrorCard("Отменить занятие можно только минимум за один день.");
                return;
            }

            const string cancelSql = """
                UPDATE zapisi
                SET status_zapisi = 'cancelled'
                WHERE id_zapisi = @id_zapisi
                  AND id_user = @id_user
                """;

            await using var cancelCommand = new NpgsqlCommand(cancelSql, connection, transaction);
            cancelCommand.Parameters.AddWithValue("@id_zapisi", info.ZapisId);
            cancelCommand.Parameters.AddWithValue("@id_user", _session.UserId);
            await cancelCommand.ExecuteNonQueryAsync();

            const string refundSql = """
                UPDATE users
                SET balance = balance + @price
                WHERE id_user = @id_user
                """;

            await using var refundCommand = new NpgsqlCommand(refundSql, connection, transaction);
            refundCommand.Parameters.AddWithValue("@price", info.Price);
            refundCommand.Parameters.AddWithValue("@id_user", _session.UserId);
            await refundCommand.ExecuteNonQueryAsync();

            await transaction.CommitAsync();

            LoadTrainings();
        }
        catch (Exception ex)
        {
            AddErrorCard("Ошибка отмены занятия: " + ex.Message);
        }
    }

    private void AddEmptyCard()
    {
        var card = new Border
        {
            Background = Brushes.White,
            BorderBrush = Brush.Parse("#E0E0E0"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(26),
            Padding = new Thickness(26)
        };

        var panel = new StackPanel
        {
            Spacing = 10
        };

        panel.Children.Add(new TextBlock
        {
            Text = "Пока нет купленных занятий",
            Foreground = Brush.Parse("#141414"),
            FontSize = 28,
            FontWeight = FontWeight.Bold
        });

        panel.Children.Add(new TextBlock
        {
            Text = "После записи на тренировку она появится здесь.",
            Foreground = Brush.Parse("#6F6F6F"),
            FontSize = 20,
            TextWrapping = TextWrapping.Wrap
        });

        card.Child = panel;
        TrainingsPanel.Children.Add(card);
    }

    private void AddErrorCard(string message)
    {
        var card = new Border
        {
            Background = Brush.Parse("#FFF1F1"),
            BorderBrush = Brush.Parse("#E32626"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(26),
            Padding = new Thickness(26)
        };

        var panel = new StackPanel
        {
            Spacing = 10
        };

        panel.Children.Add(new TextBlock
        {
            Text = "Сообщение",
            Foreground = Brush.Parse("#E32626"),
            FontSize = 26,
            FontWeight = FontWeight.Bold
        });

        panel.Children.Add(new TextBlock
        {
            Text = message,
            Foreground = Brush.Parse("#6F6F6F"),
            FontSize = 18,
            TextWrapping = TextWrapping.Wrap
        });

        card.Child = panel;
        TrainingsPanel.Children.Insert(0, card);
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

    private void OpenMainClubPage(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenUserMainClubPage();
    }

    private void OpenSchedulePage(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenUserSchedule();
    }

    private void OpenNewsPage(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenUserNews();
    }

    private void OpenProfilePage(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenUserProfile();
    }

    private class CancelInfo
    {
        public int ZapisId { get; set; }
        public decimal Price { get; set; }
    }
}