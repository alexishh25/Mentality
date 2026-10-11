using UnityEngine;

namespace Mentality.Gameplay.MG01
{
    // Umbrales del Figma: >=76 alta, 51-75 media, 26-50 baja, <=25 critica.
    public enum SanityBand { High, Mid, Low, Critical }

    public static class SanityBands
    {
        public static SanityBand FromNormalized(float normalized)
        {
            int percent = Mathf.RoundToInt(Mathf.Clamp01(normalized) * 100f);
            if (percent >= 76) return SanityBand.High;
            if (percent >= 51) return SanityBand.Mid;
            if (percent >= 26) return SanityBand.Low;
            return SanityBand.Critical;
        }

        public static string StatusText(SanityBand band)
        {
            switch (band)
            {
                case SanityBand.High: return MG01Texts.StatusHigh;
                case SanityBand.Mid: return MG01Texts.StatusMid;
                case SanityBand.Low: return MG01Texts.StatusLow;
                default: return MG01Texts.StatusCritical;
            }
        }
    }
}
