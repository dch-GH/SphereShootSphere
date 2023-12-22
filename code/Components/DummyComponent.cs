namespace Sandbox;

public class DummyComponent : Component
{
	[Property] public string Text { get; set; }
	[Property] public int Number { get; set; }
	[Property] public Model ModelReference { get; set; }
	[Property] public ParticleSystem ParticleSystemReference { get; set; }

	public void DoSomething()
	{
		Log.Info( $"DummyComponent: {Text} , {Number}, {ModelReference}, {ParticleSystemReference}" );
	}
}
