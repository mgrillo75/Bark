using Bark.Tools;
using GorillaLibrary.Models;
using GorillaLibrary.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;


namespace Bark.Interaction
{
    public class BarkInteractor : MonoBehaviour
    {
        public static string InteractionLayerName = "TransparentFX";
        public static int InteractionLayer = LayerMask.NameToLayer(InteractionLayerName);
        public static int InteractionLayerMask = LayerMask.GetMask(InteractionLayerName);
        public List<BarkInteractable>
            hovered = [],
            selected = [];
        public InputDevice device;
        public XRNode node;
        public bool IsLeft { get; protected set; }
        public bool Selecting { get; protected set; }
        public bool Activating { get; protected set; }
        public bool PrimaryPressed { get; protected set; }

        public bool IsSelecting
        {
            get { return selected.Count > 0; }
        }

        protected void Awake()
        {
            try
            {
                IsLeft = name.Contains("Left");
                gameObject.AddComponent<SphereCollider>().isTrigger = true;
                gameObject.layer = InteractionLayer;

                var gt = GestureTracker.Instance;
                device = IsLeft ? InputUtility.LeftController : InputUtility.RightController;
                node = IsLeft ? XRNode.LeftHand : XRNode.RightHand;

                gt.GetInputTracker("grip", node).OnPressed += OnGrip;
                gt.GetInputTracker("grip", node).OnReleased += OnGripRelease;
                gt.GetInputTracker("trigger", node).OnPressed += OnTrigger;
                gt.GetInputTracker("trigger", node).OnReleased += OnTriggerRelease;
                gt.GetInputTracker("primary", node).OnPressed += OnPrimary;
                gt.GetInputTracker("primary", node).OnReleased += OnPrimaryRelease;
            }
            catch (Exception e) { Logging.Exception(e); }
        }

        public void Select(BarkInteractable interactable)
        {
            try
            {
                if (!interactable.CanBeSelected(this)) return;
                // Prioritize 
                if (selected.Count > 0)
                {
                    DeselectAll(interactable.priority);
                    if (selected.Count > 0)
                        return;
                }
                interactable.OnSelect(this);
                selected.Add(interactable);
            }
            catch (Exception e) { Logging.Exception(e); }
        }

        public void Deselect(BarkInteractable interactable)
        {
            interactable.OnDeselect(this);
        }

        public void Hover(BarkInteractable interactable)
        {
            hovered.Add(interactable);
        }

        void OnGrip(InputTracker _)
        {
            if (Selecting) return;
            Selecting = true;
            BarkInteractable selected = null;
            foreach (var interactable in hovered)
            {
                if (interactable.CanBeSelected(this))
                {
                    selected = interactable;
                    break;
                }
            }
            if (!selected) return;
            Select(selected);
        }

        void OnGripRelease(InputTracker _)
        {
            if (!Selecting) return;
            Selecting = false;
            DeselectAll();
        }

        void DeselectAll(int competingPriority = -1)
        {
            foreach (var interactable in selected)
            {
                if (competingPriority < 0 || interactable.priority < competingPriority)
                {
                    Deselect(interactable);
                }
            }
            selected.RemoveAll(g => !g.selectors.Contains(this));
        }

        void OnTrigger(InputTracker _)
        {
            Activating = true;
            foreach (var interactable in selected)
                interactable.OnActivate(this);
        }

        void OnTriggerRelease(InputTracker _)
        {
            Activating = false;
            foreach (var grabbable in selected)
                grabbable.OnDeactivate(this);
        }

        void OnPrimary(InputTracker _)
        {
            Activating = true;
            foreach (var grabbable in selected)
                grabbable.OnPrimary(this);
        }

        void OnPrimaryRelease(InputTracker _)
        {
            Activating = false;
            foreach (var grabbable in selected)
                grabbable.OnPrimaryReleased(this);
        }
    }
}
