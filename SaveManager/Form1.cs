using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SaveManager
{

    public partial class Form1 : Form
    {
        Backup backup;

        public Form1()
        {
            InitializeComponent();

            Trace.Listeners.Add(new TraceWriterWrapper(consoleView));

            backup = new Backup();
            backup.AddedBackup += Backup_AddedBackup;

            imageList1.Images.Add("lock", global::SaveManager.Properties.Resources._lock);
            imageList1.Images.Add("delete", global::SaveManager.Properties.Resources.delete);
            imageList1.Images.Add("console", global::SaveManager.Properties.Resources.Console_16x);
            imageList1.Images.Add(TraceEventType.Information.ToString(), global::SaveManager.Properties.Resources.StatusInformation_16x);
            imageList1.Images.Add(TraceEventType.Warning.ToString(), global::SaveManager.Properties.Resources.StatusWarning_16x);
            imageList1.Images.Add(TraceEventType.Error.ToString(), global::SaveManager.Properties.Resources.StatusInvalid_16x);
            imageList1.Images.Add(TraceEventType.Critical.ToString(), global::SaveManager.Properties.Resources.StatusCriticalError_16x);

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
            consoleView.Columns[0].ImageKey = "console";
            consoleView.Columns.Add("Date", "Date");
            consoleView_Resize(consoleView, null);

            automaticBackupToolStripMenuItem.Checked = true;
            automaticBackupToolStripMenuItem.CheckOnClick = true;

            quitTLDOnChangeToolStripMenuItem.Checked = true;
            quitTLDOnChangeToolStripMenuItem.CheckOnClick = true;

            hideDeletedElementsToolStripMenuItem.Checked = true;
            hideDeletedElementsToolStripMenuItem.CheckOnClick = true;

            consoleToolStripMenuItem.Checked = !backup.ValidConfiguration;
            consoleToolStripMenuItem.CheckOnClick = true;
            splitContainer1.Panel2Collapsed = backup.ValidConfiguration;
            splitContainer1.Panel1Collapsed = !backup.ValidConfiguration;

            backup.EnableAutomaticBackup(automaticBackupToolStripMenuItem.Checked);

            RefreshZipList();
        }

        private void Backup_AddedBackup(object sender, EventArgs e)
        {
            listView1.Invoke(new MethodInvoker(delegate
            {
                RefreshZipList();
            }));
        }

        private void RefreshZipList()
        {
            listView1.Items.Clear();

            foreach (ContainerFile meta in backup.Backups)
            {
                ListViewItem item = new ListViewItem(new string[listView1.Columns.Count]);
                item.Tag = meta;
                item.ForeColor = meta.Deleted ? Color.LightGray : Color.Black;
                item.SubItems[listView1.Columns.IndexOfKey("Filename")].Text = meta.Filename;
                item.SubItems[listView1.Columns.IndexOfKey("Summary")].Text = meta.Summary;
                item.SubItems[listView1.Columns.IndexOfKey("Date")].Text = meta.Date;

                if (!(hideDeletedElementsToolStripMenuItem.Checked && meta.Deleted))
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

        private void backupCurrentSaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            backup.BackupCurrent();
        }

        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                OnListLeftClick(e);
            }

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
            ContainerFile meta = (ContainerFile) listView1.SelectedItems[0].Tag;
            backup.Replace(meta);

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
            backup.Save(meta);
            UpdateIcon(listViewItem);
            listView1.SelectedItems.Clear();
        }

        private void Delete(ContainerFile meta)
        {
            meta.Deleted = !meta.Deleted;
            backup.Save(meta);
        }

        private void Edit(ListViewItem listViewItem)
        {
            ContainerFile meta = (ContainerFile)listViewItem.Tag;
            string result = Microsoft.VisualBasic.Interaction.InputBox("Question?", "Title", meta.Summary);
            meta.Summary = result;
            backup.Save(meta);
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

        private void purgeDeletedListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Microsoft.VisualBasic.Interaction.MsgBox("Are you sure ?", Microsoft.VisualBasic.MsgBoxStyle.YesNo, "Deletion") == Microsoft.VisualBasic.MsgBoxResult.Yes)
            {
                backup.Purge();
                RefreshZipList();
            }
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

        private void consoleToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            splitContainer1.Panel2Collapsed = !consoleToolStripMenuItem.Checked;
        }

        private void automaticBackupToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            backup.EnableAutomaticBackup(automaticBackupToolStripMenuItem.Checked);
        }

        private void listView1_Resize(object sender, EventArgs e)
        {
            listView1.Columns[0].Width = 2 * (listView1.Width - 130 - 20) / 3 ;
            listView1.Columns[1].Width = 1 * (listView1.Width - 130 - 20) / 3;
            listView1.Columns[2].Width = 130;
        }

        private void consoleView_Resize(object sender, EventArgs e)
        {
            consoleView.Columns[0].Width = consoleView.Width - 130 - 20;
            consoleView.Columns[1].Width = 130;
        }

        private void githubToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GitRepositoryAttribute attribute = Assembly.GetEntryAssembly()
                .GetCustomAttributes(typeof(GitRepositoryAttribute), false)
                .Cast<GitRepositoryAttribute>().First();
            System.Diagnostics.Process.Start(attribute.Repository);
        }

    }
}
