namespace FogboundMaze
{
    public enum WeaponType
    {
        None,
        SubmachineGun = 1,
        LongBlade = 2
    }

    public enum GamePhase
    {
        Title,
        Guide,
        LevelSelect,
        Staging,
        Playing,
        Paused,
        Won,
        Lost
    }

    public enum EnemyState
    {
        Inactive,
        Patrol,
        Investigate,
        Chase,
        Search,
        Attack,
        Dead
    }
}
