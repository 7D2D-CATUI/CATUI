namespace Views
{
    public class XUiC_Scrollbar_Sprite : XUiV_Sprite
    {
        private const string TAG = "ScrollBar Sprite";

        public XUiC_Scrollbar_Sprite(string _id) : base(_id)
        {
        }

        public override void UpdateData()
        {
            color.a = sprite.alpha;
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
    }
}