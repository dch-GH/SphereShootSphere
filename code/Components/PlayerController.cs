using Sandbox.Network;
using System.Diagnostics.CodeAnalysis;

namespace ATMP;

public class PlayerController : Component, INetworkSerializable
{
	[Property] CharacterController _cc { get; set; }
	[Property] ManualHitbox _hitbox { get; set; }
	[Property] public Vector3 Gravity { get; set; } = new Vector3( 0, 0, 800 );
	[Property] private float MoveSpeed { get; set; } = 400f;
	[Property] public GameObject Body { get; set; }
	[Property] public GameObject Eye { get; set; }
	[Property] private float RollIntensity { get; set; } = 0.05f;
	[Property] private float RollLerpSpeed { get; set; } = 1;

	public CameraComponent Cam;
	public Vector3 WishVelocity { get; private set; }
	public Angles EyeAngles;

	TimeSince _lastFootStep;
	bool _wasGrounded;
	float _footstepVolume = 4;
	GameNetworkManager _netMan;

	protected override void OnStart()
	{
		//Log.Info( $"OnStart --- IsProxy={IsProxy}, OwnerId={Network.OwnerId}, IsOwner={Network.IsOwner}" );
		_hitbox.Target = GameObject;

		if ( IsProxy )
		{
			// NOTE: The host has to call this in OnStart because IsProxy isn't valid (for the host) for proxxies.
			SetupProxy();
			return;
		}
		else
		{
			Tags.Add( GameTags.LocalPlayer );
			Body.Components.Get<ModelRenderer>().RenderType = ModelRenderer.ShadowRenderType.ShadowsOnly;
		}

		_netMan = Scene.GetAllComponents<GameNetworkManager>().First();
		Cam = Scene.GetAllComponents<CameraComponent>().First();
		if ( Cam is not null )
		{
			EyeAngles = Cam.Transform.Rotation.Angles();
			EyeAngles.roll = 0;
		}
	}

	protected override void OnAwake()
	{
		//Log.Info( $"OnAwake --- IsHost={GameNetworkSystem.IsHost} IsProxy={IsProxy}, OwnerId={Network.OwnerId}, IsOwner={Network.IsOwner}" );
		Sound.Play( "assets/sounds/join.sound", GameObject.Transform.Position );
		if ( IsProxy )
		{
			SetupProxy();
		}
	}

	protected override void OnUpdate()
	{
		// Eye input
		if ( !IsProxy )
		{
			EyeAngles.pitch = MathX.Clamp( EyeAngles.pitch += Input.MouseDelta.y * 0.1f, -89.0f, 89.0f );
			EyeAngles.yaw -= Input.MouseDelta.x * 0.1f;

			var dot = Vector3.Dot( _cc.Velocity, Cam.GameObject.Transform.Rotation.Right ) * RollIntensity;
			if ( float.IsNaN( dot ) || (int)dot == 0 )
				dot = 0;

			EyeAngles.roll = MathX.Lerp( EyeAngles.roll, dot, Time.Delta * RollLerpSpeed );

			var lookDir = EyeAngles.ToRotation();

			Cam.Transform.Position = Eye.Transform.Position;
			Cam.Transform.Rotation = lookDir;
		}

		if ( _cc is null )
			return;

		float rotateDifference = 0;

		// rotate body to look angles
		if ( Body is not null )
		{
			var targetAngle = new Angles( 0, EyeAngles.yaw, 0 ).ToRotation();

			var v = _cc.Velocity.WithZ( 0 );

			if ( v.Length > 10.0f )
			{
				targetAngle = Rotation.LookAt( v, Vector3.Up );
			}

			rotateDifference = Body.Transform.Rotation.Distance( targetAngle );

			if ( rotateDifference > 50.0f || _cc.Velocity.Length > 10.0f )
			{
				Body.Transform.Rotation = Rotation.Lerp( Body.Transform.Rotation, targetAngle, Time.Delta * 2.0f );
			}
		}
	}

	[Broadcast]
	public void OnJump()
	{
		if ( TryGetSurfaceTrace( out var tr ) )
		{
			var snd = Sound.Play( tr.Surface.Sounds.FootLaunch, Transform.Position );
			snd.Volume = _footstepVolume;
		}
	}

	protected override void OnFixedUpdate()
	{
		// Footstep sounds
		if ( _cc.Velocity.WithZ( 0 ).Length >= 200f && _cc.IsOnGround && _lastFootStep > 0.31f )
		{
			if ( TryGetSurfaceTrace( out var tr ) )
			{
				var snd = Sound.Play( Game.Random.Next( 0, 3 ) == 1 ? tr.Surface.Sounds.FootLeft : tr.Surface.Sounds.FootRight, Transform.Position );
				snd.Volume = _footstepVolume - 1;
			}
			_lastFootStep = 0;
		}

		// Landing sound
		if ( !_wasGrounded && _cc.IsOnGround )
		{
			if ( TryGetSurfaceTrace( out var tr ) )
			{
				var snd = Sound.Play( tr.Surface.Sounds.FootLand, Transform.Position );
				snd.Volume = _footstepVolume - 1;
			}
		}

		_wasGrounded = _cc.IsOnGround;

		if ( IsProxy )
			return;

		if ( Transform.Position.z <= -100 )
			Respawn();

		BuildWishVelocity();

		if ( _cc.IsOnGround && Input.Down( "Jump" ) )
		{
			float flGroundFactor = 1.0f;
			float flMul = 250;

			_cc.Punch( Vector3.Up * flMul * flGroundFactor );

			OnJump();
		}

		if ( _cc.IsOnGround )
		{
			_cc.Velocity = _cc.Velocity.WithZ( 0 );
			_cc.Accelerate( WishVelocity );
			_cc.ApplyFriction( 4.0f );
		}
		else
		{
			_cc.Velocity -= Gravity * Time.Delta * 0.5f;
			_cc.Accelerate( WishVelocity / 2 );
			_cc.ApplyFriction( 0.1f );
		}

		_cc.Move();

		if ( !_cc.IsOnGround )
		{
			_cc.Velocity -= Gravity * Time.Delta * 0.5f;
		}
		else
		{
			_cc.Velocity = _cc.Velocity.WithZ( 0 );
		}
	}

	private void SetupProxy()
	{
		Body.Components.Get<ModelRenderer>().RenderType = ModelRenderer.ShadowRenderType.On;
		Body.Components.Get<ModelRenderer>().Tint = Color.Red;
		Tags.Remove( GameTags.LocalPlayer );
		Body.Tags.Remove( GameTags.LocalPlayer );
	}

	private void BuildWishVelocity()
	{
		var rot = EyeAngles.ToRotation();

		WishVelocity = 0;

		if ( Input.Down( "Forward" ) ) WishVelocity += rot.Forward;
		if ( Input.Down( "Backward" ) ) WishVelocity += rot.Backward;
		if ( Input.Down( "Left" ) ) WishVelocity += rot.Left;
		if ( Input.Down( "Right" ) ) WishVelocity += rot.Right;

		WishVelocity = WishVelocity.WithZ( 0 );

		if ( !WishVelocity.IsNearZeroLength )
			WishVelocity = WishVelocity.Normal;

		WishVelocity *= MoveSpeed;
	}

	private bool TryGetSurfaceTrace( [NotNullWhen( true )] out SceneTraceResult tr )
	{
		tr = Scene.Trace.Ray( Transform.Position, Transform.Position + Transform.Rotation.Down * 32 ).Run();
		if ( tr.Hit && tr.Surface is not null )
			return true;

		return false;
	}

	[Authority]
	public void Respawn()
	{
		var pos = Random.Shared.FromList( _netMan.SpawnPositions );
		GameObject.Transform.Position = pos;
	}

	public void Write( ref ByteStream net )
	{
		net.Write( EyeAngles );
	}

	public void Read( ByteStream net )
	{
		EyeAngles = net.Read<Angles>();
	}
}
