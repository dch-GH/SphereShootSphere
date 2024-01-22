using Sandbox.Network;

namespace ATMP;

public sealed class AIDummy : Component, Component.IDamageable
{
	[Property] private ManualHitbox _hitbox { get; set; }
	private CharacterController Controller;
	private RealTimeSince _sincePickedDir;
	private Vector3 _moveDir;
	[Property] private Curve _speedRange { get; set; }
	[Property] private Curve _zRange { get; set; }
	[Property] private SoundEvent _popSound { get; set; }
	[Property] private List<GameObject> _abilityDrops { get; set; }
	private RealTimeSince _lifetime;

	protected override void OnStart()
	{
		_hitbox.Target = GameObject;

		if ( !GameNetworkSystem.IsHost )
			return;

		Controller = Components.GetOrCreate<CharacterController>();
		Network.TakeOwnership();
		_lifetime = 0;
	}

	protected override void OnUpdate()
	{
		if ( !GameNetworkSystem.IsHost )
			return;

		if ( _sincePickedDir > 4 )
		{

			var mult = 50;
			var speed = Random.Shared.Float( _speedRange[0].Value * mult, _speedRange[1].Value * mult );

			var ranAngle = Random.Shared.Float( -360, 360 );
			var ranDir = Transform.Rotation * Rotation.From( new Angles( 0, ranAngle, 0 ) );
			_moveDir = ranDir.Forward.Normal * speed;
			_sincePickedDir = 0;
		}

		var ranZ = _zRange.Evaluate( Random.Shared.Float( 0.0f, 1.0f ) ) * 10;
		Controller.Velocity += Vector3.Up * MathF.Sin( Time.Now ) * ranZ * Time.Delta;
		Controller.Accelerate( _moveDir );
		Controller.Move();

		if ( _lifetime >= 60 )
			GameObject.Destroy();
	}

	public void OnDamage( in DamageInfo damage )
	{
		Die();
	}

	[Broadcast]
	private void Die()
	{
		Sound.Play( "assets/sounds/pop.sound", Transform.Position );

		if ( Random.Shared.Next( 0, 3 ) == 1 )
			Random.Shared.FromList( _abilityDrops ).Clone( Transform.Position );

		GameObject.Destroy();
	}
}
