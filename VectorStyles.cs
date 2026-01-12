using Mapsui.Styles;
using System.Collections.Generic;

namespace FFSchedule
{
    public static class VectorStyles
    {
        private static readonly Color[] RegionColors = new Color[]
        {
            new Color(70,130,180,120), 
            new Color(60,179,113,120), 
            new Color(218,165,32,120),
            new Color(255,140,0,120),  
            new Color(199,21,133,120)  
        };

        private static readonly Color PolygonBorder = new Color(0, 0, 0, 200);

        public static VectorStyle GetPolygonStyle(string name = null)
        {
            Color fillColor = RegionColors[0];
            if (!string.IsNullOrEmpty(name))
            {
                int hash = name.GetHashCode();
                int idx = (hash & int.MaxValue) % RegionColors.Length;
                fillColor = RegionColors[idx];
            }

            return new VectorStyle
            {
                Fill = new Brush(fillColor),
                Line = new Pen(PolygonBorder, 1.2f)
            };
        }

        public static LabelStyle GetLabelStyle(string text)
        {
            return new LabelStyle
            {
                Text = text ?? "",
                Font = new Mapsui.Styles.Font { Size = 14, Bold = true },
                ForeColor = new Color(0, 0, 0),
                BackColor = new Brush(new Color(255, 255, 255, 200)),
                Halo = new Pen(new Color(255, 255, 255), 1)
            };
        }

        public static List<IStyle> GetPointStylesWithLabel(string labelText)
        {
            return new List<IStyle>
            {
                new SymbolStyle
                {
                    SymbolType = SymbolType.Ellipse,
                    Fill = new Brush(new Color(255,69,0,255)), 
                    Outline = new Pen(new Color(139,0,0,255), 1),
                    SymbolScale = 0.3
                },
                new LabelStyle
                {
                    Text = labelText ?? "",
                    Font = new Mapsui.Styles.Font { Size = 12, Bold = true },
                    ForeColor = new Color(0,0,0),
                    BackColor = new Brush(new Color(255,255,255,200)),
                    Halo = new Pen(new Color(255,255,255), 1),
                    Offset = new Offset(0.0, -15) 
                }

            };
        }
    
    }
}
