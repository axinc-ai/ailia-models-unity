using System;
using System.Collections.Generic;
using System.Linq;

public static class NMSUtils
{
    // Batched NMS: boxes [N,4], scores [N], classIds [N], iouThreshold scalar
    public static List<int> BatchedNMS(List<float[]> boxes, List<float> scores, List<int> classIds, float iouThreshold)
    {
        var keptIndices = new List<int>();
        foreach (var label in classIds.Distinct())
        {
            var idxList = new List<int>();
            for (int i = 0; i < classIds.Count; i++)
                if (classIds[i] == label) idxList.Add(i);

            var subBoxes = idxList.Select(i => boxes[i]).ToList();
            var subScores = idxList.Select(i => scores[i]).ToList();

            var keepIdx = NMSBoxes(subBoxes, subScores, iouThreshold);
            foreach (var k in keepIdx)
                keptIndices.Add(idxList[k]);
        }

        // Sort by score descending
        var keptScores = keptIndices.Select(i => scores[i]).ToList();
        var keptIndicesArray = keptIndices.ToArray();
        var keptScoresArray = keptScores.ToArray();
        Array.Sort(keptScoresArray, keptIndicesArray, Comparer<float>.Create((x, y) => y.CompareTo(x)));

        return keptIndicesArray.ToList();
    }

    // NMS for a single class
    public static List<int> NMSBoxes(List<float[]> boxes, List<float> scores, float iouThreshold)
    {
        var keep = new List<bool>();
        var indices = new List<int>();

        for (int i = 0; i < boxes.Count; i++)
        {
            bool isKeep = true;
            for (int j = 0; j < i; j++)
            {
                if (!keep[j]) continue;
                float iou = BBIntersectionOverUnion(boxes[i], boxes[j]);
                if (iou >= iouThreshold)
                {
                    if (scores[i] > scores[j])
                        keep[j] = false;
                    else
                    {
                        isKeep = false;
                        break;
                    }
                }
            }
            keep.Add(isKeep);
        }

        for (int i = 0; i < keep.Count; i++)
            if (keep[i]) indices.Add(i);

        return indices;
    }

    // IoU for two boxes [x1, y1, x2, y2]
    public static float BBIntersectionOverUnion(float[] boxA, float[] boxB)
    {
        float xA = Math.Max(boxA[0], boxB[0]);
        float yA = Math.Max(boxA[1], boxB[1]);
        float xB = Math.Min(boxA[2], boxB[2]);
        float yB = Math.Min(boxA[3], boxB[3]);

        float interArea = Math.Max(0, xB - xA + 1) * Math.Max(0, yB - yA + 1);

        float boxAArea = (boxA[2] - boxA[0] + 1) * (boxA[3] - boxA[1] + 1);
        float boxBArea = (boxB[2] - boxB[0] + 1) * (boxB[3] - boxB[1] + 1);

        float iou = interArea / (boxAArea + boxBArea - interArea);
        return iou;
    }
}