using System.Windows;
using ExpressControls;

namespace Type_Express
{
    /// <summary>
    /// Interaction logic for WordCount.xaml
    /// </summary>
    public partial class WordCount : ExpressWindow
    {
        public WordCount(params int[] stats)
        {
            InitializeComponent();

            // Event handlers for all window types
            CloseBtn.Click += Funcs.CloseEvent;
            TitleBtn.PreviewMouseLeftButtonDown += Funcs.MoveFormEvent;
            Activated += Funcs.ActivatedEvent;
            Deactivated += Funcs.DeactivatedEvent;
            AppLogoBtn.PreviewMouseRightButtonUp += Funcs.SystemMenuEvent;

            WordLbl.Text = stats[0].ToString();
            CharNoSpaceLbl.Text = stats[1].ToString();
            CharSpaceLbl.Text = stats[2].ToString();
            LineLbl.Text = stats[3].ToString();
        }
    }
}
