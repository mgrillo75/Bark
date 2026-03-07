using MelonLoader.Preferences;
using System.Linq;

namespace Bark.Tools;

internal class ValueList<T>(params T[] acceptableValues) : ValueValidator
{
    public readonly T[] AcceptableValues = acceptableValues;

    public override bool IsValid(object value)
    {
        if (value is T genericValue)
        {
            return AcceptableValues.Any(x => x.Equals(genericValue));
        }

        return false;
    }

    public override object EnsureValid(object value)
    {
        if (IsValid(value))
        {
            return value;
        }

        return AcceptableValues[0];
    }
}
