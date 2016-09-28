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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NE_Science
{
    /*
    *Module used to add Experiments to the Tech tree.
    */
    public class NE_ExperimentModule : PartModule
    {

        [KSPField(isPersistant = false)]
        public string type = "";

    }


    public class ExperimentFactory
    {
        public const string OMS_EXPERIMENTS = "OMS";
        public const string KEMINI_EXPERIMENTS = "KEMINI";

        static readonly List<string> omsRegistry = new List<string>() { "NE.TEST", "NE.CCFE", "NE.CFE",
            "NE.FLEX", "NE.CFI", "NE.MIS1", "NE.MIS2", "NE.MIS3", "NE.ExpExp1", "NE.ExpExp2", "NE.CVB",
            "NE.PACE", "NE.ADUM", "NE.SpiU"};

        static readonly List<string> keminiRegistry = new List<string>() { "NE.KeminiD5", "NE.KeminiD8",
            "NE.KeminiMSC3", "NE.KeminiD7", "NE.KeminiD10"};

        /** Returns a list of all purchased experiments.
         * If includeExperimental is true, it includes unpurchased parts as long as the required tech is available. */
        public static List<ExperimentData> getAvailableExperiments(string type, bool includeExperimental = false)
        {
            List<ExperimentData> list = new List<ExperimentData>();
            List<AvailablePart> parts = getAvailableExperimentParts(type, includeExperimental);

            foreach (var part in parts)
            {
                Part pPf = part.partPrefab;
                NE_ExperimentModule exp = pPf.GetComponent<NE_ExperimentModule>();
                float mass = pPf.mass;  //pPf.GetModuleMass(0);
                float cost = part.cost; //pPf.GetModuleCosts(0);
                list.Add(getExperiment(exp.type, mass, cost));
            }

            return list;
        }

        public static List<AvailablePart> getAvailableExperimentParts(string type, bool includeExperimental = false)
        {
            List<string> partsRegistry = null;
            List<AvailablePart> list = new List<AvailablePart>();

            switch (type)
            {
                case OMS_EXPERIMENTS:
                    partsRegistry = omsRegistry;
                    break;
                case KEMINI_EXPERIMENTS:
                    partsRegistry = keminiRegistry;
                    break;
                default:
                    return list;
            }

            foreach (var pn in partsRegistry)
            {
                AvailablePart part = PartLoader.getPartInfoByName(pn);
                if (part == null) continue;
                /*
                bool isPurchased = ResearchAndDevelopment.PartModelPurchased (part);
                bool isTechAvailable = ResearchAndDevelopment.PartTechAvailable (part);
                bool isExperimental = ResearchAndDevelopment.IsExperimentalPart (part);
                NE_Helper.log ("Part " + part.name +
                    " techlevel: [" + isTechAvailable + "]" +
                    " experimental: [" + isExperimental + "]" +
                    " purchased: [" + isPurchased + "]");
                */
                if (ResearchAndDevelopment.PartModelPurchased(part) ||
                    (includeExperimental && ResearchAndDevelopment.PartTechAvailable(part)))
                {
                    list.Add(part);
                }
            }

            return list;
        }

        public static AvailablePart getPartForExperiment(string type, ExperimentData exp)
        {
            AvailablePart ap = null;
            List<string> partsRegistry = null;

            switch (type)
            {
                case OMS_EXPERIMENTS:
                    partsRegistry = omsRegistry;
                    break;
                case KEMINI_EXPERIMENTS:
                    partsRegistry = keminiRegistry;
                    break;
            }
            foreach (var pn in partsRegistry)
            {
                ap = PartLoader.getPartInfoByName(pn);
                if (ap != null)
                {
                    NE_ExperimentModule e = ap.partPrefab.GetComponent<NE_ExperimentModule>();
                    if( e.type == exp.getType() )
                    {
                        break;
                    }
                }
            }
            return ap;
        }

        public static ExperimentData getExperiment(string type, float mass, float cost)
        {
            switch (type)
            {
                case "Test":
                    return new TestExperimentData(mass, cost);
                case "CCF":
                    return new CCF_ExperimentData(mass, cost);
                case "CFE":
                    return new CFE_ExperimentData(mass, cost);
                case "FLEX":
                    return new FLEX_ExperimentData(mass, cost);
                case "CFI":
                    return new CFI_ExperimentData(mass, cost);
                case "MIS1":
                    return new MIS1_ExperimentData(mass, cost);
                case "MIS2":
                    return new MIS2_ExperimentData(mass, cost);
                case "MIS3":
                    return new MIS3_ExperimentData(mass, cost);
                case "MEE1":
                    return new MEE1_ExperimentData(mass, cost);
                case "MEE2":
                    return new MEE2_ExperimentData(mass, cost);
                case "CVB":
                    return new CVB_ExperimentData(mass, cost);
                case "PACE":
                    return new PACE_ExperimentData(mass, cost);
                case "KeminiD5":
                    return new KeminiD5_ExperimentData(mass, cost);
                case "KeminiD8":
                    return new KeminiD8_ExperimentData(mass, cost);
                case "KeminiMSC3":
                    return new KeminiMSC3_ExperimentData(mass, cost);
                case "KeminiD7":
                    return new KeminiD7_ExperimentData(mass, cost);
                case "KeminiD10":
                    return new KeminiD10_ExperimentData(mass, cost);
                case "ADUM":
                    return new ADUM_ExperimentData(mass, cost);
                case "SpiU":
                    return new SpiU_ExperimentData(mass, cost);
                case "":
                    return ExperimentData.getNullObject();
                default:
                    NE_Helper.logError("Unknown ExperimentData Type '" + type + "'.");
                    return ExperimentData.getNullObject();
            }
        }
    }
}