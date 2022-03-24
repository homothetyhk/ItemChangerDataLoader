namespace ItemChangerDataLoader
{
    public enum BackupRandoType
    {
        Manual,
        Automatic,
        None
    }

    public class GlobalSettings
    {

        public BackupRandoType BackupNewRandoSaves = BackupRandoType.Manual;
    }
}
