namespace ATMP;

public class Devcam : Component
{
	public static Action<bool> OnToggled;

	public static bool Toggled = false;
	Angles look;

	protected override void OnUpdate()
	{
		if ( !Toggled )
			return;

		var rot = Transform.Rotation;
		var move = Vector3.Zero;

		if ( Input.Down( "Forward" ) ) move += rot.Forward;
		if ( Input.Down( "Backward" ) ) move += rot.Backward;
		if ( Input.Down( "Left" ) ) move += rot.Left;
		if ( Input.Down( "Right" ) ) move += rot.Right;

		look.pitch = MathX.Clamp( look.pitch += Input.MouseDelta.y * 0.1f, -89.0f, 89.0f );
		look.yaw -= Input.MouseDelta.x * 0.1f;

		Transform.Position += move * Time.Delta * 540;
		Transform.Rotation = look;
	}

	[ConCmd("devcam")]
	private static void ToggleDevcam()
	{
		Input.ClearActions();
		Toggled = !Toggled;
		Log.Info( $"Devcam: {Toggled}" );
		OnToggled?.Invoke( Toggled );
	}
}
