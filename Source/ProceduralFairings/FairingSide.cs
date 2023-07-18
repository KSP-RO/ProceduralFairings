//  ==================================================
//  Procedural Fairings plug-in by Alexey Volynskov.

//  Licensed under CC-BY-4.0 terms: https://creativecommons.org/licenses/by/4.0/legalcode
//  ==================================================

using ProceduralFairings;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

namespace Keramzit
{
    public class ProceduralFairingSide : PartModule, IPartCostModifier, IPartMassModifier
    {
        internal ColliderPool colliderPool;
        [KSPField] public float minBaseConeAngle = 20;
        [KSPField] public float colliderShaveAngle = 5;
        [KSPField] public Vector4 baseConeShape = new Vector4(0, 0, 0, 0);
        [KSPField] public Vector4 noseConeShape = new Vector4(0, 0, 0, 0);

        [KSPField] public Vector2 mappingScale = new Vector2(1024, 1024);
        [KSPField] public Vector2 stripMapping = new Vector2(992, 1024);
        [KSPField] public Vector4 horMapping = new Vector4(0, 480, 512, 992);
        [KSPField] public Vector4 vertMapping = new Vector4(0, 160, 704, 1024);

        [KSPField] public float costPerTonne = 2000;
        [KSPField] public float specificBreakingForce = 2000;
        [KSPField] public float specificBreakingTorque = 2000;

        public float DefaultBaseConeSegments => part.partInfo.partPrefab.FindModuleImplementing<ProceduralFairingSide>().baseConeSegments;
        public float DefaultNoseConeSegments => part.partInfo.partPrefab.FindModuleImplementing<ProceduralFairingSide>().noseConeSegments;
        public float DefaultNoseHeightRatio => part.partInfo.partPrefab.FindModuleImplementing<ProceduralFairingSide>().noseHeightRatio;

        public float fairingMass;

        [KSPField(isPersistant = true)] public int numSegs = 12;
        [KSPField(isPersistant = true)] public int numSideParts = 2;
        [KSPField(isPersistant = true)] public float baseRad;
        [KSPField(isPersistant = true)] public float maxRad = 1.50f;
        [KSPField(isPersistant = true)] public float cylStart = 0.5f;
        [KSPField(isPersistant = true)] public float cylEnd = 2.5f;
        [KSPField(isPersistant = true)] public float topRad;
        [KSPField(isPersistant = true)] public float inlineHeight;
        [KSPField(isPersistant = true)] public float sideThickness = 0.05f;
        [KSPField(isPersistant = true)] public Vector3 meshPos = Vector3.zero;
        [KSPField(isPersistant = true)] public Quaternion meshRot = Quaternion.identity;
        [KSPField(isPersistant = true)] public Vector3 hingeOffset = Vector3.zero;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Density", groupName = PFUtils.PAWGroup, groupDisplayName = PFUtils.PAWName)]
        [UI_FloatRange(minValue = 0.01f, maxValue = 1.0f, stepIncrement = 0.01f)]
        public float density = 0.2f;

        [KSPField] public float minDensity = 0.01f;
        [KSPField] public float maxDensity = 1.0f;
        [KSPField] public float stepDensity = 0.01f;

        public ModuleAnimateGeneric HingeAnimation;

        [KSPField(guiActiveEditor = true, guiName = "Mass", groupName = PFUtils.PAWGroup)]
        public string massDisplay;

        [KSPField(guiActiveEditor = true, guiName = "Cost", groupName = PFUtils.PAWGroup)]
        public string costDisplay;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Use Preset", groupName = PFUtils.PAWShapeGroup, groupDisplayName = PFUtils.PAWShapeName)]
        [UI_Toggle(disabledText = "Off", enabledText = "On")]
        public bool usePreset = false;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Shape Preset", groupName = PFUtils.PAWShapeGroup)]
        [UI_ChooseOption(affectSymCounterparts = UI_Scene.Editor, scene = UI_Scene.Editor)]
        public string shapePreset;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Base Start X", guiFormat = "S4", groupName = PFUtils.PAWShapeGroup)]
        [UI_FloatEdit(sigFigs = 2, minValue = 0.0f, maxValue = 1.0f, incrementLarge = 0.1f, incrementSmall = 0.01f, incrementSlide = 0.01f)]
        public float baseCurveStartX = 0.5f;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Base Start Y", guiFormat = "S4", groupName = PFUtils.PAWShapeGroup)]
        [UI_FloatEdit(sigFigs = 2, minValue = 0.0f, maxValue = 1.0f, incrementLarge = 0.1f, incrementSmall = 0.01f, incrementSlide = 0.01f)]
        public float baseCurveStartY = 0.0f;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Base End X", guiFormat = "S4", groupName = PFUtils.PAWShapeGroup)]
        [UI_FloatEdit(sigFigs = 2, minValue = 0.0f, maxValue = 1.0f, incrementLarge = 0.1f, incrementSmall = 0.01f, incrementSlide = 0.01f)]
        public float baseCurveEndX = 1.0f;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Base End Y", guiFormat = "S4", groupName = PFUtils.PAWShapeGroup)]
        [UI_FloatEdit(sigFigs = 2, minValue = 0.0f, maxValue = 1.0f, incrementLarge = 0.1f, incrementSmall = 0.01f, incrementSlide = 0.01f)]
        public float baseCurveEndY = 0.5f;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Base Cone Segments", groupName = PFUtils.PAWShapeGroup)]
        [UI_FloatRange(minValue = 1, maxValue = 12, stepIncrement = 1)]
        public float baseConeSegments = 5;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Nose Start X", guiFormat = "S4", groupName = PFUtils.PAWShapeGroup)]
        [UI_FloatEdit(sigFigs = 2, minValue = 0.0f, maxValue = 1.0f, incrementLarge = 0.1f, incrementSmall = 0.01f, incrementSlide = 0.01f)]
        public float noseCurveStartX = 0.5f;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Nose Start Y", guiFormat = "S4", groupName = PFUtils.PAWShapeGroup)]
        [UI_FloatEdit(sigFigs = 2, minValue = 0.0f, maxValue = 1.0f, incrementLarge = 0.1f, incrementSmall = 0.01f, incrementSlide = 0.01f)]
        public float noseCurveStartY = 0.0f;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Nose End X", guiFormat = "S4", groupName = PFUtils.PAWShapeGroup)]
        [UI_FloatEdit(sigFigs = 2, minValue = 0.0f, maxValue = 1.0f, incrementLarge = 0.1f, incrementSmall = 0.01f, incrementSlide = 0.01f)]
        public float noseCurveEndX = 1.0f;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Nose End Y", guiFormat = "S4", groupName = PFUtils.PAWShapeGroup)]
        [UI_FloatEdit(sigFigs = 2, minValue = 0.0f, maxValue = 1.0f, incrementLarge = 0.1f, incrementSmall = 0.01f, incrementSlide = 0.01f)]
        public float noseCurveEndY = 0.5f;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Nose Cone Segments", groupName = PFUtils.PAWShapeGroup)]
        [UI_FloatRange(minValue = 1, maxValue = 12, stepIncrement = 1)]
        public float noseConeSegments = 7;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Nose-Height Ratio", guiFormat = "S4", groupName = PFUtils.PAWShapeGroup)]
        [UI_FloatEdit(sigFigs = 2, minValue = 0.1f, maxValue = 5.0f, incrementLarge = 1.0f, incrementSmall = 0.1f, incrementSlide = 0.01f)]
        public float noseHeightRatio = 2.0f;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Shape", groupName = PFUtils.PAWShapeGroup)]
        [UI_Toggle(disabledText = "Unlocked", enabledText = "Locked")]
        public bool shapeLock;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Petal Hinge", groupName = PFUtils.PAWGroup)]
        [UI_Toggle(disabledText = "Disabled", enabledText = "Enabled", affectSymCounterparts = UI_Scene.All)]
        public bool hingeEnabled = false;

        [KSPField] public float decouplerCostMult = 1;              // Mult to costPerTonne when decoupler is enabled
        [KSPField] public float decouplerCostBase = 0;              // Flat additional cost when decoupler is enabled
        [KSPField] public float decouplerMassMult = 1;              // Mass multiplier
        [KSPField] public float decouplerMassBase = 0.0001f;        // Flat additional mass (0.001 = 1kg)
        [KSPField] public float hingeCostMult = 1;                  // Mult to costPerTonne when hinge is enabled
        [KSPField] public float hingeCostBase = 0;                  // Flat additional cost when hinge is enabled
        [KSPField] public float hingeMassMult = 1;                  // Mass multiplier
        [KSPField] public float hingeMassBase = 0.0001f;            // Flat additional mass (0.001 = 1kg)
        public ModifierChangeWhen GetModuleCostChangeWhen() => ModifierChangeWhen.FIXED;
        public ModifierChangeWhen GetModuleMassChangeWhen() => ModifierChangeWhen.FIXED;
        public float GetModuleCost(float defcost, ModifierStagingSituation sit) => ApplyModuleCostModifier(fairingMass * costPerTonne) - defcost;
        public float GetModuleMass(float defmass, ModifierStagingSituation sit) => ApplyModuleMassModifier(fairingMass) - defmass;
        private float ApplyModuleCostModifier(float baseCost) => (baseCost * totalCostMult) + totalCostBase;
        private float ApplyModuleMassModifier(float baseMass) => (baseMass * totalMassMult) + totalMassBase;
        private bool DecouplerEnabled => part.FindModuleImplementing<ProceduralFairingDecoupler>() is ProceduralFairingDecoupler d && d.fairingStaged;
        public override string GetInfo() => "Attach to a procedural fairing base to reshape. Right-click it to set its parameters.";
        private float totalCostMult => 1 + (float)Math.Sqrt(Math.Pow(DecouplerEnabled ? decouplerCostMult : 0,2) + Math.Pow(hingeEnabled ? hingeCostMult : 0,2));
        private float totalCostBase => (DecouplerEnabled ? decouplerCostBase : 0) + (hingeEnabled ? hingeCostBase : 0);
        private float totalMassMult => (DecouplerEnabled ? decouplerMassMult : 1) * (hingeEnabled ? hingeMassMult : 1);
        private float totalMassBase => (DecouplerEnabled ? decouplerMassBase : 0) + (hingeEnabled ? hingeMassBase : 0);
        private static readonly Dictionary<string, FairingSideShapePreset> AllPresets = new Dictionary<string, FairingSideShapePreset>();

        [KSPEvent(active = true, guiActiveEditor = true, groupName = PFUtils.PAWGroup, guiName = "Toggle Open/Closed")]
        public void ToggleOpenClosed()
        {
            if (part.FindAttachNode("connect")?.attachedPart is Part fairingBase
                && fairingBase.GetComponent<ProceduralFairingBase>() is ProceduralFairingBase pm
                && pm.Fields[nameof(pm.openFairing)] is BaseField openField
                && openField.GetValue(pm) is bool openStatus)
            {
                ProceduralTools.KSPFieldTool.SetField(pm, openField, !openStatus);
            }
        }

        [KSPEvent(active = true, guiActiveEditor = true, guiActive = true, groupName = PFUtils.PAWGroup, guiName = "Toggle Petals")]
        public void TogglePetals()
        {
            HingeAnimation.Events["Toggle"].Invoke();

            foreach (Part p in part.symmetryCounterparts)
            {
                if (p.FindModuleImplementing<ProceduralFairingSide>() is ProceduralFairingSide side)
                {
                    side.HingeAnimation.Events["Toggle"].Invoke();
                }
            }
        }

        public void Start()
        {
            if (part.mass != ApplyModuleMassModifier(fairingMass))
            {
                Debug.LogWarning($"[PF] FairingSide Start(): Expected part mass {ApplyModuleMassModifier(fairingMass)} but discovered {part.mass}!");
                part.mass = ApplyModuleMassModifier(fairingMass);
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            // For prefab only: Initialize Base/Nose Curve Start/End X/Y from the Vector4.
            // All other loads should reference the persistent value.
            if (HighLogic.LoadedScene == GameScenes.LOADING)
            {
                ReadBaseCurveFromVec4(false);
                ReadNoseCurveFromVec4(false);
            }

            SetDensityField();
        }

        public override void OnStart(StartState state)
        {
            HingeAnimation = part.FindModuleImplementing<ModuleAnimateGeneric>();
          
            colliderPool = new ColliderPool(part.FindModelComponent<MeshFilter>("model"));
            if (AllPresets.Count == 0)
                LoadPresets(AllPresets);

            (Fields[nameof(shapePreset)].uiControlEditor as UI_ChooseOption).options = AllPresets.Keys.ToArray();
            (Fields[nameof(shapePreset)].uiControlEditor as UI_ChooseOption).display = AllPresets.Keys.ToArray();

            if (!AllPresets.ContainsKey(shapePreset))
                shapePreset = AllPresets.Keys.FirstOrDefault() ?? "Invalid";

            if (HighLogic.LoadedSceneIsEditor && usePreset && !(AllPresets.TryGetValue(shapePreset, out var preset) && preset.IsApplied(this)))
            {
                Debug.LogWarning($"[PF]: Incoherent preset value detected; falling back to custom shaping!", part);
                usePreset = false;
            }

            // If part is pulled from the picker (ie prefab), delay mesh rebuilding.
            if (HighLogic.LoadedSceneIsEditor && part.parent == null)
                part.OnEditorAttach += ApplyShapeOnStart;
            else
                ApplyShapeOnStart();

            SetUICallbacks();
            SetUIFieldVisibility();

            SetDensityField();
        }
        public override void OnStartFinished(StartState state)
        {
            base.OnStartFinished(state);

            HingeAnimation.Events["Toggle"].guiActiveEditor = false;
            HingeAnimation.Events["Toggle"].guiActive = false;
            HingeAnimation.Events["Toggle"].guiActiveUnfocused = false;
            HingeAnimation.Events["Toggle"].guiActiveUncommand = false;
            SetHingeToggles();

        }
        private void SetDensityField()
        {
            var floatRange = Fields[nameof(density)].uiControlEditor as UI_FloatRange;
            floatRange.minValue = minDensity;
            floatRange.maxValue = maxDensity;
            floatRange.stepIncrement = stepDensity;
        }

        public void OnDestroy() => colliderPool?.Dispose();

        private void ApplyShapeOnStart()
        {
            if (usePreset && !HighLogic.LoadedSceneIsFlight)
                ApplySelectedPreset();
            else
                rebuildMesh();
        }

        void SetUICallbacks()
        {
            Fields[nameof(usePreset)].uiControlEditor.onFieldChanged += OnUsePresetToggled;
            Fields[nameof(usePreset)].uiControlEditor.onSymmetryFieldChanged += OnUsePresetToggled;

            Fields[nameof(shapePreset)].uiControlEditor.onFieldChanged += OnShapePresetChanged;
            Fields[nameof(shapePreset)].uiControlEditor.onSymmetryFieldChanged += OnShapePresetChanged;

            Fields[nameof(baseCurveStartX)].uiControlEditor.onFieldChanged += OnChangeShapeUI;
            Fields[nameof(baseCurveStartY)].uiControlEditor.onFieldChanged += OnChangeShapeUI;
            Fields[nameof(baseCurveEndX)].uiControlEditor.onFieldChanged += OnChangeShapeUI;
            Fields[nameof(baseCurveEndY)].uiControlEditor.onFieldChanged += OnChangeShapeUI;

            Fields[nameof(baseCurveStartX)].uiControlEditor.onSymmetryFieldChanged += OnChangeShapeUI;
            Fields[nameof(baseCurveStartY)].uiControlEditor.onSymmetryFieldChanged += OnChangeShapeUI;
            Fields[nameof(baseCurveEndX)].uiControlEditor.onSymmetryFieldChanged += OnChangeShapeUI;
            Fields[nameof(baseCurveEndY)].uiControlEditor.onSymmetryFieldChanged += OnChangeShapeUI;

            Fields[nameof(noseCurveStartX)].uiControlEditor.onFieldChanged += OnChangeShapeUI;
            Fields[nameof(noseCurveStartY)].uiControlEditor.onFieldChanged += OnChangeShapeUI;
            Fields[nameof(noseCurveEndX)].uiControlEditor.onFieldChanged += OnChangeShapeUI;
            Fields[nameof(noseCurveEndY)].uiControlEditor.onFieldChanged += OnChangeShapeUI;
            Fields[nameof(noseHeightRatio)].uiControlEditor.onFieldChanged += OnChangeShapeUI;

            Fields[nameof(noseCurveStartX)].uiControlEditor.onSymmetryFieldChanged += OnChangeShapeUI;
            Fields[nameof(noseCurveStartY)].uiControlEditor.onSymmetryFieldChanged += OnChangeShapeUI;
            Fields[nameof(noseCurveEndX)].uiControlEditor.onSymmetryFieldChanged += OnChangeShapeUI;
            Fields[nameof(noseCurveEndY)].uiControlEditor.onSymmetryFieldChanged += OnChangeShapeUI;
            Fields[nameof(noseHeightRatio)].uiControlEditor.onSymmetryFieldChanged += OnChangeShapeUI;

            Fields[nameof(baseConeSegments)].uiControlEditor.onFieldChanged += OnChangeShapeUI;
            Fields[nameof(noseConeSegments)].uiControlEditor.onFieldChanged += OnChangeShapeUI;
            Fields[nameof(density)].uiControlEditor.onFieldChanged += OnChangeShapeUI;

            Fields[nameof(baseConeSegments)].uiControlEditor.onSymmetryFieldChanged += OnChangeShapeUI;
            Fields[nameof(noseConeSegments)].uiControlEditor.onSymmetryFieldChanged += OnChangeShapeUI;
            Fields[nameof(density)].uiControlEditor.onSymmetryFieldChanged += OnChangeShapeUI;

            if (part.FindModuleImplementing<ProceduralFairingDecoupler>() is ProceduralFairingDecoupler decoupler)
            {
                decoupler.Fields[nameof(decoupler.fairingStaged)].uiControlEditor.onFieldChanged += OnChangeDecouplerUI;
                decoupler.Fields[nameof(decoupler.fairingStaged)].uiControlEditor.onSymmetryFieldChanged += OnChangeDecouplerUI;
            }

            Fields[nameof(hingeEnabled)].uiControlEditor.onFieldChanged += OnHingeEnabledChanged;
            Fields[nameof(hingeEnabled)].uiControlEditor.onSymmetryFieldChanged += OnHingeEnabledChanged;
        }

        void OnShapePresetChanged(BaseField field, object obj) => ApplySelectedPreset();
        void OnChangeDecouplerUI(BaseField field, object obj) => UpdateMassAndCostDisplay();
        void OnUsePresetToggled(BaseField field, object obj)
        {
            if (usePreset)
                ApplySelectedPreset();

            SetUIFieldVisibility();
        }

        void OnChangeShapeUI(BaseField bf, object obj)
        {
            rebuildMesh();
        }

        internal void ReadBaseCurveFromVec4(bool fromPrefab = false)
        {
            baseCurveStartX = fromPrefab ? part.partInfo.partPrefab.FindModuleImplementing<ProceduralFairingSide>().baseConeShape.x : baseConeShape.x;
            baseCurveStartY = fromPrefab ? part.partInfo.partPrefab.FindModuleImplementing<ProceduralFairingSide>().baseConeShape.y : baseConeShape.y;
            baseCurveEndX = fromPrefab ? part.partInfo.partPrefab.FindModuleImplementing<ProceduralFairingSide>().baseConeShape.z : baseConeShape.z;
            baseCurveEndY = fromPrefab ? part.partInfo.partPrefab.FindModuleImplementing<ProceduralFairingSide>().baseConeShape.w : baseConeShape.w;
        }

        internal void ReadNoseCurveFromVec4(bool fromPrefab = false)
        {
            noseCurveStartX = fromPrefab ? part.partInfo.partPrefab.FindModuleImplementing<ProceduralFairingSide>().noseConeShape.x : noseConeShape.x;
            noseCurveStartY = fromPrefab ? part.partInfo.partPrefab.FindModuleImplementing<ProceduralFairingSide>().noseConeShape.y : noseConeShape.y;
            noseCurveEndX = fromPrefab ? part.partInfo.partPrefab.FindModuleImplementing<ProceduralFairingSide>().noseConeShape.z : noseConeShape.z;
            noseCurveEndY = fromPrefab ? part.partInfo.partPrefab.FindModuleImplementing<ProceduralFairingSide>().noseConeShape.w : noseConeShape.w;
        }

        void SetUIFieldVisibility()
        {
            Fields[nameof(shapePreset)].guiActiveEditor = AllPresets.Count > 0 && usePreset;

            Fields[nameof(baseCurveStartX)].guiActiveEditor = !usePreset;
            Fields[nameof(baseCurveStartY)].guiActiveEditor = !usePreset;
            Fields[nameof(baseCurveEndX)].guiActiveEditor = !usePreset;
            Fields[nameof(baseCurveEndY)].guiActiveEditor = !usePreset;
            Fields[nameof(baseConeSegments)].guiActiveEditor = !usePreset;

            Fields[nameof(noseCurveStartX)].guiActiveEditor = !usePreset;
            Fields[nameof(noseCurveStartY)].guiActiveEditor = !usePreset;
            Fields[nameof(noseCurveEndX)].guiActiveEditor = !usePreset;
            Fields[nameof(noseCurveEndY)].guiActiveEditor = !usePreset;
            Fields[nameof(noseConeSegments)].guiActiveEditor = !usePreset;
            Fields[nameof(noseHeightRatio)].guiActiveEditor = !usePreset;
        }

        private static void LoadPresets(Dictionary<string, FairingSideShapePreset> presets)
        {
            presets.Clear();
            if (GameDatabase.Instance.GetConfigNode("ProceduralFairings/PF_Settings/ProceduralFairingsSettings") is ConfigNode node)
            {
                foreach (var n in node.GetNodes("FairingSideShapePreset"))
                {
                    var p = new FairingSideShapePreset();
                    ConfigNode.LoadObjectFromConfig(p, n);
                    if (!presets.ContainsKey(p.name))
                        presets.Add(p.name, p);
                }
            }
        }

        private void ApplySelectedPreset()
        {
            if (!usePreset) return;
            if (AllPresets.TryGetValue(shapePreset, out var preset))
                preset.Apply(this);
        }

        private void UpdateNodeSize()
        {
            if (part.FindAttachNode("connect") is AttachNode node)
            {
                node.size = Math.Max(0, Mathf.RoundToInt(baseRad * 2 / 1.25f) - 1);
            }
        }

        public void UpdateMassAndCostDisplay()
        {
            int nsym = part.symmetryCounterparts.Count;
            string s = (nsym == 0) ? string.Empty : (nsym == 1) ? " (both)" : $" (all {nsym + 1})";
            float perPartCost = part.partInfo.cost + GetModuleCost(part.partInfo.cost, ModifierStagingSituation.CURRENT);
            massDisplay = PFUtils.FormatMass(ApplyModuleMassModifier(fairingMass) * (nsym + 1)) + s;
            costDisplay = $"{perPartCost * (nsym + 1):N0}{s}";
        }


        // dirs are a list of rotations in the y-plane for normals along the fairside edge
        // they are an alternate description of number of colliders per part.
        // shape[i].x is the radius of the fairingside
        // shape[i].y is the y-coord of point i, the height on the fairingside
        // shape[i].z I have no idea.  It's a lerp between magic numbers in the vertexMapping.
        // presumably this describes curvature somehow
        private void RebuildColliders(Vector3[] shape, Vector3[] dirs)
        {
            Profiler.BeginSample("PF.RebuildColliders.CacheCurrentColliders");
            //  Remove any old colliders.
            foreach (Collider c in part.FindModelComponents<Collider>())
                colliderPool.Release(c);
            Profiler.EndSample();
            if (part.FindModelComponent<MeshFilter>("model") is MeshFilter mf)
            {
                float anglePerPart = Mathf.Max((360f / numSideParts) - colliderShaveAngle, 1);
                int numColliders = dirs.Length;
                float anglePerCollider = anglePerPart / numColliders;
                float startAngle = (-anglePerPart / 2) + (anglePerCollider / 2);
                //  Add the new colliders.
                {
                    Profiler.BeginSample("PF.RebuildColliders.NoseCollider");
                    //  Nose collider.
                    GameObject obj = new GameObject("nose_collider");
                    SphereCollider coll = obj.AddComponent<SphereCollider>();
                    float r = (inlineHeight > 0) ? sideThickness / 2 : maxRad * 0.2f;
                    float tip = maxRad * noseHeightRatio;
                    float collCenter = (cylStart + cylEnd) / 2;

                    coll.transform.parent = mf.transform;
                    coll.transform.localRotation = Quaternion.identity;
                    coll.transform.localPosition = (inlineHeight > 0) ?
                                                    new Vector3(maxRad + r, collCenter, 0) :
                                                    new Vector3(r, cylEnd + tip - r * 1.2f, 0);
                    coll.center = Vector3.zero;
                    coll.radius = r;
                    Profiler.EndSample();
                }

                Profiler.BeginSample("PF.RebuildColliders.ComputeNormals");
                // build list of normals from shape[], the list of points on the inside surface
                Vector3[] normals = new Vector3[shape.Length];
                for (int i = 0; i < shape.Length; i++)
                {
                    Vector3 norm;
                    if (i == 0)
                        norm = cylStart > float.Epsilon ? new Vector3(0, 1, 0) : shape[1] - shape[0];
                    else if (i == shape.Length - 1)
                        norm = new Vector3(0, 1, 0);
                    else
                        norm = shape[i + 1] - shape[i - 1];
                    norm.Set(norm.y, -norm.x, 0);
                    normals[i] = norm.normalized;
                }
                Profiler.EndSample();
                Profiler.BeginSample("PF.RebuildColliders.SetupColliderRow");
                for (int i = 0; i < shape.Length - 1; i++)
                {
                    // p.x, p.y is a point on the 2D shape projection.  p.z == ??
                    // normals[i] is the 3D normal to (p.x, p.y, 0)
                    Vector3 p = shape[i];
                    Vector3 pNext = shape[i + 1];
                    p.z = pNext.z = 0;
                    // Project the points outward and build a grid on the outer edge.
                    p += normals[i] * sideThickness;
                    pNext += normals[i + 1] * sideThickness;

                    // Build a grid between pNext and p
                    Vector3 n = pNext - p;
                    n.Set(n.y, -n.x, 0);
                    n.Normalize();
                    // n is normal to the normal-projected shape[i],aligned to x=forward

                    // shape[i] is a list of points along a vertical slice
                    // dirs[j] is a list of normals around a horizontal slice
                    // Create faces/box colliders centered between shape[i],shape[i+1] of desired angular radius
                    // cp is the centerpoint of the collider, positioned 1/10th of sideThickness inside the fairing outer edge.
                    Vector3 cp = (pNext + p) / 2;
                    cp -= (sideThickness * 0.1f) * n;
                    float collWidth = cp.x * Mathf.PI * 2 / (numSideParts * numColliders);
                    Vector3 size = new Vector3(collWidth, (pNext - p).magnitude, sideThickness * 0.1f);
                    // Skip the collider if adjacent points are too close.
                    if (size.y > 0.001)
                        BuildColliderRow(p, cp, n, size, numColliders, startAngle, anglePerCollider);
                }
                Profiler.EndSample();
            }
            colliderPool.ReleaseCacheToPool();
        }

        private void BuildColliderRow(Vector3 p, Vector3 cp, Vector3 normal, Vector3 size, int numColliders, float startAngle, float anglePerCollider)
        {
            for (int j = 0; j < numColliders; j++)
            {
                Profiler.BeginSample("BuildColliderSingle");
                float rotAngle = startAngle + (j * anglePerCollider);
                Quaternion RotY = Quaternion.Euler(0, -rotAngle, 0);

                BoxCollider coll = colliderPool.Acquire();

                Vector3 projectedP = new Vector3(Mathf.Cos(rotAngle * Mathf.Deg2Rad) * p.x,
                                                            p.y,
                                                            Mathf.Sin(rotAngle * Mathf.Deg2Rad) * p.x);
                Vector3 projectedCP = new Vector3(Mathf.Cos(rotAngle * Mathf.Deg2Rad) * cp.x,
                                                            cp.y,
                                                            Mathf.Sin(rotAngle * Mathf.Deg2Rad) * cp.x);

                // forward = z becomes the direction of the normal; up is direction to the next point
                coll.transform.localPosition = projectedCP;
                coll.transform.localRotation = Quaternion.LookRotation(RotY * normal, (projectedCP - projectedP).normalized);
                coll.center = Vector3.zero;
                coll.size = size;
                Profiler.EndSample();
            }
        }

        private void UpdatePartParameters(double area)
        {
            float volume = Convert.ToSingle(area * sideThickness);
            fairingMass = volume * density;
            float totalMass = ApplyModuleMassModifier(fairingMass);
            part.breakingForce = totalMass * specificBreakingForce;
            part.breakingTorque = totalMass * specificBreakingTorque;
        }

        public void rebuildMesh(bool updateDragCubes = true)
        {
            var mf = part.FindModelComponent<MeshFilter>("model");
            if (!mf)
            {
                Debug.LogError("[PF]: No model for side fairing!", part);
                return;
            }

            Mesh m = mf.mesh;
            if (!m)
            {
                Debug.LogError("[PF]: No mesh in side fairing model!", part);
                return;
            }
            Profiler.BeginSample("PF.FairingSide.RebuildMesh");
            Profiler.BeginSample("PF.FairingSide.RebuildMesh.BuildShape");

            OffsetHinge(mf);

            mf.transform.localPosition = meshPos - hingeOffset;
            mf.transform.localRotation = meshRot;

            UpdateNodeSize();

            //  Build the fairing shape line.

            float tip = maxRad * noseHeightRatio;
            baseConeShape = new Vector4(baseCurveStartX, baseCurveStartY, baseCurveEndX, baseCurveEndY);
            noseConeShape = new Vector4(noseCurveStartX, noseCurveStartY, noseCurveEndX, noseCurveEndY);
            Vector3[] shape = inlineHeight <= 0 ?
                                ProceduralFairingBase.buildFairingShape(baseRad, maxRad, cylStart, cylEnd, noseHeightRatio, baseConeShape, noseConeShape, (int)baseConeSegments, (int)noseConeSegments, vertMapping, mappingScale.y) :
                                ProceduralFairingBase.buildInlineFairingShape(baseRad, maxRad, topRad, cylStart, cylEnd, inlineHeight, baseConeShape, (int)baseConeSegments, vertMapping, mappingScale.y);

            //  Set up parameters.

            var dirs = new Vector3[numSegs + 1];
            for (int i = 0; i <= numSegs; ++i)
            {
                float a = Mathf.PI * 2 * (i - numSegs * 0.5f) / (numSideParts * numSegs);
                dirs[i] = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            }

            float segOMappingScale = (horMapping.y - horMapping.x) / (mappingScale.x * numSegs);
            float segIMappingScale = (horMapping.w - horMapping.z) / (mappingScale.x * numSegs);
            float segOMappingOfs = horMapping.x / mappingScale.x;
            float segIMappingOfs = horMapping.z / mappingScale.x;

            if (numSideParts > 2)
            {
                segOMappingOfs += segOMappingScale * numSegs * (0.5f - 1f / numSideParts);
                segOMappingScale *= 2f / numSideParts;

                segIMappingOfs += segIMappingScale * numSegs * (0.5f - 1f / numSideParts);
                segIMappingScale *= 2f / numSideParts;
            }

            float stripU0 = stripMapping.x / mappingScale.x;
            float stripU1 = stripMapping.y / mappingScale.x;

            float ringSegLen = baseRad * Mathf.PI * 2 / (numSegs * numSideParts);
            float topRingSegLen = topRad * Mathf.PI * 2 / (numSegs * numSideParts);

            int numMainVerts = (numSegs + 1) * (shape.Length - 1) + 1;
            int numMainFaces = numSegs * ((shape.Length - 2) * 2 + 1);

            int numSideVerts = shape.Length * 2;
            int numSideFaces = (shape.Length - 1) * 2;

            int numRingVerts = (numSegs + 1) * 2;
            int numRingFaces = numSegs * 2;

            if (inlineHeight > 0)
            {
                numMainVerts = (numSegs + 1) * shape.Length;
                numMainFaces = numSegs * (shape.Length - 1) * 2;
            }

            int totalVerts = numMainVerts * 2 + numSideVerts * 2 + numRingVerts;
            int totalFaces = numMainFaces * 2 + numSideFaces * 2 + numRingFaces;

            if (inlineHeight > 0)
            {
                totalVerts += numRingVerts;
                totalFaces += numRingFaces;
            }

            var p = shape[shape.Length - 1];
            float topY = p.y, topV = p.z;
            Profiler.EndSample();

            Profiler.BeginSample("PF.FairingSide.RebuildMesh.Area");

            //  Compute the area.
            double area = 0;
            for (int i = 1; i < shape.Length; ++i)
            {
                area += (shape[i - 1].x + shape[i].x) * (shape[i].y - shape[i - 1].y);
            }
            area *= Mathf.PI / numSideParts;

            UpdatePartParameters(area);
            UpdateMassAndCostDisplay();

            float anglePerPart = 360f / numSideParts;
            float x = Mathf.Cos(Mathf.Deg2Rad * anglePerPart / 2);
            Vector3 offset = new Vector3(maxRad * (1 + x) / 2, topY * 0.5f, 0);
            part.CoMOffset = part.transform.InverseTransformPoint(mf.transform.TransformPoint(offset));
            part.CoLOffset = part.CoMOffset;
            Profiler.EndSample();

            Profiler.BeginSample("PF.FairingSide.RebuildMesh.Colliders");
            RebuildColliders(shape, dirs);
            Profiler.EndSample();

            //  Build the fairing mesh.

            m.Clear();
            Profiler.BeginSample("PF.FairingSide.RebuildMesh.BuildNew");

            var verts = new Vector3[totalVerts];
            var uv = new Vector2[totalVerts];
            var norm = new Vector3[totalVerts];
            var tang = new Vector4[totalVerts];
            if (inlineHeight <= 0)
            {
                //  Tip vertex.
                // FIXME: This method of generation is incorrect; there should be one copy of the
                // tip vertex per face such that each one has the correct normal and tangent.
                // As-is, the normal is suboptimal and the tangent is flat-out incorrect. This code
                // originally set the tangent to zero (also incorrect per
                // https://docs.unity3d.com/ScriptReference/Mesh-tangents.html), but that broke TU
                // and caused the tip to appear entirely black.

                verts [numMainVerts - 1].Set (0, topY + sideThickness, 0);      //  Outside.
                verts [numMainVerts * 2 - 1].Set (0, topY, 0);                  //  Inside.

                uv [numMainVerts - 1].Set (segOMappingScale * 0.5f * numSegs + segOMappingOfs, topV);
                uv [numMainVerts * 2 - 1].Set (segIMappingScale * 0.5f * numSegs + segIMappingOfs, topV);

                norm [numMainVerts - 1] = Vector3.up;
                norm [numMainVerts * 2 - 1] = -Vector3.up;

                tang [numMainVerts - 1] = tang [numMainVerts * 2 - 1] = new Vector4(0, 0, 1, 1);
            }

            //  Main vertices.

            float noseV0 = vertMapping.z / mappingScale.y;
            float noseV1 = vertMapping.w / mappingScale.y;
            float noseVScale = 1f / (noseV1 - noseV0);
            float oCenter = (horMapping.x + horMapping.y) / (mappingScale.x * 2);
            float iCenter = (horMapping.z + horMapping.w) / (mappingScale.x * 2);

            int vi = 0;

            for (int i = 0; i < shape.Length - (inlineHeight <= 0 ? 1 : 0); ++i)
            {
                p = shape [i];

                Vector2 n;

                if (i == 0)
                {
                    n = shape [1] - shape [0];
                }
                else if (i == shape.Length - 1)
                {
                    n = shape [i] - shape [i - 1];
                }
                else
                {
                    n = shape [i + 1] - shape [i - 1];
                }

                n.Set (n.y, -n.x);

                n.Normalize ();

                for (int j = 0; j <= numSegs; ++j, ++vi)
                {
                    var d = dirs [j];

                    var dp = d * p.x + Vector3.up * p.y;
                    var dn = d * n.x + Vector3.up * n.y;

                    if (i == 0 || i == shape.Length - 1)
                    {
                        verts [vi] = dp + d * sideThickness;
                    }
                    else
                    {
                        verts [vi] = dp + dn * sideThickness;
                    }

                    verts[vi + numMainVerts] = dp;

                    float v = (p.z - noseV0) * noseVScale;
                    float uo = j * segOMappingScale + segOMappingOfs;
                    float ui = (numSegs - j) * segIMappingScale + segIMappingOfs;

                    if (v > 0 && v < 1)
                    {
                        float us = 1 - v;

                        uo = (uo - oCenter) * us + oCenter;
                        ui = (ui - iCenter) * us + iCenter;
                    }

                    uv [vi].Set (uo, p.z);

                    uv [vi + numMainVerts].Set (ui, p.z);

                    norm [vi] = dn;
                    norm [vi + numMainVerts] = -dn;

                    tang [vi].Set (-d.z, 0, d.x, 0);
                    tang [vi + numMainVerts].Set (d.z, 0, -d.x, 0);
                }
            }

            //  Side strip vertices.

            float stripScale = Mathf.Abs (stripMapping.y - stripMapping.x) / (sideThickness * mappingScale.y);

            vi = numMainVerts * 2;

            float o = 0;

            for (int i = 0; i < shape.Length; ++i, vi += 2)
            {
                int si = i * (numSegs + 1);

                var d = dirs [0];

                verts [vi] = verts [si];

                uv [vi].Set (stripU0, o);
                norm [vi].Set (d.z, 0, -d.x);

                verts [vi + 1] = verts [si + numMainVerts];
                uv [vi + 1].Set (stripU1, o);
                norm [vi + 1] = norm[vi];
                tang [vi] = tang [vi + 1] = (verts [vi + 1] - verts [vi]).normalized;

                if (i + 1 < shape.Length)
                {
                    o += ((Vector2) shape [i + 1] - (Vector2) shape [i]).magnitude * stripScale;
                }
            }

            vi += numSideVerts - 2;

            for (int i = shape.Length - 1; i >= 0; --i, vi -= 2)
            {
                int si = i * (numSegs + 1) + numSegs;

                if (i == shape.Length - 1 && inlineHeight <= 0)
                {
                    si = numMainVerts - 1;
                }

                var d = dirs [numSegs];

                verts [vi] = verts [si];
                uv [vi].Set (stripU0, o);
                norm [vi].Set (-d.z, 0, d.x);

                verts [vi + 1] = verts [si + numMainVerts];
                uv [vi + 1].Set (stripU1, o);
                norm [vi + 1] = norm [vi];
                tang [vi] = tang [vi + 1] = (verts [vi + 1] - verts [vi]).normalized;

                if (i > 0)
                {
                    o += ((Vector2) shape [i] - (Vector2) shape [i - 1]).magnitude * stripScale;
                }
            }

            //  Ring vertices.

            vi = numMainVerts * 2 + numSideVerts * 2;

            o = 0;

            for (int j = numSegs; j >= 0; --j, vi += 2, o += ringSegLen * stripScale)
            {
                verts [vi] = verts [j];
                uv [vi].Set (stripU0, o);
                norm [vi] = -Vector3.up;

                verts [vi + 1] = verts [j + numMainVerts];
                uv [vi + 1].Set (stripU1, o);
                norm [vi + 1] = -Vector3.up;
                tang [vi] = tang [vi + 1] = (verts [vi + 1] - verts [vi]).normalized;
            }

            if (inlineHeight > 0)
            {
                //  Top ring vertices.

                o = 0;

                int si = (shape.Length - 1) * (numSegs + 1);

                for (int j = 0; j <= numSegs; ++j, vi += 2, o += topRingSegLen * stripScale)
                {
                    verts [vi] = verts [si + j];
                    uv [vi].Set (stripU0, o);
                    norm [vi] = Vector3.up;

                    verts [vi + 1] = verts [si + j + numMainVerts];
                    uv [vi + 1].Set (stripU1, o);
                    norm [vi + 1] = Vector3.up;
                    tang [vi] = tang [vi + 1] = (verts [vi + 1] - verts [vi]).normalized;
                }
            }

            //  Set vertex data to mesh.

            for (int i = 0; i < totalVerts; ++i)
            {
                tang [i].w = 1;
            }

            m.vertices = verts;
            m.uv = uv;
            m.normals = norm;
            m.tangents = tang;

            m.uv2 = null;
            m.colors32 = null;

            var tri = new int [totalFaces * 3];

            //  Main faces.

            vi = 0;

            int ti1 = 0, ti2 = numMainFaces * 3;

            for (int i = 0; i < shape.Length - (inlineHeight <= 0 ? 2 : 1); ++i, ++vi)
            {
                p = shape [i];

                for (int j = 0; j < numSegs; ++j, ++vi)
                {
                    tri [ti1++] = vi;
                    tri [ti1++] = vi + 1 + numSegs + 1;
                    tri [ti1++] = vi + 1;

                    tri [ti1++] = vi;
                    tri [ti1++] = vi + numSegs + 1;
                    tri [ti1++] = vi + 1 + numSegs + 1;

                    tri [ti2++] = numMainVerts + vi;
                    tri [ti2++] = numMainVerts + vi + 1;
                    tri [ti2++] = numMainVerts + vi + 1 + numSegs + 1;

                    tri [ti2++] = numMainVerts + vi;
                    tri [ti2++] = numMainVerts + vi + 1 + numSegs + 1;
                    tri [ti2++] = numMainVerts + vi + numSegs + 1;
                }
            }

            if (inlineHeight <= 0)
            {
                //  Main tip faces.

                for (int j = 0; j < numSegs; ++j, ++vi)
                {
                    tri [ti1++] = vi;
                    tri [ti1++] = numMainVerts - 1;
                    tri [ti1++] = vi + 1;

                    tri [ti2++] = numMainVerts + vi;
                    tri [ti2++] = numMainVerts + vi + 1;
                    tri [ti2++] = numMainVerts + numMainVerts - 1;
                }
            }

            //  Side strip faces.

            vi = numMainVerts * 2;
            ti1 = numMainFaces * 2 * 3;
            ti2 = ti1 + numSideFaces * 3;

            for (int i = 0; i < shape.Length - 1; ++i, vi += 2)
            {
                tri [ti1++] = vi;
                tri [ti1++] = vi + 1;
                tri [ti1++] = vi + 3;

                tri [ti1++] = vi;
                tri [ti1++] = vi + 3;
                tri [ti1++] = vi + 2;

                tri [ti2++] = numSideVerts + vi;
                tri [ti2++] = numSideVerts + vi + 3;
                tri [ti2++] = numSideVerts + vi + 1;

                tri [ti2++] = numSideVerts + vi;
                tri [ti2++] = numSideVerts + vi + 2;
                tri [ti2++] = numSideVerts + vi + 3;
            }

            //  Ring faces.

            vi = numMainVerts * 2 + numSideVerts * 2;
            ti1 = (numMainFaces + numSideFaces) * 2 * 3;

            for (int j = 0; j < numSegs; ++j, vi += 2)
            {
                tri [ti1++] = vi;
                tri [ti1++] = vi + 1;
                tri [ti1++] = vi + 3;

                tri [ti1++] = vi;
                tri [ti1++] = vi + 3;
                tri [ti1++] = vi + 2;
            }

            if (inlineHeight > 0)
            {
                //  Top ring faces.

                vi += 2;

                for (int j = 0; j < numSegs; ++j, vi += 2)
                {
                    tri [ti1++] = vi;
                    tri [ti1++] = vi + 1;
                    tri [ti1++] = vi + 3;

                    tri [ti1++] = vi;
                    tri [ti1++] = vi + 3;
                    tri [ti1++] = vi + 2;
                }
            }

            m.triangles = tri;
            if (updateDragCubes)
                ProceduralTools.DragCubeTool.UpdateDragCubes(part);
            Profiler.EndSample();

            Profiler.EndSample();
        }
        public void OnHingeEnabledChanged(BaseField f, object obj)
        {
            UpdateMassAndCostDisplay();
            SetHingeToggles();
        }

        private void SetHingeToggles()
        {
            if (HingeAnimation is null)
                return;

            Events["TogglePetals"].guiActiveEditor = hingeEnabled;
            Events["TogglePetals"].guiActive = hingeEnabled;
            Events["TogglePetals"].guiActiveUnfocused = hingeEnabled;
            Events["TogglePetals"].guiActiveUncommand = hingeEnabled;

            HingeAnimation.Fields["deployPercent"].guiActiveEditor = hingeEnabled;
            HingeAnimation.Fields["deployPercent"].guiActive = (hingeEnabled && HighLogic.LoadedSceneIsEditor);
            HingeAnimation.Fields["deployPercent"].guiActiveUnfocused = false;

            if (!hingeEnabled && HighLogic.LoadedSceneIsFlight)
                HingeAnimation.deployPercent = 0f;  //disabling still allows action groups to activate

            MonoUtilities.RefreshPartContextWindow(part);

        }
        public IEnumerator SetOffset(Vector3 offset, float time = 0.3f)
        {
            var mf = part.FindModelComponent<MeshFilter>("model");
            var lp = mf.transform.localPosition;
            float elapsedTime = 0f;

            offset -= hingeOffset;

            while (elapsedTime < time)
            {
                mf.transform.localPosition = Vector3.Lerp(lp, offset, (elapsedTime / time));
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            mf.transform.localPosition = offset;
        }

        public void SetOffset(Vector3 offset)
        {
            var mf = part.FindModelComponent<MeshFilter>("model");
            mf.transform.localPosition = offset - hingeOffset;
        }

        private void OffsetHinge(UnityEngine.MeshFilter mf)
        {
            var mh = part.FindModelTransform("Hinge");

            if (mh != null)
            {
                hingeOffset = new Vector3(-1 * sideThickness, meshPos.y, 0f);
                mh.localPosition = hingeOffset;
            }
        }
    }

    internal class ColliderPool
    {
        private readonly Queue<BoxCollider> pool;
        private readonly Queue<BoxCollider> cache;
        private readonly HashSet<BoxCollider> ownedColliders;
        private readonly MeshFilter parent;
        private int numCreated = 0;

        internal ColliderPool(MeshFilter parent)
        {
            pool = new Queue<BoxCollider>(128);
            cache = new Queue<BoxCollider>(128);
            ownedColliders = new HashSet<BoxCollider>(128);
            this.parent = parent;
        }

        internal BoxCollider Acquire()
        {
            if (cache.TryDequeue(out BoxCollider coll))
                return coll;
            if (pool.TryDequeue(out coll))
            {
                coll.transform.SetParent(parent.transform);
                coll.gameObject.SetActive(true);
                return coll;
            }
            GameObject obj = new GameObject($"collider_{parent.name}_{numCreated}");
            coll = obj.AddComponent<BoxCollider>();
            ownedColliders.Add(coll);
            coll.transform.SetParent(parent.transform);
            numCreated++;
            return coll;
        }

        internal void Release(Collider collider)
        {
            if (collider is BoxCollider && ownedColliders.Contains(collider))
                cache.Enqueue(collider as BoxCollider);
            else
                collider.gameObject.DestroyGameObject();
        }

        internal void ReleaseCacheToPool()
        {
            while (cache.TryDequeue(out BoxCollider collider))
            {
                collider.gameObject.SetActive(false);
                collider.transform.SetParent(null);
                pool.Enqueue(collider);
            }
        }

        internal void Dispose()
        {
            ReleaseCacheToPool();
            while (pool.TryDequeue(out BoxCollider collider))
                collider.gameObject.DestroyGameObject();
        }
    }
}
