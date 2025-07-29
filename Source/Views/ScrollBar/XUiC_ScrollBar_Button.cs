using Audio;
using UnityEngine;

namespace Views
{
    public class XUiC_ScrollBar_Button : XUiV_Button
    {
        private const string TAG = "ScrollBar Button";

        private new AudioClip xuiSound;

        public XUiC_ScrollBar_Button(string _id) : base(_id)
        {
        }

        public override void InitView()
        {
            base.InitView();

            UIEventListener uIEventListener = UIEventListener.Get(uiTransform.gameObject);
            uIEventListener.onPress += OnPress;

            EventOnPress = xuiSound == null;
        }

        public override void UpdateData()
        {
            currentColor.a = sprite.alpha;
            base.UpdateData();
            sprite.depth = depth;
        }

        public override void RefreshBoxCollider()
        {
            if (sprite != null && !sprite.autoResizeBoxCollider)
            {
                base.RefreshBoxCollider();
            }
        }

        public override bool ParseAttribute(string attribute, string value, XUiController parent)
        {
            if (attribute != null)
            {
                switch (attribute)
                {
                    case "sound_play_on_press_down":
                        xui.LoadData(value, (AudioClip audioClip) =>
                        {
                            xuiSound = audioClip;
                        });
                        return true;
                    default:
                        return base.ParseAttribute(attribute, value, parent); ;
                }
            }
            return false;
        }

        private new void OnPress(GameObject go, bool pressed)
        {
            if (enabled && pressed)
            {
                if (xuiSound != null && xuiSound != null && UICamera.currentTouchID == -1)
                {
                    Manager.PlayXUiSound(xuiSound, soundVolume);
                }

                controller.Pressed(UICamera.currentTouchID);
            }
        }
    }
}