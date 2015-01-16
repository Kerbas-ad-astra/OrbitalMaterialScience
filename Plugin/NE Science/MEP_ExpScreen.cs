﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NE_Science
{
    class MEP_ExpScreen : InternalModule
    {
        private double lastUpdate = 0;

        [KSPField(isPersistant = false)]
        public string folder = "NehemiahInc/Props/MEP_StatusScreen";

        [KSPField(isPersistant = false)]
        public string noExpTexture = "";

        [KSPField(isPersistant = false)]
        public string mee1Texture = "";

        [KSPField(isPersistant = false)]
        public string mee2Texture = "";

        [KSPField(isPersistant = false)]
        public float refreshInterval = 2;

        private GameDatabase.TextureInfo noExp;
        private GameDatabase.TextureInfo mee1;
        private GameDatabase.TextureInfo mee2;

        private Material screenMat = null;

        private string lastExpName = "";

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (lastUpdate + refreshInterval < Time.time)
            {
                lastUpdate = Time.time;
                ExposureLab lab = part.GetComponent<ExposureLab>();


                if (lab.experimentName != lastExpName)
                {
                    GameDatabase.TextureInfo newTexture = getTextureForState(lab.experimentName);
                    if (newTexture != null)
                    {
                        changeTexture(newTexture);
                        
                    }
                    else
                    {
                        NE_Helper.logError("New Texture null");
                    }
                    lastExpName = lab.experimentName;
                }

            }
        }

        private void changeTexture(GameDatabase.TextureInfo newTexture)
        {
            Material mat = getScreenMaterial();
            if (mat != null)
            {
                NE_Helper.log("Old Texture: " + mat.mainTexture.name);
                NE_Helper.log("Set new Texture: " + newTexture.name);
                mat.mainTexture = newTexture.texture;
            }
            else
            {
                NE_Helper.logError("Transform NOT found: " + "MEP IVA Screen");
            }
        }

        private Material getScreenMaterial()
        {
            if (screenMat == null)
            {
                Transform t = internalProp.FindModelTransform("MEP IVA Screen");
                if (t != null)
                {
                    NE_Helper.log("Transform found: " + "MEP IVA Screen");
                    screenMat = t.renderer.material;
                    return screenMat;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return screenMat;
            }
        }

        private GameDatabase.TextureInfo getTextureForState(string name)
        {
            NE_Helper.log("Experiment Name: " + name);
            switch (name)
            {
                case "No Experiment":
                     if (noExp == null) noExp = getTexture(folder, noExpTexture);
                    return noExp;
                case "Material Exposure Experiment 1":
                    if (mee1 == null) mee1 = getTexture(folder, mee1Texture);
                    return mee1;
                case "Material Exposure Experiment 2":
                    if (mee2 == null) mee2 = getTexture(folder, mee2Texture);
                    return mee2;
                default:
                    return null;
            }
        }

        private GameDatabase.TextureInfo getTexture(string folder, string textureName)
        {
            return GameDatabase.Instance.GetTextureInfoIn(folder, textureName);
        }
    }
}