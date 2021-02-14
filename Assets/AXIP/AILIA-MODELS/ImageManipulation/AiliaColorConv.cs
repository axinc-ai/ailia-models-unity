using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ailiaSDK
{
    public class AiliaColorConv
    {
        public class LAB
        {
            public float L
            {
                get; set;
            }
            public float A
            {
                get; set;
            }
            public float B
            {
                get; set;
            }

            // lightness accessors
            public float l
            {
                get { return this.L; }
                set { this.L = value; }
            }

            // a color-opponent accessor
            public float a
            {
                get { return this.A; }
                set { this.A = value; }
            }

            // b color-opponent accessor
            public float b
            {
                get { return this.B; }
                set { this.B = value; }
            }

            // constructor - takes three floats for lightness and color-opponent dimensions
            public LAB(float l, float a, float b)
            {
                this.l = l;
                this.a = a;
                this.b = b;
            }

            // constructor - takes a Color
            public LAB(Color col)
            {
                LAB temp = FromColor(col);
                l = temp.l;
                a = temp.a;
                b = temp.b;
            }

            // static function for linear interpolation between two LABColors
            public static LAB Lerp(LAB a, LAB b, float t)
            {
                return new LAB(Mathf.Lerp(a.l, b.l, t), Mathf.Lerp(a.a, b.a, t), Mathf.Lerp(a.b, b.b, t));
            }

            // static function for interpolation between two Unity Colors through normalized colorspace
            public static Color Lerp(Color a, Color b, float t)
            {
                return (LAB.Lerp(LAB.FromColor(a), LAB.FromColor(b), t)).ToColor();
            }

            // static function for returning the color difference in a normalized colorspace (Delta-E)
            public static float Distance(LAB a, LAB b)
            {
                return Mathf.Sqrt(Mathf.Pow((a.l - b.l), 2f) + Mathf.Pow((a.a - b.a), 2f) + Mathf.Pow((a.b - b.b), 2f));
            }

            // static function for converting from Color to LABColor
            public static LAB FromColor(Color c)
            {
                float D65x = 0.9505f;
                float D65y = 1.0f;
                float D65z = 1.0890f;
                float rLinear = c.r;
                float gLinear = c.g;
                float bLinear = c.b;
                //float r = (rLinear > 0.04045f) ? Mathf.Pow((rLinear + 0.055f) / (1f + 0.055f), 2.2f) : (rLinear / 12.92f);
                //float g = (gLinear > 0.04045f) ? Mathf.Pow((gLinear + 0.055f) / (1f + 0.055f), 2.2f) : (gLinear / 12.92f);
                //float b = (bLinear > 0.04045f) ? Mathf.Pow((bLinear + 0.055f) / (1f + 0.055f), 2.2f) : (bLinear / 12.92f);

                // 2.2f => 2.4f ???何で?? python版のskimageのconvertがそうなってた
                float r = (rLinear > 0.04045f) ? Mathf.Pow((rLinear + 0.055f) / (1f + 0.055f), 2.4f) : (rLinear / 12.92f);
                float g = (gLinear > 0.04045f) ? Mathf.Pow((gLinear + 0.055f) / (1f + 0.055f), 2.4f) : (gLinear / 12.92f);
                float b = (bLinear > 0.04045f) ? Mathf.Pow((bLinear + 0.055f) / (1f + 0.055f), 2.4f) : (bLinear / 12.92f);
                float x = (r * 0.4124f + g * 0.3576f + b * 0.1805f);
                float y = (r * 0.2126f + g * 0.7152f + b * 0.0722f);
                float z = (r * 0.0193f + g * 0.1192f + b * 0.9505f);
                x = (x > 0.9505f) ? 0.9505f : ((x < 0f) ? 0f : x);
                y = (y > 1.0f) ? 1.0f : ((y < 0f) ? 0f : y);
                z = (z > 1.089f) ? 1.089f : ((z < 0f) ? 0f : z);
                LAB lab = new LAB(0f, 0f, 0f);
                float fx = x / D65x;
                float fy = y / D65y;
                float fz = z / D65z;
                fx = ((fx > 0.008856f) ? Mathf.Pow(fx, (1.0f / 3.0f)) : (7.787f * fx + 16.0f / 116.0f));
                fy = ((fy > 0.008856f) ? Mathf.Pow(fy, (1.0f / 3.0f)) : (7.787f * fy + 16.0f / 116.0f));
                fz = ((fz > 0.008856f) ? Mathf.Pow(fz, (1.0f / 3.0f)) : (7.787f * fz + 16.0f / 116.0f));
                lab.l = 116.0f * fy - 16f;
                lab.a = 500.0f * (fx - fy);
                lab.b = 200.0f * (fy - fz);
                return lab;
            }

            // static function for converting from LABColor to Color
            public static Color ToColor(LAB lab)
            {
                float D65x = 0.9505f;
                float D65y = 1.0f;
                float D65z = 1.0890f;
                float delta = 6.0f / 29.0f;
                float fy = (lab.l + 16f) / 116.0f;
                float fx = fy + (lab.a / 500.0f);
                float fz = fy - (lab.b / 200.0f);
                float x = (fx > delta) ? D65x * (fx * fx * fx) : (fx - 16.0f / 116.0f) * 3f * (delta * delta) * D65x;
                float y = (fy > delta) ? D65y * (fy * fy * fy) : (fy - 16.0f / 116.0f) * 3f * (delta * delta) * D65y;
                float z = (fz > delta) ? D65z * (fz * fz * fz) : (fz - 16.0f / 116.0f) * 3f * (delta * delta) * D65z;
                float r = x * 3.2410f - y * 1.5374f - z * 0.4986f;
                float g = -x * 0.9692f + y * 1.8760f - z * 0.0416f;
                float b = x * 0.0556f - y * 0.2040f + z * 1.0570f;
                r = (r <= 0.0031308f) ? 12.92f * r : (1f + 0.055f) * Mathf.Pow(r, (1.0f / 2.4f)) - 0.055f;
                g = (g <= 0.0031308f) ? 12.92f * g : (1f + 0.055f) * Mathf.Pow(g, (1.0f / 2.4f)) - 0.055f;
                b = (b <= 0.0031308f) ? 12.92f * b : (1f + 0.055f) * Mathf.Pow(b, (1.0f / 2.4f)) - 0.055f;
                r = (r < 0) ? 0 : r;
                g = (g < 0) ? 0 : g;
                b = (b < 0) ? 0 : b;
                return new Color(r, g, b);
            }

            // function for converting an instance of LABColor to Color
            public Color ToColor()
            {
                return LAB.ToColor(this);
            }

            // override for string
            public override string ToString()
            {
                return "L:" + l + " A:" + a + " B:" + b;
            }

            // are two LABColors the same?
            public override bool Equals(System.Object obj)
            {
                if (obj == null || GetType() != obj.GetType()) return false;
                return (this == (LAB)obj);
            }

            // override hashcode for a LABColor
            public override int GetHashCode()
            {
                return l.GetHashCode() ^ a.GetHashCode() ^ b.GetHashCode();
            }

            // Equality operator
            public static bool operator ==(LAB item1, LAB item2)
            {
                return (item1.l == item2.l && item1.a == item2.a && item1.b == item2.b);
            }

            // Inequality operator
            public static bool operator !=(LAB item1, LAB item2)
            {
                return (item1.l != item2.l || item1.a != item2.a || item1.b != item2.b);
            }
        }
        public static LAB Color2Lab(Color c)
        {
            return new LAB(c);
        }

        public static Color Lab2Color(LAB lab)
        {
            return lab.ToColor();
        }
    }

}