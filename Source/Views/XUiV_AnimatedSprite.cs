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
        protected new bool enabled = true;  // 动画启用开关，默认启用

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

        /// <summary>
        /// 动画启用开关
        /// true: 播放动画
        /// false: 暂停动画
        /// </summary>
        public new bool Enabled
        {
            get { return enabled; }
            set
            {
                if(enabled != value)
                {
                    enabled = value;
                    isDirty = true;
                    UpdateAnimationState();
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
                resetAnimation = false;
            }

            // 根据启用状态控制动画
            UpdateAnimationState();

            catuiInitialized = true;
        }

        /// <summary>
        /// 更新动画状态（启用/暂停）
        /// </summary>
        private void UpdateAnimationState()
        {
            if (animation == null) return;

            if (enabled)
            {
                if (!animation.isPlaying)
                {
                    animation.Play();
                }
            }
            else
            {
                if (animation.isPlaying)
                {
                    animation.Pause();
                }
            }
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
                    case "enabled":
                        Enabled = StringParsers.ParseBool(value);
                        return true;
                    default:
                        return false;
                }
            }
            return false;
        }

        /// <summary>
        /// 启用动画
        /// </summary>
        public void PlayAnimation()
        {
            enabled = true;
            if (animation != null)
            {
                animation.Play();
            }
        }

        /// <summary>
        /// 暂停动画
        /// </summary>
        public void PauseAnimation()
        {
            enabled = false;
            if (animation != null && animation.isPlaying)
            {
                animation.Pause();
            }
        }

        /// <summary>
        /// 重置动画到开始帧
        /// </summary>
        public void ResetAnimation()
        {
            if (animation != null)
            {
                animation.ResetToBeginning();
                if (enabled)
                {
                    animation.Play();
                }
            }
        }

        /// <summary>
        /// 供外部调用设置动画状态
        /// </summary>
        /// <param name="isEnabled">是否启用</param>
        public void SetAnimationEnabled(bool isEnabled)
        {
            Enabled = isEnabled;
        }
    }
}
