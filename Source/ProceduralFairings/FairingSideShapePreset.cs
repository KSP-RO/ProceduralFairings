using Keramzit;
using UnityEngine;

namespace ProceduralFairings
{
    public class FairingSideShapePreset
    {
        [Persistent] public string name = "Conic";
        [Persistent] public Vector4 baseConeShape = new Vector4(0.3f, 0.3f, 0.7f, 0.7f);
        [Persistent] public Vector4 noseConeShape = new Vector4(0.1f, 0, 0.7f, 0.7f);
        [Persistent] public int baseConeSegments = 7;
        [Persistent] public int noseConeSegments = 11;
        [Persistent] public float noseHeightRatio = 2;

        public void Apply(ProceduralFairingSide side)
        {
            side.baseConeShape = baseConeShape;
            side.noseConeShape = noseConeShape;
            side.baseConeSegments = baseConeSegments;
            side.noseConeSegments = noseConeSegments;
            side.noseHeightRatio = noseHeightRatio;
            side.ResetNoseCurve();
            side.ResetBaseCurve();
            side.rebuildMesh();
        }
    }
}
