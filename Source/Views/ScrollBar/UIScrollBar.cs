using UnityEngine;

namespace Views
{
    public class UIScrollBar : global::UIScrollBar
    {
        private const string TAG = "XUi_UIScrollBar";

        public void setBackgroundWidget(UIWidget background)
        {
            if (backgroundWidget != background)
            {
                backgroundWidget = background;

                if (!background.GetComponent<Collider>()) return;

                UIEventListener bgl = UIEventListener.Get(background.gameObject);
                bgl.onPress += OnPressBackground;
                bgl.onDrag += OnDragBackground;
                background.autoResizeBoxCollider = true;
            }
        }

        public void setForegroundWidget(UIWidget foreground)
        {
            if (foregroundWidget != foreground)
            {
                foregroundWidget = foreground;

                if (!foreground.GetComponent<Collider>()) return;

                UIEventListener fgl = UIEventListener.Get(foreground.gameObject);
                fgl.onPress += OnPressForeground;
                fgl.onDrag += OnDragForeground;
                foreground.autoResizeBoxCollider = true;
            }
        }

        protected new void OnPressBackground(GameObject go, bool isPressed)
        {
            if (UICamera.currentScheme != UICamera.ControlScheme.Controller)
            {
                mCam = UICamera.currentCamera;
                value = ScreenToValue(UICamera.lastEventPosition);
                if (!isPressed && onDragFinished != null)
                {
                    onDragFinished();
                }
            }
        }

        protected new void OnDragBackground(GameObject go, Vector2 delta)
        {
            if (UICamera.currentScheme != UICamera.ControlScheme.Controller)
            {
                mCam = UICamera.currentCamera;
                value = ScreenToValue(UICamera.lastEventPosition);
            }
        }

        protected new void OnPressForeground(GameObject go, bool isPressed)
        {
            if (UICamera.currentScheme != UICamera.ControlScheme.Controller)
            {
                mCam = UICamera.currentCamera;
                if (isPressed)
                {
                    mOffset = mFG == null ? 0f : value - ScreenToValue(UICamera.lastEventPosition);
                }
                else if (onDragFinished != null)
                {
                    onDragFinished();
                }
            }
        }

        protected new void OnDragForeground(GameObject go, Vector2 delta)
        {
            if (UICamera.currentScheme != UICamera.ControlScheme.Controller)
            {
                mCam = UICamera.currentCamera;
                value = mOffset + ScreenToValue(UICamera.lastEventPosition);
            }
        }
    }
}