using System;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SaveManager
{

    public partial class Form1 : Form
    {
        FileSystemWatcher watcherSave = new FileSystemWatcher();

        FileSystemWatcher watcherZip = new FileSystemWatcher();

        public Form1()
        {
            InitializeComponent();

            Trace.Listeners.Add(new TraceWriterWrapper(consoleView));

            githubToolStripMenuItem.Visible = Assembly.GetEntryAssembly()
            .GetCustomAttributes(typeof(GitRepositoryAttribute), false)
            .Cast<GitRepositoryAttribute>().Any();

            statusStrip1.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
            toolStripStatusLabel2.Alignment = ToolStripItemAlignment.Right;
            toolStripStatusLabel5.Alignment = ToolStripItemAlignment.Right;
            toolStripStatusLabel4.Alignment = ToolStripItemAlignment.Right;

            listView1.View = View.Details;
            listView1.GridLines = true;
            listView1.FullRowSelect = true;
            listView1.Columns.Add("Filename", "Filename");
            listView1.Columns.Add("Summary", "Summary");
            listView1.Columns.Add("Date", "Date");
            listView1_Resize(listView1, null);

            consoleView.View = View.Details;
            consoleView.GridLines = true;
            consoleView.FullRowSelect = true;
            consoleView.Columns.Add("Message", "Message");
            consoleView.Columns.Add("Date", "Date");
            consoleView_Resize(consoleView, null);

            automaticBackupToolStripMenuItem.Checked = true;
            automaticBackupToolStripMenuItem.CheckOnClick = true;

            quitTLDOnChangeToolStripMenuItem.Checked = true;
            quitTLDOnChangeToolStripMenuItem.CheckOnClick = true;

            hideDeletedElementsToolStripMenuItem.Checked = false;
            hideDeletedElementsToolStripMenuItem.CheckOnClick = true;

            consoleToolStripMenuItem.Checked = false;
            consoleToolStripMenuItem.CheckOnClick = true;

            watcherSave.Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $@"..\Local\Packages\27620HinterlandStudio.30233944AADE4_y1bt56c4151zw\SystemAppData\wgs\000900000B79F9B4_B23B0100842A4BE28C91CA122924EC89\F8348F5BB7A4498080B3F0194C38FFC9\");
            watcherSave.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            watcherSave.Filter = "container.*";
            watcherSave.Created += OnSaveChanged;
            watcherSave.Renamed += OnRenamed;
            EnableAutomaticBackup(automaticBackupToolStripMenuItem.Checked);

            watcherZip.Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $@"..\Local\Packages\27620HinterlandStudio.30233944AADE4_y1bt56c4151zw\SystemAppData\");
            watcherZip.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            watcherZip.Filter = "*.zip";
            watcherZip.Created += OnZipChanged;
            watcherZip.EnableRaisingEvents = true;

            imageList1.Images.Add("lock", global::SaveManager.Properties.Resources._lock);
            imageList1.Images.Add("delete", global::SaveManager.Properties.Resources.delete);
            RefreshZipList();
        }

        private void RefreshZipList()
        {
            listView1.Items.Clear();
            DirectoryInfo d = new DirectoryInfo(watcherZip.Path);
            FileInfo[] Files = d.GetFiles(watcherZip.Filter);
            foreach (FileInfo file in Files)
            {
                DateTime creation = File.GetCreationTime(file.FullName);
                ListViewItem item = new ListViewItem(new string[listView1.Columns.Count]);

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
                item.Tag = meta2;
                item.ForeColor = meta2.Deleted ? Color.LightGray : Color.Black;
                item.SubItems[listView1.Columns.IndexOfKey("Filename")].Text = meta2.Filename;
                item.SubItems[listView1.Columns.IndexOfKey("Summary")].Text = meta2.Summary;
                item.SubItems[listView1.Columns.IndexOfKey("Date")].Text = meta2.Date;

                if (!(hideDeletedElementsToolStripMenuItem.Checked && meta2.Deleted))
                {
                    listView1.Items.Add(item);
                    UpdateIcon(listView1.Items[listView1.Items.Count - 1]);
                }
            }
        }

        private void UpdateIcon(ListViewItem item)
        {
            ContainerFile meta = (ContainerFile)item.Tag;
            if (meta.Deleted)
            {
                item.ImageKey = "delete";
            }
            else if (meta.Locked)
            {
                item.ImageKey = "lock";
            }
            else
            {
                item.ImageIndex = -1;
            }
        }

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshZipList();
        }

        // Define the event handlers.
        private void OnZipChanged(object source, FileSystemEventArgs e)
        {
            listView1.Invoke(new MethodInvoker(delegate
            {
                RefreshZipList();
                //listView1.Items.Add($"{e.Name}");
            }));
        }


        // Define the event handlers.
        private void OnSaveChanged(object source, FileSystemEventArgs e)
        {
            Thread.Sleep(1000);
            Trace.WriteLine("File created" + e.Name);
            if (new FileInfo(e.FullPath).Exists)
            {
                DateTime creation = File.GetCreationTime(e.FullPath);
                String folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $@"..\Local\Packages\27620HinterlandStudio.30233944AADE4_y1bt56c4151zw\SystemAppData\");
                String zipFile = creation.ToString("yyyyMMdd-HHmmss") + "-" + e.Name + ".zip";

                FileInfo zip = new FileInfo(folder + zipFile);
                ZipFile.CreateFromDirectory(folder + "wgs", folder + zipFile);

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
            String zipFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $@"..\Local\Packages\27620HinterlandStudio.30233944AADE4_y1bt56c4151zw\SystemAppData\");
            if (info.Exists)
            {
                DateTime creation = File.GetCreationTime(e.FullPath);
                ZipFile.CreateFromDirectory(zipFolder + "wgs", zipFolder + $@"\" + creation.ToString("yyyyMMdd-HHmmss") + "-" + e.Name + ".zip");
            }
        }

        private void backupCurrentSaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            String folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $@"..\Local\Packages\27620HinterlandStudio.30233944AADE4_y1bt56c4151zw\SystemAppData\wgs\000900000B79F9B4_B23B0100842A4BE28C91CA122924EC89\F8348F5BB7A4498080B3F0194C38FFC9\");
            String wgsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $@"..\Local\Packages\27620HinterlandStudio.30233944AADE4_y1bt56c4151zw\SystemAppData\wgs\");
            String zipFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $@"..\Local\Packages\27620HinterlandStudio.30233944AADE4_y1bt56c4151zw\SystemAppData\");
            
            string[] files = Directory.GetFiles(folder, "container.*");
            if (files.Length == 1)
            {
                FileInfo info = new FileInfo(files[0]);
                if (info.Exists)
                {
                    DateTime creation = DateTime.Now;
                    ZipFile.CreateFromDirectory(wgsFolder, zipFolder + $@"\" + creation.ToString("yyyyMMdd-HHmmss") + "-" + info.Name + ".zip");
                }
            }
        }

        public static void DeleteDirectory(string target_dir)
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

        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                OnListLeftClick(e);
            }

        }
        private void listView1_MouseClick(object sender, MouseEventArgs e)
        {

        }

        private void OnListLeftClick(MouseEventArgs e)
        {
            if (listView1.SelectedItems.Count != 0)
            {
                ContainerFile meta = (ContainerFile)listView1.SelectedItems[0].Tag;
                if (!meta.Deleted)
                {
                    Upload(listView1.SelectedItems[0]);
                }
            }
        }

        private void OnListMiddleClick(MouseEventArgs e)
        {
            if (listView1.SelectedItems.Count != 0)
            {
                Lock(listView1.SelectedItems[0]);
            }
        }

        private void Upload(ListViewItem listViewItem)
        {
            toolStripStatusLabel1.Image = null;
            toolStripStatusLabel1.Text = "Set save";

            Process process = null;
            String fol2der = null;
            if (quitTLDOnChangeToolStripMenuItem.Checked)
            {
                foreach (Process theprocess in Process.GetProcesses())
                {
                    if (theprocess.ProcessName == "tld")
                    {
                        process = theprocess;
                    }
                }

                if (process != null)
                {
                    Trace.WriteLine(String.Format("Process is started: {0} ID: {1}", process.ProcessName, process.Id));
                    Thread.Sleep(1000);
                    process.Kill();
                    fol2der = process.MainModule.FileName;
                    Trace.WriteLine($"fol2der: {fol2der}");
                }
            }

            Trace.WriteLine($"Replace: {listViewItem}");
            ContainerFile meta = (ContainerFile)listView1.SelectedItems[0].Tag;

            String wgsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $@"..\Local\Packages\27620HinterlandStudio.30233944AADE4_y1bt56c4151zw\SystemAppData\wgs\");
            String zipFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $@"..\Local\Packages\27620HinterlandStudio.30233944AADE4_y1bt56c4151zw\SystemAppData\");
            DeleteDirectory(wgsFolder);
            System.IO.Directory.CreateDirectory(wgsFolder);
            ZipFile.ExtractToDirectory(zipFolder + meta.Filename, wgsFolder);

            if (quitTLDOnChangeToolStripMenuItem.Checked)
            {
                if (process != null && fol2der != null)
                {
                    Thread.Sleep(1000);
                    try
                    {
                        Process.Start(fol2der);
                    }
                    catch (Exception ez)
                    {
                        Trace.WriteLine(ez.StackTrace);
                    }
                }
            }

            toolStripStatusLabel1.Text = "Set save: OK";
            Trace.WriteLine("Set save: OK");
            toolStripStatusLabel1.Image = global::SaveManager.Properties.Resources.StatusOK_16x;

            System.Threading.Tasks.Task.Delay(2 * 1000).ContinueWith(ResetStatusLine);
        }

        private void ResetStatusLine(Task obj)
        {
            toolStripStatusLabel1.Text = "";
            toolStripStatusLabel1.Image = null;
        }

        private void uploadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 1)
            {
                Upload(listView1.SelectedItems[0]);
            }
        }


        private void Lock(ListViewItem listViewItem)
        {
            ContainerFile meta = (ContainerFile)listViewItem.Tag;
            meta.Locked = !meta.Locked;
            Save(meta);
            UpdateIcon(listViewItem);
            listView1.SelectedItems.Clear();
        }

        private void Save(ContainerFile meta)
        {
            String zipFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $@"..\Local\Packages\27620HinterlandStudio.30233944AADE4_y1bt56c4151zw\SystemAppData\");
            FileInfo info = new FileInfo(zipFolder + meta.Filename.Replace(".zip", ".json"));
            File.WriteAllText(info.FullName, JsonSerializer.Serialize(meta));
        }

        private void Delete(ContainerFile meta)
        {
            meta.Deleted = !meta.Deleted;
            Save(meta);
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void listView1_KeyPress(object sender, KeyPressEventArgs e)
        {

        }

        private void listView1_KeyUp(object sender, KeyEventArgs e)
        {
            if (listView1.SelectedItems.Count != 0)
            {
                if (e.KeyCode == Keys.F2)
                {
                    Edit(listView1.SelectedItems[0]);

                }
                else if (e.KeyCode == Keys.Delete)
                {
                    Delete(listView1.SelectedItems[0]);
                }
            }
        }

        private void Edit(ListViewItem listViewItem)
        {
            ContainerFile meta = (ContainerFile)listViewItem.Tag;
            string result = Microsoft.VisualBasic.Interaction.InputBox("Question?", "Title", meta.Summary);
            meta.Summary = result;
            Save(meta);
            listView1.SelectedItems.Clear();
            RefreshZipList();
        }

        private void Delete(ListViewItem listViewItem)
        {
            ContainerFile meta = (ContainerFile)listViewItem.Tag;
            if (!meta.Locked)
            {
                Delete(meta);
                RefreshZipList();
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 1)
            {
                Delete(listView1.SelectedItems[0]);
            }
        }

        private void editSummaryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 1)
            {
                Edit(listView1.SelectedItems[0]);
            }
        }
        private void lockToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 1)
            {
                Lock(listView1.SelectedItems[0]);
            }
        }

        private void listView1_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            toolStripStatusLabel1.Text = "";
        }

        private void openFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            String zipFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $@"..\Local\Packages\27620HinterlandStudio.30233944AADE4_y1bt56c4151zw\SystemAppData\");
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
            {
                FileName = zipFolder,
                UseShellExecute = true,
                Verb = "open"
            });
        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void purgeDeletedListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Microsoft.VisualBasic.Interaction.MsgBox("Sure?", Microsoft.VisualBasic.MsgBoxStyle.YesNo, "Deletion") == Microsoft.VisualBasic.MsgBoxResult.Yes)
            {
                RefreshZipList();
            }
        }

        private void toolStripStatusLabel2_Click(object sender, EventArgs e)
        {

        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            e.Cancel = listView1.SelectedItems.Count != 1;

            if (!e.Cancel)
            {
                ContainerFile meta = (ContainerFile)listView1.SelectedItems[0].Tag;
                deleteToolStripMenuItem.Text = meta.Deleted ? "Recover" : "Delete";
                lockToolStripMenuItem.Text = meta.Locked ? "Unlock" : "Lock";

                deleteToolStripMenuItem.Visible = !meta.Locked;
                editSummaryToolStripMenuItem.Visible = !meta.Deleted;

                lockToolStripMenuItem.Visible = !meta.Deleted;
                uploadToolStripMenuItem.Visible = !meta.Deleted;
                toolStripSeparator1.Visible = uploadToolStripMenuItem.Visible;
            }
        }

        private void hideDeletedElementsToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            RefreshZipList();
        }

        private void toolStripDropDownButton1_Click(object sender, EventArgs e)
        {

        }

        private void consoleToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            splitContainer1.Panel2Collapsed = !consoleToolStripMenuItem.Checked;
        }

        private void automaticBackupToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            EnableAutomaticBackup(automaticBackupToolStripMenuItem.Checked);
        }

        private void EnableAutomaticBackup(bool value)
        {
            if (watcherSave.Path.Length > 0)
            {
                watcherSave.EnableRaisingEvents = value;
                Trace.WriteLine("Automatic backup: " + watcherSave.EnableRaisingEvents);
            }
        }

        private void listView1_Resize(object sender, EventArgs e)
        {
            listView1.Columns[0].Width = listView1.Width - 130 - 130 - 20;
            listView1.Columns[1].Width = 130;
            listView1.Columns[2].Width = 130;
        }

        private void consoleView_Resize(object sender, EventArgs e)
        {
            consoleView.Columns[0].Width = consoleView.Width - 120 - 20;
            consoleView.Columns[1].Width = 120;
        }

        private void githubToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GitRepositoryAttribute attribute = Assembly.GetEntryAssembly()
                .GetCustomAttributes(typeof(GitRepositoryAttribute), false)
                .Cast<GitRepositoryAttribute>().First();
            System.Diagnostics.Process.Start(attribute.Repository);
        }

        private void listView1_SelectedIndexChanged_1(object sender, EventArgs e)
        {

        }
    }
}
