﻿/*
 *   This file is part of Orbital Material Science.
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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;

namespace NE_Science.Contracts.Parameters
{
    public class ExperimentDataReturnExperimentParameter : ContractParameter
    {        
        private CelestialBody targetBody = null;
        private ExperimentData experiment = null;

        

        public ExperimentDataReturnExperimentParameter()
        {
        }

        public ExperimentDataReturnExperimentParameter(CelestialBody target, ExperimentData exp)
            : base()
        {
            targetBody = target;
            experiment = exp;
        }

        protected override string GetHashString()
        {
            return "return and recover experiment " + this.GetHashCode();
        }
        protected override string GetTitle()
        {
            return "Return and recover experiment " + experiment.getAbbreviation() + " on Kerbin";
        }

        protected override void OnRegister()
        {
            NE_Helper.log("On Register");
            GameEvents.onVesselRecovered.Add(OnRecovered);
            if (StageRecoveryWrapper.StageRecoveryAvailable)
            {
                StageRecoveryWrapper.AddRecoverySuccessEvent(StageRecoverySuccessEvent);
            }
        }

        protected override void OnUnregister()
        {
            GameEvents.onVesselRecovered.Remove(OnRecovered);
            if (StageRecoveryWrapper.StageRecoveryAvailable)
            {
                StageRecoveryWrapper.RemoveRecoverySuccessEvent(StageRecoverySuccessEvent);
            }
        }

        private void StageRecoverySuccessEvent(Vessel v, float[] infoArray, string reason)
        {
            if (reason == "SUCCESS")
            {
                // MKW TODO: Check if we have quick or normal stage recovery
                OnRecovered(v.protoVessel, false);
            }
        }

        private void OnRecovered(ProtoVessel pv, bool quick)
        {
            NE_Helper.log("Recovery ProtoVessel");
            if (targetBody != null && experiment != null)
            {
                if(protovesselHasDoneExperiment(pv, experiment,targetBody)){
                    SetComplete();
                }
            }
            
        }

        private bool protovesselHasDoneExperiment(ProtoVessel pv, ExperimentData experiment, CelestialBody targetBody)
        {
            foreach (ProtoPartSnapshot part in pv.protoPartSnapshots)
            {
                foreach (ProtoPartModuleSnapshot module in part.modules)
                {
                    if (module.moduleName == "ExperimentStorage")
                    {
                        ConfigNode moduleConfi = module.moduleValues;
                        ConfigNode expData = moduleConfi.GetNode(ExperimentData.CONFIG_NODE_NAME);
                        if (expData != null)
                        {
                            if (expData.GetValue(ExperimentData.TYPE_VALUE) == experiment.getType() &&
                                expData.GetValue(ExperimentData.STATE_VALUE) == ExperimentState.FINALIZED.ToString())
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        protected override void OnLoad(ConfigNode node)
        {
            int bodyID = int.Parse(node.GetValue(KEESExperimentContract.TARGET_BODY));
            foreach (var body in FlightGlobals.Bodies)
            {
                if (body.flightGlobalsIndex == bodyID)
                    targetBody = body;
            }
            experiment = ExperimentData.getExperimentDataFromNode(node.GetNode(ExperimentData.CONFIG_NODE_NAME));
        }
        protected override void OnSave(ConfigNode node)
        {
            int bodyID = targetBody.flightGlobalsIndex;
            node.AddValue(KeminiExperimentContract.TARGET_BODY, bodyID);

            node.AddNode(experiment.getNode());
        }
    }
}