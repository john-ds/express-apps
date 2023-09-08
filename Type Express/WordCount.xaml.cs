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

namespace Type_Express
{
    /// <summary>
    /// Interaction logic for WordCount.xaml
    /// </summary>
    public partial class WordCount : Window
    {
        public WordCount(params int[] stats)
        {
            InitializeComponent();

            // Event handlers for all window types
            CloseBtn.Click += Funcs.CloseEvent;
            TitleBtn.PreviewMouseLeftButtonDown += Funcs.MoveFormEvent;
            Activated += Funcs.ActivatedEvent;
            Deactivated += Funcs.DeactivatedEvent;

            WordLbl.Text = stats[0].ToString();
            CharNoSpaceLbl.Text = stats[1].ToString();
            CharSpaceLbl.Text = stats[2].ToString();
            LineLbl.Text = stats[3].ToString();
        }
    }
}
