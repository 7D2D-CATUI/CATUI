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
        private bool catuiInitialized;

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

        public XUiV_AnimatedSprite(XUi xui, string id) : base(xui, id)
        {
        }

        public override void createComponents(GameObject go)
        {
            base.createComponents(go);
            go.AddComponent<UISpriteAnimation>();
        }

        public override void updateData()
        {
            if(animation == null && !catuiInitialized)
            {
                animation = uiTransform.GetComponent<UISpriteAnimation>();
                Traverse.Create(animation).Field("mSnap").SetValue(false);
            }

            if (!string.IsNullOrEmpty(sprite.spriteName))
            {
                spriteName = sprite.spriteName;
            }

            base.updateData();

            animation.namePrefix = prefix;
            animation.framesPerSecond = frameRate;
            animation.loop = loop;

            if (resetAnimation)
            {
                animation.ResetToBeginning();
                animation.Play();
                resetAnimation = false;
            }

            catuiInitialized = true;
        }

        public bool ParseCatuiAttribute(string attribute, string value)
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
                        return false;
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
