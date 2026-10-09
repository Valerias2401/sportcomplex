using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Npgsql;
using Pulse.Core;

namespace Pulse.User;

public partial class AdminHomeView : UserControl
{
    private readonly MainWindow? _mainWindow;
    private readonly SessionData _session;

    public AdminHomeView()
    {
        InitializeComponent();
        _session = new SessionData();
        FillDemoData();
    }

    public AdminHomeView(MainWindow mainWindow, SessionData session)
    {
        InitializeComponent();

        _mainWindow = mainWindow;
        _session = session;

        FillPageData();
        LoadAdminHomeInfo();
    }

    private void FillDemoData()
    {
        WelcomeText.Text = "Добро пожаловать!";
        DateText.Text = $"Сегодня {DateTime.Now:dd.MM.yyyy}";
        TodayRecordsCountText.Text = "0";
        TodayRecordsInfoText.Text = "Сегодня пока нет записей клиентов.";
        FreeTimesCountText.Text = "0";
        FreeTimesInfoText.Text = "Нет свободного времени тренеров.";
    }

    private void FillPageData()
    {
        WelcomeText.Text = $"Добро пожаловать,\n{_session.FullName}!";
        DateText.Text = $"Сегодня {DateTime.Now:dd.MM.yyyy}";
        TodayRecordsCountText.Text = "0";
        TodayRecordsInfoText.Text = "Сегодня пока нет записей клиентов.";
        FreeTimesCountText.Text = "0";
        FreeTimesInfoText.Text = "Нет свободного времени тренеров.";
    }

    private async void LoadAdminHomeInfo()
    {
        try
        {
            await using var connection = Database.GetConnection();
            await connection.OpenAsync();

            const string todayRecordsSql = """
                SELECT COUNT(*)
                FROM zapisi z
                JOIN raspisanie r ON r.id_raspisanie = z.id_raspisanie
                WHERE r.date_training = CURRENT_DATE
                  AND z.status_zapisi = 'active'
                """;

            await using var todayCommand = new NpgsqlCommand(todayRecordsSql, connection);
            int todayRecordsCount = Convert.ToInt32(await todayCommand.ExecuteScalarAsync());

            TodayRecordsCountText.Text = todayRecordsCount.ToString();

            if (todayRecordsCount == 0)
                TodayRecordsInfoText.Text = "Сегодня пока нет активных записей клиентов.";
            else
                TodayRecordsInfoText.Text = $"На сегодня есть активные записи клиентов: {todayRecordsCount}.";

            const string freeTimesSql = """
                SELECT COUNT(*)
                FROM trainer_available_times
                WHERE status_time = 'free'
                  AND available_date >= CURRENT_DATE
                """;

            await using var freeTimesCommand = new NpgsqlCommand(freeTimesSql, connection);
            int freeTimesCount = Convert.ToInt32(await freeTimesCommand.ExecuteScalarAsync());

            FreeTimesCountText.Text = freeTimesCount.ToString();

            if (freeTimesCount == 0)
                FreeTimesInfoText.Text = "Нет свободного времени тренеров для создания занятий.";
            else
                FreeTimesInfoText.Text = $"Есть свободные интервалы тренеров: {freeTimesCount}.";
        }
        catch (Exception ex)
        {
            TodayRecordsInfoText.Text = "Ошибка загрузки данных: " + ex.Message;
            FreeTimesInfoText.Text = "Ошибка загрузки данных.";
        }
    }

    private void OpenSchedulePage(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenAdminSchedule();
    }

    private void OpenTrainerTimesPage(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenAdminTrainerTimes();
    }

    private void OpenClientsPage(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenAdminClients();
    }

    private void OpenProfilePage(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.OpenAdminProfile();
    }
}