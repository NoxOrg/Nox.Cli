namespace Nox.Cli;

public static class TypeExtensions
{
	public static bool IsSimpleType(
		this Type type)
	{
		return
			type.IsValueType ||
			type.IsPrimitive ||
			new Type[] {
				typeof(String),
				typeof(Decimal),
				typeof(DateTime),
				typeof(DateTimeOffset),
				typeof(TimeSpan),
				typeof(Guid)
			}.Contains(type) ||
			Convert.GetTypeCode(type) != TypeCode.Object;
	}
}