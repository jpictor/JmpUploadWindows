#region using

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

#endregion

namespace JmpUploadClient
{
    class JmpWaitSpinner : Control
    {
        private Color SpinnerColor = Color.Blue;
        private Color SpinnerInactiveColor = Color.Gray;
        private double BackspinSecondsPerCycle = 30;
        private double SecondsPerCycle = 1;
        private DateTime StartTime = DateTime.Now;
        private double DegreesSpacing = 10;
        private double DegreesPhase = 0;
        private int Slices = 3;
        private int LeadingSlice = 0;
        private Timer SpinTimer;

        public JmpWaitSpinner ( )
        {
            SetStyle ( ControlStyles.SupportsTransparentBackColor, true );
            this.BackColor = Color.Transparent;
            DoubleBuffered = true;
            SpinTimer = new Timer ( );
            SpinTimer.Interval = 50;
            SpinTimer.Tick += new EventHandler ( SpinTimer_Tick );
        }

        public void SetColor ( Color color )
        {
            SpinnerColor = color;
            Invalidate ( );
            Update ( );
        }

        public void SetInactiveColor ( Color color )
        {
            SpinnerInactiveColor = color;
            Invalidate ( );
            Update ( );
        }

        public void Start ( )
        {
            try
            {
                if ( InvokeRequired )
                {
                    Invoke ( new MethodInvoker ( Start ) );
                    return;
                }
                StartTime = DateTime.Now;
                SpinTimer.Start ( );
                Invalidate ( );
                Update ( );
            }
            catch { }
        }

        public void Stop ( )
        {
            try
            {
                if ( InvokeRequired )
                {
                    Invoke ( new MethodInvoker ( Stop ) );
                    return;
                }
                SpinTimer.Stop ( );
                Invalidate ( );
                Update ( );
            }
            catch { }
        }

        void SpinTimer_Tick ( object sender, EventArgs e )
        {
            Invalidate ( );
            Update ( );
        }

        private void UpdateFromTime (  )
        {
            // Update LeadingSlice
            // one cycle every SecondsPerCycle from StartTime
            double totalSeconds = ( DateTime.Now - StartTime ).TotalSeconds;
            double cycleSeconds = totalSeconds % SecondsPerCycle;
            LeadingSlice = ( int ) Math.Floor ( ( float ) Slices * cycleSeconds / SecondsPerCycle );
            // update backspin phase
            cycleSeconds = totalSeconds % BackspinSecondsPerCycle;
            DegreesPhase = -360.0f * cycleSeconds / BackspinSecondsPerCycle;
        }

        private int [ ] GetSliceOpacities ( )
        {
            int [ ] opacities = new int [ Slices ];
            for ( int i = 0; i < Slices; ++i )
            {
                if ( i == LeadingSlice )
                {
                    opacities [ i ] = 255;
                }
                else
                {
                    opacities [ i ] = 128;
                }
            }
            return opacities;
        }

        private PointF [ ] GetSlicePolygonPoints ( int sliceNum )
        {
            float centerX = Size.Width / 2.0f;
            float centerY = Size.Height / 2.0f;

            float outerRadius = Math.Min ( centerX, centerY ) - 5;
            float innerRadius = outerRadius / 2.0f;

            PointF [ ] points = new PointF [ 5 ];

            float radSpacing = ( float ) Math.PI * ( float ) DegreesSpacing / 180.0f;
            float radPhase = ( float ) Math.PI * ( float ) DegreesPhase / 180.0f;
            float radPerSlice = 2.0f * ( float ) Math.PI / Slices;

            float startAngle = ( sliceNum * radPerSlice ) + ( radSpacing / 2.0f ) + radPhase;
            double endAngle = ( ( sliceNum + 1 ) * radPerSlice ) - ( radSpacing / 2.0f ) + radPhase;

            float x1 = ( float ) Math.Cos ( startAngle );
            float y1 = ( float ) Math.Sin ( startAngle );
            float x2 = ( float ) Math.Cos ( endAngle );
            float y2 = ( float ) Math.Sin ( endAngle );

            points [ 0 ].X = ( innerRadius * x1 ) + centerX;
            points [ 0 ].Y = ( innerRadius * y1 ) + centerY;
            points [ 4 ] = points [ 0 ];

            points [ 1 ].X = ( outerRadius * x1 ) + centerX;
            points [ 1 ].Y = ( outerRadius * y1 ) + centerY;

            points [ 2 ].X = ( outerRadius * x2 ) + centerX;
            points [ 2 ].Y = ( outerRadius * y2 ) + centerY;

            points [ 3 ].X = ( innerRadius * x2 ) + centerX;
            points [ 3 ].Y = ( innerRadius * y2 ) + centerY;

            return points;
        }

        public void PaintWaitSpinner ( Graphics g )
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

            if ( SpinTimer.Enabled )
            {
                UpdateFromTime ( );
                int [ ] opacities = GetSliceOpacities ( );
                for ( int i = 0; i < Slices; ++i )
                {
                    Color c = Color.FromArgb ( opacities [ i ], SpinnerColor );
                    using ( SolidBrush b = new SolidBrush ( c ) )
                    {
                        g.FillPolygon ( b, GetSlicePolygonPoints ( i ) );
                    }
                }
            }
            else
            {
                Color c = Color.FromArgb ( 128, SpinnerInactiveColor );
                using ( SolidBrush b = new SolidBrush ( c ) )
                {
                    for ( int i = 0; i < Slices; ++i )
                    {
                        g.FillPolygon ( b, GetSlicePolygonPoints ( i ) );
                    }
                }
            }
        }

        protected override void OnPaint ( PaintEventArgs e )
        {
            base.OnPaint ( e );
            Graphics gobj = e.Graphics;
            PaintWaitSpinner ( gobj );
        }
    }
}
