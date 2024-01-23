using ATMP.UI;
using Sandbox.Network;

namespace ATMP;

partial class PlayerController
{
	public enum PlayerAbility
	{
		None,
		Zoom,
		BiggerBoom,
		Wallbang,
		LowGravity,
		Count
	}

	[Sync] public List<PlayerAbility> Abilities { get; set; }

	private float _zoomTarget = 0;
	private float _zoomSpeed = 8;

	private void UpdateAbilities()
	{
		if ( !IsProxy )
		{
			var justUnzoomed = false;
			if ( Input.Down( GameInputActions.SecondaryAttack ) && HasAbility( PlayerAbility.Zoom ) )
			{
				_zoomTarget = -45;
				_aimSpeedModifier = 0.35f;
				_fovTarget = MathX.Lerp( _fovTarget, _defaultFov + _zoomTarget, Time.Delta * _zoomSpeed );
			}
			else
			{
				justUnzoomed = true;
				_zoomTarget = 0;
				_aimSpeedModifier = 1;
				var lerpAmount = justUnzoomed ? 15 : 2.3f;
				_fovTarget = MathX.Lerp( _fovTarget, _defaultFov + _moveSpeed / 93, Time.Delta * lerpAmount );
			}

		}
	}

	public bool HasAbility( PlayerAbility ability )
	{
		return Abilities.Contains( ability );
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
			default:
				return fallBack;
		}
	}
}
