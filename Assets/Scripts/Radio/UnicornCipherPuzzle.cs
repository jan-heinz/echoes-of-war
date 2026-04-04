using System.Collections.Generic;

namespace UnicornCipher
{
    public enum CipherColor
    {
        Red = 0,
        Orange = 1,
        Yellow = 2,
        Green = 3,
        Blue = 4,
        Indigo = 5,
        Violet = 6
    }

    /// <summary>
    /// Represents a single word in the Unicorn Cipher puzzle.
    /// Each word stores its correct (decoded) form, its assigned starting color,
    /// its correct color, and a lookup table of its encoded form for every possible color.
    /// </summary>
    public class CipherWord
    {
        public string Original { get; }
        public CipherColor StartingColor { get; }
        public CipherColor CorrectColor { get; }
        public bool IsClueWord { get; }

        // Maps each color to the encoded form of the word when that color is selected
        public Dictionary<CipherColor, string> EncodedByColor { get; }

        public CipherWord(string original, CipherColor startingColor, bool isClueWord, CipherColor correctColor,
            string red, string orange, string yellow, string green,
            string blue, string indigo, string violet)
        {
            Original = original;
            StartingColor = startingColor;
            IsClueWord = isClueWord;
            CorrectColor = correctColor;

            EncodedByColor = new Dictionary<CipherColor, string>
            {
                { CipherColor.Red,    red    },
                { CipherColor.Orange, orange },
                { CipherColor.Yellow, yellow },
                { CipherColor.Green,  green  },
                { CipherColor.Blue,   blue   },
                { CipherColor.Indigo, indigo },
                { CipherColor.Violet, violet }
            };
        }

        /// <summary>
        /// Returns the encoded word for the given color selection.
        /// </summary>
        public string GetEncoded(CipherColor selectedColor)
        {
            return EncodedByColor[selectedColor];
        }

        /// <summary>
        /// Returns true if the player has selected this word's correct color.
        /// </summary>
        public bool IsSolved(CipherColor selectedColor)
        {
            return selectedColor == CorrectColor;
        }
    }

    /// <summary>
    /// The full Unicorn Cipher puzzle instance.
    /// Each word has a different correct color. Clue word: "alliance" (Blue).
    /// Original sentence: "We have formed an alliance with the dragons and sirens"
    ///
    /// Encoding: Caesar shift = (displayColor - correctColor + 7) % 7
    /// Correct colors: We=Red, have=Orange, formed=Yellow, an=Green, alliance=Blue,
    ///                 with=Indigo, the=Violet, dragons=Yellow, and=Red, sirens=Green
    /// </summary>
    public static class UnicornCipherPuzzle
    {
        public static readonly List<CipherWord> Words = new List<CipherWord>
        {
            //         original    startColor          clue   correctColor          Red      Orange   Yellow   Green    Blue     Indigo   Violet
            // wrong-color encodings have only 1-2 letters shifted so each word looks nearly correct
            new CipherWord("we",       CipherColor.Yellow, false, CipherColor.Red,    "We",    "Wf",    "Ye",    "Wh",    "Ae",    "Wj",    "Ce"),
            new CipherWord("have",     CipherColor.Indigo, false, CipherColor.Orange, "hgve",  "have",  "iave",  "haxe",  "havh",  "lave",  "hfve"),
            new CipherWord("formed",   CipherColor.Green,  false, CipherColor.Yellow, "kowmed","furmej","formed","gorned","fotmgd","frrmeg","jormid"),
            new CipherWord("an",       CipherColor.Violet, false, CipherColor.Green,  "en",    "as",    "gn",    "an",    "ao",    "cn",    "aq"),
            new CipherWord("alliance", CipherColor.Blue,   true,  CipherColor.Blue,   "dooldqfh","eppmergi","fqqnfshj","grrogtik","alliance","bmmbobodf","cnncpcepcg"),
            new CipherWord("with",     CipherColor.Orange, false, CipherColor.Indigo, "wkth",  "witk",  "aith",  "wnth",  "wizh",  "with",  "witi"),
            new CipherWord("the",      CipherColor.Red,    false, CipherColor.Violet, "thf",   "tje",   "whe",   "thi",   "tme",   "zhe",   "the"),
            new CipherWord("dragons",  CipherColor.Indigo, false, CipherColor.Yellow, "iragtns","dxagots","dragons","drbgont","draiops","gragrns","dvakons"),
            new CipherWord("and",      CipherColor.Green,  false, CipherColor.Red,    "and",   "bnd",   "anf",   "aqd",   "end",   "ani",   "atd"),
            new CipherWord("sirens",   CipherColor.Yellow, false, CipherColor.Green,  "wivens","xiwens","soreny","sirens","sirfos","skrgns","siuenv"),
        };
    }
}
