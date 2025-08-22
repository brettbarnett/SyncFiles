using SyncFiles;

string version = "1.0.0";

string ftpUser = "FtpUsername";
string ftpPassword = "FtpPassword";
string ftpServer = "FtpServerAddress";
int ftpPort = 22;

string localUser = Environment.UserName;
string localUserDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
string remoteUserDirectory = $"/mnt/array1/Backup/{localUser}/";

Spectre.Console.AnsiConsole.WriteLine("Welcome to the User Profile Backup Utility");
Spectre.Console.AnsiConsole.MarkupLine($"Version: [bold underline green]{version}[/]");

BackupClient backupClient = new BackupClient(ftpServer, ftpPort, ftpUser, ftpPassword, localUserDirectory, remoteUserDirectory);
if (!backupClient.Connect())
{
    Console.WriteLine("Failed to connect to the backup server.");
    return;
}

Console.WriteLine("1. Backup User Profile");
Console.WriteLine("2. Restore User Profile");
Console.WriteLine("3. Exit");
Console.Write("Please select an option (1-3): ");
string? choice = Console.ReadLine();
switch (choice)
{
    case "1":
        BackupUserProfile();
        break;
    case "2":
        RestoreUserProfile();
        break;
    case "3":
        Console.WriteLine("Exiting the utility.");
        break;
    default:
        Console.WriteLine("Invalid choice. Please select 1, 2, or 3.");
        break;
}

void BackupUserProfile()
{
    string[] backupFolders =
    [
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
        Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
        Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\source"
    ];

    foreach (string folder in backupFolders)
    {
        backupClient.CreateBackup(folder);
    }
}

void RestoreUserProfile()
{
    backupClient.RestoreBackup(remoteUserDirectory);
}
