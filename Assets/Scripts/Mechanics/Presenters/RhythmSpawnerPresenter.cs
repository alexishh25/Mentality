using System;
using Gameplay.Rhythm.Factories;
using Gameplay.Rhythm.Models;

public class RhythmSpawnerPresenter
{
    private readonly AudioConductorService _conductor;
    private readonly RhythmGameModel _model;
    private readonly TargetFactory _targetFactory;

    private int _lastProcessedBeat = -1;

    public RhythmSpawnerPresenter(AudioConductorService conductor, RhythmGameModel model, TargetFactory targetFactory)
    {
        _conductor = conductor ?? throw new ArgumentNullException(nameof(conductor));
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _targetFactory = targetFactory ?? throw new ArgumentNullException(nameof(targetFactory));
    }

    // Llamar al iniciar/reiniciar, despues de que el reloj vuelva a tiempo cero.
    public void ResetBeatTracking()
    {
        // Un desfase negativo puede situar beats antes del inicio del clip.
        // Esos beats se omiten; un beat exactamente en t=0 si puede aparecer.
        _lastProcessedBeat = Math.Max(-1, (int)Math.Ceiling(_conductor.SongPositionInBeats) - 1);
    }

    public void CheckAndSpawnTargets()
    {
        if (!_model.IsPlaying || !_conductor.IsPlaying || !_conductor.HasStarted)
            return;

        int currentBeat = _conductor.CurrentBeatIndex;
        if (currentBeat < 0 || currentBeat <= _lastProcessedBeat)
            return;

        // Una sola aparicion aunque varios beats hayan pasado entre frames.
        _lastProcessedBeat = currentBeat;
        _targetFactory.CreateRandomPosition();
    }
}
