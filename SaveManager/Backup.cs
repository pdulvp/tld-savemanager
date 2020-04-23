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
using System.Xml.Linq;

namespace SaveManager
{

    public class Backup
    {
        public event EventHandler AddedBackup;

        FileSystemWatcher watcherSave = new FileSystemWatcher();

        FileSystemWatcher watcherZip = new FileSystemWatcher();

        String wgsSaveFolder;
        String wgsFolder;
        String zipFolder;

        public string SaveFolder
        {
            get
            {
                return zipFolder;
            }
        }

        public bool ValidConfiguration { get; private set; }

        public Backup()
        {
            InitializeConfiguration();

            if (ValidConfiguration)
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
        }

        private void InitializeConfiguration()
        {
            try
            {
                DirectoryInfo packageAppData = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), $@"Packages\27620HinterlandStudio.30233944AADE4_y1bt56c4151zw\"));
                if (packageAppData.Exists)
                {
                    DirectoryInfo zipFolder2 = new DirectoryInfo(packageAppData.FullName + $@"Saves\");
                    zipFolder = zipFolder2.FullName;
                    if (!zipFolder2.Exists)
                    {
                        zipFolder2.Create();
                    }
                    DirectoryInfo wgs = new DirectoryInfo(packageAppData.FullName + $@"SystemAppData\wgs\");
                    if (wgs.Exists)
                    {
                        wgsFolder = wgs.FullName;
                    }
                    DirectoryInfo sub = wgs.GetDirectories().Where(x => x.Name != "t").First();
                    if (sub != null)
                    {
                        DirectoryInfo sub2 = sub.GetDirectories().First();
                        if (sub2 != null)
                        {
                            wgsSaveFolder = sub2.FullName;
                        }
                    }
                }
                ValidConfiguration = true;
                Trace.TraceInformation("Configuration is valid");
            }
            catch (Exception)
            {
                Trace.TraceError("Configuration is invalid");
                ValidConfiguration = false;
            }

            Trace.WriteLine("Backup Folder:");
            Trace.WriteLine(toPathString(zipFolder));
            Trace.WriteLine("Root TLD Save Folder:");
            Trace.WriteLine(toPathString(wgsFolder));
            Trace.WriteLine("Monitored Sub Folder:");
            Trace.WriteLine(toPathString(wgsSaveFolder));
        }

        private string toPathString(string value)
        {
            if (value != null)
            {
                value = value.Replace(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), $@"%AppData%\Local");
            }
            return value;
        }

        public List<ContainerFile> Backups
        {
            get
            {
                List<ContainerFile> files = new List<ContainerFile>();
                DirectoryInfo d = new DirectoryInfo(zipFolder);
                foreach (FileInfo zipInfo in d.GetFiles("*.zip"))
                {
                    FileInfo jsonInfo = new FileInfo(zipInfo.FullName.Replace(".zip", ".json"));
                    if (!jsonInfo.Exists)
                    {
                        ContainerFile skeleton = new ContainerFile();
                        skeleton.Summary = "";
                        skeleton.Timestamp = File.GetCreationTime(zipInfo.FullName);
                        skeleton.Locked = false;
                        skeleton.Deleted = false;
                        Save(jsonInfo, skeleton);
                    }

                    ContainerFile meta = JsonSerializer.Deserialize<ContainerFile>(File.ReadAllText(jsonInfo.FullName));
                    meta.Filename = zipInfo.Name;
                    files.Add(meta);
                }

                foreach (FileInfo jsonInfo in d.GetFiles("*.json"))
                {
                    FileInfo zipInfo = new FileInfo(jsonInfo.FullName.Replace(".json", ".zip"));
                    if (!zipInfo.Exists)
                    {
                        ContainerFile meta = JsonSerializer.Deserialize<ContainerFile>(File.ReadAllText(jsonInfo.FullName));
                        meta.Filename = zipInfo.Name;
                        meta.Deleted = true;
                        files.Add(meta);
                    }
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

        private void Save(FileInfo info, ContainerFile meta)
        {
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.IgnoreReadOnlyProperties = true;
            options.WriteIndented = true;
            File.WriteAllText(info.FullName, JsonSerializer.Serialize(meta, options));
        }

        internal void Save(ContainerFile meta)
        {
            FileInfo info = new FileInfo(zipFolder + meta.MetadataFilename);
            Save(info, meta);
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

                FileInfo info = new FileInfo(zipFolder + file.Filename);
                if (file.Deleted && info.Exists)
                {
                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(info.FullName, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                }

                FileInfo metaInfo = new FileInfo(zipFolder + file.MetadataFilename);
                if (file.Deleted || !info.Exists)
                {
                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(metaInfo.FullName, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                }
            }
        }
    }

}
