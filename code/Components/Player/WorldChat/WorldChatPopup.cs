namespace ATMP;

public sealed class WorldChatPopup : Component
{
	private RealTimeSince _lifeTime;

	protected override void OnStart()
	{
		_lifeTime = 0;
	}

	protected override void OnUpdate()
	{
		Transform.Position += Vector3.Up * Time.Delta * 10;
		Transform.Rotation = Rotation.LookAt( Scene.Camera.Transform.Rotation.Forward );
		if ( _lifeTime >= 8 )
		{
			GameObject.Destroy();
		}
	}
}
