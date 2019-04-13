﻿#region License
/*
 *   This file is part of Orbital Material Science.
 *
 *   This file implements the KEESExperiment Part Module.
 *
 *   Part of the code may originate from Station Science by ether net http://forum.kerbalspaceprogram.com/threads/54774-0-23-5-Station-Science-(fourth-alpha-low-tech-docking-port-experiment-pod-models)
 *
 *   Orbital Material Science is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   Orbital Material Sciencee is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.
 *
 *   You should have received a copy of the GNU General Public License
 *   along with Orbital Material Science.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KSP.Localization;
using KSP.UI.Screens.Flight.Dialogs;

namespace NE_Science
{
    using KAC;

    public class KEESExperiment : OMSExperiment
    {
        #region KSP Fields and state variables
        [KSPField(isPersistant = false)]
        public float exposureTimeRequired;

        [KSPField(isPersistant = true)]
        public bool docked = false;

        public float exposureTimeRemaining
        {
            get
            {
                double numExposureTime = ResourceHelper.getResourceAmount(part, Resources.EXPOSURE_TIME);
                return (float)(exposureTimeRequired - Math.Round(numExposureTime, 3));
            }
        }

        private BaseEvent DeployExperimentEvent
        {
            get { return Events["DeployExperiment"]; }
        }

        /// <summary>
        /// The KEES_Lab which we are using.
        /// </summary>
        internal KEES_Lab keesLab = null;

        #endregion

        #region KSP Events
        // Event overloaded from ModuleScienceExperiment
        // Displays button to user, the exact function of which will depend on the current state
        new public void DeployExperiment()
        {
            switch(state)
            {
                case READY: // "Start"
                    if (!checkBoring(vessel, true))
                    {
                        OnExperimentStarted();
                    }
                    break;

                case RUNNING: // "Close"
                    OnExperimentStopped();
                    break;

                case FINISHED: // Finalize
                    OnExperimentFinalized();
                    break;

                default:
                    NE_Helper.logError("Logic error - button clicked for unsupported state " + state);
                    break;
            }
        }

        // Event overloaded from ModuleScienceExperiment
        new public void ResetExperiment()
        {
            NE_Helper.log("KEESExperiment: ResetExperiment()");
            base.ResetExperiment();
            ResourceHelper.setResourceAmount(part, Resources.EXPOSURE_TIME, 0);
        }

        // Event overloaded from ModuleScienceExperiment
        new public void ResetExperimentExternal()
        {
            NE_Helper.log("KEESExperiment: ResetExperimentExternal()");
            base.ResetExperimentExternal();
            ResourceHelper.setResourceAmount(part, Resources.EXPOSURE_TIME, 0);
        }

        // Event overloaded from ModuleScienceExperiment
        new public void ResetAction(KSPActionParam p)
        {
            NE_Helper.log("KEESExperiment: ResetAction()");
            base.ResetAction(p);
            ResourceHelper.setResourceAmount(part, Resources.EXPOSURE_TIME, 0);
        }

#if (DEBUG)
        [KSPEvent(guiActive = true, guiName = "Debug Dump", active = true)]
        public void DebugDump()
        {
            /* Printed out in Player.log */
            NE_Helper.log(this.ToString());
        }
#endif
        #endregion

        #region KIS Events
        /// <summary>
        /// Process incoming KIS events
        /// </summary>
        /// eventData is a dictionary with the following entries:
        ///     action - name of action; we're interested in the following: AttachEnd, Decouple
        ///     targetPart - part we got attached to; on Decouple it's null
        ///     
        void OnKISAction(Dictionary<string, object> eventData)
        {
            NE_Helper.log("KEESExperiment: received KISAction - " + eventData.ToString());
            try
            {
                switch ((string)eventData["action"])
                {
                    case "AttachEnd":
                        // Event when we got attached. Check to make sure we got attached to the "top" node of a KEES_PEC;
                        // degenerate players could surface-attach us to something.
                        AttachNode sourceNode = (AttachNode)eventData["sourceNode"];
                        AttachNode targetNode = (AttachNode)eventData["targetNode"];
                        Part targetPart = (Part)eventData["targetPart"];
                        if ( sourceNode.id == "bottom" && targetNode?.id == "top" && targetPart?.name == "NE.KEES.PEC")
                        {
                            OnExperimentMounted();
                        }
                        break;

                    case "Decouple":
                        // No parameters; if we're docked, we'll get undocked.
                        if (docked)
                        {
                            OnExperimentUnmounted();
                        }
                        break;
                }
            } catch(Exception e)
            {
                NE_Helper.logError("Exception while handling KISAction: " + e.ToString());
            }
        }
        #endregion

        #region PartModule Overrides
        // Override from PartModule, called before OnUpdate() or OnFixedUpdate()
        public override void OnStart(StartState f_kspState)
        {
            NE_Helper.log("KEESExperiment: OnStart()");
            base.OnStart(f_kspState);
            if (f_kspState == StartState.Editor) { return; }

            // Change default status strings
            notReadyStatus = Localizer.GetStringByTag("#ne_Not_installed");
            readyStatus = Localizer.GetStringByTag("#ne_Ready");
            errorStatus = Localizer.GetStringByTag("#ne_Experiment_Ruined");

            // The only reason we have a "lab" is for the generator; a bit of overkill really.
            // By default, let's turn off the generator, it should only be running if the
            // experiment is running.
            if (keesLab is null)
            {
                keesLab = part.FindModuleImplementing<KEES_Lab>();
            }
            keesLab.doResearch = false;
            ResourceHelper.setResourceMaxAmount(part, Resources.EXPOSURE_TIME, exposureTimeRequired);
            this.part.force_activate();

            Fields["expStatus"].guiActive = true;
            DeployExperimentEvent.active = false;
            DeployExperimentEvent.guiActiveUnfocused = true;
            DeployExperimentEvent.unfocusedRange = 3;
#if (DEBUG)
            Events["DebugDump"].active = true;
#endif
            StartCoroutine(updateStatus());
        }


        private bool isExperimentsResultDialogOpen = false;
        public override void OnUpdate()
        {
            base.OnUpdate();
            // check experiments result dialog has closed on this frame
            if (isExperimentsResultDialogOpen && ExperimentsResultDialog.Instance == null)
            {
                // Do stuff if it closed
            }
            if (ExperimentsResultDialog.Instance != null)
            {
                // check experiments result dialog has opened on this frame
                if (isExperimentsResultDialogOpen == false)
                {
                    // Do stuff if it just opened

                    ExperimentsResultDialog erd = ExperimentsResultDialog.Instance;
                    // TODO: Hook into callbacks?
                    // subjectID == NE_KEES_TEST@KerbinSrfLanded ; last_subjectId == NE_KEES_TEST@KerbinSrfLandedLaunchPad
                    if (erd.currentPage.pageData.subjectID.Contains("NE_KEES"))
                    {
                        UnityEngine.UI.Button[] buttons = erd.GetComponentsInChildren<UnityEngine.UI.Button>();
                        foreach (UnityEngine.UI.Button b in buttons)
                        {
                            // Disable the Reset and Lab buttons
                            if (b.name == "ButtonReset" || b.name == "ButtonLab" || b.name == "ButtonTransmit")
                            {
                                b.interactable = false;
                            }
                        }

                        // This doesn't seem to do anything
                        //erd.currentPage.showReset = false;
                    }
                }
            }

            // update experiments result dialog open state
            isExperimentsResultDialogOpen = (ExperimentsResultDialog.Instance != null);
        }

        // Override from PartModule, called when the part is destroyed;
        // This can occur when the part is "g"rabbed using KIS.
        public override void OnInactive()
        {
            NE_Helper.log("KEESExperiment: OnInactive()");
            if (docked)
            {
                OnExperimentUnmounted();
            }
            base.OnInactive();
        }

        /// <summary>
        /// Generate the user-visible description of the part.
        /// </summary>
        public override string GetInfo()
        {
            string timeStr;
            timeStr = NE_Helper.timeToStr((long)(exposureTimeRequired * keesLab.ExposureTimePerHour));
            String ret = Localizer.Format("#ne_Exposure_time_required_1", timeStr);
            ret += "\n";
            ret += Localizer.GetStringByTag("#ne_You_need_to_install_the_experiment_on_a_KEES_PEC");

            return ret;
        }
        #endregion

        #region KEES Experiment Callbacks
        /// <summary>
        /// Called when the experiment is mounted on a PEC.
        /// </summary>
        /// This occurs either when the user manually drags the Experiment onto a PEC, or
        /// on a scene switch to a Vessel contained a mounted Experiment.
        public virtual void OnExperimentMounted()
        {
            NE_Helper.log("KEESExperiment: OnExperimentMounted()");
            switch(state)
            {
                case NOT_READY:
                    state = READY;
                    break;

                case RUNNING:
                    // Can occur when we switch back to a running station in which case the
                    // experiment may actually show up as closed.
                    openExperiment();
                    keesLab.doResearch = true;
                    break;
            }
            docked = true;
        }

        /// <summary>
        /// Called when the experiment is removed from a PEC
        /// </summary>
        public virtual void OnExperimentUnmounted()
        {
            NE_Helper.log("KEESExperiment: OnExperimentUnmounted()");
            if (!docked)
            {
                return;
            }
            DeployExperimentEvent.active = false;
            expStatus = Localizer.GetStringByTag("#ne_Not_installed_on_a_PEC");

            switch(state)
            {
                case READY:
                    state = NOT_READY;
                    break;

                case RUNNING:
                    ScreenMessages.PostScreenMessage(Localizer.Format("#ne_Warning_1_has_detached_from_station_without_being_finalized", part.partInfo.title), 2, ScreenMessageStyle.UPPER_CENTER);
                    stopResearch(NOT_READY);
                    break;

                case FINISHED:
                    ScreenMessages.PostScreenMessage(Localizer.Format("#ne_Warning_1_has_detached_from_station_without_being_finalized", part.partInfo.title), 2, ScreenMessageStyle.UPPER_CENTER);
                    break;
            }
            docked = false;
        }

        /// <summary>
        /// Message sent from PEC when it is attached to a ship
        /// </summary>
        public virtual void OnPecCoupled()
        {
            // Maybe reset the experiment to be not ruined?
        }

        /// <summary>
        /// Message sent from PEC when it is detached from a ship
        /// </summary>
        public virtual void OnPecDecoupled()
        {
            NE_Helper.log("KEESExperiment: OnPecDecoupled()");
            DeployExperimentEvent.active = false;
            switch(state)
            {
                case READY:
                    state = NOT_READY;
                    expStatus = notReadyStatus;
                    break;

                case RUNNING:
                case FINISHED:
                    Events["DeployExperiment"].active = false;
                    stopResearch(ERROR);
                    expStatus = errorStatus;
                    break;

                default:
                    // do nothing
                    break;
            }
        }

        /// <summary>
        /// Called when an experiment is (re)started manually from the user
        /// </summary>
        public virtual void OnExperimentStarted()
        {
            NE_Helper.log("KEESExperiment: OnExperimentStarted()");
            DeployExperimentEvent.guiName = Localizer.GetStringByTag("#ne_Close");
            openExperiment();

            // Save the science situation where this experiment was started. If it ever
            // changes, the experiment is ruined.
            String currentSubjectId = ScienceHelper.getScienceSubject(experimentID, vessel)?.id;
            if (String.IsNullOrEmpty(last_subjectId))
            {
                last_subjectId = ScienceHelper.getScienceSubject(experimentID, vessel)?.id;
            }
            else if(last_subjectId != currentSubjectId)
            {
                OnBiomeChanged();
                return;
            }
            setAlarm();
            state = RUNNING;
            expStatus = runningStatus;
            // Start the lab
            keesLab.doResearch = true;
            ScreenMessages.PostScreenMessage("#ne_Started_experiment", 6, ScreenMessageStyle.UPPER_CENTER);
        }
        public virtual void OnExperimentStopped()
        {
            NE_Helper.log("KEESExperiment: OnExperimentStopped()");
            DeployExperimentEvent.guiName = Localizer.GetStringByTag("#ne_Start_Experiment");
            stopResearch(READY);
            expStatus = readyStatus;
        }
        /// <summary>
        /// Called on a RUNNING experiment when a biome change has been detected.
        /// </summary>
        public virtual void OnBiomeChanged()
        {
            NE_Helper.log("KEESExperiment: OnBiomeChanged()");
            ScreenMessages.PostScreenMessage(Localizer.Format("#ne_Location_changed_mid_experiment_1_ruined", part.partInfo.title), 6, ScreenMessageStyle.UPPER_CENTER);
            DeployExperimentEvent.active = false;
            stopResearch(ERROR, false);
        }
        /// <summary>
        /// Called on a RUNNING experiment when Exposure Time has finished accumulating
        /// </summary>
        public virtual void OnExperimentFinished()
        {
            NE_Helper.log("KEESExperiment: OnExperimentFinished()");
            DeployExperimentEvent.guiName = Localizer.GetStringByTag("#ne_Finalize_Results");
            completed = (float)Planetarium.GetUniversalTime();
            state = FINISHED;
            expStatus = finalizedStatus;
            // Stop the lab
            keesLab.doResearch = false;
        }
        /// <summary>
        /// Called when user clicks on "Finalize"; bring up KSP Science Dialog showing results
        /// </summary>
        public virtual void OnExperimentFinalized()
        {
            NE_Helper.log("KEESExperiment: OnExperimentFinalized()");
            DeployExperimentEvent.active = false;
            stopResearch(FINALIZED);
            expStatus = finalizedStatus;
            base.DeployExperiment();
        }
        #endregion

        #region Coroutines
        /// <summary>
        /// Checks for any state transitions which can't be captured by events and updates the GUI
        /// </summary>
        /// We need to do the UI updates here as otherwise the UI is not properly updated on startup.
        public System.Collections.IEnumerator updateStatus()
        {
            while (true)
            {
                switch(state)
                {
                    case NOT_READY:
                        expStatus = notReadyStatus;
                        DeployExperimentEvent.active = false;
                        break;
                    case READY:
                        expStatus = readyStatus;
                        DeployExperimentEvent.guiName = Localizer.GetStringByTag("#ne_Start_Experiment");
                        DeployExperimentEvent.active = true;
                        break;
                    case RUNNING:
                        expStatus = runningStatus;
                        DeployExperimentEvent.guiName = Localizer.GetStringByTag("#ne_Close");
                        DeployExperimentEvent.active = true;
                        if (hasBiomeChangedSinceStart())
                        {
                            OnBiomeChanged();
                        }
                        if (hasExperimentFinished())
                        {
                            OnExperimentFinished();
                        }
                        break;
                    case FINISHED:
                        expStatus = finishedStatus;
                        DeployExperimentEvent.guiName = Localizer.GetStringByTag("#ne_Finalize_Results");
                        DeployExperimentEvent.active = true;
                        break;
                    case FINALIZED:
                        expStatus = finalizedStatus;
                        DeployExperimentEvent.active = false;
                        break;
                    case ERROR:
                        expStatus = errorStatus;
                        DeployExperimentEvent.active = false;
                        break;
                }
                yield return new UnityEngine.WaitForSeconds(1f);
            }
        }
        #endregion

        #region Model animation support functions
        /** Animates the experiment to open */
        internal void openExperiment()
        {
            playAnimation("Deploy", 1, 0);
        }

        /** Animates the experiment to close */
        internal void closeExperiment()
        {
            playAnimation("Deploy", -1, 1);
        }

        private void playAnimation(string animName, float speed, float time)
        {
            // MKW TODO - remove "FirstOrDefault" Linq statement
            Animation anim = part.FindModelAnimators(animName).FirstOrDefault();
            if (anim != null)
            {
                anim[animName].speed = speed;
                anim[animName].normalizedTime = time;

                anim.Play(animName);
            }
            else
            {
                NE_Helper.logError("no Animation; Name: " + animName);
            }
        }
        #endregion

        #region OMSExperiment Overloads
        internal override float getRemainingExperimentTime()
        {
            return exposureTimeRemaining * keesLab.ExposureTimePerHour;
        }

        internal override string getAlarmDescription()
        {
            return "KEES Alarm";
        }
        #endregion

        /// <summary>
        /// Converts the object to a human-readble string suitable for printing.
        /// </summary>
        /// Overloads base-class implementation.
        new public String ToString()
        {
            String ret = base.ToString() + "\n";
            ret += "\tstate:              " + this.state + "\n";
            ret += "\tresource Amount:    " + ResourceHelper.getResourceAmount(part, Resources.EXPOSURE_TIME) + "\n";
            ret += "\tScience Amount:     " + GetScienceCount() + "\n";
            ret += "\tdataIsCollectable:  " + dataIsCollectable + "\n";
            ret += "\tresettable:         " + resettable + "\n";
            ret += "\tresourceToReset:    " + resourceToReset + "\n";
            ret += "\trerunnable:         " + rerunnable + "\n";
            return ret;
        }

        public void stopResearch(int newState, bool doCloseExperiment = true)
        {
            // Stop the lab
            keesLab.doResearch = false;
            if(doCloseExperiment)
            {
                closeExperiment();
            }
            deleteAlarm();
            state = newState;
        }

        /// <summary>
        /// Returns true if the Biome has changed from when the Experiment was started.
        /// </summary>
        public bool hasBiomeChangedSinceStart()
        {
            string subjectId = ScienceHelper.getScienceSubject(experimentID, vessel)?.id;
            return subjectId != last_subjectId;
        }

        /// <summary>
        /// Returns true if the experiment has completed its requirements.
        /// </summary>
        public bool hasExperimentFinished()
        {
            return exposureTimeRemaining <= 0.0;
        }
    }

}
