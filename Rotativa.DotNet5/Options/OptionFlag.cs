using System;

namespace Rotativa.Options;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
internal class OptionFlag : Attribute
{
	public string Name { get; private set; }

	public OptionFlag(string name)
	{
		Name = name;
	}
}
