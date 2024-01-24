using ATMP.UI;
using Sandbox.Network;

namespace ATMP;

partial class PlayerController
{
	[Flags]
	public enum PlayerAbility
	{
		None = 0,
		Zoom = 1,
		BiggerBoom = 2,
		Wallbang = 4,
		LowGravity = 8,
		AirDash = 16,
	}

	[Sync, Property] public PlayerAbility Abilities { get; set; }

	private float _zoomTarget = 0;
	private float _zoomSpeed = 8;
	private RealTimeSince _sinceUnzoomed;

	private void UpdateAbilities()
	{
		if ( IsProxy )
			return;

		if ( Input.Released( GameInputActions.SecondaryAttack ) && HasAbility( PlayerAbility.Zoom ) )
			_sinceUnzoomed = 0;

		if ( Input.Down( GameInputActions.SecondaryAttack ) && HasAbility( PlayerAbility.Zoom ) )
		{
			_zoomTarget = -45;
			_aimSpeedModifier = 0.35f;
			_fovTarget = MathX.Lerp( _fovTarget, _defaultFov + _zoomTarget, Time.Delta * _zoomSpeed );
		}
		else
		{
			_zoomTarget = 0;
			_aimSpeedModifier = 1;
			var lerpIntensity = _sinceUnzoomed <= 0.5f ? 15 : 2.3f;
			_fovTarget = MathX.Lerp( _fovTarget, _defaultFov + _moveSpeed / 93, Time.Delta * lerpIntensity );
		}
	}

	public bool HasAbility( PlayerAbility ability )
	{
		return Abilities.HasFlag( ability );
	}

	public static string GetAbilityIcon( PlayerAbility ability )
	{
		const string fallBack = "assets/materials/clown-face_1f921_png_62275383.generated.vtex";
		switch ( ability )
		{
			case PlayerAbility.Zoom:
				return "assets/materials/zoom.vtex";
			case PlayerAbility.BiggerBoom:
				return "assets/materials/boom.vtex";
			case PlayerAbility.Wallbang:
				return "assets/materials/wallbang.vtex";
			case PlayerAbility.AirDash:
				return "assets/materials/dash.vtex";
			default:
				return fallBack;
		}
	}
}
