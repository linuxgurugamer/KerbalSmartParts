/*
 * Author: dtobi, Firov
 * This work is shared under Creative Commons CC BY-NC-SA 3.0 license.
 *
 */

using KSP.Localization;
using System;
using System.Collections.Generic;
using UnityEngine;

using KSP.UI.Screens;

namespace Lib
{
    public class Valve : PartModule
    {
        #region Fields/Variables

        private Dictionary<String, double> drainRatio = new Dictionary<String, double>();
        private Log Log = new Log();

        static float maxSpeedY = -1.0f;
        KSPParticleEmitter valveEffect = null;

        [KSPField(guiActiveUnfocused=true,isPersistant = true, guiName = "Opened")] // remember if the part is open
        public bool isOpen = false;

        [KSPField(guiActiveUnfocused=true,isPersistant = true)]
        private Boolean allowStage = true;

        [KSPField(guiActiveUnfocused=true,isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Outlet"), UI_FloatRange(minValue = 0f, maxValue = 100f, stepIncrement = 5f)]
        public float force = 10;

        [KSPField(guiActiveUnfocused=true,isPersistant = false)]
        public int facing;

        #endregion


        #region Events

        [KSPEvent(guiActive = false, guiActiveEditor = false, guiName = "Enable Staging", guiActiveUnfocused = true)]
        public void activateStaging() {
            enableStaging();
        }

        [KSPEvent(guiActive = false, guiActiveEditor = false, guiName = "Disable Staging", guiActiveUnfocused = true)]
        public void deactivateStaging() {
            disableStaging();
        }

        [KSPEvent(guiName = "Toggle", guiActive = true, guiActiveEditor = false, guiActiveUnfocused = true)]
        public void toggleValve() {
            setValve(!isOpen);
        }

        [KSPAction("Toggle")]
        public void toggleValveAG(KSPActionParam param) {
            setValve(!isOpen);
        }

        [KSPAction("Open")]
        public void openValveAG(KSPActionParam param) {
            setValve(true);
        }

        [KSPAction("Close")]
        public void closeValveAG(KSPActionParam param) {
            setValve(false);
        }

        #endregion


        #region Overrides
        #region NO_LOCALIZATION
        public override void OnStart(StartState state) {
            valveEffect = (KSPParticleEmitter)this.part.GetComponentInChildren<KSPParticleEmitter>();

            if (allowStage) {
                Events["activateStaging"].guiActiveEditor = false;
                Events["deactivateStaging"].guiActiveEditor = true;
            }
            else {
                Invoke("disableStaging", 0.25f);
            }
            GameEvents.onVesselChange.Add(onVesselChange);

            if (valveEffect != null) {
                valveEffect.emit = isOpen;
                valveEffect.localVelocity.y = maxSpeedY * force / 100;
            }
            else {
                Log.Info("Launch effect not found");
            }

            if (state != StartState.Editor) {
                // determine max amount of resources in parent part
                double totalResourceAmount = 0;
                foreach (PartResource resource in part.parent.Resources) {
                    if (resource.resourceName == "ElectricCharge")
                        continue;
                    totalResourceAmount += resource.maxAmount;
                }

                // determine drain ratios
                foreach (PartResource resource in part.parent.Resources) {
                    if (resource.resourceName == "ElectricCharge")
                        continue;
                    drainRatio.Add(resource.resourceName, (totalResourceAmount > 0 ? resource.maxAmount / totalResourceAmount : 0));
                    Log.Info("Valve: Adding ressource:" + resource.resourceName + " DR:" + resource.maxAmount / totalResourceAmount);
                }
            }
        }
        #endregion

        void OnDestroy()
        {
            GameEvents.onVesselChange.Remove(onVesselChange);
        }


        public override void OnUpdate() {
            if (isOpen) {
                double receivedRessource = 0;
                float timeStep = TimeWarp.deltaTime;
                //Flow rate * number of resources vented * current time step * thrust coefficient (assuming ISP of ~65 and 5 kg per unit of fuel)
                float appliedForce = force * part.parent.Resources.Count * timeStep * .65f;
                valveEffect.localVelocity.y = maxSpeedY * force / 100;
				this.part.Rigidbody.AddRelativeForce((facing == 0 ? Vector3.up : Vector3.forward) * appliedForce * 1);
                foreach (PartResource resource in part.parent.Resources) {
                    if (resource.resourceName == Localizer.Format("#LOC_SmartParts_70"))
                        continue;
                    receivedRessource += this.part.RequestResource(resource.resourceName, force * timeStep * drainRatio[resource.resourceName]);
                }
                if (receivedRessource == 0)
                    setValve(false);
            }
        }

        public override void OnActive() {
            //If staging enabled, open valve
            if (allowStage) {
                setValve(true);
            }
        }

        #endregion


        #region Methods
        #region NO_LOCALIZATION
        public void setValve(bool nextIsOpen) {
            if (valveEffect != null) {
                isOpen = nextIsOpen;
                valveEffect.emit = nextIsOpen;
            }
            this.part.stackIcon.SetIconColor((isOpen ? XKCDColors.Red : XKCDColors.White));
        }

        public void onVesselChange(Vessel newVessel) {
            if (newVessel == this.vessel && !allowStage) {
                Invoke("disableStaging", 0.25f);
            }
        }

        private void enableStaging() {
            part.stackIcon.CreateIcon();
            StageManager.Instance.SortIcons(true);
            allowStage = true;

            //Toggle button visibility so currently inactive mode's button is visible
            Events["activateStaging"].guiActiveEditor = false;
            Events["deactivateStaging"].guiActiveEditor = true;
        }

        private void disableStaging() {
            part.stackIcon.RemoveIcon();
            StageManager.Instance.SortIcons(true);
            allowStage = false;

            //Toggle button visibility so currently inactive mode's button is visible
            Events["activateStaging"].guiActiveEditor = true;
            Events["deactivateStaging"].guiActiveEditor = false;
        }
        #endregion

        #endregion
    }
}

