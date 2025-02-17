using FluentFTP;
using System.IO;
using System;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
    public bool IsConnecting => asyncConnectTask != null && !asyncConnectTask.IsCompleted;
    public bool IsWorkingDirAbsolute { get; set; } = false;
    public float UploadProgress { get; private set; } = 0.0f;
    public string WorkingDir { get; set; } = "";
    private string TitleID { get; set; } = "0100000000010000";
    private string AtmosFolder => "atmosphere\\contents";
    private string FsFolder { get; set; } = "romfs";
    private string FullPath { get { return IsWorkingDirAbsolute ? WorkingDir : Path.Combine(AtmosFolder, TitleID, FsFolder, WorkingDir); } }

    private FtpClient ftpClient;

    private Task asyncUploadTask;
    private Task asyncConnectTask;

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

    public void ConnectToServer(FtpInfo info, bool isAsync = false)
    {
        ftpClient.Host = info.addr;
        ftpClient.Port = info.port;
        ftpClient.Credentials.UserName = info.user;
        ftpClient.Credentials.Password = info.pass;

        if(isAsync)
            asyncConnectTask = Task.Run(ftpClient.AutoConnect);
        else
            ftpClient.AutoConnect();
    }

    public void DisconnectFromServer()
    {
        if (IsConnected)
            ftpClient.Disconnect();
    }

    public void UploadFileToServer(byte[] data, string fileName, Action<FtpProgress> progress = null)
    {
        Console.WriteLine($"Async Uploading {fileName} to Switch. [{data.Length} Bytes]");
        Console.WriteLine("Full Path: " + Path.Combine(FullPath, fileName));

        var status = ftpClient.UploadBytes(data, Path.Combine(FullPath, fileName), FtpRemoteExists.Overwrite, true, progress);

        Console.WriteLine("Done. Status: " + status.ToString());
    }

    public void UploadFileToServerAsync(byte[] data, string fileName)
    {
        asyncUploadTask = Task.Run(() =>
        {
            lock (ftpClient)
            {
                Action<FtpProgress> progress = delegate (FtpProgress p) {
                    if (p.Progress != 1)
                        UploadProgress = (float)(p.Progress / 100.0f);
                };

                UploadFileToServer(data, fileName, progress);

                UploadProgress = 0.0f;
            }
        });
    }
};