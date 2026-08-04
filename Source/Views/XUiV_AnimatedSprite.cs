using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

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

        // 静态缓存：atlas 名称 + prefix -> 排序后的帧名列表，避免每个实例重复扫描整个 atlas 并排序
        private static readonly Dictionary<string, string[]> spriteFrameCache = new Dictionary<string, string[]>();

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
            // UISpriteAnimation 改为惰性创建，仅当动画真正启用时才挂载，
            // 避免为每个栏位（含空栏位）都创建组件导致每帧 Update 开销
        }

        // 惰性创建动画组件，仅返回已存在的组件（未启用时保持 null）
        private UISpriteAnimation EnsureAnimation()
        {
            if (animation == null && !catuiInitialized)
            {
                if (uiTransform == null)
                {
                    return null;
                }
                animation = uiTransform.GetComponent<UISpriteAnimation>();
                if (animation == null)
                {
                    animation = uiTransform.gameObject.AddComponent<UISpriteAnimation>();
                }
                Traverse.Create(animation).Field("mSnap").SetValue(false);
                catuiInitialized = true;
            }
            return animation;
        }

        public override void updateData()
        {
            if (!string.IsNullOrEmpty(sprite.spriteName))
            {
                spriteName = sprite.spriteName;
            }

            base.updateData();

            // 未启用动画时不做任何组件配置，避免空栏位的开销
            if (!enabled)
            {
                return;
            }

            UISpriteAnimation anim = EnsureAnimation();
            if (anim == null)
            {
                return;
            }

            anim.namePrefix = prefix;
            anim.framesPerSecond = frameRate;
            anim.loop = loop;

            if (resetAnimation)
            {
                anim.ResetToBeginning();
                resetAnimation = false;
            }

            // 根据启用状态控制动画
            UpdateAnimationState();
        }

        /// <summary>
        /// 更新动画状态（启用/暂停）
        /// </summary>
        private void UpdateAnimationState()
        {
            UISpriteAnimation anim = animation;
            if (anim == null)
            {
                return;
            }

            if (enabled)
            {
                if (!anim.isPlaying)
                {
                    anim.Play();
                }
            }
            else
            {
                if (anim.isPlaying)
                {
                    anim.Pause();
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
            UISpriteAnimation anim = EnsureAnimation();
            if (anim != null)
            {
                anim.Play();
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
            UISpriteAnimation anim = EnsureAnimation();
            if (anim != null)
            {
                anim.ResetToBeginning();
                if (enabled)
                {
                    anim.Play();
                }
            }
        }

        /// <summary>
        /// 供外部调用设置动画状态
        /// </summary>
        public void SetAnimationEnabled(bool isEnabled)
        {
            Enabled = isEnabled;
        }
    }

    // 静态缓存 atlas 帧名列表，避免每个动画实例重复扫描 atlas.spriteList 并排序
    [HarmonyPatch(typeof(UISpriteAnimation))]
    public class UISpriteAnimationPatch
    {
        private static readonly Dictionary<string, string[]> spriteNameCache = new Dictionary<string, string[]>();

        [HarmonyPrefix]
        [HarmonyPatch("RebuildSpriteList")]
        public static bool RebuildSpriteListPrefix(UISpriteAnimation __instance)
        {
            Traverse t = Traverse.Create(__instance);
            UISprite mSprite = t.Field("mSprite").GetValue<UISprite>();
            if (mSprite == null)
            {
                return true;
            }
            INGUIAtlas atlas = mSprite.atlas;
            if (atlas == null)
            {
                return true;
            }
            string prefix = t.Field("mPrefix").GetValue<string>();
            string key = atlas.Name + "|" + prefix;

            string[] cached;
            if (!spriteNameCache.TryGetValue(key, out cached))
            {
                List<string> names = new List<string>();
                List<UISpriteData> spriteList = atlas.spriteList;
                for (int i = 0; i < spriteList.Count; i++)
                {
                    UISpriteData data = spriteList[i];
                    if (string.IsNullOrEmpty(prefix) || data.name.StartsWith(prefix))
                    {
                        names.Add(data.name);
                    }
                }
                names.Sort();
                cached = names.ToArray();
                spriteNameCache[key] = cached;
            }

            List<string> mSpriteNames = t.Field("mSpriteNames").GetValue<List<string>>();
            if (mSpriteNames != null)
            {
                mSpriteNames.Clear();
                mSpriteNames.AddRange(cached);
            }
            return false;
        }
    }
}
