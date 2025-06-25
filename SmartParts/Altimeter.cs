/*
 * Author: dtobi, Firov
 * This work is shared under Creative Commons CC BY-NC-SA 3.0 license.
 *
 */

using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

namespace Lib
{
    public class Altimeter : SmartSensorModuleBase
    {

        #region Fields


        [KSPField(guiActiveUnfocused=true,isPersistant = true, guiActive = true, guiName = "Kilometers", guiFormat = "F0", guiUnits = "km"),
            UI_FloatEdit(scene = UI_Scene.All, minValue = 0f, maxValue = 1000f, incrementLarge = 100f, incrementSmall = 25f, incrementSlide = 1f)]
        public float kilometerHeight = 0;

        [KSPField(guiActiveUnfocused=true,isPersistant = true, guiActive = true, guiName = "Meters", guiFormat = "F0", guiUnits = "m"),
            UI_FloatEdit(scene = UI_Scene.All, minValue = 0f, maxValue = 1000f, incrementLarge = 100f, incrementSmall = 25f, incrementSlide = 1f)]
        public float meterHeight = 0;
  
        [KSPField(guiActiveUnfocused=true,isPersistant = true, guiActive = true, guiActiveEditor = false, guiName = "Trigger on"),
            UI_ChooseOption(options = new string[] { "#LOC_SmartParts_1", "#LOC_SmartParts_2", "#LOC_SmartParts_3" })]
        public string direction = "#LOC_SmartParts_1";


        [KSPField(guiActiveUnfocused=true,isPersistant = true, guiActive = true, guiActiveEditor = false, guiName = "Use AGL"),
            UI_Toggle(disabledText = "#LOC_SmartParts_4", enabledText = "#LOC_SmartParts_5")]
        public bool useAGL = true;
        #endregion


        #region Events
        [KSPAction("Activate Detection")]
        public void doActivateAG(KSPActionParam param) {
            isArmed = true;
        }

        [KSPAction("Deactivate Detection")]
        public void doDeActivateAG(KSPActionParam param) {
            isArmed = false;
        }
        #endregion


        #region Variables

        private double alt = 0;
        private double currentWindow = 0;
        private Boolean ascending = false;
        private Boolean fireNextupdate = false;
        private string groupLastUpdate = "0"; //AGX: What was our selected group last update frame? Top slider.

        #endregion


#region Overrides

        public override void OnStart(StartState state) {
            //Initial button layout
            updateButtons();
            //Force activation no matter which stage it's on
            this.part.force_activate();
            Log.Info("KM Altimeter Detector Started");
            updateButtons();
            initLight(true, "light-go");  // NO_LOCALIZATION
        }

        public override void OnUpdate() {
            //Check to see if the device has been rearmed, if so, deactivate the lights
            if (isArmed && illuminated) {
                lightsOff();
            }
            //In order for physics to take effect on jettisoned parts, the staging event has to be fired from OnUpdate
            if (fireNextupdate) {
                int groupToFire = 0; //AGX: need to send correct group
                if (AGXInterface.AGExtInstalled()) {
                    groupToFire = int.Parse(agxGroupType);
                }
                else {
                    groupToFire = int.Parse(group);
                }
                Helper.fireEvent(this.part, groupToFire, (int)agxGroupNum);
                fireNextupdate = false;
            }
        }

        public override void OnFixedUpdate() {
            //Check current altitude
            updateAltitude();

            //If the device is armed, check for the trigger altitude
            if (isArmed) {
                //We're ascending. Trigger at or above target height
                if (direction != Localizer.Format("#LOC_SmartParts_3") && ascending && Math.Abs((alt - currentWindow) - (kilometerHeight * 1000 + meterHeight)) < currentWindow) {
                    //This flag is checked for in OnUpdate to trigger staging
                    fireNextupdate = true;
                    lightsOn();
                    isArmed = false;
                }
                //We're descending. Trigger at or below target height
                else if (direction != Localizer.Format("#LOC_SmartParts_2") && !ascending && Math.Abs((alt + currentWindow) - (kilometerHeight * 1000 + meterHeight)) < currentWindow) {
                    //This flag is checked for in OnUpdate to trigger staging
                    fireNextupdate = true;
                    lightsOn();
                    isArmed = false;
                }
            }

            //If auto reset is enabled, wait for departure from the target window and rearm
            if (!isArmed & autoReset) {
                if (ascending && Math.Abs((alt - currentWindow) - (kilometerHeight * 1000 + meterHeight)) > currentWindow) {
                    isArmed = true;
                }
                else if (!ascending && Math.Abs((alt + currentWindow) - (kilometerHeight * 1000 + meterHeight)) > currentWindow) {
                    isArmed = true;
                }
            }
        }

        #region NO_LOCALIZATION
        public void Update() //AGX: The OnUpdate above only seems to run in flight mode, Update() here runs in all scenes
        {
            if (agxGroupType == "1" & groupLastUpdate != "1" || agxGroupType != "1" & groupLastUpdate == "1") //AGX: Monitor group to see if we need to refresh window
            {
                updateButtons();
                refreshPartWindow();
                if (agxGroupType == "1") {
                    groupLastUpdate = "1";
                }
                else {
                    groupLastUpdate = "0";
                }
            }
        }
        #endregion

        private void refreshPartWindow() //AGX: Refresh right-click part window to show/hide Groups slider
        {
            UIPartActionWindow[] partWins = FindObjectsOfType<UIPartActionWindow>();
            //Log.Info("Wind count " + partWins.Count());
            foreach (UIPartActionWindow partWin in partWins) {
                partWin.displayDirty = true;
            }
        }

#endregion


#region Methods

        private void updateAltitude() {
            //Sea altitude
            double altSea = this.vessel.mainBody.GetAltitude(this.vessel.CoM);
            //Altitude over terrain. Does not factor in ocean surface.
            double altSurface = altSea - this.vessel.terrainAltitude;
            //Set the last altitude for the purpose of direction determination
            double lastAlt = alt;
            //Set current altitude
            if(!this.vessel.mainBody.ocean) {
                //If the cellestial body this craft is orbiting lacks an ocean, always use AGL
                alt = altSurface;
            }            
            else if (useAGL) {
                //If the planet has an ocean, and the "useAGL" is selected, calculate whether AGL or ASL is closer, and use that
                alt = (altSurface < altSea ? altSurface : altSea);
            }
            //Otherwise, use sea level
            else {                
                alt = altSea;
            }
            //Determine if the vessel is ascending or descending
            ascending = (lastAlt < alt ? true : false);
            //Update target window size based on current vertical velocity
            currentWindow = Math.Abs((TimeWarp.fixedDeltaTime * this.vessel.verticalSpeed) * 1.05);
        }

        #region NO_LOCALIZATION
        private void updateButtons() {
            //Change to AGX buttons if AGX installed
            if (AGXInterface.AGExtInstalled()) {
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
                else {
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
        }
        #endregion

        private void onGUI() {
            //Update buttons
            updateButtons();
        }

#endregion
    }
}

