﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpressControls
{
    public class Glyphs : ObservableCollection<string>
    {
        public Glyphs()
        {
            string[] glyphList = new string[]
                {"A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z",
                 "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z",
                 "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "!", "?", ".", ",",
                 "À", "Á", "Â", "Ã", "Ä", "Å", "Æ", "Ç", "È", "É", "Ê", "Ë", "Ì", "Í", "Î", "Ï", "Ñ", "Ò", "Ó", "Ô", "Õ", "Ö", "Ù", "Ú", "Û", "Ü",
                 "ß", "à", "á", "â", "ã", "ä", "å", "æ", "ç", "è", "é", "ê", "ë", "ì", "í", "î", "ï", "ñ", "ò", "ó", "ô", "õ", "ö", "ù", "ú", "û",
                 "ü", "\"", "%", "&", "'", "(", ")", "*", "+", "-", "/", ":", ";", "=",
                 "<", ">", "@", "#", "$", "[", "\\", "]", "^", "_", "{", "|", "}", "~", "¡", "¢", "£", "¥", "×", "¦", "÷", "©", "«", "¬", "®", "°",
                 "±", "¹", "²", "³", "µ", "»", "¼", "½", "¾", "¿", "α", "β", "γ", "δ", "ε", "ζ", "η", "θ", "ι", "κ", "λ", "μ", "ν", "ξ", "ο", "π",
                 "ρ", "σ", "τ", "υ", "φ", "χ", "ψ", "ω", "Ø", "Ý", "ð", "ø", "ý", "ÿ"};

            foreach (string i in glyphList)
                Add(i);
        }
    }
}