using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

namespace Lib
{


    internal static class ProxChannel
    {
        public static List<ProxSensor> Listeners = new List<ProxSensor>();
    }

    class ProxSensor : SmartSensorModuleBase
    {

        #region Fields

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Channel"), UI_FloatRange(minValue = 1f, maxValue = 20f, stepIncrement = 1f)]
        public float channel = 1;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Distance", guiFormat = "F0", guiUnits = "m"),
     UI_FloatEdit(scene = UI_Scene.All, minValue = 0f, maxValue = 2000f, incrementLarge = 250f, incrementSmall = 25f, incrementSlide = 1f)]
        public float meterDistance = 0;


        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = false, guiName = "Trigger on"),
            UI_ChooseOption(options = new string[] { "Both", "Approach", "Departure" })]
        public string direction = "Both";



        /* DEBUG CODE
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Get Count")]
        public void getCount() {
            Log.Info("Debug v1");
            Log.Info("Current Listener Count");
            Log.Info(ProxChannel.Listeners.Count());
            Log.Info("Current Listeners");
            foreach (var listener in ProxChannel.Listeners) {
                Log.Info(" Name - " + listener.vessel.name + " ID - " + listener.vessel.id + " Channel - " + listener.channel);
            }
        } */

        #endregion

        #region Variables

        private string groupLastUpdate = "0"; //AGX: What was our selected group last update frame? Top slider.
        private double currentDistance = 0;
        private double currentWindow = 0;
        private Boolean departing = false;
        private Boolean fireNextupdate = false;
        public Boolean justRegistered = true;

        #endregion

        #region Overrides

        public override void OnStart(StartState state)
        {
            //Clear listeners if the scene changes. Will be recreated on new scene load
            GameEvents.onGameSceneLoadRequested.Add(clearListeners);
            //Redraw buttons
            updateButtons();
            if (state != StartState.Editor)
            {
                //Force activation of proximity sensor upon load/unpack
                this.part.force_activate();
                //Register the listener
                registerListener();
            }
        }

        public override void OnFixedUpdate()
        {
            if (justRegistered)
            {
                justRegistered = false;
            }
            //Update target distance and determine target window
            updateDistance();
            //If the device is armed, check for the trigger altitude
            if (isArmed)
            {
                //We're departing. Trigger at or beyond target distance
                if (direction != "Approach" && departing && Math.Abs((currentDistance + currentWindow) - meterDistance) < currentWindow)
                {
                    Log.Info("Proximity alert. Action fired on " + this.vessel.name + " on channel " + this.channel);
                    //This flag is checked for in OnUpdate to trigger staging
                    fireNextupdate = true;
                    isArmed = false;
                }
                //We're approaching. Trigger at or closer than target distance
                else if (direction != "Departure" && !departing && Math.Abs((currentDistance - currentWindow) - meterDistance) < currentWindow)
                {
                    Log.Info("Proximity alert. Action fired on " + this.vessel.name + " on channel " + this.channel);
                    //This flag is checked for in OnUpdate to trigger staging
                    fireNextupdate = true;
                    isArmed = false;
                }
            }

            //If auto reset is enabled, wait for departure from the target window and rearm
            if (!isArmed & autoReset)
            {
                if (!isArmed & autoReset)
                {
                    if (departing && Math.Abs((currentDistance + currentWindow) - meterDistance) > currentWindow)
                    {
                        Log.Info("Proximity sensor reset on " + this.vessel.name);
                        isArmed = true;
                    }
                    else if (!departing && Math.Abs((currentDistance - currentWindow) - meterDistance) > currentWindow)
                    {
                        Log.Info("Proximity sensor reset on " + this.vessel.name);
                        isArmed = true;
                    }
                }
            }
        }

        public override void OnUpdate()
        {
            //If this proximity sensor isn't registered, register it now
            if (ProxChannel.Listeners.Any(listener => listener.vessel.id == this.vessel.id && listener.channel == this.channel) == false)
            {
                registerListener();
            }
            //In order for physics to take effect on jettisoned parts, the staging event has to be fired from OnUpdate
            if (fireNextupdate && !justRegistered)
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

        #region Methods

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

        private void updateDistance()
        {
            double testDistance = 0;
            double lastDistance = 0;
            ProxSensor closestSensor = null;

            //Set the last distance for the purpose of direction determination
            lastDistance = currentDistance;

            //Find closest target on our channel and set closestDistance
            foreach (var listener in ProxChannel.Listeners.ToList())
            {
                if (this.vessel.id != listener.vessel.id)
                {
                    testDistance = Vector3d.Distance(this.vessel.GetWorldPos3D(), listener.vessel.GetWorldPos3D());
                    //Set distance and listener to the values from the closest non-self sensor on the same channel as us
                    if (listener.channel == this.channel && (testDistance < currentDistance || closestSensor == null))
                    {
                        closestSensor = listener;
                        currentDistance = testDistance;
                    }
                }
            }

            //If no sensors detected, proceed no further
            if (closestSensor == null)
            {
                return;
            }

            //Determine if the vessel is approaching or departing
            departing = (currentDistance < lastDistance ? false : true);

            //Update target window size based on current velocity relative to target
            //If the target was just registered (AKA just entered 2.5km), it's velocity measurements are innacurate. Manually set to 0 until next pass.
            currentWindow = (closestSensor.justRegistered ? 0 : Math.Abs(currentDistance - lastDistance) * 1.05);
            //We now have one data point. Remove the justRegistered flag for the next pass
            if (closestSensor.justRegistered)
            {
                Log.Info(closestSensor.vessel.name + " inelligible for proximity detection this time. Waiting for next pass.");
            }
        }

        public void Update()
        { //AGX: The OnUpdate above only seems to run in flight mode, Update() here runs in all scenes
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

        public void OnDestroy()
        {
            GameEvents.onGameSceneLoadRequested.Remove(clearListeners);
            GameEvents.onGameSceneLoadRequested.Remove(clearListeners);
            deregisterListener(this);
        }

        public void clearListeners(GameScenes scene)
        {
            //On scene change, clear out all of the registered listeners
            ProxChannel.Listeners.Clear();
            ProxChannel.Listeners.TrimExcess();
        }

        public void registerListener()
        {
            //Remove duplicate entries from the list
            if (ProxChannel.Listeners.Any(listener => listener.vessel.id == this.vessel.id && listener.channel == this.channel) == true)
            {
                return;
            }
            Log.Info(this.vessel.vesselName + " proximity alarm has been registered on channel " + this.channel);
            //Register sensor to proximity sensor list
            ProxChannel.Listeners.Add(this);
            justRegistered = true;
        }

        public void deregisterListener(ProxSensor sensor)
        {
            if (ProxChannel.Listeners.Any(listener => listener.vessel.id == this.vessel.id && listener.channel == this.channel) == true)
            {
                Log.Info(sensor.vessel.vesselName + " proximity alarm has been deregistered on channel " + sensor.channel);
                ProxChannel.Listeners.Remove(sensor);
                ProxChannel.Listeners.TrimExcess();
            }
        }

        private void OnDetach(bool first)
        {
            //Remove this prox sensor from listener list
            deregisterListener(this);
        }

        public void onGUI()
        {
            //Update buttons
            updateButtons();
        }

        private void refreshPartWindow()
        { //AGX: Refresh right-click part window to show/hide Groups slider
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
