using Renci.SshNet;

namespace SyncFiles;

internal class BackupClient
{
    readonly string backupServer;
    readonly int backupPort;
    readonly string username;
    readonly string password;
    readonly string remoteWorkingDirectory;
    readonly string localWorkingDirectory;
    readonly string defaultHomeDirectory = "/mnt";
    readonly SftpClient sftpClient;

    string remoteUserProfilePath = "";

    public BackupClient(string BackupServer, int BackupPort, string Username, string Password, string LocalWorkingDirectory, string RemoteWorkingDirectory)
    {
        backupServer = BackupServer;
        backupPort = BackupPort;
        username = Username;
        password = Password;
        localWorkingDirectory = LocalWorkingDirectory;
        remoteWorkingDirectory = RemoteWorkingDirectory;

        sftpClient = new SftpClient(backupServer, backupPort, username, password);
    }

    public bool DisplaySkippedFiles { get; set; } = true;

    public bool Connect()
    {
        try
        {
            sftpClient.Connect();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void CreateBackup(string PathToLocalDirectory)
    {
        if (string.IsNullOrWhiteSpace(remoteUserProfilePath))
        {
            CreateUserProfileDirectory();
        }

        CreateRemoteDirectoryStructure(PathToLocalDirectory);

        string[] files = Directory.GetFiles(PathToLocalDirectory, "*", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            DateTime localModifiedDateTime = DateTime.Parse(File.GetLastWriteTime(file).ToString("G"));

            string remoteFilePath = ConvertLocalFilePathToRemoteFilePath(file);

            if (sftpClient.Exists(remoteFilePath))
            {
                DateTime remoteModifiedDateTime = DateTime.Parse(sftpClient.GetLastWriteTime(remoteFilePath).ToString("G"));

                if (localModifiedDateTime <= remoteModifiedDateTime)
                {
                    if (DisplaySkippedFiles)
                    {
                        // Display skipped files if the property is set to true
                        Spectre.Console.AnsiConsole.MarkupLine($"[yellow bold]Skipping (already current): {file}[/]");
                    }
                    continue; // Skip if the file is not modified
                }
            }

            try
            {
                using (FileStream fileToUpload = File.OpenRead(file))
                {
                    sftpClient.UploadFile(File.OpenRead(file), remoteFilePath);
                    sftpClient.SetLastWriteTime(remoteFilePath, localModifiedDateTime);
                    Spectre.Console.AnsiConsole.MarkupLine($"[green bold]Uploaded local file: {file}[/]");
                    Spectre.Console.AnsiConsole.MarkupLine($"[green bold]Uploaded remote file: {remoteFilePath}[/]");
                }
            }
            catch (Exception ex)
            {
                Spectre.Console.AnsiConsole.MarkupLine($"[red bold]Failed: {file}[/]");
                Spectre.Console.AnsiConsole.MarkupLine($"[red bold]Error: {ex.Message}[/]");
            }

            Spectre.Console.AnsiConsole.WriteLine();
        }
    }

    public void CreateRemoteDirectoryStructure(string PathToLocalDirectory)
    {
        if (!sftpClient.Exists(ConvertLocalFolderPathToRemoteFolderPath(PathToLocalDirectory)))
        {
            sftpClient.CreateDirectory(ConvertLocalFolderPathToRemoteFolderPath(PathToLocalDirectory));
            sftpClient.SetLastWriteTime(ConvertLocalFolderPathToRemoteFolderPath(PathToLocalDirectory), Directory.GetLastWriteTime(PathToLocalDirectory));
        }

        string[] directories = Directory.GetDirectories(PathToLocalDirectory, "*", SearchOption.AllDirectories);
        foreach (string directory in directories)
        {
            string path = ConvertLocalFolderPathToRemoteFolderPath(directory);

            // Consistent forward slashes
            path = path.Replace(@"\", "/");
            foreach (string dir in path.Split('/'))
            {
                if (string.IsNullOrWhiteSpace(dir))
                {
                    continue; // Ignoring leading/ending/multiple slashes
                }
                if (sftpClient.WorkingDirectory.Equals($"/{dir}"))
                {
                    continue; // Skip if the directory is the current working directory
                }

                if (!sftpClient.Exists(dir))
                {
                    sftpClient.CreateDirectory(dir);
                }
                sftpClient.ChangeDirectory(dir);
            }
            // Going back to default directory
            sftpClient.ChangeDirectory(defaultHomeDirectory);
        }
    }

    public void RestoreBackup(string PathToRemoteDirectory)
    {
        CreateLocalDirectoryStructure(PathToRemoteDirectory);

        string localFolderPath = ConvertRemoteFolderPathToLocalFolderPath(PathToRemoteDirectory);

        string[] files = RecursivelyRetrieveSftpFiles(PathToRemoteDirectory);
            //.Where(f => !f.IsDirectory && f.Name != "." && f.Name != "..")
            //.Select(f => f.FullName)
            //.ToArray();

        foreach (string file in files)
        {
            DateTime remoteModifiedDateTime = DateTime.Parse(sftpClient.GetLastWriteTime(file).ToString("G"));

            string localFilePath = ConvertRemoteFilePathToLocalFilePath(file);

            if (File.Exists(localFilePath))
            {
                DateTime localModifiedDateTime = DateTime.Parse(File.GetLastWriteTime(localFilePath).ToString("G"));
                if (localModifiedDateTime >= remoteModifiedDateTime)
                {
                    if (DisplaySkippedFiles)
                    {
                        // Display skipped files if the property is set to true
                        Spectre.Console.AnsiConsole.MarkupLine($"[yellow bold]Skipping (already current): {file}[/]");
                    }
                    continue; // Skip if the file is not modified
                }
            }
            try
            {
                using (FileStream fileToDownload = File.Create(localFilePath))
                {
                    sftpClient.DownloadFile(file, fileToDownload);
                    sftpClient.SetLastWriteTime(file, remoteModifiedDateTime);
                    Spectre.Console.AnsiConsole.MarkupLine($"[green bold]Downloaded remote file: {file}[/]");
                    Spectre.Console.AnsiConsole.MarkupLine($"[green bold]Saved locally: {localFilePath}[/]");
                }
            }
            catch (Exception ex)
            {
                Spectre.Console.AnsiConsole.MarkupLine($"[red bold]Failed: {file}[/]");
                Spectre.Console.AnsiConsole.MarkupLine($"[red bold]Error: {ex.Message}[/]");
            }
            Spectre.Console.AnsiConsole.WriteLine();
        }
    }

    public void CreateLocalDirectoryStructure(string PathToRemoteDirectory)
    {
        string localFolderPath = ConvertRemoteFolderPathToLocalFolderPath(PathToRemoteDirectory);
        if (!Directory.Exists(localFolderPath))
        {
            Directory.CreateDirectory(localFolderPath);
        }
        string[] directories = sftpClient.ListDirectory(PathToRemoteDirectory).Where(d => d.IsDirectory && d.Name != "." && d.Name != "..").Select(d => d.FullName).ToArray();
        foreach (string directory in directories)
        {
            CreateLocalDirectoryStructure(directory);
        }
    }

    private string[] RecursivelyRetrieveSftpDirectories(string PathToRemoteDirectory)
    {
        var directories = new List<string>();
        var items = sftpClient.ListDirectory(PathToRemoteDirectory);
        foreach (var item in items)
        {
            if (item.IsDirectory && item.Name != "." && item.Name != "..")
            {
                directories.Add(item.FullName);
                directories.AddRange(RecursivelyRetrieveSftpDirectories(item.FullName));
            }
        }
        return directories.ToArray();
    }

    private string[] RecursivelyRetrieveSftpFiles(string PathToRemoteDirectory)
    {
        var files = new List<string>();
        var items = sftpClient.ListDirectory(PathToRemoteDirectory);
        foreach (var item in items)
        {
            if (!item.IsDirectory && item.Name != "." && item.Name != "..")
            {
                files.Add(item.FullName);
            }
            else if (item.IsDirectory && item.Name != "." && item.Name != "..")
            {
                files.AddRange(RecursivelyRetrieveSftpFiles(item.FullName));
            }
        }
        return files.ToArray();
    }

    private void CreateUserProfileDirectory()
    {
        if (!sftpClient.Exists(remoteWorkingDirectory))
        {
            sftpClient.CreateDirectory(remoteWorkingDirectory);
        }
        sftpClient.ChangeDirectory(remoteWorkingDirectory);
        remoteUserProfilePath = sftpClient.WorkingDirectory;
        sftpClient.ChangeDirectory("/mnt");
    }

    private string ConvertLocalFilePathToRemoteFilePath(string PathToLocalFile)
    {
        char driveLetter = localWorkingDirectory[0];

        string remoteFilePath = PathToLocalFile.Trim().Replace("\\", "/");
        return remoteFilePath.Replace($"{driveLetter}:/Users/{username}/", remoteWorkingDirectory);
    }

    private string ConvertLocalFolderPathToRemoteFolderPath(string PathToLocalFolder)
    {
        char driveLetter = localWorkingDirectory[0];

        string remoteFolderPath = PathToLocalFolder.Trim().Replace("\\", "/");
        return remoteFolderPath.Replace($"{driveLetter}:/Users/{username}/", remoteWorkingDirectory);
    }

    private string ConvertRemoteFilePathToLocalFilePath(string PathToRemoteFile)
    {
        char driveLetter = localWorkingDirectory[0];

        return PathToRemoteFile.Trim().Replace(remoteWorkingDirectory, $"{driveLetter}:\\Users\\{username}\\").Replace("/", "\\");
    }

    private string ConvertRemoteFolderPathToLocalFolderPath(string PathToRemoteFolder)
    {
        char driveLetter = localWorkingDirectory[0];

        return PathToRemoteFolder.Trim().Replace(remoteWorkingDirectory, $"{driveLetter}:\\Users\\{username}\\").Replace("/", "\\");
    }
}
