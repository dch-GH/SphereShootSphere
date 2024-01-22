using Sandbox.Network;
using System.Diagnostics.CodeAnalysis;

namespace ATMP;

public class PlayerController : Component
{
	[Property] public CharacterController Controller { get; private set; }
	[Property] ManualHitbox _hitbox { get; set; }
	[Property] public Vector3 Gravity { get; set; } = new Vector3( 0, 0, 800 );
	[Property] private float MoveSpeed { get; set; } = 400f;
	[Property] public GameObject Body { get; set; }
	[Property] public GameObject Eye { get; set; }
	[Property] private float RollIntensity { get; set; } = 0.05f;
	[Property] private float RollLerpSpeed { get; set; } = 1;
	[Property] public List<Material> PlayerMaterials { get; set; }

	public CameraComponent Cam;
	public Vector3 WishVelocity { get; private set; }
	private Angles _eyeAngles;
	[Sync( Query = true )]
	public Angles EyeAngles
	{
		get { return _eyeAngles; }
		set { _eyeAngles = value; }
	}

	TimeSince _lastFootStep;
	bool _wasGrounded;
	float _footstepVolume = 4;

	protected override void OnStart()
	{
		_hitbox.Target = GameObject;

		if ( IsProxy )
		{
			SetupProxy();
			return;
		}

		Sound.Play( "assets/sounds/join.sound", GameObject.Transform.Position );
		OnSpawn();

		Tags.Add( GameTags.LocalPlayer );
		var modelRenderer = Body.Components.Get<ModelRenderer>();
		modelRenderer.RenderType = ModelRenderer.ShadowRenderType.ShadowsOnly;
		Devcam.OnToggled += ( devcamOn ) =>
		{
			if ( devcamOn )
			{
				modelRenderer.RenderType = ModelRenderer.ShadowRenderType.On;
			}
			else
			{
				modelRenderer.RenderType = ModelRenderer.ShadowRenderType.ShadowsOnly;
			}
		};

		Local.Pawn = GameObject;

		Cam = Scene.GetAllComponents<CameraComponent>().First();
		if ( Cam is not null )
		{
			_eyeAngles = Cam.Transform.Rotation.Angles();
			_eyeAngles.roll = 0;
		}
	}

	[Broadcast]
	private void OnSpawn()
	{
		if ( Rpc.Caller == Connection.Local )
			return;

		Sound.Play( "assets/sounds/join.sound", GameObject.Transform.Position );
	}

	protected override void OnUpdate()
	{
		if ( Devcam.Toggled )
			return;

		// Eye input
		if ( !IsProxy )
		{
			_eyeAngles.pitch = MathX.Clamp( _eyeAngles.pitch += Input.MouseDelta.y * 0.1f, -89.0f, 89.0f );
			_eyeAngles.yaw -= Input.MouseDelta.x * 0.1f;

			var dot = Vector3.Dot( Controller.Velocity, Cam.GameObject.Transform.Rotation.Right ) * RollIntensity;
			if ( float.IsNaN( dot ) || (int)dot == 0 )
				dot = 0;

			_eyeAngles.roll = MathX.Lerp( _eyeAngles.roll, dot, Time.Delta * RollLerpSpeed );

			var lookDir = _eyeAngles.ToRotation();
			Cam.Transform.Position = Eye.Transform.Position;
			Cam.Transform.Rotation = lookDir;
		}

		if ( Controller is null )
			return;

		float rotateDifference = 0;

		// rotate body to look angles
		if ( Body is not null )
		{
			var targetAngle = new Angles( 0, _eyeAngles.yaw, 0 ).ToRotation();

			var v = Controller.Velocity.WithZ( 0 );

			if ( v.Length > 10.0f )
			{
				targetAngle = Rotation.LookAt( v, Vector3.Up );
			}

			rotateDifference = Body.Transform.Rotation.Distance( targetAngle );

			if ( rotateDifference > 50.0f || Controller.Velocity.Length > 10.0f )
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
		if ( Controller.Velocity.WithZ( 0 ).Length >= 200f && Controller.IsOnGround && _lastFootStep > 0.31f )
		{
			if ( TryGetSurfaceTrace( out var tr ) )
			{
				var snd = Sound.Play( Sandbox.Game.Random.Next( 0, 3 ) == 1 ? tr.Surface.Sounds.FootLeft : tr.Surface.Sounds.FootRight, Transform.Position );
				snd.Volume = _footstepVolume - 1;
			}
			_lastFootStep = 0;
		}

		// Landing sound
		if ( !_wasGrounded && Controller.IsOnGround )
		{
			if ( TryGetSurfaceTrace( out var tr ) )
			{
				var snd = Sound.Play( tr.Surface.Sounds.FootLand, Transform.Position );
				snd.Volume = _footstepVolume - 1;
			}
		}

		_wasGrounded = Controller.IsOnGround;

		if ( IsProxy )
			return;

		if ( Transform.Position.z <= -100 )
			Respawn();

		BuildWishVelocity();

		if ( Controller.IsOnGround && Input.Down( "Jump" ) )
		{
			float flGroundFactor = 1.0f;
			float flMul = 250;

			Controller.Punch( Vector3.Up * flMul * flGroundFactor );

			OnJump();
		}

		if ( Controller.IsOnGround )
		{
			Controller.Velocity = Controller.Velocity.WithZ( 0 );
			Controller.Accelerate( WishVelocity );
			Controller.ApplyFriction( 4.0f );
		}
		else
		{
			Controller.Velocity -= Gravity * Time.Delta * 0.5f;
			Controller.Accelerate( WishVelocity / 2 );
			Controller.ApplyFriction( 0.1f );
		}

		Controller.Move();

		if ( !Controller.IsOnGround )
		{
			Controller.Velocity -= Gravity * Time.Delta * 0.5f;
		}
		else
		{
			Controller.Velocity = Controller.Velocity.WithZ( 0 );
		}
	}

	protected override void OnDestroy()
	{
		Local.Pawn = null;
		Local.Client = null;
	}

	private void SetupProxy()
	{
		if ( Body.Components.TryGet<ModelRenderer>( out var modelRenderer ) )
		{
			modelRenderer.MaterialOverride = Random.Shared.FromList( PlayerMaterials );
			modelRenderer.RenderType = ModelRenderer.ShadowRenderType.On;
		}
		Tags.Remove( GameTags.LocalPlayer );
		Body.Tags.Remove( GameTags.LocalPlayer );
	}

	private void BuildWishVelocity()
	{
		if ( Devcam.Toggled )
			return;

		var rot = _eyeAngles.ToRotation();

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
		var sp = Random.Shared.FromList( GameNetwork.Instance.SpawnPoints );
		GameObject.Transform.Position = sp.Transform.Position;
	}
}
