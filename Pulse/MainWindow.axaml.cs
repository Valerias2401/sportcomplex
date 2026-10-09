using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Pulse.Core;
using Pulse.User;

namespace Pulse;

public partial class MainWindow : Window
{
    private SessionData? _currentSession;

    public MainWindow()
    {
        InitializeComponent();
        TestConnection();
        TryAutoLogin();
    }

    private void TestConnection()
    {
        try
        {
            using var connection = Database.GetConnection();
            connection.Open();
            Console.WriteLine("✅ Подключение к БД успешно");
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Ошибка подключения к БД:");
            Console.WriteLine(ex.Message);
        }
    }

    private void TryAutoLogin()
    {
        var session = SessionService.LoadSession();

        if (session != null)
            OpenByRole(session);
    }

    private void OpenLoginView(object? sender, RoutedEventArgs e)
    {
        MainMenuPanel.IsVisible = false;
        ContentPanel.IsVisible = true;
        MainContent.Content = new LoginView(this);
    }

    private void OpenRegisterView(object? sender, RoutedEventArgs e)
    {
        MainMenuPanel.IsVisible = false;
        ContentPanel.IsVisible = true;
        MainContent.Content = new RegisterView(this);
    }

    public void OpenByRole(SessionData session)
    {
        _currentSession = session;
        MainMenuPanel.IsVisible = false;
        ContentPanel.IsVisible = true;

        if (session.RoleId == 1)
            MainContent.Content = new AdminHomeView(this, session);
        else if (session.RoleId == 2)
            MainContent.Content = new TrainerHomeView(this, session);
        else
            MainContent.Content = new UserHomeView(this, session);
    }

    public void OpenUserHome(SessionData session)
    {
        OpenByRole(session);
    }

    public void OpenUserMainClubPage()
    {
        if (_currentSession is null) return;

        MainMenuPanel.IsVisible = false;
        ContentPanel.IsVisible = true;
        MainContent.Content = new UserMainClubView(this, _currentSession);
    }

    public void OpenUserSchedule()
    {
        if (_currentSession is null) return;

        MainMenuPanel.IsVisible = false;
        ContentPanel.IsVisible = true;
        MainContent.Content = new UserScheduleView(this, _currentSession);
    }

    public void OpenUserSchedule(string trainerName)
    {
        if (_currentSession is null) return;

        MainMenuPanel.IsVisible = false;
        ContentPanel.IsVisible = true;
        MainContent.Content = new UserScheduleView(this, _currentSession, trainerName);
    }

    public void OpenUserNews()
    {
        if (_currentSession is null) return;

        MainMenuPanel.IsVisible = false;
        ContentPanel.IsVisible = true;
        MainContent.Content = new UserNewsView(this, _currentSession);
    }

    public void OpenNewsDetails(string title)
    {
        if (_currentSession is null) return;

        MainMenuPanel.IsVisible = false;
        ContentPanel.IsVisible = true;
        MainContent.Content = new UserNewsDetailsView(this, _currentSession, title);
    }

    public void OpenUserMyTrainings()
    {
        if (_currentSession is null) return;

        MainMenuPanel.IsVisible = false;
        ContentPanel.IsVisible = true;
        MainContent.Content = new UserMyTrainingsView(this, _currentSession);
    }

    public void OpenUserProfile()
    {
        if (_currentSession is null) return;

        MainMenuPanel.IsVisible = false;
        ContentPanel.IsVisible = true;
        MainContent.Content = new UserProfileView(this, _currentSession);
    }

    public void OpenEditProfile()
    {
        if (_currentSession is null) return;

        MainMenuPanel.IsVisible = false;
        ContentPanel.IsVisible = true;
        MainContent.Content = new UserEditProfileView(this, _currentSession);
    }

    public void OpenTrainerHome()
    {
        if (_currentSession is null) return;

        MainMenuPanel.IsVisible = false;
        ContentPanel.IsVisible = true;
        MainContent.Content = new TrainerHomeView(this, _currentSession);
    }

    public void OpenTrainerSchedule()
    {
        if (_currentSession is null) return;

        MainMenuPanel.IsVisible = false;
        ContentPanel.IsVisible = true;
        MainContent.Content = new TrainerScheduleView(this, _currentSession);
    }

    public void OpenTrainerClients()
    {
        if (_currentSession is null) return;

        MainMenuPanel.IsVisible = false;
        ContentPanel.IsVisible = true;
        MainContent.Content = new TrainerClientsView(this, _currentSession);
    }

    public void OpenTrainerAvailableTime()
    {
        if (_currentSession is null) return;

        MainMenuPanel.IsVisible = false;
        ContentPanel.IsVisible = true;
        MainContent.Content = new TrainerAvailableTimeView(this, _currentSession);
    }

    public void OpenTrainerProfile()
    {
        if (_currentSession is null) return;

        MainMenuPanel.IsVisible = false;
        ContentPanel.IsVisible = true;
        MainContent.Content = new TrainerProfileView(this, _currentSession);
    }

    public void OpenAdminHome()
    {
        if (_currentSession is null) return;

        MainMenuPanel.IsVisible = false;
        ContentPanel.IsVisible = true;
        MainContent.Content = new AdminHomeView(this, _currentSession);
    }

    public void OpenAdminSchedule()
    {
        if (_currentSession is null) return;

        MainMenuPanel.IsVisible = false;
        ContentPanel.IsVisible = true;
        MainContent.Content = new AdminScheduleView(this, _currentSession);
    }

    public void OpenAdminTrainerTimes()
    {
        if (_currentSession is null) return;

        MainMenuPanel.IsVisible = false;
        ContentPanel.IsVisible = true;
        MainContent.Content = new AdminTrainerTimesView(this, _currentSession);
    }

    public void OpenAdminClients()
    {
        if (_currentSession is null) return;

        MainMenuPanel.IsVisible = false;
        ContentPanel.IsVisible = true;
        MainContent.Content = new AdminClientsView(this, _currentSession);
    }

    public void OpenAdminProfile()
    {
        if (_currentSession is null) return;

        MainMenuPanel.IsVisible = false;
        ContentPanel.IsVisible = true;
        MainContent.Content = new AdminProfileView(this, _currentSession);
    }

    public void LogoutUser()
    {
        SessionService.ClearSession();
        _currentSession = null;

        MainContent.Content = null;
        ContentPanel.IsVisible = false;
        MainMenuPanel.IsVisible = true;
    }

    public void ShowMainMenu()
    {
        MainContent.Content = null;
        ContentPanel.IsVisible = false;
        MainMenuPanel.IsVisible = true;
    }
}