using Bark.Extensions;
using Bark.GUI;
using Bark.Interaction;
using Bark.Tools;
using GorillaLibrary.Models;
using GorillaLibrary.Utilities;
using MelonLoader;
using System;
using UnityEngine;
using Player = GorillaLocomotion.GTPlayer;
using Random = UnityEngine.Random;

namespace Bark.Modules.Movement
{
    public class Rockets : BarkModule
    {
        public static readonly string DisplayName = "Rockets";
        public static Rockets Instance;
        private GameObject rocketPrefab;
        Rocket rocketL, rocketR;

        void Awake()
        {
            try
            {
                Instance = this;
                rocketPrefab = Plugin.assetBundle.LoadAsset<GameObject>("Rocket");
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }
        }

        protected override void Start()
        {
            base.Start();
        }

        void Setup()
        {
            try
            {
                if (!rocketPrefab)
                    rocketPrefab = Plugin.assetBundle.LoadAsset<GameObject>("Rocket");

                rocketL = SetupRocket(Instantiate(rocketPrefab), true);
                rocketR = SetupRocket(Instantiate(rocketPrefab), false);
                ReloadConfiguration();
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }
        }

        Rocket SetupRocket(GameObject rocketObj, bool isLeft)
        {
            try
            {
                rocketObj.name = isLeft ? "Bark Rocket Left" : "Bark Rocket Right";
                var rocket = rocketObj.AddComponent<Rocket>().Init(isLeft);
                rocket.LocalPosition = new Vector3(0.51f, -3, 0f);
                rocket.LocalRotation = new Vector3(0, 0, -90);
                return rocket;
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }
            return null;
        }

        public Vector3 AddedVelocity()
        {
            return rocketL.Force + rocketR.Force;
        }

        protected override void Cleanup()
        {
            try
            {
                rocketL?.gameObject?.Obliterate();
                rocketR?.gameObject?.Obliterate();
            }
            catch (Exception e) { Logging.Exception(e); }
        }

        protected override void OnEnable()
        {
            if (!MenuController.Instance.Built) return;
            base.OnEnable();
            Setup();
        }

        public static MelonPreferences_Entry<int> Power;
        public static MelonPreferences_Entry<int> Volume;
        protected override void ReloadConfiguration()
        {
            var rockets = new Rocket[] { rocketL?.GetComponent<Rocket>(), rocketR?.GetComponent<Rocket>() };
            foreach (var rocket in rockets)
            {
                if (!rocket) continue;
                rocket.power = Power.Value * 2f;
                rocket.volume = MathExtensions.Map(Volume.Value, 0, 10, 0, 1);
            }
        }

        public static void BindConfigEntries()
        {
            MelonPreferences_Category category = MelonPreferences.CreateCategory(DisplayName, DisplayName);
            category.SetFilePath(UserDataPath);

            Power = category.CreateEntry("power", 5, "Power", "The power of each rocket", false, false, null);
            Volume = category.CreateEntry("thruster volume", 10, "Thruster Volume", "How loud the thrusters sound", false, false, null);
        }

        public override string GetDisplayName()
        {
            return DisplayName;
        }

        public override string Tutorial()
        {
            return $"Hold either [Grip] to summon a rocket.";

        }
    }

    public class Rocket : BarkGrabbable
    {
        public float power = 5f, volume = .2f;
        public Vector3 Force { get; private set; }
        bool isLeft;
        GestureTracker gt;
        Rigidbody rb;
        public AudioSource exhaustSound;

        protected override void Awake()
        {
            base.Awake();
            this.rb = GetComponent<Rigidbody>();
            this.exhaustSound = GetComponent<AudioSource>();
            this.exhaustSound.Stop();
        }

        public Rocket Init(bool isLeft)
        {
            this.isLeft = isLeft;
            gt = GestureTracker.Instance;

            if (isLeft) InputUtility.LeftGrip.OnPressed += Attach;
            else InputUtility.RightGrip.OnPressed += Attach;
            return this;
        }

        void Attach(InputTracker _)
        {
            var parent = (isLeft ? gt.leftPalmInteractor : gt.rightPalmInteractor);
            if (!this.CanBeSelected(parent)) return;
            this.transform.parent = null;
            this.transform.localScale = Vector3.one * Player.Instance.scale;
            parent.Select(this);
            exhaustSound.Stop();
            exhaustSound.time = Random.Range(0, exhaustSound.clip.length);
            exhaustSound.Play();
        }

        void FixedUpdate()
        {
            Player player = Player.Instance;
            Force = this.transform.forward * this.power * Time.fixedDeltaTime * Player.Instance.scale;
            if (Selected)
                player.AddForce(Force);
            else
            {
                rb.linearVelocity += Force * 10;
                Force = Vector3.zero;
                transform.Rotate(Random.insideUnitSphere);
            }
            this.exhaustSound.volume = Mathf.Lerp(.5f, 0, Vector3.Distance(
                player.headCollider.transform.position,
                transform.position
            ) / 20f) * volume;
        }

        public override void OnDeselect(BarkInteractor interactor)
        {
            base.OnDeselect(interactor);
            rb.linearVelocity = Player.Instance.RigidbodyVelocity;
        }

        public void SetupInteraction()
        {
            this.throwOnDetach = true;
            gameObject.layer = BarkInteractor.InteractionLayer;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (!gt) return;
            InputUtility.LeftGrip.OnPressed -= Attach;
            InputUtility.RightGrip.OnPressed -= Attach;
        }
    }
}
