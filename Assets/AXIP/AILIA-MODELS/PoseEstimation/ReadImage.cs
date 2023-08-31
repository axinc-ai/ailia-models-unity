using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace ailiaSDK
{
    public class ReadImage
    {

        public static Texture2D PngToTex2D(string path)
        {
            BinaryReader bin = new BinaryReader(new FileStream(path, FileMode.Open, FileAccess.Read));
            byte[] rb = bin.ReadBytes((int)bin.BaseStream.Length);
            bin.Close();
            int pos = 16, width = 0, height = 0;
            for (int i = 0; i < 4; i++) width = width * 256 + rb[pos++];
            for (int i = 0; i < 4; i++) height = height * 256 + rb[pos++];
            Texture2D texture = new Texture2D(width, height);
            texture.LoadImage(rb);
            return texture;
        }

    }
}
