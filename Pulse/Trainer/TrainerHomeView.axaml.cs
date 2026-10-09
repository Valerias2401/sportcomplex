using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Npgsql;
using Pulse.Core;

namespace Pulse.User;

public partial class TrainerHomeView : UserControl
{
    private readonly MainWindow? _mainWindow;
    private readonly SessionData _session;

    public TrainerHomeView()
    {
        InitializeComponent();
        _session = new SessionData();
        FillDemoData();
    }

    public TrainerHomeView(MainWindow mainWindow, SessionData session)
    {
        InitializeComponent();

        _mainWindow = mainWindow;
        _session = session;

        FillPageData();
        LoadTrainerHomeInfo();
    }

    private void FillDemoData()
    {
        WelcomeText.Text = "Добро пожаловать!";
        DateText.Text = $"Сегодня {DateTime.Now:dd.MM.yyyy}";
        NearestTrainingTitleText.Text = "Пока нет ближайших занятий";
        NearestTrainingInfoText.Text = "Занятия появятся после составления расписания администратором.";
        AvailableTimeTitleText.Text = "Свободное время";
        AvailableTimeInfoText.Text = "Добавьте свободные часы, чтобы администратор мог поставить занятия.";
    }

    private void FillPageData()
    {
        WelcomeText.Text = $"Добро пожаловать,\n{_session.FullName}!";
        DateText.Text = $"Сегодня {DateTime.Now:dd.MM.yyyy}";
        NearestTrainingTitleText.Text = "Пока нет ближайших занятий";
        NearestTrainingInfoText.Text = "Занятия появятся после составления расписания администратором.";
        AvailableTimeTitleText.Text = "Свободное время";
        AvailableTimeInfoText.Text = "Добавьте свободные часы, чтобы администратор мог поставить занятия.";
    }

    private async void LoadTrainerHomeInfo()
    {
        try
        {
            await using var connection = Database.GetConnection();
            await connection.OpenAsync();

            const string nearestSql = """
                SELECT 
                    u.name_uslugi,
                    r.date_training,
                    r.time_start,
                    r.time_end,
                    r.max_people,
                    COALESCE(COUNT(z.id_zapisi), 0) AS busy_places
                FROM raspisanie r
                JOIN uslugi u ON u.id_uslugi = r.id_uslugi
                LEFT JOIN zapisi z ON z.id_raspisanie = r.id_raspisanie
                              AND z.status_zapisi != 'cancelled'
                WHERE r.id_trainer = @id_trainer
                  AND r.date_training >= CURRENT_DATE
                GROUP BY 
                    u.name_uslugi,
                    r.date_training,
                    r.time_start,
                    r.time_end,
                    r.max_people
                ORDER BY r.date_training, r.time_start
                LIMIT 1
                """;

            await using var nearestCommand = new NpgsqlCommand(nearestSql, connection);
            nearestCommand.Parameters.AddWithValue("@id_trainer", _session.UserId);

            await using var nearestReader = await nearestCommand.ExecuteReaderAsync();

            if (await nearestReader.ReadAsync())
            {
                string name = nearestReader["name_uslugi"]?.ToString() ?? "Занятие";
                DateTime date = Convert.ToDateTime(nearestReader["date_training"]);
                TimeSpan start = (TimeSpan)nearestReader["time_start"];
                TimeSpan end = (TimeSpan)nearestReader["time_end"];
                int maxPeople = Convert.ToInt32(nearestReader["max_people"]);
                int busyPlaces = Convert.ToInt32(nearestReader["busy_places"]);

                NearestTrainingTitleText.Text = name;
                NearestTrainingInfoText.Text =
                    $"Дата: {date:dd.MM.yyyy}\n" +
                    $"Время: {start:hh\\:mm} — {end:hh\\:mm}\n" +
                    $"Записано клиентов: {busyPlaces} из {maxPeople}";
            }

            await nearestReader.CloseAsync();

            const string availableSql = """
                SELECT available_date, time_start, time_end
                FROM trainer_available_times
                WHERE id_trainer = @id_trainer
                  AND status_time = 'free'
                  AND available_date >= CURRENT_DATE
                ORDER BY available_date, time_start
                LIMIT 1
                """;

            await using var availableCommand = new NpgsqlCommand(availableSql, connection);
            availableCommand.Parameters.AddWithValue("@id_trainer", _session.UserId);

            await using var availableReader = await availableCommand.ExecuteReaderAsync();

            if (await availableReader.ReadAsync())
            {
                DateTime date = Convert.ToDateTime(availableReader["available_date"]);
                TimeSpan start = (TimeSpan)availableReader["time_start"];
                TimeSpan end = (TimeSpan)availableReader["time_end"];

                AvailableTimeTitleText.Text = "Ближайшее свободное время";
                AvailableTimeInfoText.Text =
                    $"Дата: {date:dd.MM.yyyy}\n" +
                    $"Время: {start:hh\\:mm} — {end:hh\\:mm}\n" +
                    "Статус: свободно";
            }
        }
        catch (Exception ex)
        {
            NearestTrainingTitleText.Text = "Ошибка загрузки";
            NearestTrainingInfoText.Text = ex.Message;
        }
    }

    private void OpenSchedulePage(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenTrainerSchedule();
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