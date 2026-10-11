using System;
using Gameplay.Rhythm.Models;

namespace Gameplay.Rhythm.Services
{
    public sealed class GlitchSignal
    {
        private double _start;
        private double _until = double.NegativeInfinity;
        public void Trigger(double songTime, double duration)
        {
            if (!Finite(songTime) || !Finite(duration) || duration <= 0)
                throw new ArgumentOutOfRangeException(nameof(duration));
            _start = songTime;
            _until = songTime + duration;
        }
        public bool IsVisible(double songTime) => songTime >= _start && songTime < _until;
        public long FrameAt(double songTime, double framesPerSecond)
        {
            if (!Finite(songTime) || !Finite(framesPerSecond) || framesPerSecond < 1 || framesPerSecond > 30)
                throw new ArgumentOutOfRangeException(nameof(framesPerSecond));
            return (long)Math.Floor(Math.Max(0, songTime - _start) * framesPerSecond);
        }
        public void Clear() => _until = double.NegativeInfinity;
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        public static void Validate(GlitchSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (!Finite(settings.DurationSeconds) || settings.DurationSeconds < 0.1f
                || !Finite(settings.Darkness) || settings.Darkness < 0 || settings.Darkness > 1
                || !Finite(settings.StaticOpacity) || settings.StaticOpacity < 0 || settings.StaticOpacity > 1
                || !Finite(settings.StaticFramesPerSecond) || settings.StaticFramesPerSecond < 1 || settings.StaticFramesPerSecond > 30)
                throw new ArgumentException("Los ajustes del glitch deben ser finitos y respetar sus rangos.", nameof(settings));
        }

        // Buffer RGBA reutilizable: grano con bandas horizontales, sin asignaciones por frame.
        public static void FillStatic(byte[] rgba, int width, int height, long frame)
        {
            if (rgba == null || width <= 0 || height <= 0 || (long)width * height * 4 != rgba.Length)
                throw new ArgumentException("Buffer de estatica invalido.", nameof(rgba));
            uint seed = unchecked((uint)frame * 747796405u + 2891336453u);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    unchecked { seed = seed * 1664525u + 1013904223u; }
                    int gray = (int)(seed >> 24);
                    if ((y + frame % 17) % 17 < 2) gray /= 3;
                    int index = (y * width + x) * 4;
                    rgba[index] = rgba[index + 1] = rgba[index + 2] = (byte)gray;
                    rgba[index + 3] = 255;
                }
        }
    }
}
