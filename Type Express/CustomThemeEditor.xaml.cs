using ExpressControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Xceed.Wpf.Toolkit;

namespace Type_Express
{
    /// <summary>
    /// Interaction logic for CustomThemeEditor.xaml
    /// </summary>
    public partial class CustomThemeEditor : Window
    {
        public Color[] ChosenColours { get; set; } = Enumerable.Repeat(Colors.Black, 8).ToArray();

        public CustomThemeEditor(Color[]? colours)
        {
            InitializeComponent();

            // Event handlers for all window types
            CloseBtn.Click += Funcs.CloseEvent;
            TitleBtn.PreviewMouseLeftButtonDown += Funcs.MoveFormEvent;
            Activated += Funcs.ActivatedEvent;
            Deactivated += Funcs.DeactivatedEvent;

            if (colours != null)
                ChosenColours = colours;

            LoadColours();
        }

        private void LoadColours()
        {
            ColourItems.ItemsSource = ChosenColours.Select((x, idx) =>
            {
                return new CustomColourItem()
                {
                    ID = idx,
                    Name = Funcs.ChooseLang("Clr" + (idx + 1).ToString() + "Str"),
                    Colour = x
                };
            });
        }

        private void OKBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void RandomiseBtn_Click(object sender, RoutedEventArgs e)
        {
            var rand = new Random();
            for (var i = 0; i < 8; i++)
                ChosenColours[i] = Color.FromRgb((byte)rand.Next(256), (byte)rand.Next(256), (byte)rand.Next(256));

            LoadColours();
        }

        private void ColorPickers_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            ColorPicker clr = (ColorPicker)sender;
            ChosenColours[(int)clr.Tag] = clr.SelectedColor ?? Colors.Black;
        }
    }
}
