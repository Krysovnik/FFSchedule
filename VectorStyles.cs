using Mapsui.Styles;
using System.Collections.Generic;
using System;
using System.Drawing.Drawing2D;

namespace FFSchedule
{
    public static class VectorStyles
    {
        private static readonly Color[] RegionColors = new Color[]
        {
            new Color(237, 125, 49, 180),  
            new Color(165, 165, 165, 180), 
            new Color(255, 192, 0, 180),   
            new Color(186, 85, 211, 180),
            new Color(180, 128, 128, 180)
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
                Line = new Pen(PolygonBorder, 1.5f),
                MinVisible = 10, 
                MaxVisible = double.MaxValue
            };
        }

        public static LabelStyle GetLabelStyle(string text)
        {
            float fontSize = 12;
            Color haloColor = new Color(255, 255, 255, 200);
            Offset offset = new Offset(0, 0);
            return new LabelStyle
            {
                Text = text ?? "",
                Font = new Mapsui.Styles.Font
                {
                    Size = fontSize,
                    Bold = true,
                    FontFamily = "Arial"
                },
                ForeColor = new Color(30, 30, 30),
                BackColor = new Brush(new Color(255, 255, 255, 180)),
                Halo = new Pen(haloColor, 2),
                Offset = offset,
                HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Center,
                VerticalAlignment = LabelStyle.VerticalAlignmentEnum.Center,
                MinVisible = 50, 
                MaxVisible = 500
            };
        }

        public static List<IStyle> GetPointStylesWithLabel(string labelText, string type = null)
        {
            Color fillColor;
            Color outlineColor;

            switch (type?.ToLower().Trim())
            {
                case "пч":
                    fillColor = new Color(0, 128, 0, 200);
                    break;

                case "псч":
                    fillColor = new Color(255, 0, 0, 200);
                    break;

                case "спсч":
                    fillColor = new Color(0, 0, 255, 200);
                    break;

                default:
                    fillColor = new Color(220, 20, 60, 200);  
                    break;
            }

            float symbolScale = 0.35f;

            return new List<IStyle>
            {
                new SymbolStyle
                {
                    SymbolType = SymbolType.Ellipse,
                    Fill = new Brush(fillColor),
                    Outline = new Pen(new Color(255, 255, 255, 255), 3.0f)
                    {
                        PenStyle = PenStyle.Solid
                    },
                    SymbolScale = symbolScale,
                    Opacity = 0.7f,
                    MinVisible = 1,
                    MaxVisible = double.MaxValue
                },
                new LabelStyle
                {
                    Text = labelText ?? "",
                    Font = new Mapsui.Styles.Font
                    {
                        Size = 12,
                        Bold = true,
                        FontFamily = "Arial"
                    },
                    ForeColor = new Color(0, 0, 0),
                    BackColor = new Brush(new Color(255, 255, 255, 220)),
                    Halo = new Pen(new Color(255, 255, 255, 200), 1.5f),
                    Offset = new Offset(0.0, -18),
                    HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Center,
                    LineHeight = 1.2,
                    MaxVisible = 80
                }
            };
        }
    }
}
