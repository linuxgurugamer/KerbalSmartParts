/*
 * Author: dtobi, Firov
 * This work is shared under Creative Commons CC BY-NC-SA 3.0 license.
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

using KSP.UI.Screens;

namespace Lib
{
    public class Timer : SmartSensorModuleBase
    {
        #region Fields
        // remember the time wehen the countdown was started
        [KSPField(guiActiveUnfocused=true,isPersistant = true, guiActive = false)]
        private double triggerTime = 0;

        // Delay in seconds. Used for precise measurement
        [KSPField(guiActiveUnfocused=true,isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "Seconds", guiFormat = "F1"),
            UI_FloatEdit(scene = UI_Scene.All, minValue = 0f, maxValue = 120f, incrementLarge = 20f, incrementSmall = 1f, incrementSlide = .05f, sigFigs = 1)]
        public float triggerDelaySeconds = 0;

        // Delay in minutes. Used for longer term measurement
        [KSPField(guiActiveUnfocused=true,isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "Minutes", guiFormat = "F2"),
            UI_FloatEdit(scene = UI_Scene.All, minValue = 0f, maxValue = 360f, incrementLarge = 60f, incrementSmall = 5f, incrementSlide = .25f, sigFigs = 2)]
        public float triggerDelayMinutes = 0;

        [KSPField(guiActiveUnfocused=true,isPersistant = true, guiActive = true, guiName = "Remaining Time", guiFormat = "F2")]
        private double remainingTime = 0;

        [KSPField(guiActiveUnfocused=true,isPersistant = true)]
        private Boolean allowStage = true;

        [KSPField(guiActiveUnfocused=true,isPersistant = true)]
        private Boolean useSeconds = true;
        #endregion


        #region Events
        [KSPEvent(guiActive = false, guiActiveEditor = false, guiName = "Use Seconds", guiActiveUnfocused = true)]
        public void setSeconds()
        {
            useSeconds = true;
            updateButtons();
        }

        [KSPEvent(guiActive = false, guiActiveEditor = false, guiName = "Use Minutes", guiActiveUnfocused = true)]
        public void setMinutes()
        {
            useSeconds = false;
            updateButtons();
        }

        [KSPEvent(guiActive = false, guiActiveEditor = false, guiName = "Enable Staging", guiActiveUnfocused = true)]
        public void activateStaging()
        {
            enableStaging();
        }

        [KSPEvent(guiActive = false, guiActiveEditor = false, guiName = "Disable Staging", guiActiveUnfocused = true)]
        public void deactivateStaging()
        {
            disableStaging();
        }

        [KSPEvent(guiName = "Start Countdown", guiActive = true, guiActiveUnfocused = true)]
        public void activateTimer()
        {
            reset();
            setTimer();
        }

        [KSPAction("Start Countdown")]
        public void activateTimerAG(KSPActionParam param)
        {
            reset();
            setTimer();
        }

        [KSPEvent(guiName = "Reset", guiActive = true, guiActiveUnfocused = true)]
        public void resetTimer()
        {
            reset();
        }

        [KSPAction("Reset")]
        public void resetTimerAG(KSPActionParam param)
        {
            reset();
        }
        #endregion


        #region Variables
        private int previousStage = 0;
        private string groupLastUpdate = "0"; //AGX: What was our selected group last update frame? Top slider.
        private bool isCountingDown = false;
        #endregion


        #region Overrides
        public override void OnStart(StartState state)
        {
            if (!isArmed)
            {
                lightsOn();
                this.part.stackIcon.SetIconColor(XKCDColors.Red);
            }
            if (allowStage)
            {
                Events["activateStaging"].guiActiveEditor = false;
                Events["deactivateStaging"].guiActiveEditor = true;
            }
            else
            {
                Invoke("disableStaging", 0.25f);
            }
            GameEvents.onVesselChange.Add(onVesselChange);
            part.ActivatesEvenIfDisconnected = true;
            //Initial button layout
            updateButtons();
            initLight(true, "light-go");
        }

        void OnDestroy()
        {
            GameEvents.onVesselChange.Remove(onVesselChange);
        }

        public override void OnActive()
        {
            //If staging enabled, set timer
            if (allowStage && isArmed)
            {
                setTimer();
            }
        }
        private const int PHYSICSWAIT = 1;
        int physicsCnt = 0;
        public override void OnUpdate()
        {
            if (FlightGlobals.fetch.activeVessel.HoldPhysics || physicsCnt++ < PHYSICSWAIT)
            {
                return;
            }
            //Check to see if the timer has been dragged in the staging list. If so, reset icon color
            if (this.part.inverseStage != previousStage && allowStage && !isArmed && this.part.inverseStage + 1 < StageManager.CurrentStage)
            {
                reset();
            }
            previousStage = this.part.inverseStage;

            //If the timer has been activated, start the countdown, activate the model's LED, and change the icon color
            if (triggerTime > 0 && isArmed)
            {
                remainingTime = triggerTime + (useSeconds ? triggerDelaySeconds : triggerDelayMinutes * 60) - Planetarium.GetUniversalTime();
                lightsOn(Utility.LightColor.GREEN);
                this.part.stackIcon.SetIconColor(XKCDColors.BrightYellow);

                //Once the timer hits 0 activate the stage/AG, disable the model's LED, and change the icon color
                if (remainingTime <= 0)
                {
                    Log.Info("Stage:" + Helper.KM_dictAGNames[int.Parse(group)]);
                    part.stackIcon.SetIconColor(XKCDColors.Red);
                    triggerTime = 0;
                    remainingTime = 0;
                    //Disable timer until reset
                    isArmed = false;
                    isCountingDown = false;
                    int groupToFire = 0; //AGX: need to send correct group
                    if (AGXInterface.AGExtInstalled())
                    {
                        groupToFire = int.Parse(agxGroupType);
                    }
                    else
                    {
                        groupToFire = int.Parse(group);
                    }
                    Helper.fireEvent(this.part, groupToFire, (int)agxGroupNum);
                    lightsOn();
                }
            }
        }

        public void Update() //AGX: The OnUpdate above only seems to run in flight mode, Update() here runs in all scenes
        {
            if (agxGroupType == "1" & groupLastUpdate != "1" || agxGroupType != "1" & groupLastUpdate == "1") //AGX: Monitor group to see if we need to refresh window
            {
                updateButtons();
                if (agxGroupType == "1")
                {
                    groupLastUpdate = "1";
                }
                else
                {
                    groupLastUpdate = "0";
                }
            }
        }

        #endregion


        #region Methods
        public void onVesselChange(Vessel newVessel)
        {
            if (newVessel == this.vessel && !allowStage)
            {
                Invoke("disableStaging", 0.25f);
            }
        }

        private void enableStaging()
        {
            part.stackIcon.CreateIcon();
            StageManager.Instance.SortIcons(true);
            allowStage = true;

            //Toggle button visibility so currently inactive mode's button is visible
            Events["activateStaging"].guiActiveEditor = false;
            Events["deactivateStaging"].guiActiveEditor = true;
        }

        private void disableStaging()
        {
            part.stackIcon.RemoveIcon();
            StageManager.Instance.SortIcons(true);
            allowStage = false;

            //Toggle button visibility so currently inactive mode's button is visible
            Events["activateStaging"].guiActiveEditor = true;
            Events["deactivateStaging"].guiActiveEditor = false;
        }

        private void setTimer()
        {
            if (isArmed && !isCountingDown)
            {
                //Set the trigger time, which will be caught in OnUpdate
                isCountingDown = true;
                triggerTime = Planetarium.GetUniversalTime();
                Log.Info("Activating Timer: " + (useSeconds ? triggerDelaySeconds : triggerDelayMinutes * 60));
            }
        }

        private void reset()
        {
            Log.Info("Timer reset");
            //Reset trigger and remaining time to 0
            triggerTime = 0;
            remainingTime = 0;
            lightsOff();
            //Reset icon color to white
            this.part.stackIcon.SetIconColor(XKCDColors.White);
            //Reset armed variable
            isArmed = true;
            isCountingDown = false;
            //Reset activation status on part
            this.part.deactivate();
        }

        private void updateButtons()
        {
            if (useSeconds)
            {
                //Show minute button
                Events["setMinutes"].guiActiveEditor = true;
                Events["setMinutes"].guiActive = true;
                //Hide minute scale
                Fields["triggerDelayMinutes"].guiActiveEditor = false;
                Fields["triggerDelayMinutes"].guiActive = false;
                //Hide seconds button
                Events["setSeconds"].guiActiveEditor = false;
                Events["setSeconds"].guiActive = false;
                //Show seconds scale
                Fields["triggerDelaySeconds"].guiActiveEditor = true;
                Fields["triggerDelaySeconds"].guiActive = true;
                //Reset minute scale
                triggerDelayMinutes = 0f;
            }
            else
            {
                //Hide minute button
                Events["setMinutes"].guiActiveEditor = false;
                Events["setMinutes"].guiActive = false;
                //Show minute scale
                Fields["triggerDelayMinutes"].guiActiveEditor = true;
                Fields["triggerDelayMinutes"].guiActive = true;
                //Show seconds button
                Events["setSeconds"].guiActiveEditor = true;
                Events["setSeconds"].guiActive = true;
                //Hide seconds scale
                Fields["triggerDelaySeconds"].guiActiveEditor = false;
                Fields["triggerDelaySeconds"].guiActive = false;
                //Reset seconds scale
                triggerDelaySeconds = 0;
            }

            //Change to AGX buttons if AGX installed
            if (AGXInterface.AGExtInstalled())
            {
                Fields["group"].guiActiveEditor = false;
                Fields["group"].guiActive = false;
                Fields["agxGroupType"].guiActiveEditor = true;
                Fields["agxGroupType"].guiActive = true;
                //Fields["agxGroupNum"].guiActiveEditor = true;
                //Fields["agxGroupNum"].guiActive = true;
                if (agxGroupType == "1") //only show groups select slider when selecting action group
                {
                    Fields["agxGroupNum"].guiActiveEditor = true;
                    Fields["agxGroupNum"].guiActive = true;
                    //Fields["agxGroupNum"].guiName = "Group:";
                }
                else
                {
                    Fields["agxGroupNum"].guiActiveEditor = false;
                    Fields["agxGroupNum"].guiActive = false;
                    //Fields["agxGroupNum"].guiName = "N/A";
                    //agxGroupNum = 1;
                }
            }
            else //AGX not installed, leave at default
            {
                Fields["group"].guiActiveEditor = true;
                Fields["group"].guiActive = true;
                Fields["agxGroupType"].guiActiveEditor = false;
                Fields["agxGroupType"].guiActive = false;
                Fields["agxGroupNum"].guiActiveEditor = false;
                Fields["agxGroupNum"].guiActive = false;
            }

            //Hide auto reset button, since we don't need, we can reactivate in AG
            Fields["autoReset"].guiActive = false;
            Fields["autoReset"].guiActiveEditor = false;
        }

        private void onGUI()
        {
            //Update Buttons
            updateButtons();
        }

        private void refreshPartWindow() //AGX: Refresh right-click part window to show/hide Groups slider
        {
            UIPartActionWindow[] partWins = FindObjectsOfType<UIPartActionWindow>();
            //Log.Info("Wind count " + partWins.Count());
            foreach (UIPartActionWindow partWin in partWins)
            {
                partWin.displayDirty = true;
            }
        }

        #endregion
    }
}
