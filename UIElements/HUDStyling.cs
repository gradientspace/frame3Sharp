using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{
    public interface IContentBoxStyle
    {
        Func<float, float, HUDShape> ShapeF { get; }
        float Width { get; }
        float Height { get; }
        Colorf BackgroundColor { get; }
        float BorderWidth { get; }
        Colorf BorderColor { get; }
    }


    public interface ITextElementStyle {
        Colorf TextColor { get; }
        float TextHeight { get; }
        HorizontalAlignment AlignmentHorz { get; }
    }



    public class HUDContentBoxStyle : IContentBoxStyle
    {
        public Func<float, float, HUDShape> ShapeF { get; set; }
        public float Height { get; set; }
        public float Width { get; set; }

        public Colorf BackgroundColor { get; set; }

        public float BorderWidth { get; set; }
        public Colorf BorderColor { get; set; }

        public HUDContentBoxStyle() {
            ShapeF = HUDUIDefaults.MakeStandardButtonShapeF;
            Width = 1.0f * HUDUIDefaults.UIScale;
            Height = 0.2f * HUDUIDefaults.UIScale;
            BackgroundColor = Colorf.Silver;
            BorderWidth = 0.05f * HUDUIDefaults.UIScale;
            BorderColor = Colorf.DimGrey;
        }
    }


    public class HUDLabelStyle : ITextElementStyle
    {
        public Colorf TextColor { get; set; }
        public float TextHeight { get; set; }
        public HorizontalAlignment AlignmentHorz { get; set; }

        public HUDLabelStyle() {
            TextColor = Colorf.VideoBlack;
            TextHeight = 0.15f * HUDUIDefaults.UIScale;
            AlignmentHorz = HorizontalAlignment.Left;
        }
    }



    public class HUDButtonStyle : HUDContentBoxStyle, ITextElementStyle
    {
        // ITextElementStyle
        public Colorf TextColor { get; set; }
        public float TextHeight { get; set; }
        public HorizontalAlignment AlignmentHorz { get; set; }

        public HUDButtonStyle() : base() {
            TextColor = Colorf.VideoBlack;
            TextHeight = 0.15f * HUDUIDefaults.UIScale;
            AlignmentHorz = HorizontalAlignment.Center;
        }
    }


}
