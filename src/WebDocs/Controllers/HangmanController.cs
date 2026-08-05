using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using WebDocs.Models;

namespace WebDocs.Controllers
{
    [Route("api/[controller]/[action]")]
    public class HangmanController : Controller
    {
        private static readonly string[] Words = new[]
        {
            "DRAPO", "PLUMBER", "REACTIVE", "DECLARATIVE", "ATTRIBUTE",
            "FUNCTION", "COMPONENT", "TEMPLATE", "MUSTACHE", "SECTOR",
            "PIPELINE", "DATAKEY", "RENDERING", "STORAGE", "DOGFOODING"
        };

        private static readonly Random Random = new Random();
        private static int _last = -1;

        // Returns a fresh random word for the hangman board. Drapo caches the GET,
        // so the "New Game" button uses ReloadData(game) to fetch a new one.
        [HttpGet]
        public HangmanWordVM GetWord()
        {
            int index = Words.Length == 1 ? 0 : PickNext();
            _last = index;
            string word = Words[index];

            return new HangmanWordVM
            {
                Word = word,
                Answer = word.Select(c => c.ToString()).Distinct().ToList(),
                Letters = word.Select(c => new HangmanLetterVM { Char = c.ToString(), Shown = false }).ToList(),
                Left = word.Distinct().Count(),
                Wrong = 0
            };
        }

        // Pick an index different from the previous one so the same word rarely repeats.
        private int PickNext()
        {
            int index;
            do
            {
                index = Random.Next(Words.Length);
            } while (index == _last);
            return index;
        }
    }
}
