using Bark.Extensions;
using Bark.Tools;
using GorillaLibrary.Extensions;
using GorillaLibrary.Models;
using GorillaLibrary.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using Player = GorillaLocomotion.GTPlayer;

namespace Bark.Interaction
{
    public class GestureTracker : MonoBehaviour
    {
        public static GestureTracker Instance;

        public GameObject
            chest,
            leftPointerObj, rightPointerObj,
            leftHand, rightHand;

        public BodyVectors leftHandVectors, rightHandVectors, headVectors;

        public BarkInteractor
            leftPalmInteractor, rightPalmInteractor,
            leftPointerInteractor, rightPointerInteractor;

        public Transform leftPointerTransform, rightPointerTransform, leftThumbTransform, rightThumbTransform;

        public const string palmPath = "palm.01.{0}";

        public const string pointerFingerPath = palmPath + "/f_index.01.{0}/f_index.02.{0}/f_index.03.{0}";

        public const string thumbPath = palmPath + "/thumb.01.{0}/thumb.02.{0}/thumb.03.{0}";

        // Gesture Actions
        public Action OnMeatBeat;
        private readonly Queue<int> meatBeatCollisions = new Queue<int>();
        private float lastBeat;

        public Action<Vector3> OnGlide;
        public Action OnIlluminati, OnKamehameha;
        public bool isIlluminatiing = false, isChargingKamehameha;

        public struct BodyVectors
        {
            public Vector3 pointerDirection, palmNormal, thumbDirection;
        }

        void Awake()
        {
            Logging.Debug("Awake");
            Instance = this;

            BuildColliders();
            var observer = chest.AddComponent<CollisionObserver>();
            observer.OnTriggerEntered += OnChestBeat;
        }

        public float camOffset = -45f;
        void FixedUpdate()
        {
            // If it's been more than one second since you last beat your chest, reset
            if (Time.time - lastBeat > 1f)
                meatBeatCollisions.Clear();
        }

        void Update()
        {
            TrackBodyVectors();
            TrackGlideGesture();
            isIlluminatiing = TrackIlluminatiGesture();
            //isChargingKamehameha = TrackKamehamehaGesture();
        }

        void TrackBodyVectors()
        {
            var left = leftHand.transform;
            leftHandVectors = new BodyVectors()
            {
                pointerDirection = left.forward,
                palmNormal = left.right,
                thumbDirection = left.up
            };
            var right = rightHand.transform;
            rightHandVectors = new BodyVectors()
            {
                pointerDirection = right.forward,
                palmNormal = right.right * -1,
                thumbDirection = right.up
            };

            var head = Player.Instance.headCollider.transform;
            headVectors = new BodyVectors()
            {
                pointerDirection = head.forward,
                palmNormal = head.right,
                thumbDirection = head.up
            };
        }

        readonly float illProximityThreshold = .1f;
        bool TrackIlluminatiGesture()
        {
            var scale = Player.Instance.scale;
            // Check if thumb and pointer are touching
            if (Vector3.Distance(
                    leftPointerTransform.position, rightPointerTransform.position
                ) > illProximityThreshold * scale) return false;
            if (Vector3.Distance(
                    leftThumbTransform.position, rightThumbTransform.position
                ) > illProximityThreshold * scale) return false;

            if (PalmsFacingSameWay())
            {
                OnIlluminati?.Invoke();
                return true;
            }
            return false;
        }

        bool TrackKamehamehaGesture()
        {
            var scale = Player.Instance.scale;
            // Check if palms are too far away. If so, leave.
            if (
                Vector3.Distance(
                    leftPalmInteractor.transform.position,
                    rightPalmInteractor.transform.position
                ) > .25f * scale)
                return false;

            if (PalmsFacingEachOther() && FingersFacingAway())
            {
                OnKamehameha?.Invoke();
                return true;
            }
            return false;
        }

        void TrackGlideGesture()
        {
            if (FingersFacingAway() && PalmsFacingSameWay())
            {
                // Check that the glide direction is toward where the player is facing
                Vector3 direction = (leftHandVectors.thumbDirection + rightHandVectors.thumbDirection) / 2;
                if (Vector3.Dot(direction, headVectors.pointerDirection) > 0f)
                    OnGlide?.Invoke(direction);
            }
        }
        public bool PalmsFacingEachOther()
        {
            Vector3 relativePosition = leftHand.transform.InverseTransformPoint(rightHand.transform.position);
            if (relativePosition.x < 0f) return false;
            return Vector3.Dot(leftHandVectors.palmNormal, rightHandVectors.palmNormal) < -.5f;
        }

        public bool PalmsFacingSameWay()
        {
            return Vector3.Dot(leftHandVectors.palmNormal, rightHandVectors.palmNormal) > .5f;
        }

        public bool FingersFacingAway()
        {
            Vector3 relativePosition = leftHand.transform.InverseTransformPoint(rightHand.transform.position);
            if (relativePosition.z > 0f) return false;
            return Vector3.Dot(leftHandVectors.pointerDirection, rightHandVectors.pointerDirection) < -.5f;
        }
        void OnChestBeat(GameObject obj, Collider collider)
        {
            if (collider.gameObject != leftHand &&
                collider.gameObject != rightHand) return;

            lastBeat = Time.time;

            if (meatBeatCollisions.Count > 3)
                meatBeatCollisions.Dequeue();
            if (collider.gameObject == leftHand)
                meatBeatCollisions.Enqueue(0);
            else if (collider.gameObject == rightHand)
                meatBeatCollisions.Enqueue(1);
            if (meatBeatCollisions.Count < 4) return;
            int current, last = -1;
            for (int i = 0; i < meatBeatCollisions.Count; i++)
            {
                current = meatBeatCollisions.ElementAt(i);
                if (last == current) return;
                last = current;
            }
            meatBeatCollisions.Clear();
            OnMeatBeat?.Invoke();
        }

        void BuildColliders()
        {
            Logging.Debug("BuildColliders");

            var player = Player.Instance;
            chest = new GameObject("Body Gesture Collider");
            chest.AddComponent<CapsuleCollider>().isTrigger = true;
            chest.AddComponent<Rigidbody>().isKinematic = true;
            chest.transform.SetParent(player.bodyCollider.transform, false);
            chest.layer = LayerMask.NameToLayer("Water");
            float
                height = 1 / 8f,
                radius = 1 / 4f;
            chest.transform.localScale = new Vector3(radius, height, radius);


            var leftPalm = GorillaTagger.Instance.offlineVRRig.GetBone(GorillaRigBone.LeftHand).Find(string.Format(palmPath, "L"));
            leftPalmInteractor = CreateInteractor("Left Palm Interactor", leftPalm, 1 / 16f);
            leftHand = leftPalmInteractor.gameObject;
            leftHand.transform.localRotation = Quaternion.Euler(-90, -90, 0);

            var rightPalm = GorillaTagger.Instance.offlineVRRig.GetBone(GorillaRigBone.RightHand).Find(string.Format(palmPath, "R"));
            rightPalmInteractor = CreateInteractor("Right Palm Interactor", rightPalm, 1 / 16f);
            rightHand = rightPalmInteractor.gameObject;
            rightHand.transform.localRotation = Quaternion.Euler(-90, 0, 0);

            leftPointerTransform = GorillaTagger.Instance.offlineVRRig.GetBone(GorillaRigBone.LeftHand).Find(string.Format(pointerFingerPath, "L"));
            leftPointerInteractor = CreateInteractor("Left Pointer Interactor", leftPointerTransform, 1 / 32f);
            leftPointerObj = leftPointerInteractor.gameObject;

            rightPointerTransform = GorillaTagger.Instance.offlineVRRig.GetBone(GorillaRigBone.RightHand).Find(string.Format(pointerFingerPath, "R"));
            rightPointerInteractor = CreateInteractor("Right Pointer Interactor", rightPointerTransform, 1 / 32f);
            rightPointerObj = rightPointerInteractor.gameObject;

            leftThumbTransform = GorillaTagger.Instance.offlineVRRig.GetBone(GorillaRigBone.LeftHand).Find(string.Format(thumbPath, "L"));
            rightThumbTransform = GorillaTagger.Instance.offlineVRRig.GetBone(GorillaRigBone.RightHand).Find(string.Format(thumbPath, "R"));
        }

        public BarkInteractor CreateInteractor(string name, Transform parent, float scale)
        {
            var obj = new GameObject(name);
            var interactor = obj.AddComponent<BarkInteractor>();
            obj.transform.SetParent(parent, false);
            obj.transform.localScale = Vector3.one * scale;
            return interactor;
        }

        [Obsolete]
        public XRController GetController(bool isLeft)
        {
            foreach (var controller in FindObjectsByType<XRController>(FindObjectsSortMode.None))
            {
                if (isLeft && controller.name.ToLowerInvariant().Contains("left"))
                {
                    return controller;
                }
                if (!isLeft && controller.name.ToLowerInvariant().Contains("right"))
                {
                    return controller;
                }
            }
            return null;
        }

        public void OnDestroy()
        {
            Logging.Debug("Gesture Tracker Destroy");
            leftHand?.Obliterate();
            rightHand?.Obliterate();
            leftPointerObj?.Obliterate();
            rightPointerObj?.Obliterate();
            Instance = null;
            OnMeatBeat = null;
        }

        public void HapticPulse(bool isLeft, float strength = .5f, float duration = .05f)
        {
            var hand = isLeft ? InputUtility.LeftController : InputUtility.RightController;
            hand.SendHapticImpulse(0u, strength, duration);
        }

        public InputTracker GetInputTracker(string name, XRNode node) => name switch
        {
            "grip" => node == XRNode.LeftHand ? InputUtility.LeftGrip : InputUtility.RightGrip,
            "trigger" => node == XRNode.LeftHand ? InputUtility.LeftTrigger : InputUtility.RightTrigger,
            "stick" => node == XRNode.LeftHand ? InputUtility.LeftStickAxis : InputUtility.RightStickAxis,
            "primary" => node == XRNode.LeftHand ? InputUtility.LeftPrimary : InputUtility.RightPrimary,
            "secondary" => node == XRNode.LeftHand ? InputUtility.LeftSecondary : InputUtility.RightSecondary,
            "a/x" => node == XRNode.LeftHand ? InputUtility.LeftPrimary : InputUtility.RightPrimary,
            "b/y" => node == XRNode.LeftHand ? InputUtility.LeftSecondary : InputUtility.RightSecondary,
            "a" => InputUtility.RightPrimary,
            "b" => InputUtility.RightSecondary,
            "x" => InputUtility.LeftPrimary,
            "y" => InputUtility.LeftSecondary,
            _ => null,
        };
    }
}
