namespace JmpUploadClient
{
    partial class JmpUploadForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose ( bool disposing )
        {
            if ( disposing && ( components != null ) )
            {
                components.Dispose ( );
            }
            base.Dispose ( disposing );
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent ( )
        {
            this.MyCancelButton = new System.Windows.Forms.Button();
            this.StatusTitleLabel = new System.Windows.Forms.Label();
            this.StatusBodyLabel = new System.Windows.Forms.Label();
            this.LogRichTextBox = new System.Windows.Forms.RichTextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.NumChannelsUpDown = new System.Windows.Forms.NumericUpDown();
            this.WaitSpinner = new JmpUploadClient.JmpWaitSpinner();
            ((System.ComponentModel.ISupportInitialize)(this.NumChannelsUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // MyCancelButton
            // 
            this.MyCancelButton.AutoSize = true;
            this.MyCancelButton.Location = new System.Drawing.Point(501, 514);
            this.MyCancelButton.Name = "MyCancelButton";
            this.MyCancelButton.Size = new System.Drawing.Size(101, 33);
            this.MyCancelButton.TabIndex = 1;
            this.MyCancelButton.Text = "CancelButton";
            this.MyCancelButton.UseVisualStyleBackColor = true;
            this.MyCancelButton.Click += new System.EventHandler(this.MyCancelButton_Click);
            // 
            // StatusTitleLabel
            // 
            this.StatusTitleLabel.AutoSize = true;
            this.StatusTitleLabel.Location = new System.Drawing.Point(6, 9);
            this.StatusTitleLabel.Name = "StatusTitleLabel";
            this.StatusTitleLabel.Padding = new System.Windows.Forms.Padding(5);
            this.StatusTitleLabel.Size = new System.Drawing.Size(93, 23);
            this.StatusTitleLabel.TabIndex = 1;
            this.StatusTitleLabel.Text = "StatusTitleLabel";
            this.StatusTitleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // StatusBodyLabel
            // 
            this.StatusBodyLabel.AutoSize = true;
            this.StatusBodyLabel.Location = new System.Drawing.Point(6, 32);
            this.StatusBodyLabel.Name = "StatusBodyLabel";
            this.StatusBodyLabel.Padding = new System.Windows.Forms.Padding(5);
            this.StatusBodyLabel.Size = new System.Drawing.Size(97, 23);
            this.StatusBodyLabel.TabIndex = 2;
            this.StatusBodyLabel.Text = "StatusBodyLabel";
            // 
            // LogRichTextBox
            // 
            this.LogRichTextBox.Location = new System.Drawing.Point(5, 553);
            this.LogRichTextBox.Name = "LogRichTextBox";
            this.LogRichTextBox.Size = new System.Drawing.Size(601, 123);
            this.LogRichTextBox.TabIndex = 4;
            this.LogRichTextBox.Text = "";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 529);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(190, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Number of Concurrent HTTP Channels";
            // 
            // NumChannelsUpDown
            // 
            this.NumChannelsUpDown.Location = new System.Drawing.Point(208, 529);
            this.NumChannelsUpDown.Maximum = new decimal(new int[] {
            15,
            0,
            0,
            0});
            this.NumChannelsUpDown.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.NumChannelsUpDown.Name = "NumChannelsUpDown";
            this.NumChannelsUpDown.Size = new System.Drawing.Size(52, 20);
            this.NumChannelsUpDown.TabIndex = 6;
            this.NumChannelsUpDown.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // WaitSpinner
            // 
            this.WaitSpinner.BackColor = System.Drawing.Color.Transparent;
            this.WaitSpinner.Location = new System.Drawing.Point(542, 9);
            this.WaitSpinner.Name = "WaitSpinner";
            this.WaitSpinner.Size = new System.Drawing.Size(64, 64);
            this.WaitSpinner.TabIndex = 5;
            this.WaitSpinner.Text = "waitSpinner1";
            // 
            // JmpUploadForm
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(614, 688);
            this.Controls.Add(this.MyCancelButton);
            this.Controls.Add(this.NumChannelsUpDown);
            this.Controls.Add(this.WaitSpinner);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.LogRichTextBox);
            this.Controls.Add(this.StatusBodyLabel);
            this.Controls.Add(this.StatusTitleLabel);
            this.Name = "JmpUploadForm";
            this.Text = "JmpUploadForm";
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.JmpUploadForm_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.JmpUploadForm_DragEnter);
            ((System.ComponentModel.ISupportInitialize)(this.NumChannelsUpDown)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button MyCancelButton;
        private System.Windows.Forms.Label StatusTitleLabel;
        private System.Windows.Forms.Label StatusBodyLabel;
        private System.Windows.Forms.RichTextBox LogRichTextBox;
        private JmpWaitSpinner WaitSpinner;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown NumChannelsUpDown;
    }
}