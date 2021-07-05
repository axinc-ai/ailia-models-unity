using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;

namespace ailiaSDK
{
    public class FaceRecognitionUtil
    {
        public static float[] LoadNpy(string path)
        {
            float[] npyarray = null;
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(fs))
                {
                    var magicstring = br.ReadBytes(6);
                    var major_version = br.ReadByte();
                    var minor_version = br.ReadByte();
                    var header_len = br.ReadUInt16();

                    var header_string = new string(br.ReadChars(header_len));
                    Debug.Log($"{path} : {header_string}");

                    var npy_param_datas = header_string.Split(',');
                    Dictionary<string, string> npyparams = new Dictionary<string, string>();

                    foreach (var n in npy_param_datas)
                    {
                        var kv = n.Split(':');
                        Debug.Log($"npy param : {n}:{kv.Length}");
                        // npyparams.Add(kv[0], kv[1]);
                    }

                    // TODO: header_stringのshapeの項目に合わせたarrayを作った方が良い
                    var datas = new List<float>();

                    do {
                        datas.Add(br.ReadSingle());
                    } while (0 < (br.BaseStream.Length - br.BaseStream.Position));

                    npyarray = datas.ToArray();

                    br.Close();
                }
            }
            return npyarray;
        }
    }
}