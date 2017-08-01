using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{
    /// <summary>
    /// Basic popup dialog box, with arbitrary set of buttons
    /// Note that most fields must be configured before calling Create()
    /// </summary>
    public class HUDPopupDialog : HUDPanel
    {
        HUDShapeElement background;
        HUDLabel header_label;
        HUDMultiLineLabel message_area;
        List<HUDLabel> buttons;

        // Styling

        public Func<float, float, HUDShape> BackgroundShapeF = HUDUIDefaults.MakeDialogBackgroundShapeF;
        public Colorf BackgroundColor { get; set; }


        public virtual HUDLabelStyle HeaderStyle {
            get { return header_style; }
            set { header_style = value; update_header_style(); }
        }
        HUDLabelStyle header_style = new HUDLabelStyle();


        public virtual HUDLabelStyle MessageStyle
        {
            get { return message_style; }
            set { message_style = value; update_message_style(); }
        }
        HUDLabelStyle message_style = new HUDLabelStyle();


        public virtual HUDButtonStyle ButtonStyle {
            get { return button_style; }
            set { button_style = value; update_button_style(); }
        }
        HUDButtonStyle button_style = new HUDButtonStyle();


        // Text strings

        string titleText;
        public string TitleText
        {
            get { return titleText; }
            set { titleText = value; UpdateText(); }
        }

        string messageText;
        public string MessageText
        {
            get { return messageText; }
            set { messageText = value; UpdateText(); }
        }


        public HUDPopupDialog()
        {
            Width = 10;
            Height = 5;

            BackgroundColor = Colorf.Silver;
            header_style.TextHeight = 4 * message_style.TextHeight;
            header_style.TextColor = Colorf.DarkSlateGrey;

            titleText = "Question?";
            messageText = "This is a popup message!";

            buttons = new List<HUDLabel>();
        }




        public virtual void AppendButton(string label, Action clickedF)
        {
            HUDLabel button = new HUDLabel(ButtonStyle, ButtonStyle) {
                Text = label
            };
            button.OnClicked += (s, e) => { clickedF(); };
            buttons.Add(button);
        }




        public override void Create() {
            base.Create();

            HUDElementLayout layout = new HUDElementLayout(this, new HUDPanelContentBox(this), this.Children);
            float fZ = -0.01f;
            Vector2f vertFieldOffset = -this.Padding * Vector2f.AxisY;

            background = new HUDShapeElement() {
                Shape = BackgroundShapeF(this.Width, this.Height),
                Color = this.BackgroundColor, IsInteractive = false
            };
            background.Create();
            background.Name = "background";
            layout.Add(background, new LayoutOptions() { Flags = LayoutFlags.None,
                PinSourcePoint2D = LayoutUtil.BoxPointF(background, BoxPosition.Center),
                PinTargetPoint2D = LayoutUtil.BoxPointF(layout.BoxElement, BoxPosition.Center)
            });


            header_label = new HUDLabel(this.PaddedWidth, HeaderStyle.TextHeight, HeaderStyle)
                { Text = TitleText, IsInteractive = false };
            header_label.Create();
            header_label.Name = "header_label";
            BoxPosition headerBoxPos = BoxModel.ToPosition(HeaderStyle.AlignmentHorz, VerticalAlignment.Top);
            layout.Add(header_label, 
                LayoutUtil.PointToPoint(header_label, headerBoxPos, layout.BoxElement, headerBoxPos, fZ) );


            float message_height = this.PaddedHeight - HeaderStyle.TextHeight - ButtonStyle.Height;
            message_area = new HUDMultiLineLabel(this.PaddedWidth, message_height, MessageStyle) 
                { Text = MessageText, IsInteractive = false };
            message_area.Create();
            message_area.Name = "message_area";
            BoxPosition messageAreaBoxPos = BoxModel.ToPosition(MessageStyle.AlignmentHorz, VerticalAlignment.Top);
            layout.Add(message_area, 
                LayoutUtil.PointToPoint(message_area, messageAreaBoxPos, header_label, BoxModel.ToBottom(headerBoxPos), vertFieldOffset, fZ) );


            IBoxModelElement prev = layout.BoxElement;
            BoxPosition prevPos = BoxPosition.BottomRight;
            Vector2f prevShift = Vector2f.Zero;
            for ( int i = buttons.Count-1; i >= 0; --i ) {
                buttons[i].Create();
                buttons[i].Name = buttons[i].Text;
                layout.Add(buttons[i],
                    LayoutUtil.PointToPoint(buttons[i], BoxPosition.BottomRight, prev, prevPos, prevShift, fZ));
                prev = buttons[i];
                prevPos = BoxPosition.BottomLeft;
                prevShift = -Padding * Vector2f.AxisX;
            }


        }



        void update_button_style()
        {
            // [TODO]
        }
        void update_header_style()
        {
        }
        void update_message_style()
        {
        }
        


        public void Dismiss()
        {
            FUtil.SafeSendEvent(OnDismissed, this, new EventArgs());
        }


        void UpdateText()
        {
            //if ( bgMesh != null ) {
            //    bgMesh.SetColor(BackgroundColor);
            //}
            //if (textMesh != null) {
            //    textMesh.SetColor( TextColor );
            //    textMesh.SetText(Text);
            //}
            //if (titleTextMesh != null) {
            //    titleTextMesh.SetColor( TitleTextColor );
            //    titleTextMesh.SetText(TitleText);
            //}
        }



        // events for clicked and dismissed.
        public event EventHandler OnDismissed;



    }

}
