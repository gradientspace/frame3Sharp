using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace f3
{

    public class HUDToggleGroup
    {
        List<HUDToggleButton> vButtons;
        int nEnabled;
        bool in_update;

        public HUDToggleGroup()
        {
            vButtons = new List<HUDToggleButton>();
            nEnabled = -1;
            in_update = false;
        }


        public List<HUDToggleButton> Buttons { get { return vButtons; } }

        public void AddButton(HUDToggleButton button)
        {
            if (vButtons.Count == 0) {
                button.Checked = true;
                nEnabled = 0;
            } else
                button.Checked = false;

            vButtons.Add(button);
            button.AddToGroup(this);
            button.OnToggled += OnButtonToggled;
        }

        public int Count {
            get { return vButtons.Count; }
        }

        public int Selected
        {
            get { return nEnabled; }
            set { vButtons[value].Checked = true; }
        }

        public void SelectModulo(int nIndex)
        {
            if (nIndex >= vButtons.Count)
                nIndex = nIndex % vButtons.Count;
            else if (nIndex < 0)
                nIndex = vButtons.Count + nIndex;
            Selected = nIndex;
        }



        public delegate void HUDToggleGroupEventHandler(object sender, int nIndex);
        public event HUDToggleGroupEventHandler OnToggled;

        protected virtual void SendOnToggled()
        {
            var tmp = OnToggled;
            if (tmp != null)
                tmp(this, nEnabled);
        }


        void OnButtonToggled(object sender, bool bEnabled)
        {
            if (in_update)
                return;         // ignore additional child button events during state update below
            in_update = true;

            HUDToggleButton modified = sender as HUDToggleButton;
            int nIndex = vButtons.FindIndex(x => x == modified);
            Debug.Assert(nIndex >= 0);

            Debug.Assert(bEnabled);   // we should only be getting enabled events here, 
                                      // otherwise we are going to disable all buttons. bug upstream.

            // disable currently-enabled button
            vButtons[nEnabled].Checked = false;

            nEnabled = nIndex;

            in_update = false;

            SendOnToggled();
        }

    }
}
