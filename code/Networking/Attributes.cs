namespace Sandbox.Network;

[Sandbox.CodeGenerator( CodeGeneratorFlags.WrapMethod | CodeGeneratorFlags.Instance, "Sandbox.Network.TargetedRPCAttribute.OnRpc" )]
public class TargetedRPCAttribute : System.Attribute
{
	public static void OnRpc( WrappedMethod m, params object[] args )
	{
		m.Resume();
	}

	public static void OnRpc( WrappedMethod m, Guid to, params object[] args )
	{
		if ( Connection.Local.Id == to )
		{
			m.Resume();
		}
	}
}

[Sandbox.CodeGenerator( CodeGeneratorFlags.WrapMethod | CodeGeneratorFlags.Instance, "Sandbox.Network.ClientRPCAttribute.OnRpc" )]
public class ClientRPCAttribute : System.Attribute
{
	public static void OnRpc( WrappedMethod m, params object[] args )
	{
		if ( m.Attributes.Any( x => x is ServerRPCAttribute ) )
		{
			Log.Error( "Tried to mark an RPC as both Client and Server..." );
			return;
		}

		if ( GameNetworkSystem.IsHost )
			return;

		m.Resume();
	}
}

[Sandbox.CodeGenerator( CodeGeneratorFlags.WrapMethod | CodeGeneratorFlags.Instance, "Sandbox.Network.ServerRPCAttribute.OnRpc" )]
public class ServerRPCAttribute : System.Attribute
{
	public static void OnRpc( WrappedMethod m, params object[] args )
	{
		if ( m.Attributes.Any( x => x is ClientRPCAttribute ) )
		{
			Log.Error( "Tried to mark an RPC as both Server and Client..." );
			return;
		}

		if ( !GameNetworkSystem.IsHost )
			return;

		m.Resume();
	}
}
