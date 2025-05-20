using System;

public static class MathUtils
{
    public static float Sigmoid(float x)
    {
        return 1.0f / (1.0f + (float)Math.Exp(-x));
    }
}