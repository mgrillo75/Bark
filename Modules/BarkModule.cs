using Bark.Networking;
using Bark.Tools;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Bark.Modules
{
    public abstract class BarkModule : MonoBehaviour
    {
        public List<MelonPreferences_Entry> ConfigEntries;
        public static BarkModule LastEnabled;
        public static Dictionary<string, bool> enabledModules = [];
        public static string enabledModulesKey = "BarkEnabledModules";

        protected virtual void ReloadConfiguration() { }

        public abstract string GetDisplayName();

        protected void SettingsChanged(string path)
        {
            if (Path.GetFileName(path) == "Bark.cfg") ReloadConfiguration();
        }

        public abstract string Tutorial();
        public ButtonController button;

        protected abstract void Cleanup();

        protected virtual void Start()
        {
            this.enabled = false;

        }

        protected virtual void OnEnable()
        {
            LastEnabled = this;
            MelonPreferences.OnPreferencesSaved.Subscribe(SettingsChanged);
            if (this.button)
                this.button.IsPressed = true;

            SetStatus(true);
        }

        protected virtual void OnDisable()
        {
            MelonPreferences.OnPreferencesSaved.Unsubscribe(SettingsChanged);
            if (this.button)
                this.button.IsPressed = false;
            this.Cleanup();
            SetStatus(false);
        }

        public void SetStatus(bool enabled)
        {
            string name = GetDisplayName();
            if (enabledModules.ContainsKey(name))
                enabledModules[name] = enabled;
            else
                enabledModules.Add(name, enabled);
            NetworkPropertyHandler.Instance?.ChangeProperty(enabledModulesKey, enabledModules);
        }

        protected virtual void OnDestroy()
        {
            this.Cleanup();
        }

        public static List<Type> GetBarkModuleTypes()
        {
            try
            {
                var types = Assembly.GetExecutingAssembly().GetTypes().Where(t => typeof(BarkModule).IsAssignableFrom(t)).ToList();
                types.Sort((x, y) =>
                {
                    FieldInfo xField = x.GetField("DisplayName", BindingFlags.Public | BindingFlags.Static);
                    FieldInfo yField = y.GetField("DisplayName", BindingFlags.Public | BindingFlags.Static);

                    if (xField == null || yField == null)
                        return 0;

                    string xValue = (string)xField.GetValue(null);
                    string yValue = (string)yField.GetValue(null);

                    return string.Compare(xValue, yValue);
                });
                return types;
            }
            catch (ReflectionTypeLoadException ex)
            {
                Logging.Exception(ex);
                Logging.Warning("Inner exceptions:");
                foreach (Exception inner in ex.LoaderExceptions)
                {
                    Logging.Exception(inner);
                }
            }
            return null;
        }

    }
}
