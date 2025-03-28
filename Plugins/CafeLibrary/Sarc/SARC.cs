﻿using System;
using System.Collections.Generic;
using System.Text;
using Toolbox.Core;
using Toolbox.Core.IO;
using System.IO;
using MapStudio.UI;
using UIFramework;
using System.Linq;

namespace CafeLibrary
{
    public class SARC : FileEditor, IFileFormat, IArchiveFile, IDisposable
    {
        public bool CanSave { get; set; } = true;

        public string[] Description { get; set; } = new string[] { "SARC" };
        public string[] Extension { get; set; } = new string[] { "*.sarc" };

        public File_Info FileInfo { get; set; }

        public bool CanAddFiles { get; set; } = true;
        public bool CanRenameFiles { get; set; } = true;
        public bool CanReplaceFiles { get; set; } = true;
        public bool CanDeleteFiles { get; set; } = true;

        public List<ArchiveFileInfo> files = new List<ArchiveFileInfo>();
        public IEnumerable<ArchiveFileInfo> Files => files;

        public bool Identify(File_Info fileInfo, System.IO.Stream stream)
        {
            using (var reader = new FileReader(stream, true))
            {
                return reader.CheckSignature(4, "SARC");
            }
        }

        public SarcData SarcData;

        public SARC()
        {
            FileInfo = new File_Info();
            SarcData = new SarcData()
            {
                Files = new Dictionary<string, byte[]>(),
            };
        }

        public static byte[] GetFile(string sarcPath, string file)
        {
            Stream stream = File.OpenRead(sarcPath);
            if (YAZ0.IsCompressed(sarcPath))
                stream = new MemoryStream(YAZ0.Decompress(sarcPath));

            var sarc = SARC_Parser.UnpackRamN(stream);
            return sarc.Files[file];
        }

        public static byte[] TryGetFile(string sarcPath, string file)
        {
            Stream stream = File.OpenRead(sarcPath);
            if (YAZ0.IsCompressed(sarcPath))
                stream = new MemoryStream(YAZ0.Decompress(sarcPath));

            var sarc = SARC_Parser.UnpackRamN(stream);
            if (sarc.Files.ContainsKey(file))
                return sarc.Files[file];
            else
                return [];
        }

        public void Load(System.IO.Stream stream)
        {
            files.Clear();
            SarcData = SARC_Parser.UnpackRamN(stream);
            foreach (var file in SarcData.Files)
            {
                var fileEntry = new ArchiveFileInfo();
                fileEntry.FileName = file.Key;
                fileEntry.SetData(file.Value);
                files.Add(fileEntry);
            }
            files = files.OrderBy(x => x.FileName).ToList();
        }

        public void SetFileData(string key, Stream stream)
        {
            foreach (var f in files)
            {
                if (f.FileName == key)
                    f.FileData = stream;
            }
        }

        public void ClearFiles() { files.Clear(); }

        public bool AddFile(ArchiveFileInfo archiveFileInfo)
        {
            files.Add(new ArchiveFileInfo()
            {
                FileData = archiveFileInfo.FileData,
                FileName = archiveFileInfo.FileName,
            });
            return true;
        }

        public bool DeleteFile(ArchiveFileInfo archiveFileInfo)
        {
            files.Remove(archiveFileInfo);
            return true;
        }

        public void Save(System.IO.Stream stream)
        {
            SarcData.Files.Clear();
            foreach (var file in files)
            {
                file.SaveFileFormat();

                SarcData.Files.Add(file.FileName, file.AsBytes());
            }

            //Save data to stream
            var saved = SARC_Parser.PackN(SarcData);
            stream.Write(saved.Item2);

            //Save alignment to compression type yaz0
            if (FileInfo.Compression != null && FileInfo.Compression is Yaz0)
            {
                ((Yaz0)FileInfo.Compression).Alignment = saved.Item1;
            }
        }

        public void Dispose()
        {
            foreach (var file in files)
            {
                if (file.FileFormat != null && file.FileFormat is IDisposable)
                    ((IDisposable)file.FileFormat).Dispose();
                file.FileData?.Dispose();
            }
        }

        /// <summary>
        /// Prepares the dock layouts to be used for the file format.
        /// </summary>
        public override List<DockWindow> PrepareDocks()
        {
            List<DockWindow> windows = new List<DockWindow>();
            windows.Add(Workspace.Outliner);
            windows.Add(Workspace.PropertyWindow);
            return windows;
        }
    }
}
