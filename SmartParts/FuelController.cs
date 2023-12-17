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

namespace Lib
{
    public class FuelController : SmartSensorModuleBase
    {
        [KSPField(guiActiveUnfocused=true,isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Flow enabled")]
        bool flowEnabled = false;

        public override void OnStart(PartModule.StartState state) {
            this.part.fuelCrossFeed = flowEnabled;
        }

        [KSPEvent(guiName = "Toggle Crossfeed", guiActiveEditor = true, guiActive = true, guiActiveUnfocused = true)]
        public void toggleCrossfeed() {
            flowEnabled = !flowEnabled;
            this.part.fuelCrossFeed = flowEnabled;
        }

        [KSPAction("Toggle Crossfeed")]
        public void toggleCrossfeedAction(KSPActionParam param) {
            toggleCrossfeed();
        }

        [KSPAction("Activate Crossfeed")]
        public void activateCrossfeedAction(KSPActionParam param) {
            flowEnabled = true;
            this.part.fuelCrossFeed = flowEnabled;
        }

        [KSPAction("Deactivate Crossfeed")]
        public void deactivateCrossfeedAction(KSPActionParam param) {
            flowEnabled = false;
            this.part.fuelCrossFeed = flowEnabled;
        }
    }
}

