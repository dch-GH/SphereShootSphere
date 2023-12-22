using Sandbox.Network;
using static Sandbox.Component;

namespace ATMP;

public class Weapon : Component, IRenderOverlay
{
	[Property] public SceneParticleManager Beam { get; set; }
	[Property] public SceneParticleManager Splash { get; set; }

	private float _gunCharge = 1.0f;
	private float _xHairSize = 6;
	private PlayerController _player;

	protected override void OnStart()
	{
		if ( IsProxy )
			return;

		_player = GameObject.Components.Get<PlayerController>();
	}

	protected override void OnUpdate()
	{
		if ( !IsProxy && Input.Pressed( GameInputActions.PrimaryAttack ) && _gunCharge >= 1.0f )
		{
			_gunCharge = 0.0f;

			var eyes = _player.Eye.Transform.Position;
			var fwd = _player.Cam.GameObject.Transform.LocalRotation.Forward;
			var tr = Scene.Trace.Ray( eyes, eyes + fwd * 5000 ).UseHitboxes().Run();

			ShootEffect( eyes, tr.Hit, tr.Hit ? tr.HitPosition : eyes + fwd * 2500 );

			if ( !tr.Hit || tr.Hitbox is null )
				return;

			var go = tr.Hitbox.GameObject;
			if ( go.Components.TryGet<PlayerController>( out var victim ) )
			{
				_player.Score += 1;
				victim.Respawn();
			}
		}

		_gunCharge = MathX.Clamp( _gunCharge += Time.Delta, 0f, 1.0f );
	}

	[Broadcast]
	private void ShootEffect( Vector3 eye, bool traceHit, Vector3 dest )
	{
		if ( traceHit )
		{
			var splash = Splash.Spawn();
			splash.SetControlPoint( 0, dest );
		}

		var beam = Beam.Spawn();
		var origin = eye + Vector3.Down * 16 + Vector3.Right * 8;

		var dir = (dest - origin).Normal;
		//beam.SetNamedValue( "beam_dir", Vector3.Down );
		beam.SetControlPoint( 0, origin );
		beam.SetControlPoint( 1, dest );
	}

	public void OnRenderOverlay( SceneCamera camera )
	{
		if ( IsProxy )
			return;

		var canShoot = _gunCharge >= 1.0f;
		Draw2D.Circle( Screen.Size / 2, outerRadius: _xHairSize, innerRadius: _xHairSize - 2, color: canShoot ? Color.White.WithAlpha( 0.9f ) : Color.White.Darken( 0.25f ), pointCount: 64 );

		{
			const int height = 24;
			var x = Screen.Width / 3;
			var y = Screen.Height - height;
			var mapped = MathX.Remap( _gunCharge, 0.0f, 1.0f, x, Screen.Width - Screen.Width / 3 );
			Draw2D.Line( Color.White, height, new Vector2( x, y ), new Vector2( mapped, y ) );
		}
	}
}
