using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

// Data-Packet Loss Detector
namespace Lib
{
    class DPLD : SmartSensorModuleBase
    {

        #region Fields


        [KSPField(guiActiveUnfocused=true,isPersistant = true, guiActive = true, guiActiveEditor = false, guiName = "Trigger on"),
            UI_ChooseOption(options = new string[] { "#LOC_SmartParts_6", "#LOC_SmartParts_7", "#LOC_SmartParts_8" })]
        //UI_ChooseOption(options = new string[] { "KSC loss", "Total Comm Loss", "Network Initialized" })]
        public string actionMode = "#LOC_SmartParts_6";


        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Lights On", guiActiveUnfocused=true)]
        public void doLightsOn()
        {
            lightsOn();
        }
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Lights Off", guiActiveUnfocused = true)]
        public void doLightsOff()
        {
            lightsOff();
        }


        [KSPField]
        public string activeLight = "";

#endregion

        private string groupLastUpdate = "0"; //AGX: What was our selected group last update frame? Top slider.
        private Boolean fireNextupdate = false;
#region Events

        private void Start()
        {
            GameEvents.CommNet.OnCommHomeStatusChange.Add(onCommHomeStatusChange);
            GameEvents.CommNet.OnCommStatusChange.Add(onCommStatusChange);
            GameEvents.CommNet.OnNetworkInitialized.Add(onNetworkInitialized);

            initLight(true, activeLight);
            updateButtons();
        }

        void OnDestroy()
        {
            GameEvents.CommNet.OnCommHomeStatusChange.Remove(onCommHomeStatusChange);
            GameEvents.CommNet.OnCommStatusChange.Remove(onCommStatusChange);
            GameEvents.CommNet.OnNetworkInitialized.Remove(onNetworkInitialized);
        }

        private const int PHYSICSWAIT = 1;
        int physicsCnt = 0;
        public override void OnUpdate()
        {
            //Check to see if the device has been rearmed, if so, deactivate the lights
            if (isArmed && illuminated)
            {
                lightsOff();
            }

            if (FlightGlobals.fetch.activeVessel.HoldPhysics || physicsCnt++ < PHYSICSWAIT)
            {
                fireNextupdate = false;
                return; 
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
                    Log.Info("DPLD.OnUpdate Fire");
                    groupToFire = int.Parse(group);
                }
                Helper.fireEvent(this.part, groupToFire, (int)agxGroupNum);
                fireNextupdate = false;
            }
        }

        #region NO_LOCALIZATION
        public void Update()
        {
            //AGX: The OnUpdate above only seems to run in flight mode, Update() here runs in all scenes
            if (agxGroupType == "1" & groupLastUpdate != "1" || agxGroupType != "1" & groupLastUpdate == "1") //AGX: Monitor group to see if we need to refresh window
            {
                updateButtons();
                //refreshPartWindow();
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

        public override string GetInfo()
        {
            return Localizer.Format("#LOC_SmartParts_9");
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


        void onCommHomeStatusChange(Vessel v, bool b)
        {
            Log.Info("DPLD.onCommHomeStatusChange");
            //This flag is checked for in OnUpdate to trigger staging
            fireNextupdate = true;
            lightsOn();
            isArmed = false;
        }
        void onCommStatusChange(Vessel v, bool b)
        {
            Log.Info("DPLD.onCommStatusChange");
            //This flag is checked for in OnUpdate to trigger staging
            fireNextupdate = true;
            lightsOn();
            isArmed = false;
        }
        void onNetworkInitialized()
        {
            Log.Info("DPLD.onNetworkInitialized");
            //This flag is checked for in OnUpdate to trigger staging
            fireNextupdate = true;
            lightsOn();
            isArmed = false;
        }
        #endregion
        #endregion

    }
}
