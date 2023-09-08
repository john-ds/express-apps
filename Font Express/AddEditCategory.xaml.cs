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

namespace Font_Express
{
    /// <summary>
    /// Interaction logic for AddEditCategory.xaml
    /// </summary>
    public partial class AddEditCategory : Window
    {
        private bool IsEditingCategory = false;
        public string ChosenName { get; set; } = "";
        public FontCategoryIcon ChosenIcon { get; set; } = FontCategoryIcon.A;

        public AddEditCategory(FontCategory? category = null)
        {
            InitializeComponent();

            // Event handlers for all window types
            CloseBtn.Click += Funcs.CloseEvent;
            TitleBtn.PreviewMouseLeftButtonDown += Funcs.MoveFormEvent;
            Activated += Funcs.ActivatedEvent;
            Deactivated += Funcs.DeactivatedEvent;

            if (category != null)
            {
                ChosenName = category.Name;
                ChosenIcon = category.Icon;
                IsEditingCategory = true;

                CategoryTxt.Text = ChosenName;
            }

            IconItems.ItemsSource = Enum.GetValues<FontCategoryIcon>().Where(x =>
            {
                return x != FontCategoryIcon.None;

            }).Select(x =>
            {
                return new SelectableIconItem()
                {
                    ID = (int)x,
                    Icon = (Viewbox)TryFindResource(MainWindow.GetCategoryIcon(x)),
                    Selected = x == ChosenIcon
                };
            });
        }

        private void OKBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CategoryTxt.Text))
            {
                Funcs.ShowMessageRes("NoCategoryNameStr", "CategoryErrorStr", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else if (!(IsEditingCategory && ChosenName == CategoryTxt.Text) && 
                MainWindow.GetSavedCategories(false).Any(x => x.Name == CategoryTxt.Text))
            {
                if (Funcs.ShowPromptRes("CategoryTakenWarningStr", "CategoryWarningStr", 
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation) != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            ChosenName = CategoryTxt.Text;
            DialogResult = true;
            Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void IconBtns_Click(object sender, RoutedEventArgs e)
        {
            ChosenIcon = (FontCategoryIcon)((RadioButton)sender).Tag;
        }
    }
}
