using Bark.Extensions;
using Bark.Interaction;
using Bark.Modules;
using Bark.Modules.Misc;
using Bark.Modules.Movement;
using Bark.Modules.Multiplayer;
using Bark.Modules.Physics;
using Bark.Modules.Teleportation;
using Bark.Tools;
using GorillaLibrary.Models;
using MelonLoader;
using Photon.Pun;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;
using Player = GorillaLocomotion.GTPlayer;

namespace Bark.GUI
{
    public class MenuController : BarkGrabbable
    {
        public static MenuController Instance;
        public bool Built { get; private set; }
        public Vector3
            initialMenuOffset = new(0, .035f, .65f),
            btnDimensions = new(.3f, .05f, .05f);
        public Rigidbody _rigidbody;
        private List<Transform> modPages;
        private List<ButtonController> buttons;
        public List<BarkModule> modules;
        public GameObject modPage, settingsPage;
        public Text helpText;
        public static InputTracker SummonTracker;
        public static MelonPreferences_Entry<string> SummonInput;
        public static MelonPreferences_Entry<string> SummonInputHand;
        bool docked;

        protected override void Awake()
        {
            Instance = this;
            try
            {
                Logging.Debug("Awake");
                base.Awake();
                this.throwOnDetach = true;
                gameObject.AddComponent<PositionValidator>();
                MelonPreferences.OnPreferencesSaved.Subscribe(SettingsChanged);
                modules =
                [
                    // Locomotion
                    gameObject.AddComponent<Airplane>(),
                    gameObject.AddComponent<Bubble>(),
                    gameObject.AddComponent<Fly>(),
                    gameObject.AddComponent<GrapplingHooks>(),
                    //gameObject.AddComponent<Climb>(),
                    gameObject.AddComponent<Platforms>(),
                    gameObject.AddComponent<NailGun>(),
                    gameObject.AddComponent<Rockets>(),
                    gameObject.AddComponent<SpeedBoost>(),
                    //gameObject.AddComponent<Swim>(),
                    gameObject.AddComponent<Wallrun>(),
                    gameObject.AddComponent<Zipline>(),

                    //// Physics
                    gameObject.AddComponent<LowGravity>(),
                    gameObject.AddComponent<NoCollide>(),
                    gameObject.AddComponent<NoSlip>(),
                    gameObject.AddComponent<Potions>(),
                    gameObject.AddComponent<SlipperyHands>(),

                    //// Teleportation
                    gameObject.AddComponent<Checkpoint>(),
                    //gameObject.AddComponent<Portal>(),
                    gameObject.AddComponent<Pearl>(),
                    gameObject.AddComponent<Teleport>(),
                
                    //// Multiplayer
                    gameObject.AddComponent<Boxing>(),
                    gameObject.AddComponent<Piggyback>(),
                    gameObject.AddComponent<Telekinesis>(),
                    gameObject.AddComponent<Fireflies>(),
                    gameObject.AddComponent<XRay>(),

                    //// Misc
                    // gameObject.AddComponent<Lobby>(),
                ];

                Halo halo = gameObject.AddComponent<Halo>();
                if (PhotonNetwork.LocalPlayer.UserId == "JD3moEFc6tOGYSAp4MjKsIwVycfrAUR5nLkkDNSvyvE=".DecryptString())
                    modules.Add(halo);
                ReloadConfiguration();
            }
            catch (Exception e) { Logging.Exception(e); }
        }

        private void ReloadConfiguration()
        {
            SummonTracker?.OnPressed -= Summon;
            GestureTracker.Instance.OnMeatBeat -= Summon;

            var hand = SummonInputHand.Value == "left"
                ? XRNode.LeftHand : XRNode.RightHand;

            if (SummonInput.Value == "gesture")
            {
                GestureTracker.Instance.OnMeatBeat += Summon;
            }
            else
            {
                SummonTracker = GestureTracker.Instance.GetInputTracker(
                    SummonInput.Value, hand
                );

                SummonTracker?.OnPressed += Summon;
            }
        }

        void SettingsChanged(string path)
        {
            if (Path.GetFileName(path) == "Bark.cfg") ReloadConfiguration();
        }

        void Summon(InputTracker _) { Summon(); }

        void Summon()
        {
            if (!Built)
                BuildMenu();
            else
                ResetPosition();
        }

        void FixedUpdate()
        {
            if (Keyboard.current.bKey.wasPressedThisFrame)
            {
                if (!docked)
                    Summon();
                else
                {
                    _rigidbody.isKinematic = false;
                    _rigidbody.useGravity = true;
                    transform.SetParent(null);
                    AddBlockerToAllButtons(ButtonController.Blocker.MENU_FALLING);
                    docked = false;
                }
            }

            // The potions tutorial needs to be updated frequently to keep the current size
            // up-to-date, even when the mod is disabled
            if (BarkModule.LastEnabled && BarkModule.LastEnabled == Potions.Instance)
            {
                helpText.text = Potions.Instance.Tutorial();
            }
        }

        void ResetPosition()
        {
            _rigidbody.isKinematic = true;
            _rigidbody.linearVelocity = Vector3.zero;
            transform.SetParent(Player.Instance.bodyCollider.transform);
            transform.localPosition = initialMenuOffset;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            foreach (var button in buttons)
            {
                button.RemoveBlocker(ButtonController.Blocker.MENU_FALLING);
            }
            docked = true;
        }

        void BuildMenu()
        {
            Logging.Debug("Building menu...");
            try
            {
                helpText = this.gameObject.transform.Find("Help Canvas").GetComponentInChildren<Text>();
                helpText.text = "Enable a module to see its tutorial.";
                this.gameObject.transform.Find("Version Canvas").GetComponentInChildren<Text>().text =
                    $"{Melon<Plugin>.Instance.Info.Name} {Melon<Plugin>.Instance.Info.Version}";

                var collider = this.gameObject.GetOrAddComponent<BoxCollider>();
                collider.isTrigger = true;
                _rigidbody = gameObject.GetComponent<Rigidbody>();
                _rigidbody.isKinematic = true;

                SetupInteraction();
                SetupModPages();
                SetupSettingsPage();

                transform.SetParent(Player.Instance.bodyCollider.transform);
                ResetPosition();
                Logging.Debug("Build successful.");
            }
            catch (Exception ex) { Logging.Warning(ex.Message); Logging.Warning(ex.StackTrace); return; }
            Built = true;
        }

        private void SetupSettingsPage()
        {
            GameObject button = this.gameObject.transform.Find("Settings Button").gameObject;
            ButtonController btnController = button.AddComponent<ButtonController>();
            buttons.Add(btnController);
            btnController.OnPressed += (obj, pressed) =>
            {
                settingsPage.SetActive(pressed);
                if (pressed)
                    settingsPage.GetComponent<SettingsPage>().UpdateText();
                modPage.SetActive(!pressed);
            };

            settingsPage = this.transform.Find("Settings Page").gameObject;
            settingsPage.AddComponent<SettingsPage>();
            settingsPage.SetActive(false);
        }

        public static bool debugger = true;
        public void SetupModPages()
        {
            var modPageTemplate = this.gameObject.transform.Find("Mod Page");
            int buttonsPerPage = modPageTemplate.childCount - 2; // Excludes the prev/next page btns
            int numPages = ((modules.Count - 1) / buttonsPerPage) + 1;

#if DEBUG
            numPages++;
#endif

            modPages = [modPageTemplate];
            for (int i = 0; i < numPages - 1; i++)
                modPages.Add(Instantiate(modPageTemplate, this.gameObject.transform));

            buttons = [];
            for (int i = 0; i < modules.Count; i++)
            {
                var module = modules[i];

                var page = modPages[i / buttonsPerPage];
                var button = page.Find($"Button {i % buttonsPerPage}").gameObject;

                ButtonController btnController = button.AddComponent<ButtonController>();
                buttons.Add(btnController);
                btnController.OnPressed += (obj, pressed) =>
                {
                    module.enabled = pressed;
                    if (pressed)
                        helpText.text = module.GetDisplayName().ToUpper() +
                            "\n\n" + module.Tutorial().ToUpper();
                };
                module.button = btnController;
                btnController.SetText(module.GetDisplayName().ToUpper());
            }

            AddDebugButtons();

            foreach (Transform modPage in modPages)
            {
                foreach (Transform button in modPage)
                {
                    if (button.name == "Button Left" && modPage != modPages[0])
                    {
                        var btnController = button.gameObject.AddComponent<ButtonController>();
                        btnController.OnPressed += PreviousPage;
                        btnController.SetText("Prev Page");
                        buttons.Add(btnController);
                        continue;
                    }
                    else if (button.name == "Button Right" && modPage != modPages[^1])
                    {
                        var btnController = button.gameObject.AddComponent<ButtonController>();
                        btnController.OnPressed += NextPage;
                        btnController.SetText("Next Page");
                        buttons.Add(btnController);
                        continue;
                    }
                    else if (!button.GetComponent<ButtonController>())
                        button.gameObject.SetActive(false);

                }
                modPage.gameObject.SetActive(false);
            }
            modPageTemplate.gameObject.SetActive(true);
            modPage = modPageTemplate.gameObject;
        }

        private void AddDebugButtons()
        {
            AddDebugButton("Debug Log", (btn, isPressed) =>
            {
                debugger = isPressed;
                Logging.Debug("Debugger", debugger ? "active" : "inactive");
                Plugin.debugText.text = "";
            });

            AddDebugButton("Close game", (btn, isPressed) =>
            {
                debugger = isPressed;
                if (btn.text.text == "You sure?")
                {
                    Application.Quit();
                }
                else
                {
                    btn.text.text = "You sure?";
                }
            });

            AddDebugButton("Show Colliders", (btn, isPressed) =>
            {
                if (isPressed)
                {
                    foreach (var c in FindObjectsByType<Collider>(FindObjectsSortMode.None))
                        c.gameObject.AddComponent<ColliderRenderer>();
                }
                else
                {
                    foreach (var c in FindObjectsByType<ColliderRenderer>(FindObjectsSortMode.None))
                        c.Obliterate();
                }
            });
        }

        int debugButtons = 0;
        private void AddDebugButton(string title, Action<ButtonController, bool> onPress)
        {
#if DEBUG
            var page = modPages.Last();
            var button = page.Find($"Button {debugButtons}").gameObject;
            var btnController = button.gameObject.AddComponent<ButtonController>();
            btnController.OnPressed += onPress;
            btnController.SetText(title);
            buttons.Add(btnController);
            debugButtons++;
#endif
        }

        private int pageIndex = 0;
        public void PreviousPage(ButtonController button, bool isPressed)
        {
            button.IsPressed = false;
            pageIndex--;
            for (int i = 0; i < modPages.Count; i++)
            {
                modPages[i].gameObject.SetActive(i == pageIndex);
            }
            modPage = modPages[pageIndex].gameObject;
        }
        public void NextPage(ButtonController button, bool isPressed)
        {
            button.IsPressed = false;
            pageIndex++;
            for (int i = 0; i < modPages.Count; i++)
            {
                modPages[i].gameObject.SetActive(i == pageIndex);
            }
            modPage = modPages[pageIndex].gameObject;
        }

        public void SetupInteraction()
        {
            this.throwOnDetach = true;
            this.priority = 100;
            this.OnSelectExit += (_, __) =>
            {
                AddBlockerToAllButtons(ButtonController.Blocker.MENU_FALLING);
                docked = false;
            };
            this.OnSelectEnter += (_, __) =>
            {
                RemoveBlockerFromAllButtons(ButtonController.Blocker.MENU_FALLING);
            };

        }

        public Material GetMaterial(string name)
        {
            foreach (var renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                string _name = renderer.material.name.ToLower();
                if (_name.Contains(name))
                {
                    return renderer.material;
                }
            }
            return null;
        }

        public void AddBlockerToAllButtons(ButtonController.Blocker blocker)
        {
            foreach (var button in buttons)
            {
                button.AddBlocker(blocker);
            }
        }

        public void RemoveBlockerFromAllButtons(ButtonController.Blocker blocker)
        {
            foreach (var button in buttons)
            {
                button.RemoveBlocker(blocker);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            MelonPreferences.OnPreferencesSaved.Unsubscribe(SettingsChanged);
        }

        public static void BindConfigEntries()
        {
            try
            {
                MelonPreferences_Category category = Melon<Plugin>.Instance.CreateCategory("general", "General");

                SummonInput = category.CreateEntry("summonInput", "gesture", "open menu", "Which button you press to open the menu (gesture, stick, a/x, b/y)", false, false, new ValueList<string>("gesture", "stick", "a/x", "b/y"));
                SummonInputHand = category.CreateEntry("summonInputHand", "right", "open hand", "Which hand can open the menu", false, false, new ValueList<string>("left", "right"));
            }
            catch (Exception e) { Logging.Exception(e); }
        }
    }
}