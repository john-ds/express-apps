using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using WinDrawing = System.Drawing;

namespace ExpressControls
{
    public class FontItems : ObservableCollection<string>
    {
        public FontItems()
        {
            WinDrawing.Text.FontCollection objFontCollection = new WinDrawing.Text.InstalledFontCollection();

            foreach (string objFontFamily in objFontCollection.Families
                .Select((el) => el.Name).Where((el) => !string.IsNullOrEmpty(el)))
            {
                Add(objFontFamily);
            }
        }
    }
}
