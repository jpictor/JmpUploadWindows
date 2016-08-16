#region using

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;

#endregion

namespace JmpUploadClient
{

    public class JmpProgressBar : ProgressBar
    {
        private bool ErrorMode = false;

        public JmpProgressBar ( )
        {
        }

        public void SetErrorMode ( )
        {
            SetStyle ( ControlStyles.OptimizedDoubleBuffer, true );
            SetStyle ( ControlStyles.UserPaint, true );
            ErrorMode = true;
            Invalidate ( );
            Update ( );
        }

        protected override void OnPaint ( PaintEventArgs e )
        {
            base.OnPaint ( e );

            if ( ErrorMode )
            {
                LinearGradientBrush brush = null;
                Rectangle rec = new Rectangle ( 0, 0, this.Width, this.Height );
                if ( ProgressBarRenderer.IsSupported )
                {
                    ProgressBarRenderer.DrawHorizontalBar ( e.Graphics, rec );
                }
                rec.Width = rec.Width - 4;
                rec.Height -= 4;
                brush = new LinearGradientBrush ( rec, Color.Red, this.BackColor, LinearGradientMode.Vertical );
                e.Graphics.FillRectangle ( brush, 2, 2, rec.Width, rec.Height );
            }
        }
    }
}
