using Bark.Tools;
using MelonLoader;
using System;
using UnityEngine;

namespace Bark.Extensions
{
    public static class ConfigExtensions
    {
        public struct ConfigValueInfo
        {
            public object[] AcceptableValues;
            public int InitialValue;
        }

        public static ConfigValueInfo ValuesInfo(this MelonPreferences_Entry entry)
        {
            Type settingType = entry.GetReflectedType();

            if (settingType == typeof(bool))
            {
                return new ConfigValueInfo
                {
                    AcceptableValues = [false, true],
                    InitialValue = (bool)entry.BoxedValue ? 1 : 0
                };
            }
            else if (settingType == typeof(int))
            {
                return new ConfigValueInfo
                {
                    AcceptableValues = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10],
                    InitialValue = Mathf.Clamp((int)entry.BoxedValue, 0, 10)
                };
            }
            else if (settingType == typeof(string))
            {
                var acceptableValues = ((ValueList<string>)entry.Validator)?.AcceptableValues;
                for (int i = 0; i < acceptableValues.Length; i++)
                {
                    if (acceptableValues[i] == (string)entry.BoxedValue)
                        return new ConfigValueInfo
                        {
                            AcceptableValues = acceptableValues,
                            InitialValue = i
                        };
                }
            }

            throw new Exception($"Unknown config type {settingType.FullName}");
        }
    }
}
