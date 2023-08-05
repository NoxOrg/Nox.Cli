using System.Collections;

namespace Nox.Cli.Variables;

public static class TypeExtensions
{
	public static bool IsSimpleType(
		this Type type)
	{
		return
			type.IsPrimitive ||
			type.IsEnum ||
			new Type[] {
				typeof(String),
				typeof(Decimal),
				typeof(DateTime),
				typeof(DateTimeOffset),
				typeof(TimeSpan),
				typeof(Guid),
				typeof(Uri)
			}.Contains(type) ||
			Convert.GetTypeCode(type) != TypeCode.Object;
	}

	public static bool IsEnumerable(this Type type)
	{
		return type.GetInterfaces().Any(i => i == typeof(IEnumerable));
	}

	public static bool IsDictionary(this Type type)
	{
		return type.GetInterfaces().Any(i => i == typeof(IDictionary));
	}
}