using Bark;
using Bark.Extensions;
using Bark.GUI;
using Bark.Interaction;
using Bark.Modules;
using Bark.Networking;
using Bark.Patches;
using Bark.Tools;
using GorillaLibrary;
using GorillaLibrary.GameModes.Attributes;
using GorillaNetworking;
using GorillaTagScripts;
using MelonLoader;
using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[assembly: MelonInfo(typeof(Plugin), "Bark", "1.0.0", "KyleTheScientist")]
[assembly: MelonGame("Another Axiom", "Gorilla Tag")]
[assembly: HarmonyDontPatchAll]

namespace Bark;

[ModdedGamemode]
public class Plugin : GorillaMod
{
    public static Plugin Instance;
    public static bool initialized, inRoom;
    bool pluginEnabled = false;
    public static AssetBundle assetBundle;
    public static MenuController menuController;
    public static GameObject monkeMenuPrefab;

    GestureTracker gt;
    NetworkPropertyHandler nph;

    private GameObject _root;


    public void Setup()
    {
        if (menuController || !pluginEnabled || !inRoom) return;
        Logging.Debug("Menu:", menuController, "Plugin Enabled:", pluginEnabled, "InRoom:", inRoom);
        try
        {
            gt = _root.GetOrAddComponent<GestureTracker>();
            nph = _root.GetOrAddComponent<NetworkPropertyHandler>();
            menuController = Object.Instantiate(monkeMenuPrefab).AddComponent<MenuController>();
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    public void Cleanup()
    {
        try
        {
            Logging.Debug("Cleaning up");
            menuController?.gameObject?.Obliterate();
            gt?.Obliterate();
            nph?.Obliterate();
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    public override void OnEarlyInitializeMelon()
    {
        try
        {
            Instance = this;
            Logging.Init();

            MenuController.BindConfigEntries();
            Logging.Debug("Found", BarkModule.GetBarkModuleTypes().Count, "modules");
            foreach (Type moduleType in BarkModule.GetBarkModuleTypes())
            {
                MethodInfo bindConfigs = moduleType.GetMethod("BindConfigEntries");
                if (bindConfigs is null) continue;
                bindConfigs.Invoke(null, null);
            }
        }
        catch (Exception e) { Logging.Exception(e); }
    }

    public override void OnInitializeMelon()
    {
        Events.Game.OnGameInitialized.Subscribe(OnGameInitialized);

        try
        {
            assetBundle = Tools.AssetUtils.LoadAssetBundle("Bark/Resources/barkbundle");
            monkeMenuPrefab = assetBundle.LoadAsset<GameObject>("Bark Menu");
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    public static Text debugText;

    public override void OnMelonEnabled()
    {
        try
        {
            Logging.Debug("OnEnable");
            this.pluginEnabled = true;
            HarmonyPatches.ApplyHarmonyPatches();
            if (initialized) Setup();
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    public override void OnMelonDisabled()
    {
        try
        {
            Logging.Debug("OnDisable");
            this.pluginEnabled = false;
            HarmonyPatches.RemoveHarmonyPatches();
            Cleanup();
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    void OnGameInitialized()
    {
        try
        {
            _root = new GameObject("Bark");
            Object.DontDestroyOnLoad(_root);

            Logging.Debug("OnGameInitialized");
            initialized = true;

#if DEBUG
            CreateDebugGUI();
#endif
        }
        catch (Exception ex)
        {
            Logging.Exception(ex);
        }
    }

    [ModdedGamemodeJoin]
    void RoomJoined()
    {
        Logging.Debug("RoomJoined");
        inRoom = true;
        Setup();
    }

    [ModdedGamemodeLeave]
    void RoomLeft()
    {
        Logging.Debug("RoomLeft");
        inRoom = false;
        Cleanup();
    }

    public async void JoinLobby(string name, string gamemode)
    {
        if (FriendshipGroupDetection.Instance.IsInParty)
        {
            FriendshipGroupDetection.Instance.LeaveParty();
            await Task.Delay(1000);
        }

        await NetworkSystem.Instance.ReturnToSinglePlayer();

        string gamemodeCache = GorillaComputer.instance.currentGameMode.Value;
        Logging.Debug("Changing gamemode from", gamemodeCache, "to", gamemode);
        GorillaComputer.instance.SetGameModeWithoutButton(gamemode);

        await PhotonNetworkController.Instance.AttemptToJoinSpecificRoomAsync(name, JoinType.Solo, null);

        GorillaComputer.instance.SetGameModeWithoutButton(gamemodeCache);
    }

#if DEBUG

    void CreateDebugGUI()
    {
        try
        {
            if (Player.Instance)
            {
                var canvas = Player.Instance.headCollider.transform.GetComponentInChildren<Canvas>();
                if (!canvas)
                {
                    canvas = new GameObject("~~~Bark Debug Canvas").AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.WorldSpace;
                    canvas.transform.SetParent(Player.Instance.headCollider.transform);
                    canvas.transform.localPosition = Vector3.forward * .35f;
                    canvas.transform.localRotation = Quaternion.identity;
                    canvas.transform.localScale = Vector3.one;
                    canvas.gameObject.AddComponent<CanvasScaler>();
                    canvas.gameObject.AddComponent<GraphicRaycaster>();
                    canvas.GetComponent<RectTransform>().localScale = Vector3.one * .035f;
                    var text = new GameObject("~~~Text").AddComponent<Text>();
                    text.transform.SetParent(canvas.transform);
                    text.transform.localPosition = Vector3.zero;
                    text.transform.localRotation = Quaternion.identity;
                    text.transform.localScale = Vector3.one;
                    text.color = Color.green;
                    //text.text = "Hello World";
                    text.fontSize = 24;
                    text.font = Font.CreateDynamicFontFromOSFont("Arial", 24);
                    text.alignment = TextAnchor.MiddleCenter;
                    text.horizontalOverflow = HorizontalWrapMode.Overflow;
                    text.verticalOverflow = VerticalWrapMode.Overflow;
                    text.color = Color.white;
                    text.GetComponent<RectTransform>().localScale = Vector3.one * .02f;
                    debugText = text;
                }
            }
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

#endif
}
