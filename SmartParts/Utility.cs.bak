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


    public static class Utility
    {
        public enum LightColor
        {
            RED,
            GREEN,
            BLUE,
            WHITE
        }
       
        public static void switchEmissive(SmartSensorModuleBase baseM, Light light, bool state, LightColor color = LightColor.WHITE)
        {
            light.intensity = (state ? 1 : 0);
            light.enabled = state;
            light.enabled = true;
            light.range = 0.25f;
            switch (color)
            {
                case LightColor.RED:
                    light.color = new Color(1, 0, 0);
                    break;
                case LightColor.GREEN:
                    light.color = new Color(0, 1, 0);
                    break;
                case LightColor.BLUE:
                    light.color = new Color(0, 0, 1);
                    break;
                case LightColor.WHITE:
                    light.color = new Color(1, 1, 1);
                    break;
            }
        }

        private static void traverseChildren(Part p, int nextStage, ref List<Part> resultList) {
            if (p.inverseStage >= nextStage) {
                resultList.Add(p);
            }
            foreach (Part child in p.children) {

                traverseChildren(child, nextStage, ref resultList);
            }
        }


        public static void playAnimation(Part p, string animationName, bool forward, bool play, float speed) {
            Animation anim;
            anim = p.FindModelAnimators(animationName).FirstOrDefault();
            if (anim != null) {


                if (forward) {
                    anim[animationName].speed = 1f * speed;
                    //PartModule.Log.Info ("NTime forward: " + anim [animationName].normalizedTime);
                    if (!play || !anim.isPlaying)
                        anim[animationName].normalizedTime = (play ? 0f : 1f);
                    anim.Blend(animationName, 2f);
                }
                else {
                    anim[animationName].speed = -1f * speed;
                    //PartModule.Log.Info ("NTime backward: " + anim [animationName].normalizedTime);
                    if (!play || !anim.isPlaying)
                        anim[animationName].normalizedTime = (play ? 1f : 0f);
                    anim.Blend(animationName, 2f);
                }
            }
        }

        public static void playAnimationSetToPosition(Part p, string animationName, float position) {
            Animation anim;
            anim = p.FindModelAnimators(animationName).FirstOrDefault();
            if (anim != null) {
                anim[animationName].normalizedTime = position;
                anim[animationName].speed = 0f;
                anim.Play(animationName);
                // anim [animationName].speed = 0;
            }
        }


        public static void playAudio(Part p, string clipName) {
            if (clipName != "") {
                AudioSource sound;
                sound = p.gameObject.AddComponent<AudioSource>();
                sound.clip = GameDatabase.Instance.GetAudioClip(clipName);
                if (sound.clip != null) {
                    sound.volume = 1;
                    sound.Stop();
                    sound.Play();
                }
            }
        }
    }
}










