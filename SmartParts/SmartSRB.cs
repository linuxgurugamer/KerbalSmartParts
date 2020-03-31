using System;


namespace Lib
{
    class SmartSRB : SmartSensorModuleBase
    {
        [KSPField(isPersistant = true, guiActive = true, guiName = "SRB TWR %", guiFormat = "F0", guiUnits = "%"),
        UI_FloatEdit(scene = UI_Scene.All, minValue = 100f, maxValue = 150f, incrementSlide = 1f)]
        public float StagePercentageMass = 0;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Trigger on Flameout"), UI_Toggle()]
        public bool triggerOnFlameout = true;

        [KSPField(isPersistant = false, guiActive = true, guiName = "SRB TWR", guiFormat = "F2")]
        private double displayTWR = 0;

        [KSPField(guiActive = false, guiName = "Fire next update")]
        private Boolean fireNextupdate = false;

        #region Variables
        ModuleEngines engineModule;

        double maxTWR = 0;
        bool engineDirty = false;
        bool wasArmed = false;
        bool isRunning = false;
        bool checkResearched = true;
        private string groupLastUpdate = "0"; //AGX: What was our selected group last update frame? Top slider.

        #endregion

        #region Overrides

        public override void OnStart(StartState state)
        {
            Log.setTitle("SmartSRB");

            ValidateEngine();

            if (HighLogic.LoadedSceneIsFlight && !isEnabled)
            {
                Log.Info("Disabled - not researched.");
                DisableGUI();
                return;
            }

            Log.Info("Started");

            if (HighLogic.LoadedSceneIsFlight && engineModule == null)
                Log.Error("An engine module was not found.");

            //Initial button layout
            updateButtons();
            //Force activation no matter which stage it's on
            if (!observingSelf())
                this.part.force_activate();
            
            wasArmed = isArmed;

            Fields["autoReset"].guiActiveEditor = false;
            Fields["autoReset"].guiActive = false;
            
            initLight(true, "light-go");
        }


        public override void OnUpdate()
        {
            if (!isEnabled)
                return;

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
                isArmed = false;
                wasArmed = false;
                maxTWR = 0; // prevents triggering right away if rearmed
                lightsOn();
            }


            if (engineDirty)
                ValidateEngine(); // unreachable?

            double twr = GetTWR();
            displayTWR = twr;
           
            if (isArmed)
            {
                if (maxTWR > 0 && twr >= 0 && twr < (StagePercentageMass / 100) && twr < maxTWR)
                {
                    Log.Info("fireNextupdate maxTWR: " + maxTWR.ToString("F2") + ", twr: " + twr.ToString("F2"));
                    fireNextupdate = true;
                    //Helper.fireEvent(this.part, 0, (int)0);
                }
                else if (maxTWR > 0 && twr < 0) // will get here if engine flames out with triggerOnFlameout = false
                {
                    isArmed = false;
                }
                maxTWR = Math.Max(maxTWR, twr);
            }

            if (wasArmed != isArmed) // toggled or flamed out with triggerOnFlameout = false
            {
                wasArmed = isArmed;
                maxTWR = 0;
            }
            if (isArmed && illuminated)
                lightsOff();
        }
        #endregion

        public new void Awake()
        {
            base.Awake();
            Log.Info("SmartSRB.Awake");
            GameEvents.onEditorPartPlaced.Add(OnEditorPartPlaced);
        }
  
        void OnEditorPartPlaced(Part p)
        {
            if (!observingSelf())
            {
                if (this.part.parent == null)
                {
                    engineModule = null;
                    engineDirty = true;
                }
                else
                    ValidateEngine();
            }
        }

        bool observingSelf()
        {
            return engineModule != null && engineModule.part == part;
        }

        bool FindEngine(Part p)
        {
            engineModule = null;
            if (p != null)
            {
                foreach (ModuleEngines engine in p.FindModulesImplementing<ModuleEngines>())
                {
                    if (engine.throttleLocked)
                    {
                        engineModule = engine;
                        break;
                    }
                }
            }
            return engineModule != null;
        }

        void ValidateEngine()
        {
            Log.Info("SmartSRB.ValidateEngine");
            engineDirty = false;

            if (!FindEngine(part))
                FindEngine(part.parent);

            if (engineModule != null)
            {
                Fields["isArmed"].guiActiveEditor = true;
                Fields["isArmed"].guiActive = true;
            }
            else
            {
                ScreenMessages.PostScreenMessage("SmartSRB only works on SRBs", 5f, ScreenMessageStyle.UPPER_CENTER);
                Fields["isArmed"].guiActiveEditor = false;
                Fields["isArmed"].guiActive = false;
            }
        }

        void Destroy()
        {
            GameEvents.onEditorPartPlaced.Remove(OnEditorPartPlaced);
        }

        public double GetTWR()
        {
            double twr = -1;
            if (engineModule != null)
            {
                double thrust = engineModule.GetCurrentThrust();
                if (thrust > 0)
                {
                    isRunning = true;
                    Part p = engineModule.part;
                    double partTotalMass = p.mass + p.GetModuleMass(p.mass) + p.GetResourceMass();
                    //double gravHeight = vessel.altitude + vessel.mainBody.Radius; //gravity force at this altitude (not in m/s^2)
                    //double gravForce = vessel.mainBody.gMagnitudeAtCenter / Math.Pow(gravHeight, 2); //accel down due to gravity in m/s^2

                    twr = thrust / (partTotalMass * vessel.graviticAcceleration.magnitude);
                }
                else if (triggerOnFlameout && isRunning) // engineModule.flameout not always set
                {
                    twr = 0;
                }
                isRunning = thrust > 0;
            }
            return twr;
        }

        private void DisableGUI()
        {
            foreach (BaseAction a in Actions)
            {
                a.active = false;
            }
            foreach (BaseField f in Fields)
            {
                f.guiActive = false;
                f.guiActiveEditor = false;
            }
            foreach (BaseEvent ev in Events)
            {
                ev.guiActive = false;
                ev.guiActiveEditor = false;
            }
        }

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
        private void refreshPartWindow() //AGX: Refresh right-click part window to show/hide Groups slider
        {
            UIPartActionWindow[] partWins = FindObjectsOfType<UIPartActionWindow>();
            //Log.Info("Wind count " + partWins.Count());
            foreach (UIPartActionWindow partWin in partWins)
            {
                partWin.displayDirty = true;
            }
        }
        public void Update() //AGX: The OnUpdate above only seems to run in flight mode, Update() here runs in all scenes
        {
            if (!isEnabled)
                return;

            bool observingSelf = engineModule != null && engineModule.part == part;
            if (HighLogic.LoadedSceneIsEditor && observingSelf && checkResearched)
            {
                checkResearched = false;
                AvailablePart ap = PartLoader.getPartInfoByName("");
                if (!(ResearchAndDevelopment.PartTechAvailable(ap) && ResearchAndDevelopment.PartModelPurchased(ap)))
                {
                    DisableGUI();
                    isEnabled = false;
                }
            }

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
    }
}
