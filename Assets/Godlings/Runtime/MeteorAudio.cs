using System;
using UnityEngine;

namespace Godlings
{
    /// <summary>
    /// Temporary synthesized meteor sound: a falling whistle/noisy rush, then
    /// a short bass impact. No downloaded recordings or artwork are used.
    /// </summary>
    public sealed class MeteorAudio : MonoBehaviour
    {
        private AudioSource source;
        private readonly AudioClip[] falls = new AudioClip[3], impacts = new AudioClip[3];
        private bool paused;
        private const int SampleRate = 22050;

        public void Initialize(float warningSeconds)
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0;
            source.volume = 0.35f;
            for (int size=1;size<=3;size++)
            {
                float pitch = Mathf.Lerp(1.65f,1,(size-1)/2f);
                falls[size-1] = MakeClip($"Meteor {size} falling",warningSeconds,false,pitch);
                impacts[size-1] = MakeClip($"Meteor {size} impact",Mathf.Lerp(0.25f,0.65f,(size-1)/2f),true,pitch);
            }
        }

        private static AudioClip MakeClip(string label, float seconds, bool isImpact, float pitch)
        {
            var samples = new float[Mathf.CeilToInt(seconds * SampleRate)];
            var noise = new System.Random(4107);
            float phase = 0, filteredNoise = 0;
            for (int i=0;i<samples.Length;i++)
            {
                float time = i/(float)SampleRate, progress = i/(float)samples.Length;
                float random = (float)noise.NextDouble()*2-1;
                filteredNoise = Mathf.Lerp(filteredNoise,random,isImpact ? 0.12f : 0.3f);
                float frequency = isImpact ? Mathf.Lerp(95,30,progress) : Mathf.Lerp(1000,170,progress);
                phase += 2*Mathf.PI*frequency*pitch/SampleRate;
                float envelope = isImpact ? Mathf.Exp(-7*progress) * Mathf.Min(1,time/0.005f)
                    : Mathf.Sin(Mathf.PI*progress) * Mathf.Lerp(0.25f,1,progress);
                samples[i] = Mathf.Clamp((Mathf.Sin(phase)*0.4f+filteredNoise*0.9f)*envelope,-1,1);
            }
            var clip = AudioClip.Create(label,samples.Length,1,SampleRate,false);
            clip.SetData(samples,0);
            return clip;
        }

        public void PlayFall(int size) => source.PlayOneShot(falls[Mathf.Clamp(size,1,3)-1],size/3f);
        public void PlayImpact(int size) => source.PlayOneShot(impacts[Mathf.Clamp(size,1,3)-1],size/3f);
        public void Stop() => source.Stop();
        public void SetPaused(bool value)
        {
            if (paused == value) return;
            paused = value;
            if (paused) source.Pause(); else source.UnPause();
        }
        private void OnDestroy()
        {
            foreach (var clip in falls) if (clip != null) Destroy(clip);
            foreach (var clip in impacts) if (clip != null) Destroy(clip);
        }
    }
}
