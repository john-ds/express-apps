using System.Collections.ObjectModel;
using System.Linq;
using WinDrawing = System.Drawing;

namespace ExpressControls
{
    public class FontItems : ObservableCollection<string>
    {
        public FontItems() { }

        public FontItems(bool load = false)
        {
            if (load)
            {
                WinDrawing.Text.FontCollection objFontCollection =
                    new WinDrawing.Text.InstalledFontCollection();

                foreach (
                    string objFontFamily in objFontCollection
                        .Families.Select((el) => el.Name)
                        .Where((el) => !string.IsNullOrEmpty(el))
                )
                {
                    Add(objFontFamily);
                }
            }
        }
    }
}
