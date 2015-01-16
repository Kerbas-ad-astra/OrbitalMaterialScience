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
using UnityEngine;

namespace NE_Science
{
    class KEES_PayloadCarrier : PartModule
    {

        private bool kasInstalled = false;


        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);

            if (checkedForKAS())
            {
                kasInstalled = true;
                NE_Helper.log("KAS Installed");
                Events["attachPEC"].active = false;
                Events["attachPEC"].guiActive = false;
            }
            else
            {
                kasInstalled = false;
                NE_Helper.log("No KAS");
                Events["attachPEC"].active = true;
                Events["attachPEC"].guiActive = true;
            }
        }

        private bool checkedForKAS()
        {
            GameObject go = GameDatabase.Instance.GetModel("KAS/Parts/container1/container1");
            return (go != null);
        }

        [KSPEvent(guiActive = false, guiName = "Attach PEC", active = false)]
        public void attachPEC()
        {

            //GameObject pec = GameDatabase.Instance.GetModel("NehemiahInc/Parts/KEES/PEC/model");
            //Vector3 pos = FlightGlobals.ActiveVessel.GetTransform().position;
            //Quaternion rot = FlightGlobals.ActiveVessel.GetTransform().rotation;

            Part my_part = PartLoader.getPartInfoByName("NE.KEES.PEC").partPrefab;
            if (my_part != null)
            {
                Part parent_part = this.part;
                my_part.setParent(parent_part);
                my_part.transform.position += Vector3.right;
                my_part.attachJoint = PartJoint.Create(my_part, parent_part, my_part.srfAttachNode, null, AttachModes.SRF_ATTACH);
                this.vessel.Parts.Add(my_part);
            }
        }
    }
}