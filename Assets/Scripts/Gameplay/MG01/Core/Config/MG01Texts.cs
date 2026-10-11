namespace Mentality.Gameplay.MG01
{
    // Todos los textos de UI de MG01 en un solo lugar (punto de entrada para una futura localizacion).
    public static class MG01Texts
    {
        // Decision abierta: el GDD dice "Thomas" y CUS-012 "Tamwell".
        public const string ProtagonistName = "Thomas";

        // HUD
        public const string SanityLabel = "CORDURA";
        public const string LevelCaption = "MG01 · CANTA SI PUEDES";
        public const string AimHint = "CLIC SOBRE LAS PALABRAS HOSTILES";
        public const string SongLabel = "CANCIÓN";
        public const string AbilityName = "PUÑO";
        public const string AbilityLocked = "BLOQUEADO";
        public const string AbilityUnlocked = "DESBLOQUEADO";

        public const string StatusHigh = "MENTE ESTABLE";
        public const string StatusMid = "INQUIETUD";
        public const string StatusLow = "FRACTURA";
        public const string StatusCritical = "COLAPSO INMINENTE";
        public const string StatusHitFormat = "IMPACTO · −{0}";
        public const string DamageFormat = "−{0}";

        // Game Over
        public const string GameOverTitle = "Perdiste la cordura";
        public const string GameOverSubtitle = "El encuentro se detuvo. Vuelve a intentarlo desde el inicio.";

        // Recompensa
        public const string RewardTitle = ProtagonistName + " recupera un fragmento";
        public const string RewardLine = "Recuperaste un fragmento: tus manos recuperan el color rojo. Desbloqueaste: Golpear.";
        // Texto literal del GDD v0.2 (Reglas y Resultado, Cierre).
        public const string AlphaClosingMessage = "Gracias por jugar Mentality. Esta es una Alpha en desarrollo y, por ahora, solo incluye el nivel Canta si puedes";

        // Estadisticas
        public const string StatWordsDestroyed = "Palabras destruidas";
        public const string StatHitsTaken = "Impactos";
        public const string StatGhostClicks = "Clics en fantasmas";
        public const string StatSanityRemaining = "Cordura restante";

        // Botones
        public const string ButtonRetry = "Reintentar";
        public const string ButtonReplay = "Repetir nivel";
        public const string ButtonBackToMenu = "Volver al menú";
        public const string ButtonResume = "Reanudar";
        public const string ButtonSettings = "Ajustes";
        public const string ButtonExit = "Salir";

        public const string PauseTitle = "PAUSA";
    }
}
