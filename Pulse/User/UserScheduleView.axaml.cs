using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Npgsql;
using Pulse.Core;

namespace Pulse.User;

public partial class UserScheduleView : UserControl
{
    private readonly MainWindow? _mainWindow;
    private readonly SessionData _session;
    private readonly string? _selectedTrainerName;

    private readonly List<ScheduleItem> _allSchedule = new();

    public UserScheduleView()
    {
        InitializeComponent();
        _session = new SessionData();
    }

    public UserScheduleView(MainWindow mainWindow, SessionData session)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
        _session = session;
        _ = LoadDataAsync();
    }

    public UserScheduleView(MainWindow mainWindow, SessionData session, string trainerName)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
        _session = session;
        _selectedTrainerName = trainerName;
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        await LoadTrainersAsync();
        await LoadScheduleAsync();

        if (!string.IsNullOrWhiteSpace(_selectedTrainerName))
            ApplyFilters(null, new RoutedEventArgs());
    }

    private async Task LoadTrainersAsync()
    {
        TrainerComboBox.Items.Clear();

        TrainerComboBox.Items.Add(new ComboBoxItem
        {
            Content = "Все тренеры",
            Tag = 0
        });

        try
        {
            await using var connection = Database.GetConnection();
            await connection.OpenAsync();

            const string sql = """
                SELECT id_user, full_name
                FROM users
                WHERE id_role = 2
                ORDER BY full_name
                """;

            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                TrainerComboBox.Items.Add(new ComboBoxItem
                {
                    Content = reader["full_name"]?.ToString() ?? "",
                    Tag = Convert.ToInt32(reader["id_user"])
                });
            }

            TrainerComboBox.SelectedIndex = 0;

            if (!string.IsNullOrWhiteSpace(_selectedTrainerName))
            {
                for (int i = 0; i < TrainerComboBox.Items.Count; i++)
                {
                    if (TrainerComboBox.Items[i] is ComboBoxItem item &&
                        item.Content?.ToString() == _selectedTrainerName)
                    {
                        TrainerComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowMessage("Ошибка загрузки тренеров: " + ex.Message, true);
        }
    }

    private async Task LoadScheduleAsync()
    {
        _allSchedule.Clear();
        SchedulePanel.Children.Clear();

        try
        {
            await using var connection = Database.GetConnection();
            await connection.OpenAsync();

            const string sql = """
                SELECT 
                    r.id_raspisanie,
                    r.id_uslugi,
                    r.id_trainer,
                    r.date_training,
                    r.time_start,
                    r.time_end,
                    r.max_people,
                    u.name_uslugi,
                    u.description_uslugi,
                    u.duration_minutes,
                    COALESCE(r.price_training, u.price_uslugi) AS price_uslugi,
                    u.type_uslugi,
                    trainer.full_name AS trainer_name,
                    COALESCE(COUNT(z.id_zapisi), 0) AS busy_places
                FROM raspisanie r
                JOIN uslugi u ON u.id_uslugi = r.id_uslugi
                LEFT JOIN users trainer ON trainer.id_user = r.id_trainer
                LEFT JOIN zapisi z ON z.id_raspisanie = r.id_raspisanie
                              AND z.status_zapisi != 'cancelled'
                WHERE r.date_training >= CURRENT_DATE
                GROUP BY 
                    r.id_raspisanie,
                    r.id_uslugi,
                    r.id_trainer,
                    r.date_training,
                    r.time_start,
                    r.time_end,
                    r.max_people,
                    u.name_uslugi,
                    u.description_uslugi,
                    u.duration_minutes,
                    r.price_training,
                    u.price_uslugi,
                    u.type_uslugi,
                    trainer.full_name
                ORDER BY r.date_training, r.time_start
                """;

            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var item = new ScheduleItem
                {
                    Id = Convert.ToInt32(reader["id_raspisanie"]),
                    UslugiId = Convert.ToInt32(reader["id_uslugi"]),
                    TrainerId = Convert.ToInt32(reader["id_trainer"]),
                    Name = reader["name_uslugi"]?.ToString() ?? "",
                    Description = reader["description_uslugi"]?.ToString() ?? "",
                    DurationMinutes = Convert.ToInt32(reader["duration_minutes"]),
                    Price = Convert.ToDecimal(reader["price_uslugi"]),
                    Type = reader["type_uslugi"]?.ToString() ?? "",
                    TrainerName = reader["trainer_name"]?.ToString() ?? "Тренер не указан",
                    Date = Convert.ToDateTime(reader["date_training"]),
                    StartTime = (TimeSpan)reader["time_start"],
                    EndTime = (TimeSpan)reader["time_end"],
                    MaxPeople = Convert.ToInt32(reader["max_people"]),
                    BusyPlaces = Convert.ToInt32(reader["busy_places"])
                };

                _allSchedule.Add(item);
            }

            ShowSchedule(_allSchedule);
        }
        catch (Exception ex)
        {
            ShowMessage("Ошибка загрузки расписания: " + ex.Message, true);
        }
    }

    private void ShowSchedule(List<ScheduleItem> schedule)
    {
        SchedulePanel.Children.Clear();

        if (schedule.Count == 0)
        {
            SchedulePanel.Children.Add(CreateEmptyCard());
            return;
        }

        foreach (var item in schedule)
            SchedulePanel.Children.Add(CreateScheduleCard(item));
    }

    private Border CreateScheduleCard(ScheduleItem item)
    {
        int freePlaces = item.MaxPeople - item.BusyPlaces;
        if (freePlaces < 0)
            freePlaces = 0;

        var root = new Border
        {
            Background = Brushes.White,
            BorderBrush = Brush.Parse("#E0E0E0"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(26),
            Padding = new Thickness(26)
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,260"),
            ColumnSpacing = 24
        };

        var left = new StackPanel { Spacing = 10 };

        left.Children.Add(new TextBlock
        {
            Text = item.Name,
            Foreground = Brush.Parse("#141414"),
            FontSize = 30,
            FontWeight = FontWeight.Bold,
            TextWrapping = TextWrapping.Wrap
        });

        left.Children.Add(new TextBlock
        {
            Text = GetTypeText(item.Type),
            Foreground = Brush.Parse("#E32626"),
            FontSize = 22,
            FontWeight = FontWeight.Bold,
            TextWrapping = TextWrapping.Wrap
        });

        left.Children.Add(new TextBlock
        {
            Text = $"Тренер: {item.TrainerName}",
            Foreground = Brush.Parse("#6F6F6F"),
            FontSize = 21,
            TextWrapping = TextWrapping.Wrap
        });

        left.Children.Add(new TextBlock
        {
            Text = $"Дата: {item.Date:dd.MM.yyyy}     Время: {item.StartTime:hh\\:mm} — {item.EndTime:hh\\:mm}",
            Foreground = Brush.Parse("#6F6F6F"),
            FontSize = 21,
            TextWrapping = TextWrapping.Wrap
        });

        left.Children.Add(new TextBlock
        {
            Text = $"Длительность: {item.DurationMinutes} минут",
            Foreground = Brush.Parse("#6F6F6F"),
            FontSize = 21,
            TextWrapping = TextWrapping.Wrap
        });

        left.Children.Add(new TextBlock
        {
            Text = item.Description,
            Foreground = Brush.Parse("#6F6F6F"),
            FontSize = 20,
            TextWrapping = TextWrapping.Wrap
        });

        Grid.SetColumn(left, 0);
        grid.Children.Add(left);

        var rightBorder = new Border
        {
            Background = Brush.Parse("#F7F7F7"),
            BorderBrush = Brush.Parse("#E0E0E0"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(24),
            Padding = new Thickness(22),
            VerticalAlignment = VerticalAlignment.Center
        };

        var right = new StackPanel { Spacing = 12 };

        right.Children.Add(new TextBlock
        {
            Text = $"{item.Price:0.00} ₽",
            Foreground = Brush.Parse("#141414"),
            FontSize = 32,
            FontWeight = FontWeight.Bold,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        right.Children.Add(new TextBlock
        {
            Text = $"Свободно мест: {freePlaces}",
            Foreground = freePlaces > 0 ? Brush.Parse("#27AE60") : Brush.Parse("#E32626"),
            FontSize = 19,
            FontWeight = FontWeight.Bold,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        var button = new Button
        {
            Content = freePlaces > 0 ? "ЗАПИСАТЬСЯ" : "МЕСТ НЕТ",
            Height = 56,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Background = freePlaces > 0 ? Brush.Parse("#E32626") : Brush.Parse("#8A8A8A"),
            Foreground = Brushes.White,
            BorderBrush = freePlaces > 0 ? Brush.Parse("#E32626") : Brush.Parse("#8A8A8A"),
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(18),
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Tag = item,
            IsEnabled = freePlaces > 0
        };

        button.Click += SignUpTraining;

        right.Children.Add(button);
        rightBorder.Child = right;

        Grid.SetColumn(rightBorder, 1);
        grid.Children.Add(rightBorder);

        root.Child = grid;
        return root;
    }

    private Border CreateEmptyCard()
    {
        return new Border
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
                        Text = "Занятий не найдено",
                        Foreground = Brush.Parse("#141414"),
                        FontSize = 28,
                        FontWeight = FontWeight.Bold,
                        TextWrapping = TextWrapping.Wrap
                    },
                    new TextBlock
                    {
                        Text = "Попробуйте изменить фильтры или выберите другую дату.",
                        Foreground = Brush.Parse("#6F6F6F"),
                        FontSize = 20,
                        TextWrapping = TextWrapping.Wrap
                    }
                }
            }
        };
    }

    private string GetTypeText(string type)
    {
        return type switch
        {
            "group" => "Групповое занятие",
            "personal" => "Персональное занятие",
            "recovery" => "Восстановление",
            _ => "Занятие"
        };
    }

    private void ApplyFilters(object? sender, RoutedEventArgs e)
    {
        var filtered = new List<ScheduleItem>(_allSchedule);

        if (TypeComboBox.SelectedItem is ComboBoxItem typeItem)
        {
            string selectedType = typeItem.Content?.ToString() ?? "Все типы";

            if (selectedType == "Групповые")
                filtered = filtered.FindAll(x => x.Type == "group");

            if (selectedType == "Персональные")
                filtered = filtered.FindAll(x => x.Type == "personal");

            if (selectedType == "Восстановительные")
                filtered = filtered.FindAll(x => x.Type == "recovery");
        }

        if (DateFilterPicker.SelectedDate.HasValue)
        {
            DateTime selectedDate = DateFilterPicker.SelectedDate.Value.Date;
            filtered = filtered.FindAll(x => x.Date.Date == selectedDate);
        }

        if (TrainerComboBox.SelectedItem is ComboBoxItem trainerItem)
        {
            int trainerId = Convert.ToInt32(trainerItem.Tag);

            if (trainerId != 0)
                filtered = filtered.FindAll(x => x.TrainerId == trainerId);
        }

        ShowSchedule(filtered);
    }

    private void ClearFilters(object? sender, RoutedEventArgs e)
    {
        TypeComboBox.SelectedIndex = 0;
        DateFilterPicker.SelectedDate = null;
        TrainerComboBox.SelectedIndex = 0;
        ShowSchedule(_allSchedule);
    }

    private async void SignUpTraining(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not ScheduleItem item)
            return;

        try
        {
            await using var connection = Database.GetConnection();
            await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync();

            const string checkSql = """
                SELECT COUNT(*)
                FROM zapisi
                WHERE id_user = @id_user
                  AND id_raspisanie = @id_raspisanie
                  AND status_zapisi != 'cancelled'
                """;

            await using var checkCommand = new NpgsqlCommand(checkSql, connection, transaction);
            checkCommand.Parameters.AddWithValue("@id_user", _session.UserId);
            checkCommand.Parameters.AddWithValue("@id_raspisanie", item.Id);

            int exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());

            if (exists > 0)
            {
                await transaction.RollbackAsync();
                ShowMessage("Вы уже записаны на это занятие.", true);
                return;
            }

            const string placesSql = """
                SELECT 
                    r.max_people,
                    COALESCE(COUNT(z.id_zapisi), 0) AS busy_places
                FROM raspisanie r
                LEFT JOIN zapisi z ON z.id_raspisanie = r.id_raspisanie
                              AND z.status_zapisi != 'cancelled'
                WHERE r.id_raspisanie = @id_raspisanie
                GROUP BY r.max_people
                """;

            await using var placesCommand = new NpgsqlCommand(placesSql, connection, transaction);
            placesCommand.Parameters.AddWithValue("@id_raspisanie", item.Id);

            await using var placesReader = await placesCommand.ExecuteReaderAsync();

            if (!await placesReader.ReadAsync())
            {
                await placesReader.CloseAsync();
                await transaction.RollbackAsync();
                ShowMessage("Занятие не найдено.", true);
                return;
            }

            int maxPeople = Convert.ToInt32(placesReader["max_people"]);
            int busyPlaces = Convert.ToInt32(placesReader["busy_places"]);
            await placesReader.CloseAsync();

            if (busyPlaces >= maxPeople)
            {
                await transaction.RollbackAsync();
                ShowMessage("На это занятие уже нет свободных мест.", true);
                return;
            }

            decimal priceToPay = item.Price;

            if (item.Type != "personal")
            {
                const string abonementSql = """
                    SELECT COUNT(*)
                    FROM user_abonements
                    WHERE id_user = @id_user
                      AND status_abonement = 'active'
                      AND end_date >= CURRENT_DATE
                    """;

                await using var abonementCommand = new NpgsqlCommand(abonementSql, connection, transaction);
                abonementCommand.Parameters.AddWithValue("@id_user", _session.UserId);

                int activeAbonementCount = Convert.ToInt32(await abonementCommand.ExecuteScalarAsync());

                if (activeAbonementCount > 0)
                    priceToPay = 0;
            }

            const string balanceSql = """
                SELECT balance
                FROM users
                WHERE id_user = @id_user
                LIMIT 1
                """;

            await using var balanceCommand = new NpgsqlCommand(balanceSql, connection, transaction);
            balanceCommand.Parameters.AddWithValue("@id_user", _session.UserId);

            var balanceObject = await balanceCommand.ExecuteScalarAsync();
            decimal currentBalance = balanceObject == null ? 0 : Convert.ToDecimal(balanceObject);

            if (currentBalance < priceToPay)
            {
                await transaction.RollbackAsync();
                ShowMessage($"Недостаточно средств. Нужно: {priceToPay:0.00} ₽, на балансе: {currentBalance:0.00} ₽.", true);
                return;
            }

            if (priceToPay > 0)
            {
                const string updateBalanceSql = """
                    UPDATE users
                    SET balance = balance - @price
                    WHERE id_user = @id_user
                    """;

                await using var updateBalanceCommand = new NpgsqlCommand(updateBalanceSql, connection, transaction);
                updateBalanceCommand.Parameters.AddWithValue("@price", priceToPay);
                updateBalanceCommand.Parameters.AddWithValue("@id_user", _session.UserId);
                await updateBalanceCommand.ExecuteNonQueryAsync();
            }

            const string insertSql = """
                INSERT INTO zapisi (id_user, id_raspisanie, status_zapisi, date_zapisi)
                VALUES (@id_user, @id_raspisanie, 'active', NOW())
                """;

            await using var insertCommand = new NpgsqlCommand(insertSql, connection, transaction);
            insertCommand.Parameters.AddWithValue("@id_user", _session.UserId);
            insertCommand.Parameters.AddWithValue("@id_raspisanie", item.Id);
            await insertCommand.ExecuteNonQueryAsync();

            await transaction.CommitAsync();

            if (priceToPay == 0)
                ShowMessage("Вы успешно записались на занятие. Абонемент применён, списано: 0 ₽.", false);
            else
                ShowMessage($"Вы успешно записались на занятие. Списано: {priceToPay:0.00} ₽.", false);

            await LoadScheduleAsync();

            if (!string.IsNullOrWhiteSpace(_selectedTrainerName))
                ApplyFilters(null, new RoutedEventArgs());
        }
        catch (Exception ex)
        {
            ShowMessage("Ошибка записи: " + ex.Message, true);
        }
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

    private void OpenMainClubPage(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenUserMainClubPage();
    }

    private void OpenNewsPage(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenUserNews();
    }

    private void OpenMyTrainingsPage(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenUserMyTrainings();
    }

    private void OpenProfilePage(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenUserProfile();
    }

    private class ScheduleItem
    {
        public int Id { get; set; }
        public int UslugiId { get; set; }
        public int TrainerId { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int DurationMinutes { get; set; }
        public decimal Price { get; set; }
        public string Type { get; set; } = "";
        public string TrainerName { get; set; } = "";
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int MaxPeople { get; set; }
        public int BusyPlaces { get; set; }
    }
}