namespace Sandbox;
public static class NetworkingUtils
{
	public static bool IsReceiver( this Connection self ) => Connection.Local.Id == self.Id;
	public static bool IsReceiver( this Guid self ) => Connection.Local.Id == self;
}
