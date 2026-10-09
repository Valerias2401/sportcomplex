using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Npgsql;
using Pulse.Core;

namespace Pulse.User;

public partial class TrainerAvailableTimeView : UserControl
{
    private readonly MainWindow? _mainWindow;
    private readonly SessionData _session;

    public TrainerAvailableTimeView()
    {
        InitializeComponent();
        _session = new SessionData();
    }

    public TrainerAvailableTimeView(MainWindow mainWindow, SessionData session)
    {
        InitializeComponent();

        _mainWindow = mainWindow;
        _session = session;

        DatePicker.SelectedDate = DateTimeOffset.Now;

        LoadAvailableTimes();
    }

    private async void LoadAvailableTimes()
    {
        AvailableTimesPanel.Children.Clear();

        try
        {
            await using var connection = Database.GetConnection();
            await connection.OpenAsync();

            const string sql = """
                SELECT id_available_time, available_date, time_start, time_end, status_time
                FROM trainer_available_times
                WHERE id_trainer = @id_trainer
                  AND available_date >= CURRENT_DATE
                ORDER BY available_date, time_start
                """;

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id_trainer", _session.UserId);

            await using var reader = await command.ExecuteReaderAsync();

            bool hasRows = false;

            while (await reader.ReadAsync())
            {
                hasRows = true;

                int id = Convert.ToInt32(reader["id_available_time"]);
                DateTime date = Convert.ToDateTime(reader["available_date"]);
                TimeSpan start = (TimeSpan)reader["time_start"];
                TimeSpan end = (TimeSpan)reader["time_end"];
                string status = reader["status_time"]?.ToString() ?? "";

                AddAvailableTimeCard(id, date, start, end, status);
            }

            if (!hasRows)
                AddEmptyCard();
        }
        catch (Exception ex)
        {
            AddErrorCard(ex.Message);
        }
    }

    private async void AddAvailableTime(object? sender, RoutedEventArgs e)
    {
        HideMessage();

        if (!DatePicker.SelectedDate.HasValue)
        {
            ShowMessage("Выберите дату.", true);
            return;
        }

        DateTime date = DatePicker.SelectedDate.Value.DateTime.Date;

        if (date < DateTime.Today)
        {
            ShowMessage("Нельзя добавить график на прошедшую дату.", true);
            return;
        }

        if (!TimeSpan.TryParse(StartTimeTextBox.Text?.Trim(), out TimeSpan start))
        {
            ShowMessage("Введите начало в формате 10:00.", true);
            return;
        }

        if (!TimeSpan.TryParse(EndTimeTextBox.Text?.Trim(), out TimeSpan end))
        {
            ShowMessage("Введите конец в формате 18:00.", true);
            return;
        }

        if (start >= end)
        {
            ShowMessage("Время начала должно быть раньше времени окончания.", true);
            return;
        }

        try
        {
            await using var connection = Database.GetConnection();
            await connection.OpenAsync();

            const string checkSql = """
                SELECT COUNT(*)
                FROM trainer_available_times
                WHERE id_trainer = @id_trainer
                  AND available_date = @available_date
                  AND status_time != 'cancelled'
                  AND (
                        (@time_start >= time_start AND @time_start < time_end)
                     OR (@time_end > time_start AND @time_end <= time_end)
                     OR (@time_start <= time_start AND @time_end >= time_end)
                  )
                """;

            await using var checkCommand = new NpgsqlCommand(checkSql, connection);
            checkCommand.Parameters.AddWithValue("@id_trainer", _session.UserId);
            checkCommand.Parameters.AddWithValue("@available_date", date);
            checkCommand.Parameters.AddWithValue("@time_start", start);
            checkCommand.Parameters.AddWithValue("@time_end", end);

            int exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());

            if (exists > 0)
            {
                ShowMessage("На это время уже есть запись в графике.", true);
                return;
            }

            const string insertSql = """
                INSERT INTO trainer_available_times
                (id_trainer, available_date, time_start, time_end, status_time)
                VALUES
                (@id_trainer, @available_date, @time_start, @time_end, 'free')
                """;

            await using var insertCommand = new NpgsqlCommand(insertSql, connection);
            insertCommand.Parameters.AddWithValue("@id_trainer", _session.UserId);
            insertCommand.Parameters.AddWithValue("@available_date", date);
            insertCommand.Parameters.AddWithValue("@time_start", start);
            insertCommand.Parameters.AddWithValue("@time_end", end);

            await insertCommand.ExecuteNonQueryAsync();

            ShowMessage("Свободное время добавлено.", false);

            StartTimeTextBox.Text = "";
            EndTimeTextBox.Text = "";

            LoadAvailableTimes();
        }
        catch (Exception ex)
        {
            ShowMessage("Ошибка добавления графика: " + ex.Message, true);
        }
    }

    private void AddAvailableTimeCard(
        int id,
        DateTime date,
        TimeSpan start,
        TimeSpan end,
        string status)
    {
        IBrush statusColor = status switch
        {
            "free" => Brush.Parse("#27AE60"),
            "used" => Brush.Parse("#8A8A8A"),
            "cancelled" => Brush.Parse("#E32626"),
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
            ColumnDefinitions = new ColumnDefinitions("*,220"),
            ColumnSpacing = 22
        };

        var left = new StackPanel
        {
            Spacing = 8
        };

        left.Children.Add(new TextBlock
        {
            Text = $"Дата: {date:dd.MM.yyyy}",
            Foreground = Brush.Parse("#141414"),
            FontSize = 28,
            FontWeight = FontWeight.Bold
        });

        left.Children.Add(new TextBlock
        {
            Text = $"Время: {start:hh\\:mm} — {end:hh\\:mm}",
            Foreground = Brush.Parse("#6F6F6F"),
            FontSize = 21
        });

        left.Children.Add(new TextBlock
        {
            Text = $"Статус: {GetStatusName(status)}",
            Foreground = statusColor,
            FontSize = 21,
            FontWeight = FontWeight.Bold
        });

        Grid.SetColumn(left, 0);
        grid.Children.Add(left);

        var right = new StackPanel
        {
            Spacing = 12,
            VerticalAlignment = VerticalAlignment.Center
        };

        var statusBox = new Border
        {
            Background = status switch
            {
                "free" => Brush.Parse("#EAF8EF"),
                "used" => Brush.Parse("#F0F0F0"),
                "cancelled" => Brush.Parse("#FFECEC"),
                _ => Brush.Parse("#F7F7F7")
            },
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
                HorizontalAlignment = HorizontalAlignment.Center
            }
        };

        right.Children.Add(statusBox);

        if (status == "free")
        {
            var cancelButton = new Button
            {
                Content = "ОТМЕНИТЬ",
                Height = 54,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Background = Brushes.White,
                Foreground = Brush.Parse("#E32626"),
                BorderBrush = Brush.Parse("#E32626"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(18),
                FontSize = 18,
                FontWeight = FontWeight.Bold,
                Tag = id
            };

            cancelButton.Click += CancelAvailableTime;
            right.Children.Add(cancelButton);
        }

        Grid.SetColumn(right, 1);
        grid.Children.Add(right);

        card.Child = grid;
        AvailableTimesPanel.Children.Add(card);
    }

    private async void CancelAvailableTime(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not int id)
            return;

        try
        {
            await using var connection = Database.GetConnection();
            await connection.OpenAsync();

            const string sql = """
                UPDATE trainer_available_times
                SET status_time = 'cancelled'
                WHERE id_available_time = @id_available_time
                  AND id_trainer = @id_trainer
                  AND status_time = 'free'
                """;

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id_available_time", id);
            command.Parameters.AddWithValue("@id_trainer", _session.UserId);

            await command.ExecuteNonQueryAsync();

            ShowMessage("Свободное время отменено.", false);

            LoadAvailableTimes();
        }
        catch (Exception ex)
        {
            ShowMessage("Ошибка отмены времени: " + ex.Message, true);
        }
    }

    private void AddEmptyCard()
    {
        AvailableTimesPanel.Children.Add(new Border
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
                        Text = "Свободное время пока не добавлено",
                        Foreground = Brush.Parse("#141414"),
                        FontSize = 28,
                        FontWeight = FontWeight.Bold
                    },
                    new TextBlock
                    {
                        Text = "Добавьте дату и время, чтобы администратор мог поставить занятие.",
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
        AvailableTimesPanel.Children.Add(new Border
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
                        Text = "Ошибка загрузки графика",
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

    private string GetStatusName(string status)
    {
        return status switch
        {
            "free" => "Свободно",
            "used" => "Занято",
            "cancelled" => "Отменено",
            _ => status
        };
    }

    private void ShowMessage(string message, bool isError)
    {
        MessageText.Text = message;
        MessageBorder.IsVisible = true;

        if (isError)
        {
            MessageBorder.Background = Brush.Parse("#33D92525");
            MessageBorder.BorderBrush = Brush.Parse("#D92525");
            MessageText.Foreground = Brush.Parse("#D92525");
        }
        else
        {
            MessageBorder.Background = Brush.Parse("#3327AE60");
            MessageBorder.BorderBrush = Brush.Parse("#27AE60");
            MessageText.Foreground = Brush.Parse("#141414");
        }
    }

    private void HideMessage()
    {
        MessageBorder.IsVisible = false;
        MessageText.Text = "";
    }

    private void OpenHomePage(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenTrainerHome();
    }

    private void OpenSchedulePage(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenTrainerSchedule();
    }

    private void OpenClientsPage(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenTrainerClients();
    }

    private void OpenProfilePage(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenTrainerProfile();
    }
}