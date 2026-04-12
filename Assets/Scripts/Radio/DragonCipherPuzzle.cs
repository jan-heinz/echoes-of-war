using System.Collections.Generic;
using UnityEngine;

namespace DragonCipher
{
    /// <summary>
    /// A single word in the Dragon Cipher puzzle.
    /// StartingColor is the cipher color the word is displayed in at puzzle start.
    /// Clue word "sending" starts red. All others start in their cipher color and
    /// turn red when the player solves the puzzle.
    /// </summary>
    public class DragonCipherWord
    {
        public string Original      { get; }
        public string Encoded       { get; }
        public bool   IsClueWord    { get; }
        public Color  StartingColor { get; }

        public DragonCipherWord(string original, string encoded, bool isClueWord, Color startingColor)
        {
            Original      = original;
            Encoded       = encoded;
            IsClueWord    = isClueWord;
            StartingColor = startingColor;
        }
    }

    /// <summary>
    /// Dragon Cipher puzzle data.
    ///
    /// Each word is displayed in the color whose Caesar shift was used to encode it.
    /// The clue word "sending" starts red. Typing the correct answer turns all words red.
    ///
    /// Message A: "We are sending the dragons in for an airborne strike"
    ///   We(Orange +1)  are(Yellow +2)  sending(clue/Red)  the(Indigo +5)
    ///   dragons(Yellow +2)  in(Violet +6)  for(Blue +4)  an(Green +3)
    ///   airborne(Orange +1)  strike(Indigo +5)
    ///
    /// Message B: "We are sending the sirens in for a waterborne strike"
    ///   We(Orange +1)  are(Yellow +2)  sending(clue/Red)  the(Indigo +5)
    ///   sirens(Green +3)  in(Violet +6)  for(Blue +4)  a(Violet +6)
    ///   waterborne(Orange +1)  strike(Indigo +5)
    /// </summary>
    public static class DragonCipherPuzzle
    {
        // Cipher color palette
        private static readonly Color Orange = new Color(1.00f, 0.60f, 0.20f);
        private static readonly Color Yellow = new Color(1.00f, 0.92f, 0.23f);
        private static readonly Color Indigo = new Color(0.44f, 0.50f, 0.94f);
        private static readonly Color Violet = new Color(0.72f, 0.45f, 1.00f);
        private static readonly Color Blue   = new Color(0.25f, 0.70f, 1.00f);
        private static readonly Color Green  = new Color(0.22f, 0.85f, 0.40f);
        // Pastel rose — the solved "red" color, also used for the clue word from the start
        public  static readonly Color Red    = new Color(0.96f, 0.62f, 0.67f);

        // ── Message A: Dragons / airborne ────────────────────────────────────
        public static readonly List<DragonCipherWord> DragonWords = new List<DragonCipherWord>
        {
            //                  original       encoded        clue?   starting color
            new DragonCipherWord("We",         "Wf",         false,  Orange),  // e+1 → f
            new DragonCipherWord("are",        "cre",        false,  Yellow),  // a+2 → c
            new DragonCipherWord("sending",    "sending",    true,   Red),
            new DragonCipherWord("the",        "tme",        false,  Indigo),  // h+5 → m
            new DragonCipherWord("dragons",    "fragqns",    false,  Yellow),  // d+2→f, o+2→q
            new DragonCipherWord("in",         "on",         false,  Violet),  // i+6 → o
            new DragonCipherWord("for",        "fov",        false,  Blue),    // r+4 → v
            new DragonCipherWord("an",         "dn",         false,  Green),   // a+3 → d
            new DragonCipherWord("airborne",   "bircosne",   false,  Orange),  // a+1→b, b+1→c, r+1→s
            new DragonCipherWord("strike",     "xtrikj",     false,  Indigo),  // s+5→x, e+5→j
        };

        public const string DragonAnswer = "We are sending the dragons in for an airborne strike";

        // ── Message B: Sirens / waterborne ───────────────────────────────────
        public static readonly List<DragonCipherWord> SirenWords = new List<DragonCipherWord>
        {
            //                  original       encoded        clue?   starting color
            new DragonCipherWord("We",         "Wf",         false,  Orange),  // e+1 → f
            new DragonCipherWord("are",        "cre",        false,  Yellow),  // a+2 → c
            new DragonCipherWord("sending",    "sending",    true,   Red),
            new DragonCipherWord("the",        "tme",        false,  Indigo),  // h+5 → m
            new DragonCipherWord("sirens",     "viuens",     false,  Green),   // s+3→v, r+3→u
            new DragonCipherWord("in",         "on",         false,  Violet),  // i+6 → o
            new DragonCipherWord("for",        "fov",        false,  Blue),    // r+4 → v
            new DragonCipherWord("a",          "g",          false,  Violet),  // a+6 → g
            new DragonCipherWord("waterborne", "wbtfrbproe", false,  Orange),  // a+1→b, e+1→f, o+1→p, n+1→o
            new DragonCipherWord("strike",     "xtrikj",     false,  Indigo),  // s+5→x, e+5→j
        };

        public const string SirenAnswer = "We are sending the sirens in for a waterborne strike";
    }
}
