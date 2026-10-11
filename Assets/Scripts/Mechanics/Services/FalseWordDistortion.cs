using System;
using System.Collections.Generic;
using System.Globalization;

namespace Gameplay.Rhythm.Services
{
    // Intercambia letras sin separar acentos ni pares Unicode.
    public sealed class FalseWordDistortion
    {
        private readonly string[] _variants;
        public FalseWordDistortion(string word)
        {
            if (word == null) throw new ArgumentNullException(nameof(word));
            var elements = new List<string>();
            var iterator = StringInfo.GetTextElementEnumerator(word);
            while (iterator.MoveNext()) elements.Add(iterator.GetTextElement());
            var variants = new List<string>();
            for (int a = 0; a < elements.Count; a++)
                for (int b = a + 1; b < elements.Count; b++)
                {
                    if (!char.IsLetter(elements[a], 0) || !char.IsLetter(elements[b], 0)
                        || elements[a] == elements[b]) continue;
                    var copy = elements.ToArray();
                    copy[a] = elements[b]; copy[b] = elements[a];
                    string variant = string.Concat(copy);
                    if (!variants.Contains(variant)) variants.Add(variant);
                }
            _variants = variants.Count == 0 ? new[] { word } : variants.ToArray();
        }
        public string At(double elapsedSongSeconds, double intervalSeconds)
        {
            if (double.IsNaN(elapsedSongSeconds) || double.IsInfinity(elapsedSongSeconds)
                || double.IsNaN(intervalSeconds) || double.IsInfinity(intervalSeconds) || intervalSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(intervalSeconds));
            double step = Math.Floor(Math.Max(0, elapsedSongSeconds) / intervalSeconds);
            return _variants[(int)(step % _variants.Length)];
        }
    }
}
