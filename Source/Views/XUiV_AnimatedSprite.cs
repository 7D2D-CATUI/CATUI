using HarmonyLib;
using UnityEngine;

namespace Views
{
    public class XUiV_AnimatedSprite : XUiV_Sprite
    {

        private const string TAG = "AnimatedSprite";

        protected UISpriteAnimation animation;

        protected string prefix;
        protected bool loop = true;
        protected int frameRate = 30;

        private bool resetAnimation = false;

        public string SpriteNamePrefix
        {
            get
            {
                return prefix;
            }

            set
            {
                if(prefix != value)
                {
                    prefix = value;
                    isDirty = true;
                    resetAnimation = true;
                }
            }
        }

        public bool Loop
        {
            get { return loop; }
            set
            {
                if(loop != value)
                {
                    loop = value;
                    isDirty = true;
                    resetAnimation = true;
                }
            }
        }

        public int FrameRate
        {
            get { return frameRate; }
            set
            {
                if(frameRate != value)
                {
                    frameRate = value;
                    isDirty = true;
                }
            }
        }

        public XUiV_AnimatedSprite(string id) : base(id)
        {
        }

        public override void CreateComponents(GameObject go)
        {
            base.CreateComponents(go);
            go.AddComponent<UISpriteAnimation>();
        }

        public override void UpdateData()
        {
            if(animation == null && !initialized)
            {
                animation = uiTransform.GetComponent<UISpriteAnimation>();
                Traverse.Create(animation).Field("mSnap").SetValue(false);
            }

            if (!string.IsNullOrEmpty(sprite.spriteName))
            {
                spriteName = sprite.spriteName;
            }

            base.UpdateData();

            animation.namePrefix = prefix;
            animation.framesPerSecond = frameRate;
            animation.loop = loop;

            if (resetAnimation)
            {
                animation.ResetToBeginning();
                animation.Play();
                resetAnimation = false;
            }
        }

        public override bool ParseAttribute(string attribute, string value, XUiController parent)
        {
            if (attribute != null)
            {
                switch (attribute)
                {
                    case "spriteprefix":
                        SpriteNamePrefix = value;
                        return true;
                    case "loop":
                        Loop = StringParsers.ParseBool(value);
                        return true;
                    case "framerate":
                        FrameRate = int.Parse(value);
                        return true;
                    default:
                        return base.ParseAttribute(attribute, value, parent);
                }
            }
            return false;
        }

        public void PlayAnimation()
        {
            animation.Play();
        }

        public void PauseAnimation()
        {
            animation.Pause();
        }

        public void ResetAnimation()
        {
            animation.ResetToBeginning();
        }
    }
}
