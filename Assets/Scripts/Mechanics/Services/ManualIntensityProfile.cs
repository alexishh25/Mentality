using System;
using System.Collections.Generic;
using Gameplay.Rhythm.Models;
namespace Gameplay.Rhythm.Services
{
    public sealed class ManualIntensityProfile
    {
        private readonly MusicalActiveInterval[] _intervals;
        private readonly double _transition;
        private readonly ManualDifficultySection[] _sections;
        public bool HasSections => _sections.Length > 0;
        public IEnumerable<double> SectionBoundaries
        {
            get { foreach (var section in _sections) { yield return section.Start; yield return section.End; } }
        }
        public int MaximumTargets { get; private set; }
        public ManualIntensityProfile(double duration, ManualIntensitySettings settings)
        {
            if (settings == null || !Finite(settings.TransitionSeconds) || settings.TransitionSeconds < 0.01f || settings.TransitionSeconds > 2f
                || settings.Intervals == null || settings.Sections == null
                || (settings.Enabled && settings.Intervals.Length == 0 && settings.Sections.Length == 0))
                throw new ArgumentException("Tramos manuales invalidos: el modo manual activo necesita al menos un intervalo.");
            _transition = settings.TransitionSeconds;
            _intervals = new MusicalActiveInterval[settings.Intervals.Length];
            double previousEnd = -1;
            for (int i = 0; i < _intervals.Length; i++)
            {
                var item = settings.Intervals[i];
                if (item == null || !Finite(item.Start) || !Finite(item.End) || item.Start < 0 || item.End <= item.Start
                    || item.End > duration || item.Start < previousEnd) throw new ArgumentException("Revisa inicio, fin y orden de los tramos manuales.");
                _intervals[i] = new MusicalActiveInterval { Start = item.Start, End = item.End };
                previousEnd = item.End;
            }
            _sections = new ManualDifficultySection[settings.Sections.Length];
            previousEnd = -1;
            for (int i = 0; i < _sections.Length; i++)
            {
                var s = settings.Sections[i];
                if (s == null || !Finite(s.Start) || !Finite(s.End) || s.Start < 0 || s.End <= s.Start || s.End > duration || s.Start < previousEnd
                    || !Finite(s.SpeedMultiplier) || s.SpeedMultiplier < 1 || s.SpeedMultiplier > 4
                    || !Finite(s.Frenzy) || s.Frenzy < 0 || s.Frenzy > 1 || s.MaximumTargets < 3 || s.MaximumTargets > 9
                    || !Finite(s.FalseProbability) || s.FalseProbability < 0 || s.FalseProbability > 1
                    || !Finite(s.MinimumInterval) || s.MinimumInterval < .15f || s.MinimumInterval > .3f
                    || s.AttackEveryBeats < 1 || s.AttackEveryBeats > 8) throw new ArgumentException("Seccion de dificultad invalida.");
                _sections[i] = new ManualDifficultySection {Start=s.Start,End=s.End,SpeedMultiplier=s.SpeedMultiplier,Frenzy=s.Frenzy,
                    MaximumTargets=s.MaximumTargets,FalseProbability=s.FalseProbability,MinimumInterval=s.MinimumInterval,AttackEveryBeats=s.AttackEveryBeats};
                MaximumTargets = Math.Max(MaximumTargets,s.MaximumTargets); previousEnd=s.End;
            }
        }
        public ManualDifficultySection SectionAt(double time)
        {
            foreach (var s in _sections) if (time >= s.Start && time < s.End) return s;
            return null;
        }
        private double Blend(double time, Func<ManualDifficultySection,double> selector, double outside)
        {
            for(int i=0;i<_sections.Length;i++)
            {
                var s=_sections[i]; if(time<s.Start || time>=s.End) continue;
                double ramp=Math.Min(_transition,(s.End-s.Start)/2d);
                double target=selector(s);
                double initial=i>0 && _sections[i-1].End==s.Start ? selector(_sections[i-1]) : outside;
                double enter=Math.Min(1d,(time-s.Start)/ramp); enter=enter*enter*(3-2*enter);
                double value=initial+(target-initial)*enter;
                if(i==_sections.Length-1 || _sections[i+1].Start!=s.End){double exit=Math.Min(1d,(s.End-time)/ramp);exit=exit*exit*(3-2*exit);value=outside+(value-outside)*exit;}
                return value;
            }
            return outside;
        }
        public double FrenzyAt(double time) => Blend(time,s=>s.Frenzy,0d);
        public double SpeedAt(double time) => Blend(time,s=>s.SpeedMultiplier,1d);
        public double? FalseProbabilityAt(double time) => SectionAt(time)?.FalseProbability;
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        public bool Contains(double time)
        {
            if(HasSections) return SectionAt(time)!=null;
            foreach (var interval in _intervals) if (time >= interval.Start && time < interval.End) return true;
            return false;
        }
        public double AmountAt(double time)
        {
            foreach (var interval in _intervals)
                if (time >= interval.Start && time < interval.End)
                {
                    double ramp = Math.Min(_transition, (interval.End - interval.Start) / 2d);
                    double value = Math.Min(1d, Math.Min((time - interval.Start) / ramp, (interval.End - time) / ramp));
                    return value * value * (3d - 2d * value);
                }
            return 0d;
        }
    }
}
