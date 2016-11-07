﻿using System;
using System.Collections.Generic;
using System.Text;

namespace NE_Science
{
    public abstract class MEPExperimentData : MultiStepExperimentData<MEPResourceExperimentStep>
    {

        private Guid cachedVesselID;
        private int partCount;
        private MEP_Module[] physicsLabCache = null;

        protected MEPExperimentData(string id, string type, string name, string abb, EquipmentRacks eq, float mass, float cost, int numSteps)
            : base(id, type, name, abb, eq, mass, cost, numSteps)
        { }

        /** Sets up the required number of test subjects */
        protected void setExperimentStep(string resourceName, float resourceAmount, string stepName, int index)
        {
            steps[index]= new MEPResourceExperimentStep(this, resourceName, resourceAmount, stepName, index);
        }

        public override List<Lab> getFreeLabsWithEquipment(Vessel vessel)
        {
            List<Lab> ret = new List<Lab>();
            if (physicsLabCache == null || cachedVesselID != vessel.id || partCount != vessel.parts.Count)
            {
                physicsLabCache = UnityFindObjectsOfType(typeof(MEP_Module)) as MEP_Module[];
                cachedVesselID = vessel.id;
                partCount = vessel.parts.Count;
                NE_Helper.log("Lab Cache refresh");
            }
            for (int idx = 0, count = physicsLabCache.Length; idx < count; idx++)
            {
                var lab = physicsLabCache[idx];
                if (lab.vessel == vessel && lab.hasEquipmentFreeExperimentSlot(neededEquipment))
                {
                    ret.Add(lab);
                }
            }
            return ret;
        }

        public override bool canInstall(Vessel vessel)
        {
            List<Lab> labs = getFreeLabsWithEquipment(vessel);
            return labs.Count > 0 && state == ExperimentState.STORED;
        }
    }

    public class MEE1_ExperimentData : MEPExperimentData
    {
        public MEE1_ExperimentData(float mass, float cost)
            : base("NE_MEE1", "MEE1", "Material Exposure Experiment 1", "MEE1", EquipmentRacks.EXPOSURE, mass, cost, 2)
        {
            setExperimentStep(Resources.LAB_TIME, 1, "Preparation", 0);
            setExperimentStep(Resources.EXPOSURE_TIME, 20, "Exposure", 1);
        }
    }

    public class MEE2_ExperimentData : MEPExperimentData
    {
        public MEE2_ExperimentData(float mass, float cost)
            : base("NE_MEE2", "MEE2", "Material Exposure Experiment 2", "MEE2", EquipmentRacks.EXPOSURE, mass, cost, 3)
        {
            setExperimentStep(Resources.LAB_TIME, 1, "Preparation", 0);
            setExperimentStep(Resources.EXPOSURE_TIME, 40, "Exposure", 1);
            setExperimentStep(Resources.LAB_TIME, 2, "Store Samples", 2);
        }
    }
}
