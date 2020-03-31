using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

namespace Lib
{
    public class SmartSensorModuleBase : PartModule
    {
        public bool illuminated = false;

        protected GameObject gameObjectOn;
        protected Light lightComponentOn;

        protected GameObject gameObjectOff;
        protected Light lightComponentOff;

        protected Log Log = new Log();

        //Highlighting.Highlighter a;


        #region Fields
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Group"),
            UI_ChooseOption(
            options = new String[] {
                "0",
                "1",
                "2",
                "3",
                "4",
                "5",
                "6",
                "7",
                "8",
                "9",
                "10",
                "11",
                "12",
                "13",
                "14",
                "15",
                "16"
            },
            display = new String[] {
                "Stage",
                "AG1",
                "AG2",
                "AG3",
                "AG4",
                "AG5",
                "AG6",
                "AG7",
                "AG8",
                "AG9",
                "AG10",
                "Lights",
                "RCS",
                "SAS",
                "Brakes",
                "Abort",
                "Gear"
            }
        )]
        public string group = "0";

        //AGXGroup shows if AGX installed and hides Group above
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Group"),
            UI_ChooseOption(
            options = new String[] {
                "0",
                "1",
                "11",
                "12",
                "13",
                "14",
                "15",
                "16"
            },
            display = new String[] {
                "Stage",
                "Action Group:",
                "Lights",
                "RCS",
                "SAS",
                "Brakes",
                "Abort",
                "Gear"
            }
        )]
        public string agxGroupType = "0";

        // AGX Action groups, use own slider if selected, only show this field if AGXGroup above is 1
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "Group:", guiFormat = "N0"),
            UI_FloatEdit(scene = UI_Scene.All, minValue = 1f, maxValue = 250f, incrementLarge = 75f, incrementSmall = 25f, incrementSlide = 1f)]
        public float agxGroupNum = 1;

        // following not for:  RadioControl
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = false, guiName = "Active"),
            UI_Toggle(disabledText = "False", enabledText = "True")]
        public bool isArmed = true;

        // following not for: Stager, Timer
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = false, guiName = "Auto Reset"),
            UI_Toggle(disabledText = "False", enabledText = "True")]
        public bool autoReset = false;
        #endregion

        void displayAllComponents()
        {
            //var allComponents : Component[];
           Component[]  allComponents = part.gameObject.GetComponents<Highlighting.Highlighter>();
            foreach (var component  in allComponents)
            {
                Log.Info("light found: " + component.name);
                var c = component as Highlighting.Highlighter;
                c.ConstantOff();
                
            }
        }

        protected void initLight(bool b, string lightName)
        {
            Log.Info("initLight:  b: " + b.ToString() + "   lightName: " + lightName);

            Transform lightTransform = part.FindModelTransform(lightName);
            if (!lightTransform)
            {
                Log.Info("lightTransform: " + lightName + " not found");
                return;
            }

            displayAllComponents();

            if (b)
            {
                Log.Info("Creating gameObject");
                gameObjectOn = new GameObject("Light");

                gameObjectOn.transform.parent = lightTransform.transform;
                gameObjectOn.transform.localPosition = lightTransform.localPosition;
                lightComponentOn = gameObjectOn.AddComponent<Light>();
                lightComponentOn.type = LightType.Point;
                lightComponentOn.enabled = false;
                lightComponentOn.intensity = 0;
                lightComponentOn.enabled = true;
            }
            else
            {
                Log.Info("Creating gameObject");
                gameObjectOff = new GameObject("Light");

                gameObjectOff.transform.parent = lightTransform.transform;
                gameObjectOff.transform.localPosition = lightTransform.localPosition;

                lightComponentOff = gameObjectOff.AddComponent<Light>();
                lightComponentOff.type = LightType.Point;
                lightComponentOff.enabled = false;
                
            }
        }

        protected void lightsOn(Utility.LightColor color = Utility.LightColor.WHITE)
        {
            if (!lightComponentOn) return;
            //Switch on model lights
            Utility.switchEmissive(this, lightComponentOn, true, color);
            //Utility.switchLight(this.part, "light-go", true);
            Utility.playAnimationSetToPosition(this.part, "glow", 1);
            illuminated = true;
        }

        protected void lightsOff()
        {
            if (!lightComponentOn) return;
            //Switch off model lights
            Utility.switchEmissive(this, lightComponentOn, false);
            //Utility.switchLight(this.part, "light-go", false);
            Utility.playAnimationSetToPosition(this.part, "glow", 0);
            illuminated = false;
        }
    }
}
