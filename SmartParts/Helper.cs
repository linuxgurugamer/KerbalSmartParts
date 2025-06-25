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
using KSP.UI.Screens;

namespace Lib
{
    public static class Helper
    {
        private static Log Log = new Log();
        #region Dictionary

        public static Dictionary<int, KSPActionGroup> KM_dictAG = new Dictionary<int, KSPActionGroup> {
            { 0,  KSPActionGroup.None },
            { 1,  KSPActionGroup.Custom01 },
            { 2,  KSPActionGroup.Custom02 },
            { 3,  KSPActionGroup.Custom03 },
            { 4,  KSPActionGroup.Custom04 },
            { 5,  KSPActionGroup.Custom05 },
            { 6,  KSPActionGroup.Custom06 },
            { 7,  KSPActionGroup.Custom07 },
            { 8,  KSPActionGroup.Custom08 },
            { 9,  KSPActionGroup.Custom09 },
            { 10, KSPActionGroup.Custom10 },
            { 11, KSPActionGroup.Light },
            { 12, KSPActionGroup.RCS },
            { 13, KSPActionGroup.SAS },
            { 14, KSPActionGroup.Brakes },
            { 15, KSPActionGroup.Abort },
            { 16, KSPActionGroup.Gear }
        };

        public static Dictionary<int, String> KM_dictAGNames = new Dictionary<int, String> {
            { 0,  Localizer.Format("#LOC_SmartParts_10") },
            { 1,  Localizer.Format("#LOC_SmartParts_11") },
            { 2,  Localizer.Format("#LOC_SmartParts_12") },
            { 3,  Localizer.Format("#LOC_SmartParts_13") },
            { 4,  Localizer.Format("#LOC_SmartParts_14") },
            { 5,  Localizer.Format("#LOC_SmartParts_15") },
            { 6,  Localizer.Format("#LOC_SmartParts_16") },
            { 7,  Localizer.Format("#LOC_SmartParts_17") },
            { 8,  Localizer.Format("#LOC_SmartParts_18") },
            { 9,  Localizer.Format("#LOC_SmartParts_19") },
            { 10, Localizer.Format("#LOC_SmartParts_20") },
            { 11, Localizer.Format("#LOC_SmartParts_21") },
            { 12, Localizer.Format("#LOC_SmartParts_22") },
            { 13, Localizer.Format("#LOC_SmartParts_23") },
            { 14, Localizer.Format("#LOC_SmartParts_24") },
            { 15, Localizer.Format("#LOC_SmartParts_25") },
            { 16, Localizer.Format("#LOC_SmartParts_26") },
            { 17, Localizer.Format("#LOC_SmartParts_27") },
        };

        public static int maxEvent = 17;

        #endregion


        #region Staging/AG Methods

        public static void fireEvent(Part p, int eventID, int AGXgroup = -1) { //AGXgroup is only used if Action Groups Extended is installed, ignore it otherwiseDebug.Log("fire " + p.name + eventID + "||" + AGXgroup);
            if (p == null)
                return;
            if (eventID == 0) {
                Log.Info("Fire Stage from part " + p);
                fireNextNonEmptyStage(p.vessel);
                return;
            }
            else if(AGXInterface.AGExtInstalled() && eventID == 1 && AGXgroup != -1) 
            {
                AGXInterface.AGX2VslToggleGroup(p.vessel.rootPart.flightID, AGXgroup); //call to agx to activate group
            }
            else if (eventID > 0 && eventID <= maxEvent) {
                Log.Info("Fire Event " + KM_dictAGNames[eventID] + " from part " + p);
                p.vessel.ActionGroups.ToggleGroup(KM_dictAG[eventID]);
            }
        }

        public static void fireNextNonEmptyStage(Vessel v) {
            // the parts to be fired
            List<Part> resultList = new List<Part>();

            int highestNextStage = getHighestNextStage(v.rootPart, v.currentStage);
            traverseChildren(v.rootPart, highestNextStage, ref resultList);

            foreach (Part stageItem in resultList) {
                Log.Info("Activate:" + stageItem);
                stageItem.activate(highestNextStage, stageItem.vessel);
                stageItem.inverseStage = v.currentStage;
            }
            v.currentStage = highestNextStage;
            //If this is the currently active vessel, activate the next, now empty, stage. This is an ugly, ugly hack but it's the only way to clear out the empty stage.
            //Switching to a vessel that has been staged this way already clears out the empty stage, so this isn't required for those.
            if (v.isActiveVessel) {
				StageManager.ActivateNextStage ();
            }
        }

        private static int getHighestNextStage(Part p, int currentStage) {

            int highestChildStage = 0;

            // if this is the root part and its a decoupler: ignore it. It was probably fired before.
            // This is dirty guesswork but everything else seems not to work. KSP staging is too messy.
            if (p.vessel.rootPart == p &&
                (p.name.IndexOf("ecoupl") != -1 || p.name.IndexOf("eparat") != -1)) {  // NO_LOCALIZATION
            }
            else if (p.inverseStage < currentStage) {
                highestChildStage = p.inverseStage;
            }


            // Check all children. If this part has no children, inversestage or current Stage will be returned
            int childStage = 0;
            foreach (Part child in p.children) {
                childStage = getHighestNextStage(child, currentStage);
                if (childStage > highestChildStage && childStage < currentStage) {
                    highestChildStage = childStage;
                }
            }
            return highestChildStage;
        }

        private static void traverseChildren(Part p, int nextStage, ref List<Part> resultList) {
            if (p.inverseStage >= nextStage) {
                resultList.Add(p);
            }
            foreach (Part child in p.children) {
                traverseChildren(child, nextStage, ref resultList);
            }
        }

        #endregion
    }
}
