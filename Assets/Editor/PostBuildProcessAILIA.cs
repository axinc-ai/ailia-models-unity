//必要なフレームワークの追加

using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
#endif
namespace ailiaSDK
{
    public class PostBuildProcessAILIA
    {

        [PostProcessBuild]
        public static void OnPostProcessBuild(BuildTarget buildTarget, string path)
        {
#if UNITY_IOS
        string projPath = Path.Combine (path, "Unity-iPhone.xcodeproj/project.pbxproj");

        PBXProject proj = new PBXProject ();
        proj.ReadFromString (File.ReadAllText (projPath));

#if UNITY_2019_1_OR_NEWER
        string target =  proj.GetUnityFrameworkTargetGuid();
#else
        string target = proj.TargetGuidByName ("Unity-iPhone");
#endif

        List<string> frameworks = new List<string> () {
            "Accelerate.framework",
            "MetalPerformanceShaders.framework"
        };

        foreach (var framework in frameworks) {
            proj.AddFrameworkToProject (target, framework, false);
        }

        //Add
        File.WriteAllText (projPath, proj.WriteToString ());
#endif
        }
    }
}