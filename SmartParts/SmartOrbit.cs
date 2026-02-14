using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

namespace Lib
{
    public class SmartOrbit : SmartSensorModuleBase
    {

        #region Fields

        [KSPField(guiActiveUnfocused=true,isPersistant = true, guiActive = true, guiActiveEditor = false, guiName = "Element"),
            UI_ChooseOption(options = new string[] { "#LOC_SmartParts_34", "#LOC_SmartParts_35" })]
        public string element = "#LOC_SmartParts_34";

        [KSPField(guiActiveUnfocused=true,isPersistant = false, guiActive = true, guiName = "Altitude", guiFormat = "N3", guiUnits = "km")]
        private double displayAlt = 0;

        [KSPField(guiActiveUnfocused=true,isPersistant = true, guiActive = true, guiName = "Kilometers", guiFormat = "F0", guiUnits = "km"),
            UI_FloatEdit(scene = UI_Scene.All, minValue = 0f, maxValue = 1000f, incrementLarge = 100f, incrementSmall = 25f, incrementSlide = 1f)]
        public float kilometerHeight = 0;

        [KSPField(guiActiveUnfocused=true,isPersistant = true, guiActive = true, guiName = "Meters", guiFormat = "F0", guiUnits = "m"),
            UI_FloatEdit(scene = UI_Scene.All, minValue = 0f, maxValue = 1000f, incrementLarge = 100f, incrementSmall = 25f, incrementSlide = 1f)]
        public float meterHeight = 0;

        [KSPField(guiActiveUnfocused=true,isPersistant = true, guiActive = true, guiActiveEditor = false, guiName = "Trigger on"),
            UI_ChooseOption(options = new string[] { "#LOC_SmartParts_All", "#LOC_SmartParts_36", "#LOC_SmartParts_37" })]
        public string direction = "#LOC_SmartParts_All";


        #endregion


        #region Events
        [KSPAction("Activate Detection")]
        public void doActivateAG(KSPActionParam param)
        {
            isArmed = true;
        }

        [KSPAction("Deactivate Detection")]
        public void doDeActivateAG(KSPActionParam param)
        {
            isArmed = false;
        }

        #endregion


        #region Variables

        private double alt = double.NaN;
        private double currentWindow = 0;
        private Boolean increasing = false;
        private Boolean fireNextupdate = false;
        private string groupLastUpdate = "0"; //AGX: What was our selected group last update frame? Top slider.

        #endregion


        #region Overrides

        public override void OnStart(StartState state)
        {
            Log.setTitle(Localizer.Format("#LOC_SmartParts_38"));
            Log.Info("Started");

            //Initial button layout
            updateButtons();
            //Force activation no matter which stage it's on
            this.part.force_activate();
            updateButtons();
            initLight(true, Localizer.Format("#LOC_SmartParts_33"));
        }

        public override void OnUpdate()
        {
            //Check to see if the device has been rearmed, if so, deactivate the lights
            if (isArmed && illuminated)
            {
                lightsOff();
            }
            //In order for physics to take effect on jettisoned parts, the staging event has to be fired from OnUpdate
            if (fireNextupdate)
            {
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
                fireNextupdate = false;
            }
        }

        public override void OnFixedUpdate()
        {
            updateAltitude();

            if (isArmed)
            {
                //We're increasing. Trigger at or above target height
                if (direction != Localizer.Format("#LOC_SmartParts_37") && increasing && Math.Abs((alt - currentWindow) - (kilometerHeight * 1000 + meterHeight)) < currentWindow)
                {
                    //This flag is checked for in OnUpdate to trigger staging
                    fireNextupdate = true;
                    lightsOn();
                    isArmed = false;
                }
                //We're decreasing. Trigger at or below target height
                else if (direction != Localizer.Format("#LOC_SmartParts_36") && !increasing && Math.Abs((alt + currentWindow) - (kilometerHeight * 1000 + meterHeight)) < currentWindow)
                {
                    //This flag is checked for in OnUpdate to trigger staging
                    fireNextupdate = true;
                    lightsOn();
                    isArmed = false;
                }
            }

            //If auto reset is enabled, wait for departure from the target window and rearm
            if (!isArmed & autoReset)
            {
                if (increasing && Math.Abs((alt - currentWindow) - (kilometerHeight * 1000 + meterHeight)) > currentWindow)
                {
                    isArmed = true;
                }
                else if (!increasing && Math.Abs((alt + currentWindow) - (kilometerHeight * 1000 + meterHeight)) > currentWindow)
                {
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


        #region Methods

        private void updateAltitude()
        {
            double lastAlt = alt;

            if (element == Localizer.Format("#LOC_SmartParts_34"))
            {
                alt = vessel.orbit.ApA;
            }
            else
            {
                alt = vessel.orbit.PeA;
            }

            displayAlt = alt / 1000;

            if (double.IsNaN(lastAlt)) // First pass
                lastAlt = alt;

            //Determine if the vessel is ascending or descending
            increasing = lastAlt < alt;

            //Rate of change
            currentWindow = Math.Abs(lastAlt - alt);
        }

        #region NO_LOCALIZATION
        private void updateButtons()
        {
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
        }
        #endregion

        private void onGUI()
        {
            //Update buttons
            updateButtons();
        }

        #endregion
    }
}

