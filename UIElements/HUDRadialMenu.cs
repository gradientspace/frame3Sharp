using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace f3
{
    public class HUDRadialMenu : HUDStandardItem
    {

        public class MenuItem
        {
            public string Label { get; set; }
            public GameObject GO { get; set; }

            public List<MenuItem> SubItems { get; set; }
            public MenuItem ParentItem { get; set; }

            public event EventHandler OnSelected;
            public void selected(object sender) {
                FUtil.SafeSendEvent(OnSelected, sender, new EventArgs());
            }

            public bool hasParent(MenuItem i) {
                MenuItem parent = ParentItem;
                while ( parent != null ) {
                    if (parent == i)
                        return true;
                    parent = parent.ParentItem;
                }
                return false;
            }
        };



        protected List<MenuItem> TopLevelItems { get; set; }
        protected List<MenuItem> AllItems { get; set; }


        GameObject menuContainer;
        GameObject centerPoint;
        Material itemMaterial;
        Material highlightMaterial;


        public float AngleSpan { get; set; }
        public float AngleShift { get; set; }
        public float WedgePadding { get; set; }
        public float Radius { get; set; }
        public float DeadZoneRadiusFactor { get; set; }     // [0..1] multiplied by Radius
        public Color ItemColor { get; set; }
        public Color HighlightColor { get; set; }
        public float TextScale { get; set; }
        public float TextCenterPointFactor { get; set; }    // [0..1] from disc center to edge (default=0.6)

        public float SubItemRadialPadding { get; set; }
        public float SubItemRadialWidth { get; set; }
        public float SubItemTextScale { get; set; }

        public HUDRadialMenu()
        {
            TopLevelItems = new List<MenuItem>();
            AllItems = new List<MenuItem>();
            AngleSpan = 360.0f;
            AngleShift = 0.0f;
            WedgePadding = 5.0f;
            Radius = 0.5f;
            DeadZoneRadiusFactor = 0.2f;
            ItemColor = ColorUtil.replaceAlpha(ColorUtil.ForestGreen, 0.8f);
            HighlightColor = ColorUtil.replaceAlpha(ColorUtil.SelectionGold, 0.9f);
            TextScale = 0.01f;
            TextCenterPointFactor = 0.6f;

            SubItemRadialWidth = 0.3f;
            SubItemRadialPadding = 0.025f;
            SubItemTextScale = 0.05f;
        }


        public MenuItem AppendMenuItem(string label, EventHandler selectedHandler )
        {
            if (menuContainer != null)
                throw new Exception("HUDRadialMenu cannot call AppendMenuItem after Create");

            MenuItem i = new MenuItem() { Label = label };
            i.OnSelected += selectedHandler;
            TopLevelItems.Add(i);
            AllItems.Add(i);
            return i;
        }

        public MenuItem AppendSubMenuItem(MenuItem parent, string label, EventHandler selectedHandler)
        {
            if (menuContainer != null)
                throw new Exception("HUDRadialMenu cannot call AppendSubMenuItem after Create");
            if (AllItems.Contains(parent) == false)
                throw new Exception("HUDRadialMenu does not contain this MenuItem with label " + parent.Label);

            MenuItem i = new MenuItem() { Label = label };
            i.OnSelected += selectedHandler;
            if (parent.SubItems == null)
                parent.SubItems = new List<MenuItem>();
            parent.SubItems.Add(i);
            i.ParentItem = parent;
            AllItems.Add(i);
            return i;
        }


        public void Create()
        {
            menuContainer = new GameObject(UniqueNames.GetNext("HUDRadialMenu"));

            itemMaterial = MaterialUtil.CreateFlatMaterial(ItemColor);
            highlightMaterial = MaterialUtil.CreateFlatMaterial(HighlightColor);


            float fInnerRadius = Radius * DeadZoneRadiusFactor;

            centerPoint = AppendUnityPrimitiveGO("center_point", PrimitiveType.Sphere, highlightMaterial, menuContainer, false);
            centerPoint.transform.localScale = fInnerRadius * 0.25f * Vector3.one;

            int nItems = TopLevelItems.Count;
            float fWedgeSpan = (AngleSpan / (float)nItems) - WedgePadding;
            int nSlicesPerWedge = 64 / nItems;

            float fCurAngle = AngleShift;
            for ( int i = 0; i < nItems; ++i ) {
                fMesh m = MeshGenerators.CreatePuncturedDisc(fInnerRadius, Radius, nSlicesPerWedge, 
                    fCurAngle, fCurAngle+fWedgeSpan);

                float fMidAngleRad = (fCurAngle + fWedgeSpan * 0.5f) * Mathf.Deg2Rad;
                Vector2 vMid = new Vector2(Mathf.Cos(fMidAngleRad), Mathf.Sin(fMidAngleRad));

                TopLevelItems[i].GO = AppendMeshGO(TopLevelItems[i].Label, m, itemMaterial, menuContainer);
                TopLevelItems[i].GO.transform.Rotate(Vector3.right, -90.0f); // ??

                // [TODO] this is to improve font centering. Right now we do absolute centering
                //   but visually this looks wrong because weight of font is below center-y.
                //   Right thing would be to align at top of lowercase letters, rather than center-y
                //   (to fix in future)
                float fudge = 0.0f;
                if (nItems == 2 && i == 1)
                    fudge = -0.1f;

                TextLabelGenerator textGen = new TextLabelGenerator() {
                    Text = TopLevelItems[i].Label, Scale = TextScale,
                    Translate = vMid * (TextCenterPointFactor + fudge) * Radius,
                    Align = TextLabelGenerator.Alignment.HVCenter
                };
                AddVisualElements(textGen.Generate(), true);

                // debug: add red dots at center points to see how well text is aligned...
                //GameObject tmp = AppendUnityPrimitiveGO("center", PrimitiveType.Sphere, MaterialUtil.CreateStandardMaterial(Color.red), RootGameObject);
                //tmp.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
                //tmp.transform.position = textGen.Translate;

                if (TopLevelItems[i].SubItems != null)
                    create_sub_items(TopLevelItems[i], Radius, fCurAngle, fCurAngle + fWedgeSpan);

                fCurAngle += WedgePadding + fWedgeSpan;
            }
        }



        void create_sub_items(MenuItem topItem, float fParentRadius, float fParentAngleMin, float fParentAngleMax )
        {
            float fInnerRadius = fParentRadius + SubItemRadialPadding;
            float fOuterRadius = fInnerRadius + SubItemRadialWidth;

            int nItems = topItem.SubItems.Count;
            //float fParentAngleSpan = fParentAngleMax - fParentAngleMin;
            float fParentAngleMid = 0.5f * (fParentAngleMax + fParentAngleMin);
            //float fSubMenuSpan = 180.0f - (WedgePadding * (float)(nItems-1));
            //float fWedgeSpan = fSubMenuSpan / (float)nItems;
            float fWedgeSpan = 45.0f;
            float fSubMenuSpan = nItems*fWedgeSpan + (float)(nItems-1)*WedgePadding;
            int nSlicesPerWedge = 16;

            float fCurAngle = fParentAngleMid - fSubMenuSpan * 0.5f;
            for ( int i = 0; i < nItems; ++i ) {
                MenuItem item = topItem.SubItems[i];
                fMesh m = MeshGenerators.CreatePuncturedDisc(fInnerRadius, fOuterRadius, nSlicesPerWedge,
                     fCurAngle, fCurAngle + fWedgeSpan);

                float fMidAngleRad = (fCurAngle + fWedgeSpan * 0.5f) * Mathf.Deg2Rad;
                Vector2 vMid = new Vector2(Mathf.Cos(fMidAngleRad), Mathf.Sin(fMidAngleRad));

                GameObject itemGO = AppendMeshGO(item.Label, m, itemMaterial, menuContainer);
                itemGO.transform.Rotate(Vector3.right, -90.0f); // ??
                topItem.SubItems[i].GO = itemGO;

                // [TODO] this is to improve font centering. Right now we do absolute centering
                //   but visually this looks wrong because weight of font is below center-y.
                //   Right thing would be to align at top of lowercase letters, rather than center-y
                //   (to fix in future)
                float fudge = 0.0f;
                if (nItems == 2 && i == 1)
                    fudge = -0.1f;

                TextLabelGenerator textGen = new TextLabelGenerator() {
                    Text = item.Label, Scale = SubItemTextScale,
                    Translate = vMid * (fInnerRadius + (TextCenterPointFactor + fudge) * SubItemRadialWidth),
                    Align = TextLabelGenerator.Alignment.HVCenter
                };
                List<GameObject> vTextElems = textGen.Generate();
                AddVisualElements(vTextElems, true);
                // actually want these GOs to be parented to itemGO
                UnityUtil.AddChildren(itemGO, vTextElems, true);

                itemGO.SetVisible(false);

                if (item.SubItems != null)
                    create_sub_items(item, fOuterRadius, fCurAngle, fCurAngle + fWedgeSpan);

                fCurAngle += WedgePadding + fWedgeSpan;
            }
        }


        MenuItem ActiveMenuItem = null;

        void show_sub_menu(MenuItem topItem)
        {
            if (topItem.SubItems == null)
                return;
            foreach (MenuItem i in topItem.SubItems)
                i.GO.SetVisible(true);
        }

        void hide_sub_menu(MenuItem topItem)
        {
            if (topItem.SubItems == null)
                return;
            foreach (MenuItem i in topItem.SubItems)
                i.GO.SetVisible(false);
        }

        void hide_child_menus(MenuItem topItem)
        {
            if (topItem.SubItems == null)
                return;
            foreach (MenuItem i in topItem.SubItems) {
                hide_sub_menu(i);
                hide_child_menus(i);
            }
        }

        void show_parent_menus(MenuItem childItem)
        {
            MenuItem parent = childItem.ParentItem;
            while ( parent != null ) { 
                show_sub_menu(parent);
                parent = parent.ParentItem;
            }
        }

        #region SceneUIElement implementation

        override public fGameObject RootGameObject
        {
            get { return menuContainer; }
        }


        override public bool WantsCapture(InputEvent e)
        {
            return (Enabled && HasGO(e.hit.hitGO));
        }

        override public bool BeginCapture(InputEvent e)
        {
            return true;
        }

        override public bool UpdateCapture(InputEvent e)
        {
            GameObject hitGO = FindHitGO(e.ray);

            // find item we want to higlight
            MenuItem highlightItem =  (hitGO == null) ? null : AllItems.Find((x) => x.GO == hitGO);

            // update visual higlight
            foreach (MenuItem item in AllItems) {
                if (item == highlightItem)
                    MaterialUtil.SetMaterial(item.GO, highlightMaterial);
                else
                    MaterialUtil.SetMaterial(item.GO, itemMaterial);
            }

            // TODO cannot automatically hide when we miss, or we never expand the submenu.
            if (highlightItem == null)
                return true;

            if (ActiveMenuItem == highlightItem)
                return true;

            if (ActiveMenuItem != null) {
                hide_child_menus(ActiveMenuItem);
                hide_sub_menu(ActiveMenuItem);
                ActiveMenuItem = null;
            }

            if ( highlightItem != null ) {
                show_parent_menus(highlightItem);
                show_sub_menu(highlightItem);
                ActiveMenuItem = highlightItem;
            }

            return true;
        }

        override public bool EndCapture(InputEvent e)
        {
            GameObject hitGO = FindHitGO(e.ray);
            if ( hitGO != null) {

                GameObjectRayHit hit;
                if (FindGORayIntersection(e.ray, out hit)) {
                    float fDist = (hit.hitPos - RootGameObject.GetPosition()).Length;
                    if (fDist < Radius * DeadZoneRadiusFactor)
                        return true;
                } else
                    return true;  // how??


                foreach (MenuItem item in AllItems) {
                    if (item.GO == hitGO) {
                        item.selected(this);
                        break;
                    }
                }
            }
            return true;
        }


        #endregion


    }
}
