namespace ATMP;

partial class PlayerController
{
	[Flags]
	public enum PlayerAbility
	{
		None,
		Zoom,
		BiggerBoom,
		Wallbang,
		LowGravity
	}

	[Sync] public PlayerAbility Abilities { get; set; } = PlayerAbility.None;

	private float _zoomTarget = 0;
	private float _zoomSpeed = 8;
	// private RealTimeSince _sinceUnZoomed;

	private void UpdateAbilities()
	{
		if ( !IsProxy )
		{
			var justUnzoomed = false;
			if ( Input.Down( GameInputActions.SecondaryAttack ) && Abilities.HasFlag( PlayerAbility.Zoom ) )
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
}
