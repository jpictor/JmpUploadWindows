#region using

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Reflection;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

using JmpUploadEngine;

#endregion

namespace JmpUploadClient
{
    public partial class JmpUploadForm : Form
    {
        #region Members

        JmpUploadClientEngine UploadClient;

        private class UploadEntry : JmpUploadEngine.UploadEntry
        {
            public int ZipProgressTasks;
            public int ZipProgressTasksComplete;
            public int UploadTasks;
            public int UploadTasksComplete;

            public Label Label;
            public CheckBox CheckBox;
            public Label UploadRateLabel;
            public Label StatusLabel;
            public JmpProgressBar ProgressBar;

            public void UpdateProgressBar ( )
            {
                int value = 0;
                if ( ZipProgressTasks > 0 )
                {
                    double v = 100.0 * ( ( double ) ZipProgressTasksComplete / ( double ) ZipProgressTasks );
                    value += (int) Math.Round ( v );
                }
                if ( UploadTasks > 0 )
                {
                    double v = 100.0 * ( ( double ) UploadTasksComplete / ( double ) UploadTasks );
                    value += ( int ) Math.Round ( v );
                }
                UploadRateLabel.Text = string.Format ( "{0:0.00}MB/sec", UploadBandwidthMBSec );
                ProgressBar.Value = value;
            }

            public void UpdateStatusLabel ( )
            {
                if ( State == JmpUploadEngine.UploadEntryState.Completed )
                {
                    StatusLabel.Text = string.Format ( "{0}/{1}", State.ToString ( ), CompletedState.ToString ( ) );
                }
                else
                {
                    StatusLabel.Text = State.ToString ( );
                }
            }
        }

        private List<UploadEntry> UploadEntries;

        // GUI controls
        private CheckBox SelectAllCheckBox;
        private TableLayoutPanel UploadTable;
        private Panel ScrollablePanel;
        private Panel FileListPanel;

        #endregion

        #region Constructors

        public JmpUploadForm ( )
        {
            UploadEntries = new List<UploadEntry> ( );
            UploadClient = new JmpUploadClientEngine ( );
            UploadClient.UploadUrl = Properties.Settings.Default.JmpUploadURL;

            InitializeComponent ( );
            CenterToScreen ( );

            // label GUI
            Text = "JmpUploadClient";
            MyCancelButton.Text = "Exit";
            StatusTitleLabel.Text = "Upload Files";
            StatusBodyLabel.Text = "Drop files on this application to begin upload.";

            // customize fonts
            FontFamily fontFamily = StatusTitleLabel.Font.FontFamily;
            StatusTitleLabel.Location = new Point ( MarginPixels, MarginPixels );
            StatusTitleLabel.Font = new Font ( fontFamily, 25F, FontStyle.Bold );
            StatusBodyLabel.Location = new Point ( StatusTitleLabel.Location.Y, StatusTitleLabel.Location.Y + StatusTitleLabel.Size.Height + MarginPixels );
            StatusBodyLabel.AutoSize = false;
            StatusBodyLabel.Size = new Size ( 400, StatusBodyLabel.Font.Height * 4 );
            
            // numeric up/down for setting number of concurrent connections
            NumChannelsUpDown.Value = UploadClient.NumberOfUploadChannels;
            NumChannelsUpDown.ValueChanged += new EventHandler ( NumChannelsUpDown_ValueChanged );

            InitializeFileListPanel ( );

            KeyPreview = true;
            KeyDown += new KeyEventHandler ( JmpUploadForm_KeyDown );

            UploadClient.UpdateUploadProgressEvent = UpdateUploadProgressBar;
            UploadClient.UploadEntryStateChangedEvent = UploadEntryStateChanged;
        }

        protected override void OnSizeChanged ( EventArgs e )
        {
            base.OnSizeChanged ( e );
            LayoutControls ( );
        }

        public static int MarginPixels = 10;
        public static int LogRichTextBoxHeight = 100;

        public void LayoutControls ( )
        {
            WaitSpinner.Location = new Point ( ClientSize.Width - WaitSpinner.Size.Width - MarginPixels, MarginPixels );

            LogRichTextBox.Location = new Point ( MarginPixels, ClientSize.Height - LogRichTextBoxHeight - MarginPixels );
            LogRichTextBox.Size = new Size ( ClientSize.Width - 2 * MarginPixels, LogRichTextBoxHeight );

            MyCancelButton.Location = new Point ( 
                ClientSize.Width - MyCancelButton.Size.Width - MarginPixels, 
                LogRichTextBox.Location.Y - MyCancelButton.Size.Height - MarginPixels );

            label1.Location = new Point ( MarginPixels, LogRichTextBox.Location.Y - label1.Size.Height - MarginPixels );
            NumChannelsUpDown.Location = new Point (
                label1.Location.X + label1.Size.Width + MarginPixels,
                label1.Location.Y - 3 );

        }

        protected override void OnClosing ( CancelEventArgs e )
        {
            base.OnClosing ( e );
            CloseApp ( );
        }

        #endregion

        #region Events

        void NumChannelsUpDown_ValueChanged ( object sender, EventArgs e )
        {
            UploadClient.SetNumberOfUploadChannels ( ( int ) NumChannelsUpDown.Value );
        }

        void JmpUploadForm_KeyDown ( object sender, KeyEventArgs e )
        {
            if ( e.KeyCode == Keys.F1 )
            {
                e.Handled = true;
                ToggleLogWindowVisible ( );
            }
        }

        private void ToggleLogWindowVisible ( )
        {
        }

        private void SelectAllCheckBox_CheckedChanged ( object sender, EventArgs e )
        {
            // do nothing if not enabled
            if ( !SelectAllCheckBox.Enabled )
            {
                return;
            }

            foreach ( UploadEntry ue in UploadEntries )
            {
                if ( ue.CheckBox.Enabled )
                {
                    ue.CheckBox.Checked = SelectAllCheckBox.Checked;
                }

            }
        }

        private void MyCancelButton_Click ( object sender, EventArgs e )
        {
            CloseApp ( );
            Close ( );
        }

        private void CloseApp ( )
        {
            MyCancelButton.Enabled = false;
            UploadClient.Shutdown ( );
        }

        private void JmpUploadForm_DragEnter ( object sender, DragEventArgs e )
        {
            if ( e.Data.GetDataPresent ( DataFormats.FileDrop ) )
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void JmpUploadForm_DragDrop ( object sender, DragEventArgs e )
        {
            if ( e.Data.GetDataPresent ( DataFormats.FileDrop ) )
            {
                string [ ] filePaths = ( string [ ] ) ( e.Data.GetData ( DataFormats.FileDrop ) );
                foreach ( string path in filePaths )
                {
                    if ( File.Exists ( path ) )
                    {
                        AddUploadEntry ( path );
                    }
                }
            }
        }

        #endregion

        #region API Methods

        public delegate void AddUploadEntryDelegate ( string path );

        public void AddUploadEntry ( string path )
        {
            // disable some GUI
            NumChannelsUpDown.Enabled = false;
            WaitSpinner.Start ( );
            StatusTitleLabel.Text = "Uploading Files...";
            StatusBodyLabel.Text = "Drop files on this application to queue upload.";
            
            // create UploadEntries for each
            UploadEntry ue = new UploadEntry ( );
            UploadEntries.Add ( ue );
            ue.UploadFilePath = path;
            UploadClient.AddUploadEntry ( ue );

            // add to GUI
            AddUploadEntryToTable ( ue );
        }

        #endregion

        #region Logging Hook

        public delegate void LogLineDelegate ( string s );

        public void LogLine ( string s )
        {
            try
            {
                if ( InvokeRequired )
                {
                    BeginInvoke ( new LogLineDelegate ( LogLine ), new object [ ] { s } );
                    return;
                }
            }
            catch 
            {
                return;
            }

            try
            {
                s += System.Environment.NewLine;

                int start = LogRichTextBox.TextLength;
                LogRichTextBox.AppendText ( s );
                int end = LogRichTextBox.TextLength;
                LogRichTextBox.Select ( start, end - start );
                if ( s.IndexOf ( "ERROR" ) != -1 )
                {
                    LogRichTextBox.SelectionColor = Color.Red;
                }
                else if ( s.IndexOf ( "WARN" ) != -1 )
                {
                    LogRichTextBox.SelectionColor = Color.Orange;
                }
                else if ( s.IndexOf ( "exception", StringComparison.InvariantCultureIgnoreCase ) != -1 )
                {
                    LogRichTextBox.SelectionColor = Color.LightSalmon;
                }
                else if ( s.IndexOf ( "dirty", StringComparison.InvariantCultureIgnoreCase ) != -1 )
                {
                    LogRichTextBox.SelectionColor = Color.Yellow;
                }

                LogRichTextBox.SelectionLength = 0;
                LogRichTextBox.SelectionStart = LogRichTextBox.Text.Length;
                LogRichTextBox.ScrollToCaret ( );
            }
            catch { } // swallow exceptions that can be caused by the window handle for this control being killed
        }

        #endregion

        #region Send File Table GUI

        private void InitializeFileListPanel ( )
        {
            int x = 0;
            int y = 0;
            int vertSpacing = 10;

            // figure out location
            x = 10;
            y = StatusBodyLabel.Location.Y + StatusBodyLabel.Size.Height + 10;

            FileListPanel = new Panel ( );
            FileListPanel.Location = new System.Drawing.Point ( x, y );
            FileListPanel.Size = new System.Drawing.Size ( 600, 400 );
            Controls.Add ( FileListPanel );


            // select all checkbox
            x = 3;

            SelectAllCheckBox = new CheckBox ( );
            SelectAllCheckBox.AutoSize = true;
            SelectAllCheckBox.Text = "Select All";
            SelectAllCheckBox.Location = new System.Drawing.Point ( x, 0 );
            SelectAllCheckBox.CheckedChanged += new EventHandler ( SelectAllCheckBox_CheckedChanged );
            FileListPanel.Controls.Add ( SelectAllCheckBox );

            // column headers
            int locationY = SelectAllCheckBox.Location.Y + SelectAllCheckBox.Size.Height + vertSpacing;
            
            Label label = new Label ( );
            label.Text = "File Name";
            label.Location = new System.Drawing.Point ( 0, locationY );
            label.Width = 175;
            label.BackColor = Color.LightBlue;
            label.TextAlign = ContentAlignment.MiddleCenter;
            FileListPanel.Controls.Add ( label );

            label = new Label ( );
            label.Text = "Size";
            label.Location = new System.Drawing.Point ( 175, locationY );
            label.Width = 100;
            label.BackColor = Color.LightBlue;
            label.TextAlign = ContentAlignment.MiddleCenter;
            FileListPanel.Controls.Add ( label );

            label = new Label ( );
            label.Text = "Rate";
            label.Location = new System.Drawing.Point ( 275, locationY );
            label.Width = 100;
            label.BackColor = Color.LightBlue;
            label.TextAlign = ContentAlignment.MiddleCenter;
            FileListPanel.Controls.Add ( label );

            label = new Label ( );
            label.Text = "Status";
            label.Location = new System.Drawing.Point ( 375, locationY );
            label.Width = 100;
            label.BackColor = Color.LightBlue;
            label.TextAlign = ContentAlignment.MiddleCenter;
            FileListPanel.Controls.Add ( label );

            label = new Label ( );
            label.Text = "Progress";
            label.Location = new System.Drawing.Point ( 475, locationY );
            label.Width = 100;
            label.BackColor = Color.LightBlue;
            label.TextAlign = ContentAlignment.MiddleCenter;
            FileListPanel.Controls.Add ( label );

            // scrollable panel for table of files
            x = 0;
            y = label.Location.Y + label.Size.Height + vertSpacing;

            ScrollablePanel = new Panel ( );
            ScrollablePanel.AutoScroll = true;
            ScrollablePanel.Location = new System.Drawing.Point ( x, y );
            ScrollablePanel.Size = new System.Drawing.Size ( 600, 300 );
            FileListPanel.Controls.Add ( ScrollablePanel );

            ClearUploadTable ( );
        }

        private void ClearUploadTable ( )
        {
            if ( UploadTable != null )
            {
                ScrollablePanel.Controls.Remove ( UploadTable );
                foreach ( UploadEntry ue in UploadEntries )
                {
                    if ( ue.CheckBox != null )
                    {
                        ue.CheckBox.Dispose ( );
                        ue.CheckBox = null;
                    }
                    if ( ue.Label != null )
                    {
                        ue.Label.Dispose ( );
                        ue.Label = null;
                    }
                    if ( ue.ProgressBar != null )
                    {
                        ue.ProgressBar.Dispose ( );
                        ue.ProgressBar = null;
                    }
                }
                UploadTable.Dispose ( );
            }
            UploadTable = new TableLayoutPanel ( );
            UploadTable.AutoSize = true;
            UploadTable.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            UploadTable.Location = new System.Drawing.Point ( 0, 0 );
            UploadTable.Padding = new System.Windows.Forms.Padding ( 0 );
            UploadTable.Margin = new System.Windows.Forms.Padding ( 0 );
            UploadTable.RowCount = 0;
            UploadTable.ColumnCount = 6;
            UploadTable.GrowStyle = TableLayoutPanelGrowStyle.AddRows;
            UploadTable.ColumnStyles.Add ( new ColumnStyle ( SizeType.Absolute, 25 ) );
            UploadTable.ColumnStyles.Add ( new ColumnStyle ( SizeType.Absolute, 150 ) );
            UploadTable.ColumnStyles.Add ( new ColumnStyle ( SizeType.Absolute, 100 ) );
            UploadTable.ColumnStyles.Add ( new ColumnStyle ( SizeType.Absolute, 100 ) );
            UploadTable.ColumnStyles.Add ( new ColumnStyle ( SizeType.Absolute, 100 ) );
            UploadTable.ColumnStyles.Add ( new ColumnStyle ( SizeType.Absolute, 100 ) );
            ScrollablePanel.Controls.Add ( UploadTable );
        }

        private void RefreshUploadFileTable ( )
        {
            ClearUploadTable ( );
            UploadTable.SuspendLayout ( );
            foreach ( UploadEntry ue in UploadEntries )
            {
                AddUploadEntryToTable ( ue );
            }
            UploadTable.ResumeLayout ( );
        }

        private void AddUploadEntryToTable ( UploadEntry ue )
        {
            int rowIndex = UploadTable.RowCount;
            int rowCount = UploadTable.RowCount + 1;
            UploadTable.RowCount = rowCount;

            CheckBox cb = new CheckBox ( );
            ue.CheckBox = cb;
            cb.Dock = DockStyle.Fill;
            ue.CheckBox.CheckedChanged += new EventHandler ( CheckBox_CheckedChanged );

            Label label = new Label ( );
            ue.Label = label;
            label.AutoSize = true;
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Text = ue.Name;

            Label fileSizeLabel = new Label ( );
            fileSizeLabel.AutoSize = true;
            fileSizeLabel.Dock = DockStyle.Fill;
            fileSizeLabel.TextAlign = ContentAlignment.MiddleLeft;
            fileSizeLabel.Text = string.Format ( "{0:0.00}MB", ue.ChunkFileInfo.FileSizeMB );

            Label uploadRateLabel = new Label ( );
            ue.UploadRateLabel = uploadRateLabel;
            uploadRateLabel.AutoSize = true;
            uploadRateLabel.Dock = DockStyle.Fill;
            uploadRateLabel.TextAlign = ContentAlignment.MiddleLeft;
            uploadRateLabel.Text = string.Format ( "{0:0.00}MB/sec", ue.UploadBandwidthMBSec );

            Label statusLabel = new Label ( );
            ue.StatusLabel = statusLabel;
            statusLabel.AutoSize = true;
            statusLabel.Dock = DockStyle.Fill;
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            statusLabel.Text = "";

            int rowHeight = ( int ) ( label.Font.Height * 1.5 );
            int pbHeight = ( int ) ( label.Font.Height * 0.75 );

            JmpProgressBar pb = new JmpProgressBar ( );
            ue.ProgressBar = pb;
            pb.Dock = DockStyle.Fill;
            pb.Height = pbHeight;
            pb.Width = 100;

            RowStyle rowStyle = new RowStyle ( );
            rowStyle.SizeType = SizeType.Absolute;
            rowStyle.Height = rowHeight;
            UploadTable.RowStyles.Add ( rowStyle );

            UploadTable.Controls.Add ( cb, 0, rowIndex );
            UploadTable.Controls.Add ( label, 1, rowIndex );
            UploadTable.Controls.Add ( fileSizeLabel, 2, rowIndex );
            UploadTable.Controls.Add ( uploadRateLabel, 3, rowIndex );
            UploadTable.Controls.Add ( statusLabel, 4, rowIndex );
            UploadTable.Controls.Add ( pb, 5, rowIndex );
        }

        void CheckBox_CheckedChanged ( object sender, EventArgs e )
        {
            // no updates if the checkbox is not enabled
            CheckBox cb = ( CheckBox ) sender;
            if ( !cb.Enabled )
            {
                return;
            }
        }

        private void UpdateZipProgressBar ( JmpUploadEngine.UploadEntry ue, int unitsCompleted, int unitsTotal )
        {
            try
            {
                if ( InvokeRequired )
                {
                    BeginInvoke ( new JmpUploadClientEngine.UpdateProgressDelegate ( UpdateZipProgressBar ), new object [ ] { ue, unitsCompleted, unitsTotal } );
                    return;
                }
                int value = ( int ) ( 100.0 * ( double ) unitsCompleted / ( double ) unitsTotal );
                UploadEntry myUe = ( UploadEntry ) ue;
                myUe.ZipProgressTasksComplete = unitsCompleted;
                myUe.ZipProgressTasks = unitsTotal;
                myUe.UpdateProgressBar ( );
            }
            catch { }
        }

        private void UpdateUploadProgressBar ( JmpUploadEngine.UploadEntry ue, int unitsCompleted, int unitsTotal )
        {
            try
            {
                if ( InvokeRequired )
                {
                    BeginInvoke ( new JmpUploadClientEngine.UpdateProgressDelegate ( UpdateUploadProgressBar ), new object [ ] { ue, unitsCompleted, unitsTotal } );
                    return;
                }
                int value = ( int ) ( 100.0 * ( double ) unitsCompleted / ( double ) unitsTotal );
                UploadEntry myUe = ( UploadEntry ) ue;
                myUe.UploadTasksComplete = unitsCompleted;
                myUe.UploadTasks = unitsTotal;
                myUe.UpdateProgressBar ( );
            }
            catch { }
        }

        #endregion

        #region JmpUploadClientEngine State Change

        private void UpdateUploadEntryState ( UploadEntry ue )
        {
            if ( UploadClient.UploadEntries.Count == 0 )
            {
                NumChannelsUpDown.Enabled = true;
                WaitSpinner.Stop ( );
                StatusTitleLabel.Text = "Upload Files";
                StatusBodyLabel.Text = "Drop files on this application to begin upload.";
            }
            ue.UpdateStatusLabel ( );
        }

        private void UploadEntryStateChanged ( JmpUploadEngine.UploadEntry ue )
        {
            try
            {
                if ( InvokeRequired )
                {
                    BeginInvoke ( new JmpUploadClientEngine.UploadEntryStateChangedDelegate ( UploadEntryStateChanged ) , new object [ ] { ue } );
                    return;
                }
                UpdateUploadEntryState ( ( UploadEntry ) ue );
            }
            catch { }
        }

        #endregion
    }
}
