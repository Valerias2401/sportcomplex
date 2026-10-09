using System;
using System.IO;
using System.Text.Json;

namespace Pulse.Core;

public static class SessionService
{
    private static readonly string FolderPath =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Pulse");

    private static readonly string FilePath =
        Path.Combine(FolderPath, "session.json");

    public static void SaveSession(SessionData session)
    {
        if (!Directory.Exists(FolderPath))
            Directory.CreateDirectory(FolderPath);

        string json = JsonSerializer.Serialize(session, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(FilePath, json);
    }

    public static SessionData? LoadSession()
    {
        try
        {
            if (!File.Exists(FilePath))
                return null;

            string json = File.ReadAllText(FilePath);

            if (string.IsNullOrWhiteSpace(json))
                return null;

            return JsonSerializer.Deserialize<SessionData>(json);
        }
        catch
        {
            return null;
        }
    }

    public static void ClearSession()
    {
        if (File.Exists(FilePath))
            File.Delete(FilePath);
    }
}