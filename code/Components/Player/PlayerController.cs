﻿using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace ATMP;

public sealed partial class PlayerController : Sandbox.Component
{

	[Property] public CharacterController Controller { get; private set; }
	[Property] ModelRenderer _renderer { get; set; }
	[Property] ManualHitbox _hitbox { get; set; }
	[Property] public Vector3 Gravity { get; set; } = new Vector3( 0, 0, 800 );
	[Property] private float MoveSpeed { get; set; } = 400f;
	[Property] public float DashIntensity { get; private set; } = 300;
	[Property] public GameObject Body { get; set; }
	[Property] public GameObject Eye { get; set; }
	[Property] private float RollIntensity { get; set; } = 0.05f;
	[Property] private float RollLerpSpeed { get; set; } = 1;
	[Property] public List<Material> PlayerMaterials { get; set; }

	public Vector3 WishVelocity { get; private set; }
	public bool ShowFastAbilityToasts { get; private set; }

	[Sync( Query = true )]
	public Angles EyeAngles
	{
		get { return _eyeAngles; }
		set { _eyeAngles = value; }
	}

	private Angles _eyeAngles;
	public CameraComponent Cam; // TODO: Aimray

	TimeSince _lastFootStep;
	bool _wasGrounded;
	float _footstepVolume = 4;

	const float _defaultFov = 90;
	float _fovTarget = _defaultFov;
	private float _aimSpeedModifier = 1;
	private float _moveSpeed;
	private float _timeAirbornSecs;
	private int _numDashes;

	protected override void OnStart()
	{
		_hitbox.Target = GameObject;

		if ( IsProxy )
		{
			SetupProxy();
			return;
		}

		Abilities = new();
		Local.OnSpawned?.Invoke( GameObject );
		Sound.Play( "assets/sounds/join.sound", GameObject.Transform.Position );
		SpawnRPC();

		Tags.Add( GameTags.LocalPlayer );

		_renderer.RenderType = ModelRenderer.ShadowRenderType.ShadowsOnly;
		Devcam.OnToggled += ( devcamOn ) =>
		{
			if ( devcamOn )
			{
				_renderer.RenderType = ModelRenderer.ShadowRenderType.On;
			}
			else
			{
				_renderer.RenderType = ModelRenderer.ShadowRenderType.ShadowsOnly;
			}
		};

		Local.Pawn = GameObject;
		Local.Player = this;

		Cam = Scene.GetAllComponents<CameraComponent>().First();
		if ( Cam is not null )
		{
			_eyeAngles = Cam.Transform.Rotation.Angles();
			_eyeAngles.roll = 0;
		}
	}

	[Broadcast]
	private void SpawnRPC()
	{
		if ( Rpc.Caller == Connection.Local )
			return;

		Sound.Play( "assets/sounds/join.sound", GameObject.Transform.Position );
	}

	protected override void OnUpdate()
	{
		Cam ??= Scene.GetAllComponents<CameraComponent>().First();

		UpdateAbilities();

		if ( Devcam.Toggled )
			return;

		// Eye input
		if ( !IsProxy )
		{
			_eyeAngles.pitch = MathX.Clamp( _eyeAngles.pitch += Input.MouseDelta.y * 0.1f * _aimSpeedModifier, -89.0f, 89.0f );
			_eyeAngles.yaw -= Input.MouseDelta.x * 0.1f * _aimSpeedModifier;

			var dot = Vector3.Dot( Controller.Velocity, Cam.GameObject.Transform.Rotation.Right ) * RollIntensity;
			if ( float.IsNaN( dot ) || (int)dot == 0 )
				dot = 0;

			_eyeAngles.roll = MathX.Lerp( _eyeAngles.roll, dot, Time.Delta * RollLerpSpeed );

			var lookDir = _eyeAngles.ToRotation();
			Cam.Transform.Position = Eye.Transform.Position;
			Cam.Transform.Rotation = lookDir;

			Cam.FieldOfView = _fovTarget;

			// Bunnyhop is too fast for OnFixedUpdate
			// to catch IsOnGround
			if ( Controller.IsOnGround )
			{
				_numDashes = 0;
			}
		}

		if ( Controller is null )
			return;

		_moveSpeed = Controller.Velocity.Length;
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

		// TODO: Do we need this?
		if ( Transform.Position.z <= -1000 )
		{
			SetPosition( Vector3.Up * 64 );
		}

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
			_timeAirbornSecs = 0;
		}
		else
		{
			Controller.Velocity -= Gravity * Time.Delta * 0.5f;
			Controller.Accelerate( WishVelocity / 2 );
			Controller.ApplyFriction( 0.1f );
			_timeAirbornSecs += Time.Delta;
		}

		if ( Input.Pressed( GameInputActions.Jump ) && HasAbility( PlayerAbility.AirDash ) && _timeAirbornSecs >= 0.20f && _numDashes <= 0 )
		{
			var mvDir = Input.AnalogMove.y;
			if ( mvDir != 0 )
			{
				var eyesRot = _eyeAngles.ToRotation();
				var dashDir = eyesRot.Right * -mvDir * DashIntensity;
				Controller.Punch( dashDir + Vector3.Up * DashIntensity / 1.5f );
				_numDashes += 1;
			}
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
		if ( IsProxy )
			return;

		Local.Pawn = null;
		Local.Player = null;
		Local.Client = null;
	}

	private void SetupProxy()
	{
		_renderer ??= Body.Components.Get<ModelRenderer>();
		{
			_renderer.MaterialOverride = Random.Shared.FromList( PlayerMaterials );
			_renderer.RenderType = ModelRenderer.ShadowRenderType.On;
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
		if ( !IsProxy )
		{
			// You died noob, start over
			Abilities = PlayerAbility.None;
			ShowFastAbilityToasts = true;
		}

		var sp = Random.Shared.FromList( GameNetwork.Instance.SpawnPoints );
		SetPosition( sp.Transform.Position );
	}

	[Authority]
	private void SetPosition( Vector3 pos )
	{
		GameObject.Transform.Position = pos;
	}
}
