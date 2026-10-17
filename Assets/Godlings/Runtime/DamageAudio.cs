using UnityEngine;

namespace Godlings
{
    /// <summary>Temporary synthetic fox yelp and burps, replaceable by supplied recordings.</summary>
    public sealed class DamageAudio : MonoBehaviour
    {
        private AudioSource source, bossSource;
        private AudioClip fiHurt, burp, bossBurp;
        private bool ownsFiClip, paused;
        private const int Rate = 22050;

        public void Initialize(AudioClip recordedFoxYelp)
        {
            fiHurt = recordedFoxYelp;
            ownsFiClip = fiHurt == null;
            if (ownsFiClip) fiHurt = MakeFoxYelp();
            source = NewSource(0.55f);
            bossSource = NewSource(0.95f);
            burp = MakeBurp("Enemy burp",0.38f,125,55);
            bossBurp = MakeBurp("Deep long boss burp",1.4f,72,28);
            bossSource.clip = bossBurp;
        }

        private AudioSource NewSource(float volume)
        {
            var result = gameObject.AddComponent<AudioSource>();
            result.playOnAwake = false;
            result.spatialBlend = 0;
            result.volume = volume;
            return result;
        }

        private static AudioClip MakeBurp(string label, float seconds, float startPitch, float endPitch)
        {
            var samples = new float[Mathf.CeilToInt(Rate*seconds)];
            var noise = new System.Random(113);
            float phase = 0, filtered = 0;
            for (int i=0;i<samples.Length;i++)
            {
                float time = i/(float)Rate, progress = i/(float)samples.Length;
                float frequency = Mathf.Lerp(startPitch,endPitch,progress)*(1+0.08f*Mathf.Sin(time*95));
                phase += 2*Mathf.PI*frequency/Rate;
                filtered = Mathf.Lerp(filtered,(float)noise.NextDouble()*2-1,0.18f);
                float tone = Mathf.Sin(phase)+0.3f*Mathf.Sin(phase*2)+0.15f*Mathf.Sin(phase*3);
                float envelope = Mathf.Sin(Mathf.PI*progress)*(0.75f+0.25f*Mathf.Sin(time*70));
                samples[i] = Mathf.Clamp((tone*0.55f+filtered*0.3f)*envelope,-1,1);
            }
            return Clip(label,samples);
        }

        private static AudioClip MakeFoxYelp()
        {
            var samples = new float[Mathf.CeilToInt(Rate*0.26f)];
            var noise = new System.Random(27);
            float phase = 0, filtered = 0;
            for (int i=0;i<samples.Length;i++)
            {
                float progress = i/(float)samples.Length;
                float frequency = progress < 0.2f ? Mathf.Lerp(650,1100,progress/0.2f)
                    : Mathf.Lerp(1100,420,(progress-0.2f)/0.8f);
                phase += 2*Mathf.PI*frequency/Rate;
                filtered = Mathf.Lerp(filtered,(float)noise.NextDouble()*2-1,0.25f);
                float envelope = Mathf.Pow(Mathf.Sin(Mathf.PI*progress),0.7f);
                samples[i] = (Mathf.Sin(phase)*0.52f+Mathf.Sin(phase*2)*0.14f+filtered*0.18f)*envelope;
            }
            return Clip("Synthetic young fox yelp",samples);
        }

        private static AudioClip Clip(string label, float[] samples)
        {
            var clip = AudioClip.Create(label,samples.Length,1,Rate,false);
            clip.SetData(samples,0);
            return clip;
        }

        public void PlayFiHurt() => source.PlayOneShot(fiHurt);
        public void PlayBurp() => source.PlayOneShot(burp);
        // Restart one dedicated voice on each hit: rapid sword hits cannot
        // stack several loud boss burps on top of one another.
        public void PlayBossBurp() => bossSource.Play();
        public void SetPaused(bool value)
        {
            if (paused == value) return;
            paused = value;
            if (paused) { source.Pause(); bossSource.Pause(); }
            else { source.UnPause(); bossSource.UnPause(); }
        }
        private void OnDestroy()
        {
            if (burp != null) Destroy(burp);
            if (bossBurp != null) Destroy(bossBurp);
            if (ownsFiClip && fiHurt != null) Destroy(fiHurt);
        }
    }
}
