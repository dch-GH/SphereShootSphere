using Sandbox.Network;

namespace ATMP;

public sealed class AISpawner : Component
{
    [Property] private GameObject _enemyPrefab { get; set; }
    [Property] private float _spawnRate { get; set; } = 3;
    private RealTimeSince _sinceLastSpawn;

    protected override void OnFixedUpdate()
    {
        if ( !Networking.IsHost )
            return;

        if ( _sinceLastSpawn < _spawnRate )
            return;

        var enemy = _enemyPrefab.Clone( Transform.Position );
        enemy.BreakFromPrefab();
        enemy.Name = Time.Now.ToString();
        enemy.NetworkSpawn();

        _sinceLastSpawn = 0;
    }
}
