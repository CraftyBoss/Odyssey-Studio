using FluentFTP;
using System.IO;
using System;
using System.Threading.Tasks;

namespace RedStarLibrary.UI;
public class FtpInfo
{
    public string addr;
    public string user;
    public string pass;
    public int port;
}
public class SwitchFileUploader
{
    public bool IsConnected => ftpClient != null && ftpClient.IsConnected;
    public bool IsUploading => asyncUploadTask != null && !asyncUploadTask.IsCompleted;
    public float UploadProgress { get; private set; } = 0.0f;
    public string WorkingDir { get; set; } = "";
    private string TitleID { get; set; } = "0100000000010000";
    private string AtmosFolder => $"atmosphere\\contents";
    private string FsFolder { get; set; } = "romfs";
    private string FullPath { get { return Path.Combine(AtmosFolder, TitleID, FsFolder, WorkingDir); } }

    private FtpClient ftpClient;

    private Task asyncUploadTask;

    public SwitchFileUploader(string titleID, string fsFolder, FtpInfo info)
    {
        TitleID = titleID;

        FsFolder = fsFolder;

        ftpClient = new FtpClient(info.addr, info.user, info.pass, info.port);

        ftpClient.AutoConnect();

    }

    public SwitchFileUploader(FtpInfo info)
    {
        ftpClient = new FtpClient(info.addr, info.user, info.pass, info.port);

        ftpClient.AutoConnect();
    }

    public SwitchFileUploader()
    {
        ftpClient = new FtpClient();
    }

    public void ConnectToServer(FtpInfo info)
    {
        ftpClient.Host = info.addr;
        ftpClient.Port = info.port;
        ftpClient.Credentials.UserName = info.user;
        ftpClient.Credentials.Password = info.pass;

        ftpClient.AutoConnect();
    }

    public void DisconnectFromServer()
    {
        if (IsConnected)
            ftpClient.Disconnect();
    }

    public void UploadFileToServer(byte[] data, string fileName, Action<FtpProgress> progress = null)
    {
        Console.WriteLine($"Uploading {fileName} to Switch.");
        Console.WriteLine("Full Path: " + FullPath);

        var status = ftpClient.UploadBytes(data, $"{FullPath}\\{fileName}", FtpRemoteExists.Overwrite, true, progress);
        Console.WriteLine();

        Console.WriteLine("Done. Status: " + status.ToString());
    }

    public void UploadFileToServerAsync(byte[] data, string fileName)
    {
        Console.WriteLine($"Async Uploading {fileName} to Switch.");
        Console.WriteLine("Full Path: " + FullPath);

        asyncUploadTask = Task.Run(async () =>
        {
            Action<FtpProgress> progress = delegate (FtpProgress p) {
                if (p.Progress != 1)
                    UploadProgress = (float)(p.Progress / 100.0f);
            };

            ftpClient.UploadBytes(data, $"{FullPath}\\{fileName}", FtpRemoteExists.Overwrite, true, progress);

            UploadProgress = 0.0f;
        });
    }
};