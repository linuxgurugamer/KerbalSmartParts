using KSP.Localization;
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

        #region NO_LOCALIZATION
        #region Fields
        [KSPField(guiActiveUnfocused=true,isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Group"),
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
        #endregion
            display = new String[] {
                "#LOC_SmartParts_10",
                "#LOC_SmartParts_39",
                "#LOC_SmartParts_40",
                "#LOC_SmartParts_41",
                "#LOC_SmartParts_42",
                "#LOC_SmartParts_43",
                "#LOC_SmartParts_44",
                "#LOC_SmartParts_45",
                "#LOC_SmartParts_46",
                "#LOC_SmartParts_47",
                "#LOC_SmartParts_48",
                "#LOC_SmartParts_49",
                "#LOC_SmartParts_22",
                "#LOC_SmartParts_23",
                "#LOC_SmartParts_24",
                "#LOC_SmartParts_25",
                "#LOC_SmartParts_26"
            }
        )]

        #region NO_LOCALIZATION
        public string group = "0";

        //AGXGroup shows if AGX installed and hides Group above
        [KSPField(guiActiveUnfocused=true,isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Group"),
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
        #endregion
            display = new String[] {
                "#LOC_SmartParts_10",
                "#LOC_SmartParts_50",
                "#LOC_SmartParts_49",
                "#LOC_SmartParts_22",
                "#LOC_SmartParts_23",
                "#LOC_SmartParts_24",
                "#LOC_SmartParts_25",
                "#LOC_SmartParts_26"
            }
        )]
        public string agxGroupType = "0";

        // AGX Action groups, use own slider if selected, only show this field if AGXGroup above is 1
        [KSPField(guiActiveUnfocused=true,isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "Group:", guiFormat = "N0"),
            UI_FloatEdit(scene = UI_Scene.All, minValue = 1f, maxValue = 250f, incrementLarge = 75f, incrementSmall = 25f, incrementSlide = 1f)]
        public float agxGroupNum = 1;

        // following not for:  RadioControl
        [KSPField(guiActiveUnfocused=true,isPersistant = true, guiActive = true, guiActiveEditor = false, guiName = "Active"),
            UI_Toggle(disabledText = "#LOC_SmartParts_4", enabledText = "#LOC_SmartParts_5")]
        public bool isArmed = true;

        // following not for: Stager, Timer
        [KSPField(guiActiveUnfocused=true,isPersistant = true, guiActive = true, guiActiveEditor = false, guiName = "Auto Reset"),
            UI_Toggle(disabledText = "#LOC_SmartParts_4", enabledText = "#LOC_SmartParts_5")]
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
            Utility.playAnimationSetToPosition(this.part, Localizer.Format("#LOC_SmartParts_51"), 1);
            illuminated = true;
        }

        protected void lightsOff()
        {
            if (!lightComponentOn) return;
            //Switch off model lights
            Utility.switchEmissive(this, lightComponentOn, false);
            //Utility.switchLight(this.part, "light-go", false);
            Utility.playAnimationSetToPosition(this.part, Localizer.Format("#LOC_SmartParts_51"), 0);
            illuminated = false;
        }
    }
}
