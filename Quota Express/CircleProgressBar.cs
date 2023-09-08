using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using System.Windows;
using System.Windows.Controls;

namespace Quota_Express
{
    public partial class CircularProgressBar : ProgressBar
    {
        public CircularProgressBar()
        {
            ValueChanged += CircularProgressBar_ValueChanged;
        }

        private void CircularProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            CircularProgressBar bar = (CircularProgressBar)sender;
            double currentAngle = bar.Angle;
            double targetAngle = e.NewValue / (double)bar.Maximum * 359.999;
            var anim = new DoubleAnimation(currentAngle, targetAngle, TimeSpan.FromMilliseconds(500));
            bar.BeginAnimation(AngleProperty, anim, HandoffBehavior.SnapshotAndReplace);
        }

        public static readonly DependencyProperty AngleProperty = DependencyProperty.Register(nameof(Angle), typeof(double), typeof(CircularProgressBar), new PropertyMetadata(0.0));

        public double Angle
        {
            get
            {
                return Convert.ToDouble(GetValue(AngleProperty));
            }
            set
            {
                SetValue(AngleProperty, value);
            }
        }

        public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(nameof(StrokeThickness), typeof(double), typeof(CircularProgressBar), new PropertyMetadata(10.0));

        public double StrokeThickness
        {
            get
            {
                return Convert.ToDouble(GetValue(StrokeThicknessProperty));
            }
            set
            {
                SetValue(StrokeThicknessProperty, value);
            }
        }
    }
}
