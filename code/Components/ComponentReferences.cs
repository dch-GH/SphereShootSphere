namespace Sandbox;

public class ComponentReferences : Component
{
	[Property] private DummyComponent Component1 { get; set; }
	[Property] private DummyComponent Component2 { get; set; }

	protected override void OnAwake()
	{
		Component1.DoSomething();
		Component2.DoSomething();
	}
}
