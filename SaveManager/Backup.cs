using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SaveManager
{

    public class Backup
    {
        public event EventHandler AddedBackup;

        FileSystemWatcher watcherSave = new FileSystemWatcher();

        FileSystemWatcher watcherZip = new FileSystemWatcher();

        String wgsSaveFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $@"..\Local\Packages\27620HinterlandStudio.30233944AADE4_y1bt56c4151zw\SystemAppData\wgs\000900000B79F9B4_B23B0100842A4BE28C91CA122924EC89\F8348F5BB7A4498080B3F0194C38FFC9\");
        String wgsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $@"..\Local\Packages\27620HinterlandStudio.30233944AADE4_y1bt56c4151zw\SystemAppData\wgs\");
        String zipFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $@"..\Local\Packages\27620HinterlandStudio.30233944AADE4_y1bt56c4151zw\SystemAppData\");

        public Backup()
        {
            watcherSave.Path = wgsSaveFolder;
            watcherSave.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            watcherSave.Filter = "container.*";
            watcherSave.Created += OnSaveChanged;
            watcherSave.Renamed += OnRenamed;
            
            watcherZip.Path = zipFolder;
            watcherZip.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            watcherZip.Filter = "*.zip";
            watcherZip.Created += OnZipChanged;
            watcherZip.EnableRaisingEvents = true;
        }

        public List<ContainerFile> Backups
        {
            get
            {
                List<ContainerFile> files = new List<ContainerFile>();
                DirectoryInfo d = new DirectoryInfo(zipFolder);
                FileInfo[] Files = d.GetFiles(watcherZip.Filter);
                foreach (FileInfo file in Files)
                {
                    DateTime creation = File.GetCreationTime(file.FullName);

                    FileInfo info = new FileInfo(file.FullName.Replace(".zip", ".json"));
                    if (!info.Exists)
                    {
                        ContainerFile meta = new ContainerFile();
                        meta.Filename = file.Name;
                        meta.Summary = "";
                        meta.Timestamp = creation;
                        meta.Locked = false;
                        meta.Deleted = false;
                        File.WriteAllText(info.FullName, JsonSerializer.Serialize(meta));
                    }

                    ContainerFile meta2 = JsonSerializer.Deserialize<ContainerFile>(File.ReadAllText(info.FullName));
                    files.Add(meta2);
                }
                return files;

            }
        }

        private void OnZipChanged(object source, FileSystemEventArgs e)
        {
            AddedBackup.Invoke(this, new EventArgs());
        }

        internal void Replace(ContainerFile meta)
        {
            DeleteDirectory(wgsFolder);
            System.IO.Directory.CreateDirectory(wgsFolder);
            ZipFile.ExtractToDirectory(zipFolder + meta.Filename, wgsFolder);
        }

        private void DeleteDirectory(string target_dir)
        {
            string[] files = Directory.GetFiles(target_dir);
            string[] dirs = Directory.GetDirectories(target_dir);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }
            Thread.Sleep(100);
            Directory.Delete(target_dir, false);
        }

        public void EnableAutomaticBackup(bool value)
        {
            if (watcherSave.Path.Length > 0)
            {
                watcherSave.EnableRaisingEvents = value;
                Trace.WriteLine("Automatic backup: " + watcherSave.EnableRaisingEvents);
            }
        }

        internal void BackupCurrent()
        {
            string[] files = Directory.GetFiles(wgsSaveFolder, "container.*");
            if (files.Length == 1)
            {
                FileInfo info = new FileInfo(files[0]);
                if (info.Exists)
                {
                    DateTime creation = DateTime.Now;
                    String zipFile = creation.ToString("yyyyMMdd-HHmmss") + "-" + info.Name + ".zip";
                    ZipFile.CreateFromDirectory(wgsFolder, zipFolder + zipFile);
                }
            }
        }

        // Define the event handlers.
        private void OnSaveChanged(object source, FileSystemEventArgs e)
        {
            Thread.Sleep(1000);
            Trace.WriteLine("File created" + e.Name);
            FileInfo info = new FileInfo(e.FullPath);
            if (info.Exists)
            {
                DateTime creation = File.GetCreationTime(e.FullPath);
                String zipFile = creation.ToString("yyyyMMdd-HHmmss") + "-" + e.Name + ".zip";
                ZipFile.CreateFromDirectory(wgsFolder, zipFolder + zipFile);
            }
            else
            {
                Trace.WriteLine("File not exist" + e.Name);
            }
        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {
            Thread.Sleep(1000);
            Trace.WriteLine("File renamed" + e.Name);
            FileInfo info = new FileInfo(e.FullPath);
            if (info.Exists)
            {
                DateTime creation = File.GetCreationTime(e.FullPath);
                String zipFile = creation.ToString("yyyyMMdd-HHmmss") + "-" + e.Name + ".zip";
                ZipFile.CreateFromDirectory(wgsFolder, zipFolder + zipFile);
            }
        }

        internal void Purge()
        {
            foreach (ContainerFile file in Backups) {
                if (file.Deleted)
                {
                    FileInfo info = new FileInfo(zipFolder + file.Filename);
                    if (info.Exists)
                    {
                        Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(info.FullName, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                    }
                }
            }
        }
    }

}
