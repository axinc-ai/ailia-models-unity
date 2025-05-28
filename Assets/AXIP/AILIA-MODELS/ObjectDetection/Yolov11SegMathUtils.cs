using System;

public static class Yolov11SegMathUtils
{
    public static float Sigmoid(float x)
    {
        return 1.0f / (1.0f + (float)Math.Exp(-x));
    }
}