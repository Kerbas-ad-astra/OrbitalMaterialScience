﻿using System;
using System.Collections.Generic;
using System.Text;

namespace NE_Science
{
    /*
 * Experiments needing MSL-1000
 */
    public class MSLExperimentData : StepExperimentData
    {
        private Guid cachedVesselID;
        private int partCount;
        private MSL_Module[] physicsLabCache = null;

        protected MSLExperimentData(string id, string type, string name, string abb, EquipmentRacks eq, float mass, float cost)
            : base(id, type, name, abb, eq, mass, cost)
        { }

        public override List<Lab> getFreeLabsWithEquipment(Vessel vessel)
        {
            List<Lab> ret = new List<Lab>();
            MSL_Module[] allPhysicsLabs = null;
            if(cachedVesselID == vessel.id && partCount == vessel.parts.Count && physicsLabCache != null)
            {
                allPhysicsLabs = physicsLabCache;
            }
            else 
            {
                allPhysicsLabs = UnityFindObjectsOfType(typeof(MSL_Module)) as MSL_Module[];
                physicsLabCache = allPhysicsLabs;
                cachedVesselID = vessel.id;
                partCount = vessel.parts.Count;
                NE_Helper.log("Lab Cache refresh");
            }
            for (int idx = 0, count = allPhysicsLabs.Length; idx < count; idx++)
            {
                var lab = allPhysicsLabs[idx];
                if (lab.vessel == vessel && lab.hasEquipmentInstalled(neededEquipment) && lab.hasEquipmentFreeExperimentSlot(neededEquipment))
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

    public class FLEX_ExperimentData : MSLExperimentData
    {
        public FLEX_ExperimentData(float mass, float cost)
            : base("NE_FLEX", "FLEX", "Flame Extinguishment Experiment", "FLEX", EquipmentRacks.CIR, mass, cost)
        {
            step = new ResourceExperimentStep(this, Resources.CIR_BURN_TIME, 18, "", 0);
        }
    }

    public class CFI_ExperimentData : MSLExperimentData
    {
        public CFI_ExperimentData(float mass, float cost)
            : base("NE_CFI", "CFI", "Cool Flames Investigation", "CFI", EquipmentRacks.CIR, mass, cost)
        {
            step = new ResourceExperimentStep(this, Resources.CIR_BURN_TIME, 50, "", 0);
        }
    }

    public class MIS1_ExperimentData : MSLExperimentData
    {
        public MIS1_ExperimentData(float mass, float cost)
            : base("NE_MIS1", "MIS1", "3D Printer Demonstration Test", "MIS-1", EquipmentRacks.PRINTER, mass, cost)
        {
            step = new ResourceExperimentStep(this, Resources.PRINT_LAYER, 100, "", 0);
        }
    }

    public class MIS2_ExperimentData : MSLExperimentData
    {
        public MIS2_ExperimentData(float mass, float cost)
            : base("NE_MIS2", "MIS2", "Made in Space: Tools", "MIS-2", EquipmentRacks.PRINTER, mass, cost)
        {
            step = new ResourceExperimentStep(this, Resources.PRINT_LAYER, 200, "", 0);
        }
    }

    public class MIS3_ExperimentData : MSLExperimentData
    {
        public MIS3_ExperimentData(float mass, float cost)
            : base("NE_MIS3", "MIS3", "Made in Space: Jebediah figure", "MIS-3", EquipmentRacks.PRINTER, mass, cost)
        {
            step = new ResourceExperimentStep(this, Resources.PRINT_LAYER, 300, "", 0);
        }
    }

    public class CVB_ExperimentData : MSLExperimentData
    {
        public CVB_ExperimentData(float mass, float cost)
            : base("NE_CVB", "CVB", "Constrained Vapor Bubble", "CVB", EquipmentRacks.FIR, mass, cost)
        {
            step = new ResourceExperimentStep(this, Resources.FIR_TEST_RUN, 35, "", 0);
        }
    }

    public class PACE_ExperimentData : MSLExperimentData
    {
        public PACE_ExperimentData(float mass, float cost)
            : base("NE_PACE", "PACE", "Preliminary Advanced Colloids Experiment", "PACE", EquipmentRacks.FIR, mass, cost)
        {
            step = new ResourceExperimentStep(this, Resources.FIR_TEST_RUN, 22, "", 0);
        }
    }
}
