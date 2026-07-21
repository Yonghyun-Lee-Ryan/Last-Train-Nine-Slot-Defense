using System;
using UnityEngine;

namespace LastTrain.Data
{
    /// <summary>UGUI Image용 프레임 애니메이션 클립.</summary>
    [Serializable]
    public struct SpriteAnimationClip
    {
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float framesPerSecond;
        [SerializeField] private bool loop;

        public SpriteAnimationClip(Sprite[] frames, float framesPerSecond = 8f, bool loop = true)
        {
            this.frames = frames;
            this.framesPerSecond = framesPerSecond;
            this.loop = loop;
        }

        public Sprite[] Frames => frames;
        public float FramesPerSecond => framesPerSecond > 0f ? framesPerSecond : 8f;
        public bool Loop => loop;
        public bool HasFrames => frames != null && frames.Length > 0;

        public Sprite FirstFrame => HasFrames ? frames[0] : null;
    }
}
