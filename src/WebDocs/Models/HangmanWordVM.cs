using System.Collections.Generic;

namespace WebDocs.Models
{
    public class HangmanWordVM
    {
        // The full word, revealed in the "you lost" banner.
        public string Word { set; get; }

        // Distinct letters of the word, used to test whether a guess is correct.
        public List<string> Answer { set; get; }

        // One tile per letter, in order; Shown flips to true once its letter is guessed.
        public List<HangmanLetterVM> Letters { set; get; }

        // Number of distinct letters still to reveal; the game is won when this reaches 0.
        public int Left { set; get; }

        // Number of wrong guesses so far; the game is lost at 6.
        public int Wrong { set; get; }
    }

    public class HangmanLetterVM
    {
        public string Char { set; get; }
        public bool Shown { set; get; }
    }
}
