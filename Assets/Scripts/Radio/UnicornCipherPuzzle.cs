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
    /// and a lookup table of its encoded form for every possible color.
    /// </summary>
    public class CipherWord
    {
        public string Original { get; }
        public CipherColor StartingColor { get; }
        public bool IsClueWord { get; }

        // Maps each color to the encoded form of the word when that color is selected
        public Dictionary<CipherColor, string> EncodedByColor { get; }

        public CipherWord(string original, CipherColor startingColor, bool isClueWord,
            string red, string orange, string yellow, string green,
            string blue, string indigo, string violet)
        {
            Original = original;
            StartingColor = startingColor;
            IsClueWord = isClueWord;

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
        /// Returns true if the player has selected the correct color (Blue).
        /// </summary>
        public bool IsSolved(CipherColor selectedColor)
        {
            return selectedColor == CipherColor.Blue;
        }
    }

    /// <summary>
    /// The full Unicorn Cipher puzzle instance.
    /// Target color: Blue (4). Clue word: "alliance".
    /// Original sentence: "We have formed an alliance with the dragons and sirens"
    /// </summary>
    public static class UnicornCipherPuzzle
    {
        public static readonly CipherColor TargetColor = CipherColor.Blue;

        public static readonly List<CipherWord> Words = new List<CipherWord>
        {
            //         original    startColor          clue    Red     Orange  Yellow  Green   Blue        Indigo  Violet
            new CipherWord("we",       CipherColor.Yellow, false,  "Zh",   "Ai",   "Bj",   "Ck",   "We",       "Xf",   "Yg"),
            new CipherWord("have",     CipherColor.Indigo, false,  "kdyh", "lezi", "mfaj", "ngbk", "have",     "ibwf", "jcxg"),
            new CipherWord("formed",   CipherColor.Green,  false,  "iruphg","jsvqih","ktwrji","luxskj","formed","gpsnfe","hqtofg"),
            new CipherWord("an",       CipherColor.Violet, false,  "dq",   "er",   "fs",   "gt",   "an",       "bo",   "cp"),
            new CipherWord("alliance", CipherColor.Blue,   true,   "dooldqfh","eppmergi","fqqnfshj","grrogtik","alliance","bmmbobodf","cnncpcepcg"),
            new CipherWord("with",     CipherColor.Orange, false,  "zlwk", "amxl", "bnym", "cozn", "with",     "xjui", "ykvj"),
            new CipherWord("the",      CipherColor.Red,    false,  "wkh",  "xli",  "ymj",  "znk",  "the",      "uif",  "vjg"),
            new CipherWord("dragons",  CipherColor.Indigo, false,  "gudjrqv","hveksrw","iwfltsx","jxgmuty","dragons","esbhpot","ftciqpu"),
            new CipherWord("and",      CipherColor.Green,  false,  "dqg",  "erh",  "fsi",  "gtj",  "and",      "boe",  "cpf"),
            new CipherWord("sirens",   CipherColor.Yellow, false,  "vluhqv","wmviri","xnwsjw","yoxtkx","sirens", "tjsfot","uktgpu"),
        };
    }
}