using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Npgsql;
using Pulse.Core;

namespace Pulse.User;

public partial class AdminTrainerTimesView : UserControl
{
    private readonly MainWindow? _mainWindow;
    private readonly SessionData _session;

    private TimeItem? _selectedTimeItem;

    public AdminTrainerTimesView()
    {
        InitializeComponent();
        _session = new SessionData();
    }

    public AdminTrainerTimesView(MainWindow mainWindow, SessionData session)
    {
        InitializeComponent();

        _mainWindow = mainWindow;
        _session = session;

        LoadServices();
        LoadTimes();
    }

    private async void LoadServices()
    {
        ServiceComboBox.Items.Clear();

        try
        {
            await using var connection = Database.GetConnection();
            await connection.OpenAsync();

            const string sql = """
                SELECT id_uslugi, name_uslugi
                FROM uslugi
                ORDER BY id_uslugi
                """;

            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                ServiceComboBox.Items.Add(new ComboBoxItem
                {
                    Content = reader["name_uslugi"]?.ToString() ?? "",
                    Tag = Convert.ToInt32(reader["id_uslugi"])
                });
            }

            if (ServiceComboBox.Items.Count > 0)
                ServiceComboBox.SelectedIndex = 0;
        }
        catch
        {
        }
    }

    private async void LoadTimes()
    {
        TimesPanel.Children.Clear();

        try
        {
            await using var connection = Database.GetConnection();
            await connection.OpenAsync();

            const string sql = """
                SELECT 
                    t.id_available_time,
                    t.available_date,
                    t.time_start,
                    t.time_end,
                    t.status_time,
                    u.full_name,
                    u.id_user
                FROM trainer_available_times t
                JOIN users u ON u.id_user = t.id_trainer
                WHERE t.available_date >= CURRENT_DATE
                ORDER BY t.available_date, t.time_start
                """;

            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            bool hasRows = false;

            while (await reader.ReadAsync())
            {
                hasRows = true;

                int id = Convert.ToInt32(reader["id_available_time"]);
                DateTime date = Convert.ToDateTime(reader["available_date"]);
                TimeSpan start = (TimeSpan)reader["time_start"];
                TimeSpan end = (TimeSpan)reader["time_end"];
                string status = reader["status_time"]?.ToString() ?? "free";
                string trainer = reader["full_name"]?.ToString() ?? "";
                int trainerId = Convert.ToInt32(reader["id_user"]);

                AddCard(id, trainerId, trainer, date, start, end, status);
            }

            if (!hasRows)
                AddEmptyCard();
        }
        catch (Exception ex)
        {
            AddErrorCard(ex.Message);
        }
    }

    private void AddCard(int id, int trainerId, string trainer, DateTime date, TimeSpan start, TimeSpan end, string status)
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
            CornerRadius = new CornerRadius(24),
            Padding = new Thickness(22)
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,240"),
            ColumnSpacing = 20
        };

        var left = new StackPanel
        {
            Spacing = 7
        };

        left.Children.Add(new TextBlock
        {
            Text = trainer,
            Foreground = Brush.Parse("#141414"),
            FontSize = 28,
            FontWeight = FontWeight.Bold,
            TextWrapping = TextWrapping.Wrap
        });

        left.Children.Add(new TextBlock
        {
            Text = $"Дата: {date:dd.MM.yyyy}",
            Foreground = Brush.Parse("#6F6F6F"),
            FontSize = 20
        });

        left.Children.Add(new TextBlock
        {
            Text = $"Время: {start:hh\\:mm} — {end:hh\\:mm}",
            Foreground = Brush.Parse("#6F6F6F"),
            FontSize = 20
        });

        left.Children.Add(new TextBlock
        {
            Text = $"Статус: {GetStatusText(status)}",
            Foreground = statusColor,
            FontSize = 20,
            FontWeight = FontWeight.Bold
        });

        Grid.SetColumn(left, 0);
        grid.Children.Add(left);

        var right = new StackPanel
        {
            Spacing = 12,
            VerticalAlignment = VerticalAlignment.Center
        };

        right.Children.Add(new Border
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
            CornerRadius = new CornerRadius(18),
            Padding = new Thickness(14),
            Child = new TextBlock
            {
                Text = GetStatusText(status),
                Foreground = statusColor,
                FontSize = 20,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center
            }
        });

        if (status == "free")
        {
            var button = new Button
            {
                Content = "СОЗДАТЬ ЗАНЯТИЕ",
                Height = 54,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Background = Brush.Parse("#E32626"),
                Foreground = Brushes.White,
                BorderBrush = Brush.Parse("#E32626"),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(18),
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Tag = new TimeItem
                {
                    Id = id,
                    TrainerId = trainerId,
                    TrainerName = trainer,
                    Date = date,
                    Start = start,
                    End = end
                }
            };

            button.Click += OpenCreatePanel;
            right.Children.Add(button);
        }

        Grid.SetColumn(right, 1);
        grid.Children.Add(right);

        card.Child = grid;
        TimesPanel.Children.Add(card);
    }

    private void OpenCreatePanel(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not TimeItem item)
            return;

        _selectedTimeItem = item;

        SelectedTrainerText.Text = item.TrainerName;
        SelectedTimeText.Text = $"Свободное время: {item.Date:dd.MM.yyyy}, {item.Start:hh\\:mm} — {item.End:hh\\:mm}";

        StartTimeTextBox.Text = item.Start.ToString(@"hh\:mm");
        EndTimeTextBox.Text = item.End.ToString(@"hh\:mm");
        MaxPeopleTextBox.Text = "10";

        CreateMessageBorder.IsVisible = false;
        CreateMessageText.Text = "";

        CreateTrainingOverlay.IsVisible = true;
    }

    private void CloseCreatePanel(object? sender, RoutedEventArgs e)
    {
        CreateTrainingOverlay.IsVisible = false;
        _selectedTimeItem = null;
    }

    private async void CreateTraining(object? sender, RoutedEventArgs e)
    {
        if (_selectedTimeItem is null)
            return;

        CreateMessageBorder.IsVisible = false;
        CreateMessageText.Text = "";

        if (ServiceComboBox.SelectedItem is not ComboBoxItem serviceItem)
        {
            ShowCreateMessage("Выберите услугу.", true);
            return;
        }

        int serviceId = Convert.ToInt32(serviceItem.Tag);

        if (!TimeSpan.TryParse(StartTimeTextBox.Text?.Trim(), out TimeSpan trainingStart))
        {
            ShowCreateMessage("Введите начало в формате 10:00.", true);
            return;
        }

        if (!TimeSpan.TryParse(EndTimeTextBox.Text?.Trim(), out TimeSpan trainingEnd))
        {
            ShowCreateMessage("Введите конец в формате 11:00.", true);
            return;
        }

        if (trainingStart >= trainingEnd)
        {
            ShowCreateMessage("Начало занятия должно быть раньше конца.", true);
            return;
        }

        if (trainingStart < _selectedTimeItem.Start || trainingEnd > _selectedTimeItem.End)
        {
            ShowCreateMessage("Занятие должно быть внутри свободного времени тренера.", true);
            return;
        }

        if (!int.TryParse(MaxPeopleTextBox.Text?.Trim(), out int maxPeople))
        {
            ShowCreateMessage("Введите количество мест.", true);
            return;
        }

        if (maxPeople <= 0)
        {
            ShowCreateMessage("Количество мест должно быть больше нуля.", true);
            return;
        }

        try
        {
            await using var connection = Database.GetConnection();
            await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync();

            const string checkSql = """
                SELECT COUNT(*)
                FROM raspisanie
                WHERE id_trainer = @id_trainer
                  AND date_training = @date_training
                  AND (
                        (@time_start >= time_start AND @time_start < time_end)
                     OR (@time_end > time_start AND @time_end <= time_end)
                     OR (@time_start <= time_start AND @time_end >= time_end)
                  )
                """;

            await using var checkCommand = new NpgsqlCommand(checkSql, connection, transaction);
            checkCommand.Parameters.AddWithValue("@id_trainer", _selectedTimeItem.TrainerId);
            checkCommand.Parameters.AddWithValue("@date_training", _selectedTimeItem.Date);
            checkCommand.Parameters.AddWithValue("@time_start", trainingStart);
            checkCommand.Parameters.AddWithValue("@time_end", trainingEnd);

            int exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());

            if (exists > 0)
            {
                await transaction.RollbackAsync();
                ShowCreateMessage("У тренера уже есть занятие на это время.", true);
                return;
            }

            const string insertScheduleSql = """
                INSERT INTO raspisanie
                (id_uslugi, id_trainer, date_training, time_start, time_end, max_people)
                VALUES
                (@id_uslugi, @id_trainer, @date_training, @time_start, @time_end, @max_people)
                """;

            await using var insertScheduleCommand = new NpgsqlCommand(insertScheduleSql, connection, transaction);
            insertScheduleCommand.Parameters.AddWithValue("@id_uslugi", serviceId);
            insertScheduleCommand.Parameters.AddWithValue("@id_trainer", _selectedTimeItem.TrainerId);
            insertScheduleCommand.Parameters.AddWithValue("@date_training", _selectedTimeItem.Date);
            insertScheduleCommand.Parameters.AddWithValue("@time_start", trainingStart);
            insertScheduleCommand.Parameters.AddWithValue("@time_end", trainingEnd);
            insertScheduleCommand.Parameters.AddWithValue("@max_people", maxPeople);

            await insertScheduleCommand.ExecuteNonQueryAsync();

            bool hasBefore = trainingStart > _selectedTimeItem.Start;
            bool hasAfter = trainingEnd < _selectedTimeItem.End;

            if (!hasBefore && !hasAfter)
            {
                const string updateFullSql = """
                    UPDATE trainer_available_times
                    SET status_time = 'used'
                    WHERE id_available_time = @id_available_time
                    """;

                await using var updateFullCommand = new NpgsqlCommand(updateFullSql, connection, transaction);
                updateFullCommand.Parameters.AddWithValue("@id_available_time", _selectedTimeItem.Id);
                await updateFullCommand.ExecuteNonQueryAsync();
            }
            else
            {
                if (hasBefore)
                {
                    const string updateBeforeSql = """
                        UPDATE trainer_available_times
                        SET time_end = @time_end
                        WHERE id_available_time = @id_available_time
                        """;

                    await using var updateBeforeCommand = new NpgsqlCommand(updateBeforeSql, connection, transaction);
                    updateBeforeCommand.Parameters.AddWithValue("@time_end", trainingStart);
                    updateBeforeCommand.Parameters.AddWithValue("@id_available_time", _selectedTimeItem.Id);
                    await updateBeforeCommand.ExecuteNonQueryAsync();
                }
                else
                {
                    const string updateAfterOnlySql = """
                        UPDATE trainer_available_times
                        SET time_start = @time_start
                        WHERE id_available_time = @id_available_time
                        """;

                    await using var updateAfterOnlyCommand = new NpgsqlCommand(updateAfterOnlySql, connection, transaction);
                    updateAfterOnlyCommand.Parameters.AddWithValue("@time_start", trainingEnd);
                    updateAfterOnlyCommand.Parameters.AddWithValue("@id_available_time", _selectedTimeItem.Id);
                    await updateAfterOnlyCommand.ExecuteNonQueryAsync();
                }

                if (hasAfter && hasBefore)
                {
                    const string insertAfterSql = """
                        INSERT INTO trainer_available_times
                        (id_trainer, available_date, time_start, time_end, status_time)
                        VALUES
                        (@id_trainer, @available_date, @time_start, @time_end, 'free')
                        """;

                    await using var insertAfterCommand = new NpgsqlCommand(insertAfterSql, connection, transaction);
                    insertAfterCommand.Parameters.AddWithValue("@id_trainer", _selectedTimeItem.TrainerId);
                    insertAfterCommand.Parameters.AddWithValue("@available_date", _selectedTimeItem.Date);
                    insertAfterCommand.Parameters.AddWithValue("@time_start", trainingEnd);
                    insertAfterCommand.Parameters.AddWithValue("@time_end", _selectedTimeItem.End);
                    await insertAfterCommand.ExecuteNonQueryAsync();
                }

                const string insertUsedSql = """
                    INSERT INTO trainer_available_times
                    (id_trainer, available_date, time_start, time_end, status_time)
                    VALUES
                    (@id_trainer, @available_date, @time_start, @time_end, 'used')
                    """;

                await using var insertUsedCommand = new NpgsqlCommand(insertUsedSql, connection, transaction);
                insertUsedCommand.Parameters.AddWithValue("@id_trainer", _selectedTimeItem.TrainerId);
                insertUsedCommand.Parameters.AddWithValue("@available_date", _selectedTimeItem.Date);
                insertUsedCommand.Parameters.AddWithValue("@time_start", trainingStart);
                insertUsedCommand.Parameters.AddWithValue("@time_end", trainingEnd);
                await insertUsedCommand.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();

            CreateTrainingOverlay.IsVisible = false;
            _selectedTimeItem = null;

            LoadTimes();
        }
        catch (Exception ex)
        {
            ShowCreateMessage("Ошибка создания занятия: " + ex.Message, true);
        }
    }

    private void ShowCreateMessage(string message, bool isError)
    {
        CreateMessageText.Text = message;
        CreateMessageBorder.IsVisible = true;

        if (isError)
        {
            CreateMessageBorder.Background = Brush.Parse("#33D92525");
            CreateMessageBorder.BorderBrush = Brush.Parse("#D92525");
            CreateMessageText.Foreground = Brush.Parse("#D92525");
        }
        else
        {
            CreateMessageBorder.Background = Brush.Parse("#3327AE60");
            CreateMessageBorder.BorderBrush = Brush.Parse("#27AE60");
            CreateMessageText.Foreground = Brush.Parse("#141414");
        }
    }

    private void AddEmptyCard()
    {
        TimesPanel.Children.Add(new Border
        {
            Background = Brushes.White,
            BorderBrush = Brush.Parse("#E0E0E0"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(24),
            Padding = new Thickness(22),
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Свободного времени пока нет",
                        Foreground = Brush.Parse("#141414"),
                        FontSize = 28,
                        FontWeight = FontWeight.Bold
                    },
                    new TextBlock
                    {
                        Text = "Когда тренеры добавят свободное время, оно появится здесь.",
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
        TimesPanel.Children.Add(new Border
        {
            Background = Brush.Parse("#FFF1F1"),
            BorderBrush = Brush.Parse("#E32626"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(24),
            Padding = new Thickness(22),
            Child = new TextBlock
            {
                Text = message,
                Foreground = Brush.Parse("#E32626"),
                FontSize = 18,
                TextWrapping = TextWrapping.Wrap
            }
        });
    }

    private string GetStatusText(string status)
    {
        return status switch
        {
            "free" => "Свободно",
            "used" => "Использовано",
            "cancelled" => "Отменено",
            _ => status
        };
    }

    private void OpenHome(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenAdminHome();
    }

    private void OpenSchedule(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenAdminSchedule();
    }

    private void OpenClients(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenAdminClients();
    }

    private void OpenProfile(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenAdminProfile();
    }

    private class TimeItem
    {
        public int Id { get; set; }
        public int TrainerId { get; set; }
        public string TrainerName { get; set; } = "";
        public DateTime Date { get; set; }
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
    }
}