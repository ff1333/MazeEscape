using UnityEngine;

namespace FogboundMaze
{
    public static class ProceduralAudio
    {
        private static AudioClip sniperRifle;
        private static AudioClip machete;
        private static AudioClip impact;
        private static AudioClip reload;
        private static AudioClip reloadComplete;
        private static AudioClip dryFire;
        private static AudioClip boltAction;
        public static AudioClip Impact => impact ??= Create("Hit Confirm", .075f, 240f, 900f, .65f, 103);
        public static AudioClip Reload => reload ??= Create("Magazine", .13f, 110f, 1800f, .8f, 221);
        public static AudioClip ReloadComplete => reloadComplete ??= CreateReloadComplete();
        public static AudioClip DryFire => dryFire ??= Create("Dry Fire", .055f, 420f, 680f, .18f, 417);
        public static AudioClip BoltAction => boltAction ??= Create("Bolt Action", .16f, 180f, 1350f, .78f, 519);

        private static AudioClip CreateReloadComplete()
        {
            const int rate = 22050;
            var samples = new float[Mathf.CeilToInt(rate * .28f)];
            var random = new System.Random(712);
            for (var i = 0; i < samples.Length; i++)
            {
                var time = i / (float)rate;
                var first = time < .065f ? Mathf.Exp(-65f * time) : 0f;
                var secondTime = time - .10f;
                var second = secondTime >= 0f ? Mathf.Exp(-32f * secondTime) : 0f;
                var noise = (float)random.NextDouble() * 2f - 1f;
                samples[i] = .55f * first * noise
                    + .5f * second * (Mathf.Sin(secondTime * 1500f * Mathf.PI * 2f) * .7f + noise * .3f);
            }
            var clip = AudioClip.Create("Reload Complete - Bolt Ready", samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public static AudioClip SniperRifle => sniperRifle ??= Create("Sniper Rifle", 0.24f, 70f, 1250f, 0.68f, 37);
        public static AudioClip Machete => machete ??= Create("Machete", 0.18f, 90f, 230f, 0.24f, 91);

        private static AudioClip Create(string name, float duration, float low, float high, float noiseMix, int seed)
        {
            const int sampleRate = 22050;
            var count = Mathf.CeilToInt(duration * sampleRate);
            var samples = new float[count];
            var random = new System.Random(seed);
            for (var i = 0; i < count; i++)
            {
                var time = i / (float)sampleRate;
                var progress = i / (float)count;
                var frequency = Mathf.Lerp(high, low, progress);
                var tone = Mathf.Sin(time * frequency * Mathf.PI * 2f);
                var noise = (float)random.NextDouble() * 2f - 1f;
                samples[i] = (tone * (1f - noiseMix) + noise * noiseMix) * (1f - progress) * 0.55f;
            }
            var clip = AudioClip.Create(name, count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
