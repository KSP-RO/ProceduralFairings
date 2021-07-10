//  ==================================================
//  Procedural Fairings plug-in by Alexey Volynskov.

//  Licensed under CC-BY-4.0 terms: https://creativecommons.org/licenses/by/4.0/legalcode
//  ==================================================

using ProceduralFairings;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Keramzit
{
    public class ProceduralFairingBase : PartModule, IPartCostModifier, IPartMassModifier
    {
        public const float MaxCylinderDimension = 50;
        protected const float verticalStep = 0.1f;
        public enum BaseMode { Payload, Adapter, Plate, Other }

        [KSPField(isPersistant = true)] public string mode = Enum.GetName(typeof(BaseMode), BaseMode.Payload);
        public BaseMode Mode => (BaseMode) Enum.Parse(typeof(BaseMode), mode);

        private const float ABSOLUTE_MIN_SIZE = 0.1f;
        [KSPField] public float minSize = 1;
        [KSPField] public float maxSize = 1.5f;
        public float MaxSize() => Math.Min(maxSize, HighLogic.CurrentGame.Parameters.CustomParams<PFSettings>().maxDiameter);

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Base Size", guiFormat = "S4", guiUnits = "m", groupName = PFUtils.PAWGroup, groupDisplayName = PFUtils.PAWName)]
        [UI_FloatEdit(sigFigs = 3, unit = "m", minValue = 0.1f, maxValue = 5, incrementLarge = 1.25f, incrementSmall = 0.125f, incrementSlide = 0.001f)]
        public float baseSize = 1.25f;

        [KSPField(isPersistant = true, guiName = "Top", guiFormat = "S4", guiUnits = "m", groupName = PFUtils.PAWGroup)]
        [UI_FloatEdit(sigFigs = 3, unit = "m", minValue = 0.1f, maxValue = 5, incrementLarge = 1.25f, incrementSmall = 0.125f, incrementSlide = 0.001f)]
        public float topSize = 1.25f;

        [KSPField(isPersistant = true, guiName = "Height", guiFormat = "S4", guiUnits = "m", groupName = PFUtils.PAWGroup)]
        [UI_FloatEdit(sigFigs = 3, unit = "m", minValue = 0.1f, maxValue = 50, incrementLarge = 1.0f, incrementSmall = 0.1f, incrementSlide = 0.001f)]
        public float height = 1;

        [KSPField(isPersistant = true, guiName = "Extra height", guiFormat = "S4", guiUnits = "m", groupName = PFUtils.PAWGroup, groupDisplayName = PFUtils.PAWName)]
        [UI_FloatEdit(sigFigs = 3, unit = "m", minValue = 0, maxValue = 50, incrementLarge = 1.0f, incrementSmall = 0.1f, incrementSlide = 0.001f)]
        public float extraHeight = 0;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Extra radius", groupName = PFUtils.PAWGroup, groupDisplayName = PFUtils.PAWName)]
        [UI_FloatRange(minValue = -1, maxValue = 2, stepIncrement = 0.01f)]
        public float extraRadius;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Fairing Auto-struts", groupName = PFUtils.PAWGroup)]
        [UI_Toggle(disabledText = "Off", enabledText = "On")]
        public bool autoStrutSides = true;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Fairing Auto-shape", groupName = PFUtils.PAWGroup)]
        [UI_Toggle(disabledText = "Off", enabledText = "On")]
        public bool autoShape = true;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Max. size", guiFormat = "S4", guiUnits = "m", groupName = PFUtils.PAWGroup)]
        [UI_FloatEdit(sigFigs = 3, unit = "m", minValue = 0.1f, maxValue = 5, incrementLarge = 1.25f, incrementSmall = 0.125f, incrementSlide = 0.001f)]
        public float manualMaxSize = 0.625f;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Cyl. start", guiFormat = "S4", guiUnits = "m", groupName = PFUtils.PAWGroup)]
        [UI_FloatEdit(sigFigs = 3, unit = "m", minValue = 0, maxValue = MaxCylinderDimension, incrementLarge = 1.0f, incrementSmall = 0.1f, incrementSlide = 0.001f)]
        public float manualCylStart = 0;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Cyl. end", guiFormat = "S4", guiUnits = "m", groupName = PFUtils.PAWGroup)]
        [UI_FloatEdit(sigFigs = 3, unit = "m", minValue = 0, maxValue = MaxCylinderDimension, incrementLarge = 1.0f, incrementSmall = 0.1f, incrementSlide = 0.001f)]
        public float manualCylEnd = 1;

        [KSPField] public float diameterStepLarge = 1.25f;
        [KSPField] public float diameterStepSmall = 0.125f;

        [KSPField] public float heightStepLarge = 1.0f;
        [KSPField] public float heightStepSmall = 0.1f;

        [KSPField] public int circleSegments = 24;
        [KSPField] public float sideThickness = 0.05f;
        [KSPField] public float outlineWidth = 0.05f;
        [KSPField] public int outlineSlices = 12;
        [KSPField] public Vector4 outlineColor = new Vector4(0, 0, 0.2f, 1);

        [KSPField] public Vector4 specificMass = new Vector4(0.005f, 0.011f, 0.009f, 0f);
        [KSPField] public float costPerTonne = 2000;
        [KSPField] public float specificBreakingForce = 6050;       // Adapter
        [KSPField] public float specificBreakingTorque = 6050;      // Adapter
        //[KSPField] public float specificBreakingForce = 1536;     // Payload base
        //[KSPField] public float specificBreakingTorque = 1536;    // Payload base

        [KSPField] public float decouplerCostMult = 1;              // Mult to costPerTonne when decoupler is enabled
        [KSPField] public float decouplerCostBase = 0;              // Flat additional cost when decoupler is enabled
        [KSPField] public float decouplerMassMult = 1;              // Mass multiplier
        [KSPField] public float decouplerMassBase = 0;              // Flat additional mass (0.001 = 1kg)

        [KSPField(guiActiveEditor = true, guiName = "Mass", groupName = PFUtils.PAWGroup)]
        public string massDisplay;

        [KSPField(guiActiveEditor = true, guiName = "Cost", groupName = PFUtils.PAWGroup)]
        public string costDisplay;

        [KSPField(isPersistant = true, guiName = "Decouple When Fairing Gone:", groupName = PFUtils.PAWGroup, groupDisplayName = PFUtils.PAWName)]
        [UI_Toggle(disabledText = "No", enabledText = "Yes")]
        public bool autoDecoupleTopNode;

        [KSPField] public string topNodeName = "top1";

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Interstage Nodes", groupName = PFUtils.PAWGroup)]
        [UI_Toggle(disabledText = "Off", enabledText = "On")]
        public bool showInterstageNodes = true;

        [UI_Toggle(scene = UI_Scene.Editor, disabledText = "Closed", enabledText = "Open", suppressEditorShipModified = true)]
        [KSPField(guiActiveEditor = true, guiName = "Fairing", groupName = PFUtils.PAWGroup)]
        public bool openFairing = false;

        float fairingBaseMass = 0;

        public bool needShapeUpdate = true;
        private LineRenderer line;
        private TextMeshPro decouplerHint;
        private TextMeshPro nonDecouplerHint;
        readonly List<LineRenderer> outline = new List<LineRenderer>();
        readonly List<ConfigurableJoint> joints = new List<ConfigurableJoint>();
        public DragCubeUpdater dragCubeUpdater;
        public ModuleDecouple Decoupler;
        private readonly List<ProceduralFairingSide> sides = new List<ProceduralFairingSide>();
        public Vector3 editorOpenOffset => new Vector3(minOffset, 0, 0);

        public const float openSpeed = 0.3f;
        private const float minOffset = 2f;
        
        public void OnToggleOpen(BaseField field, object oldValue) => UpdateOpen();

        float lastBaseSize = -1000;
        float lastTopSize = -1000;
        float lastHeight = -1000;
        float lastExtraHt = -1000;
        bool lastDecouplerStaged = false;

        [KSPField] public bool requestLegacyLoad;

        public int TopNodeSize => Mathf.RoundToInt((Mode == BaseMode.Adapter ? topSize : baseSize) / diameterStepLarge);
        public int BottomNodeSize => Mathf.RoundToInt(baseSize / diameterStepLarge);
        public int InterstageNodeSize => Math.Max(1, TopNodeSize - 1);
        public int FairingBaseNodeSize => Math.Max(1, TopNodeSize - 1);
        public float CalcSideThickness() => Mode == BaseMode.Adapter ? Mathf.Min(sideThickness * Mathf.Max(baseSize, topSize), 0.25f * Mathf.Min(baseSize, topSize))
                                            : Mode == BaseMode.Payload ? baseSize * Mathf.Min(sideThickness, 0.25f)
                                            : 0;

        public ModifierChangeWhen GetModuleCostChangeWhen() => ModifierChangeWhen.FIXED;
        public ModifierChangeWhen GetModuleMassChangeWhen() => ModifierChangeWhen.FIXED;
        public float GetModuleCost(float defcost, ModifierStagingSituation sit) => ApplyDecouplerCostModifier(fairingBaseMass * costPerTonne) - defcost;
        public float GetModuleMass(float defmass, ModifierStagingSituation sit) => ApplyDecouplerMassModifier(fairingBaseMass) - defmass;
        private float ApplyDecouplerCostModifier(float baseCost) => DecouplerEnabled ? (baseCost * decouplerCostMult) + decouplerCostBase : baseCost;
        private float ApplyDecouplerMassModifier(float baseMass) => DecouplerEnabled ? (baseMass * decouplerMassMult) + decouplerMassBase : baseMass;
        private bool DecouplerEnabled => part.FindModuleImplementing<ModuleDecouple>() is ModuleDecouple d && d.stagingEnabled;
        public override string GetInfo() => "Attach side fairings and they will be shaped for your attached payload.\nRemember to enable the decoupler if you need one.";

        #region KSP Common Callbacks

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (node.name != "CURRENTUPGRADE")
                requestLegacyLoad = !node.HasValue(nameof(mode));
        }

        public override void OnAwake()
        {
            base.OnAwake();

            if (HighLogic.LoadedSceneIsEditor)
            {
                GameEvents.onEditorPartEvent.Add(OnEditorPartEvent);
                InitializeHintTexts();
            }
        }

        public override void OnStart (StartState state)
        {
            dragCubeUpdater = new DragCubeUpdater(part);
            Decoupler = part.FindModuleImplementing<ModuleDecouple>();

            if (requestLegacyLoad)
                LegacyLoad();

            if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight) return;

            PFUtils.hideDragStuff(part);
            if (HighLogic.LoadedSceneIsEditor)
            {
                ConfigureTechLimits();
                if (line)
                    line.transform.Rotate (0, 90, 0);

                DestroyAllLineRenderers();
                DestroyOutline();
                if (Mode == BaseMode.Adapter || Mode == BaseMode.Payload)
                    InitializeFairingOutline(outlineSlices, outlineColor, outlineWidth);

                SetUIChangedCallBacks();
                SetUIFieldVisibility();
                SetUIFieldLimits();
                GameEvents.onPartAttach.Add(OnPartAttach);
                GameEvents.onPartRemove.Add(OnPartRemove);
                GameEvents.onVariantApplied.Add(OnPartVariantApplied);
                GameEvents.onEditorShipModified.Add(OnEditorShipModified);

                StartCoroutine(EditorChangeDetector());
            }
            else
            {
                GameEvents.onVesselWasModified.Add(OnVesselModified);
            }
            lastBaseSize = baseSize;
            lastExtraHt = extraHeight;
            lastTopSize = topSize;
            lastHeight = height;
            lastDecouplerStaged = (Decoupler && Decoupler.staged);
        }

        public override void OnStartFinished(StartState state) 
        {
            base.OnStartFinished(state);
            // Don't update drag cubes before a part has been attached in the editor.
            UpdatePartProperties();
            UpdateNodes(false);
            if (!HighLogic.LoadedSceneIsEditor)
                dragCubeUpdater.Update();
            if (HighLogic.LoadedSceneIsEditor)
                ShowHideInterstageNodes();
            if (Mode == BaseMode.Adapter)
                StartCoroutine(HandleAutomaticDecoupling());
        }

        public void OnDestroy()
        {
            GameEvents.onPartAttach.Remove(OnPartAttach);
            GameEvents.onPartRemove.Remove(OnPartRemove);
            GameEvents.onVariantApplied.Remove(OnPartVariantApplied);
            GameEvents.onEditorPartEvent.Remove(OnEditorPartEvent);
            GameEvents.onVesselWasModified.Remove(OnVesselModified);
            GameEvents.onEditorShipModified.Remove(OnEditorShipModified);

            if (line)
            {
                Destroy(line.gameObject);
                line = null;
            }
            DestroyAllLineRenderers();
            DestroyOutline();

            decouplerHint?.gameObject.DestroyGameObject();
            nonDecouplerHint?.gameObject.DestroyGameObject();
        }
        #endregion

        #region Event Callbacks
        private void OnPartVariantApplied(Part p, PartVariant variant)
        {
            if (p == part)
                StartCoroutine(OnPartVariantAppliedCR());
        }

        private System.Collections.IEnumerator OnPartVariantAppliedCR()
        {
            yield return new WaitForFixedUpdate();
            if (HighLogic.LoadedSceneIsFlight)
                UpdateNodes(false);
            else
                UpdateShape(false);
            // PartVariants will only push nodes based on the delta in node.originalPosition
            // If we keep originalPosition the same, we won't need to re-adjust parts.
            // NodeNumberTweaker will NOT like that.
            // But UpdateShape() will regenerate the side fairings and move their mesh?
        }

        private void OnEditorShipModified(ShipConstruct ship)
        {
            needShapeUpdate = true;
            if (Mode == BaseMode.Adapter || Mode == BaseMode.Payload)
                StartCoroutine(DisplayFairingOutline());
        }

        public void OnPartPack() => RemoveJoints();

        public void onShieldingDisabled(List<Part> shieldedParts) => RemoveJoints();

        public void onShieldingEnabled(List<Part> shieldedParts)
        {
            if (HighLogic.LoadedSceneIsFlight && autoStrutSides)
                StartCoroutine(CreateAutoStruts());
        }

        void OnPartAttach(GameEvents.HostTargetAction<Part, Part> action)
        {
            // On loading any craft, the sideFairing knows its shape already.
            // Thus only need to do this when our attachment state will change.
            needShapeUpdate = HighLogic.LoadedSceneIsEditor;

            if (action.host == part || action.target == part)
                ToggleNodeHints(false);
        }

        void OnPartRemove(GameEvents.HostTargetAction<Part, Part> action)
        {
            needShapeUpdate = HighLogic.LoadedSceneIsEditor;
            if (Mode == BaseMode.Adapter || Mode == BaseMode.Payload)
                StartCoroutine(DisplayFairingOutline());

            if (action.host == part || action.target == part)
                ToggleNodeHints(true);
        }

        void OnVesselModified(Vessel v)
        {
            if (vessel == v && !part.packed && Mode == BaseMode.Adapter)
            {
                if (GetTopPart() == null)
                    RemoveJoints();
                StartCoroutine(HandleAutomaticDecoupling());
            }
        }

        private void OnEditorPartEvent(ConstructionEventType type, Part part)
        {
            if (type == ConstructionEventType.PartCreated && this.part == part)
                ToggleNodeHints(true);
        }
        #endregion

        #region UI

        void SetUIChangedCallBacks()
        {
            Fields[nameof(autoShape)].uiControlEditor.onFieldChanged += OnChangeAutoshapeUI;
            Fields[nameof(autoShape)].uiControlEditor.onSymmetryFieldChanged += OnChangeAutoshapeUI;
            
            Fields[nameof(extraRadius)].uiControlEditor.onFieldChanged += OnChangeShapeUI;
            Fields[nameof(extraRadius)].uiControlEditor.onSymmetryFieldChanged += OnChangeShapeUI;
            Fields[nameof(manualMaxSize)].uiControlEditor.onFieldChanged += OnChangeShapeUI;
            Fields[nameof(manualMaxSize)].uiControlEditor.onSymmetryFieldChanged += OnChangeShapeUI;
            Fields[nameof(manualCylStart)].uiControlEditor.onFieldChanged += OnChangeShapeUI;
            Fields[nameof(manualCylStart)].uiControlEditor.onSymmetryFieldChanged += OnChangeShapeUI;
            Fields[nameof(manualCylEnd)].uiControlEditor.onFieldChanged += OnChangeShapeUI;
            Fields[nameof(manualCylEnd)].uiControlEditor.onSymmetryFieldChanged += OnChangeShapeUI;

            Fields[nameof(baseSize)].uiControlEditor.onFieldChanged += OnBaseSizeChanged;
            Fields[nameof(baseSize)].uiControlEditor.onSymmetryFieldChanged += OnBaseSizeChanged;

            Fields[nameof(topSize)].uiControlEditor.onFieldChanged += OnTopSizeChanged;
            Fields[nameof(topSize)].uiControlEditor.onSymmetryFieldChanged += OnTopSizeChanged;

            Fields[nameof(height)].uiControlEditor.onFieldChanged += OnHeightChanged;
            Fields[nameof(height)].uiControlEditor.onSymmetryFieldChanged += OnHeightChanged;

            Fields[nameof(extraHeight)].uiControlEditor.onFieldChanged += OnExtraHeightChanged;
            Fields[nameof(extraHeight)].uiControlEditor.onSymmetryFieldChanged += OnExtraHeightChanged;

            Fields[nameof(showInterstageNodes)].uiControlEditor.onFieldChanged += OnNodeVisibilityChanged;
            Fields[nameof(showInterstageNodes)].uiControlEditor.onSymmetryFieldChanged += OnNodeVisibilityChanged;
            
            Fields[nameof(openFairing)].uiControlEditor.onFieldChanged += OnToggleOpen;
            Fields[nameof(openFairing)].uiControlEditor.onSymmetryFieldChanged += OnToggleOpen;
        }

        void SetUIFieldVisibility()
        {
            Fields[nameof(manualMaxSize)].guiActiveEditor = !autoShape;
            Fields[nameof(manualCylStart)].guiActiveEditor = !autoShape;
            Fields[nameof(manualCylEnd)].guiActiveEditor = !autoShape;
            Fields[nameof(topSize)].guiActiveEditor = Mode == BaseMode.Adapter;
            Fields[nameof(height)].guiActiveEditor = Mode == BaseMode.Adapter;
            Fields[nameof(extraHeight)].guiActiveEditor = Mode == BaseMode.Adapter;
            Fields[nameof(autoDecoupleTopNode)].guiActive = Mode == BaseMode.Adapter;
            Fields[nameof(autoDecoupleTopNode)].guiActiveEditor = Mode == BaseMode.Adapter;
            Fields[nameof(showInterstageNodes)].guiActiveEditor = part.FindAttachNodes("interstage") != null;
            Fields[nameof(extraRadius)].guiActiveEditor = Mode == BaseMode.Adapter || Mode == BaseMode.Payload;
            Fields[nameof(autoStrutSides)].guiActiveEditor = Mode == BaseMode.Adapter || Mode == BaseMode.Payload;
            Fields[nameof(autoShape)].guiActiveEditor = Mode == BaseMode.Adapter || Mode == BaseMode.Payload;
        }

        private void SetUIFieldLimits()
        {
            UI_FloatEdit start = Fields[nameof(manualCylStart)].uiControlEditor as UI_FloatEdit;
            UI_FloatEdit end = Fields[nameof(manualCylEnd)].uiControlEditor as UI_FloatEdit;
            start.maxValue = Mathf.Min(manualCylEnd, MaxCylinderDimension - 0.1f);
            end.minValue = Mathf.Min(manualCylStart, MaxCylinderDimension - 0.1f);
            bool refresh = manualCylStart > start.maxValue || manualCylEnd < end.minValue;
            manualCylStart = Mathf.Min(manualCylStart, start.maxValue);
            manualCylEnd = Mathf.Max(manualCylEnd, end.minValue);
            if (refresh)
                MonoUtilities.RefreshPartContextWindow(part);
        }

        public void OnBaseSizeChanged(BaseField f, object obj)
        {
            if (baseSize != lastBaseSize)
                UpdateShape(true);
            lastBaseSize = baseSize;
        }

        public void OnTopSizeChanged(BaseField f, object obj)
        {
            if (topSize != lastTopSize)
                UpdateShape(true);
            lastTopSize = topSize;
        }

        public void OnHeightChanged(BaseField f, object obj)
        {
            if (height != lastHeight)
                UpdateShape(true);
            lastHeight = height;
        }

        public void OnExtraHeightChanged(BaseField f, object obj)
        {
            if (extraHeight != lastExtraHt)
                UpdateShape(true);
            lastExtraHt = extraHeight;
        }

        void OnChangeAutoshapeUI(BaseField bf, object obj)
        {
            SetUIFieldVisibility();
            UpdateShape(true);
        }

        void OnChangeShapeUI(BaseField bf, object obj) => UpdateShape(true);
        public void OnNodeVisibilityChanged(BaseField f, object obj) => ShowHideInterstageNodes();

        public void ConfigureTechLimits()
        {
            float minSize = Math.Max(this.minSize, ABSOLUTE_MIN_SIZE);
            float maxSize = MaxSize();

            PFUtils.setFieldRange(Fields[nameof(baseSize)], minSize, maxSize);
            (Fields[nameof(baseSize)].uiControlEditor as UI_FloatEdit).incrementLarge = diameterStepLarge;
            (Fields[nameof(baseSize)].uiControlEditor as UI_FloatEdit).incrementSmall = diameterStepSmall;

            PFUtils.setFieldRange(Fields[nameof(manualMaxSize)], minSize, maxSize * 2);
            (Fields[nameof(manualMaxSize)].uiControlEditor as UI_FloatEdit).incrementLarge = diameterStepLarge;
            (Fields[nameof(manualMaxSize)].uiControlEditor as UI_FloatEdit).incrementSmall = diameterStepSmall;

            (Fields[nameof(manualCylStart)].uiControlEditor as UI_FloatEdit).incrementLarge = heightStepLarge;
            (Fields[nameof(manualCylStart)].uiControlEditor as UI_FloatEdit).incrementSmall = heightStepSmall;
            (Fields[nameof(manualCylEnd)].uiControlEditor as UI_FloatEdit).incrementLarge = heightStepLarge;
            (Fields[nameof(manualCylEnd)].uiControlEditor as UI_FloatEdit).incrementSmall = heightStepSmall;

            (Fields[nameof(extraHeight)].uiControlEditor as UI_FloatEdit).incrementLarge = heightStepLarge;
            (Fields[nameof(extraHeight)].uiControlEditor as UI_FloatEdit).incrementSmall = heightStepSmall;

            PFUtils.setFieldRange(Fields[nameof(topSize)], minSize, maxSize);

            (Fields[nameof(topSize)].uiControlEditor as UI_FloatEdit).incrementLarge = diameterStepLarge;
            (Fields[nameof(topSize)].uiControlEditor as UI_FloatEdit).incrementSmall = diameterStepSmall;

            (Fields[nameof(height)].uiControlEditor as UI_FloatEdit).incrementLarge = heightStepLarge;
            (Fields[nameof(height)].uiControlEditor as UI_FloatEdit).incrementSmall = heightStepSmall;
        }

        private System.Collections.IEnumerator EditorChangeDetector()
        {
            while (HighLogic.LoadedSceneIsEditor)
            {
                yield return new WaitForFixedUpdate();
                if (needShapeUpdate)
                    UpdateShape(true);
                needShapeUpdate = false;

                if (baseSize != lastBaseSize)
                    Fields[nameof(baseSize)].uiControlEditor.onFieldChanged.Invoke(Fields[nameof(baseSize)], lastBaseSize);
                if (topSize != lastTopSize)
                    Fields[nameof(topSize)].uiControlEditor.onFieldChanged.Invoke(Fields[nameof(topSize)], lastTopSize);
                if (height != lastHeight)
                    Fields[nameof(height)].uiControlEditor.onFieldChanged.Invoke(Fields[nameof(height)], lastHeight);
                if (extraHeight != lastExtraHt)
                    Fields[nameof(extraHeight)].uiControlEditor.onFieldChanged.Invoke(Fields[nameof(extraHeight)], lastExtraHt);
                if (Decoupler && lastDecouplerStaged != Decoupler.stagingEnabled)
                {
                    UpdateMassAndCostDisplay();
                    lastDecouplerStaged = Decoupler.stagingEnabled;
                }
            }
        }

        #endregion

        #region Shape Updaters

        public void UpdateShape(bool pushAttachments)
        {
            SetUIFieldLimits();
            UpdatePartProperties();
            UpdateNodes(pushAttachments);
            if (!HighLogic.LoadedSceneIsFlight)
                recalcShape();
            dragCubeUpdater.Update();
            UpdateFairingSideDragCubes();
        }

        public List<ProceduralFairingSide> GetFairingSides(Part p) =>
            p.FindAttachNodes("connect")
             .Where(x => x.attachedPart is Part sp && sp.GetComponent<ProceduralFairingSide>() is ProceduralFairingSide)
             .Select(y => y.attachedPart.GetComponent<ProceduralFairingSide>()).ToList();

        public void UpdateFairingSideDragCubes()
        {
            foreach (var p in GetFairingSides(part))
                p.dragCubeUpdater?.Update();
        }
        
        public void UpdateOpen()
        {
            var offsetAmount = Vector3.zero;
            if (openFairing)
                offsetAmount = editorOpenOffset;
            foreach (var side in GetFairingSides(part))
            {
                StartCoroutine(side.SetOffset(side.meshPos + offsetAmount, openSpeed));
            }
        }

        public void UpdatePartProperties()
        {
            float baseDiameterAdj = baseSize - (2 * CalcSideThickness());
            float baseRadiusAdj = baseDiameterAdj / 2;

            part.breakingForce = specificBreakingForce * Mathf.Pow(baseRadiusAdj, 2);
            part.breakingTorque = specificBreakingTorque * Mathf.Pow(baseRadiusAdj, 2);
            UpdateMassAndCostDisplay();

            if (part.FindModelTransform("model") is Transform model)
                model.localScale = Vector3.one * baseDiameterAdj;
            else
                Debug.LogError("[PF]: No 'model' transform found in part!", this);
            part.rescaleFactor = baseDiameterAdj;
        }

        public void UpdateMassAndCostDisplay()
        {
            float baseDiameterAdj = baseSize - (2 * CalcSideThickness());
            fairingBaseMass = (((((specificMass.x * baseDiameterAdj) + specificMass.y) * baseDiameterAdj) + specificMass.z) * baseDiameterAdj) + specificMass.w;
            massDisplay = PFUtils.formatMass(ApplyDecouplerMassModifier(fairingBaseMass));
            costDisplay = PFUtils.formatCost(part.partInfo.cost + GetModuleCost(part.partInfo.cost, ModifierStagingSituation.CURRENT));
        }

        // Sub-functions of UpdateShape()
        public void UpdateNodes(bool pushAttachments)
        {
            float baseDiameterAdj = baseSize - (2 * CalcSideThickness());
            float baseRadiusAdj = baseDiameterAdj / 2;

            float topHeight = 0, bottomHeight = 0;

            if (part.FindAttachNode("top") is AttachNode baseTopNode)
            {
                UpdateNode(baseTopNode, baseTopNode.originalPosition * baseDiameterAdj, TopNodeSize, pushAttachments);
                UpdateHintPosForNode(baseTopNode, nonDecouplerHint, isDecoupleableNode: false);
                topHeight = baseTopNode.position.y;
            }
            if (part.FindAttachNode("bottom") is AttachNode bottomNode &&
                part.FindAttachNodes("bottom") is AttachNode[] nodes)
            {
                UpdateNode(bottomNode, bottomNode.originalPosition * baseDiameterAdj, BottomNodeSize, pushAttachments);
                bottomHeight = bottomNode.position.y;

                // For thrust plate with list of nodes bottom01-bottom16
                foreach (AttachNode n in nodes.Where(x => x != bottomNode))
                {
                    Vector3 newPos = new Vector3(n.position.x, bottomNode.position.y, n.position.z);
                    UpdateNode(n, newPos, bottomNode.size, pushAttachments);
                }
            }
            if (Mode == BaseMode.Adapter)
            {
                if (part.FindAttachNode(topNodeName) is AttachNode topNode)
                {
                    Vector3 newPos = new Vector3(topNode.position.x, height, topNode.position.z);
                    UpdateNode(topNode, newPos, TopNodeSize, pushAttachments);
                    UpdateHintPosForNode(topNode, decouplerHint, isDecoupleableNode: true);
                }
                else
                    Debug.LogError($"[PF]: No '{topNodeName}' node in part {part}!");
            }

            // Adopt Resize interstage handling
            // ProcAdapter forced incrementing N nodes within height - topHeight.
            // Change ProcAdapter-type parts to have interstage nodes with height < 1.
            if (part.FindAttachNodes("interstage") is AttachNode[] internodes)
            {
                float nodeScale = Mode == BaseMode.Adapter ? height - topHeight : baseDiameterAdj;
                foreach (AttachNode node in internodes)
                {
                    // Show/Hide hack offsets the node.position by large x/z.  When moving here, only update the y coordinate!
                    Vector3 newPos = node.position;
                    newPos.y = ((node.originalPosition * nodeScale) + (Vector3.up * bottomHeight)).y;
                    UpdateNode(node, newPos, InterstageNodeSize, pushAttachments);
                }
                ShowHideInterstageNodes();
            }

            // Let the NumberNodeTweaker push the connect and/or bottom nodes.
            if (part.FindModuleImplementing<KzNodeNumberTweaker>() is KzNodeNumberTweaker nnt)
            {
                if (Mode == BaseMode.Plate)
                {
                    nnt.SetRadius(Math.Min(nnt.radius, baseSize / 2), pushAttachments);
                    (nnt.Fields[nameof(nnt.radius)].uiControlEditor as UI_FloatEdit).maxValue = baseSize / 2;
                } else
                    nnt.SetRadius(baseRadiusAdj + CalcSideThickness(), pushAttachments);
            }

            // NNT won't know to adjust position.y, so fix-up.
            // (Order doesn't matter here, but we're doing two translations.)

            float y = (topHeight + bottomHeight) / 2;
            if (part.FindAttachNodes("connect") is AttachNode[] fairingSideNodes)
            {
                foreach (AttachNode n in fairingSideNodes)
                {
                    Vector3 newPos = new Vector3(n.position.x, y, n.position.z);
                    UpdateNode(n, newPos, FairingBaseNodeSize, pushAttachments);
                }
            }
        }

        public void UpdateNode(AttachNode node, Vector3 newPosition, int size, bool pushAttachments, float attachDiameter = 0)
        {
            // Typically the node size is scaled by diameterStepLarge, so just un-scale it for the notification.
            attachDiameter = (attachDiameter > 0) ? attachDiameter : size / diameterStepLarge;
            PFUtils.UpdateNode(part, node, newPosition, size, pushAttachments, attachDiameter);
        }

        private void UpdateHintPosForNode(AttachNode node, TextMeshPro hintObj, bool isDecoupleableNode)
        {
            if (!HighLogic.LoadedSceneIsEditor) return;

            Vector3 hintPos = node.position;
            float horizTextOffset = node.radius / 2 + hintObj.renderedWidth / 2;
            horizTextOffset *= isDecoupleableNode ? 1 : -1;    // move text to the left for non-decoupleable nodes
            var offset = new Vector3(horizTextOffset, 0, 0);

            hintPos += hintObj.transform.rotation * offset;
            hintObj.transform.localPosition = hintPos;
        }

        #endregion

        private void LegacyLoad()
        {
            ProceduralFairingAdapter adapter = part.FindModuleImplementing<ProceduralFairingAdapter>();
            mode = adapter ? Enum.GetName(typeof(BaseMode), BaseMode.Adapter) : Enum.GetName(typeof(BaseMode), BaseMode.Payload);
            Debug.Log($"[PF] LegacyLoad() for {part}, Mode: {Mode}, skipping");
        }

        public System.Collections.IEnumerator HandleAutomaticDecoupling()
        {
            //  We used to remove the engine fairing (if there is any) from topmost node, but lately that's been causing NREs.  
            //  Since KSP gives us this option nativley, let's just use KSP to do that if we want.
            if (!HighLogic.LoadedSceneIsFlight) yield break;
            yield return new WaitForFixedUpdate();

            if (TopNodePartPresent && autoDecoupleTopNode && !FairingPresent)
            {
                if (part.FindModuleImplementing<ModuleDecouple>() is ModuleDecouple item)
                {
                    RemoveTopPartJoints();
                    item.Decouple();
                }
                else
                {
                    Debug.LogError($"[PF]: Cannot decouple from top part! {this}");
                }
            }
            Fields[nameof(autoDecoupleTopNode)].guiActive = Mode == BaseMode.Adapter && TopNodePartPresent;
        }

        #region Node / Attached Part Utilities

        public Part GetBottomPart() => (part.FindAttachNode("bottom") is AttachNode node) ? node.attachedPart : null;
        public Part GetTopPart() => (part.FindAttachNode(topNodeName) is AttachNode node) ? node.attachedPart : null;
        public bool TopNodePartPresent => GetTopPart() is Part;
        public bool FairingPresent
        {
            get
            {
                if (part.FindAttachNodes("connect") is AttachNode[] nodes)
                {
                    foreach (AttachNode n in nodes)
                    {
                        if (n.attachedPart is Part p && p.FindModuleImplementing<ProceduralFairingSide>() is ProceduralFairingSide)
                            return true;
                    }
                }
                return false;
            }
        }
        private Part FindTopBasePart()
        {
            Part top = null;
            if (Mode == BaseMode.Adapter)
            {
                top = GetTopPart();
            }
            else if (Mode == BaseMode.Payload)
            {
                var scan = ScanPayload();
                if (scan.targets.Count > 0)
                    top = scan.targets[0];
            }
            return top;
        }

        private bool HasTopOrSideNode()
        {
            if (Mode == BaseMode.Payload && ScanPayload() is PayloadScan scan && scan.targets.Count > 0)
                return true;
            if (HasNodeComponent<ProceduralFairingSide>(part.FindAttachNodes("connect")) is AttachNode)
                return true;
            return false;
        }

        void SetNodeVisibility(AttachNode node, bool show) => node.position.x = show ? 0 : 10000;

        public void ShowHideInterstageNodes()
        {
            if (part.FindAttachNodes("interstage") is AttachNode[] nodes)
            {
                foreach (AttachNode node in nodes)
                {
                    if (node.attachedPart == null)
                        SetNodeVisibility(node, showInterstageNodes);
                }
            }
        }

        #endregion

        #region Struts and Joints

        private void RemoveJoints()
        {
            foreach (ConfigurableJoint joint in joints)
                Destroy(joint);
            joints.Clear();
        }

        void RemoveTopPartJoints()
        {
            if (GetTopPart() is Part topPart && GetBottomPart() is Part bottomPart &&
                topPart.gameObject.GetComponents<ConfigurableJoint>() is ConfigurableJoint[] components)
            {
                foreach (ConfigurableJoint configurableJoint in components)
                {
                    if (configurableJoint.connectedBody == bottomPart.Rigidbody)
                        Destroy(configurableJoint);
                }
            }
        }

        private IEnumerator<YieldInstruction> CreateAutoStruts()
        {
            while (!FlightGlobals.ready || vessel.packed || !vessel.loaded)
            {
                yield return new WaitForFixedUpdate();
            }
            if (part.GetComponent<KzNodeNumberTweaker>() is KzNodeNumberTweaker nnt &&
                part.FindAttachNodes("connect") is AttachNode[] attached)
            {
                Part topBasePart = FindTopBasePart();
                for (int i = 0; i < nnt.numNodes; ++i)
                {
                    if (attached[i].attachedPart is Part p && p.Rigidbody &&
                        attached[i > 0 ? i - 1 : nnt.numNodes - 1].attachedPart is Part pp)
                    {
                        AddStrut(p, pp);
                        if (topBasePart != null)
                            AddStrut(p, topBasePart);
                    }
                }
            }
        }

        private ConfigurableJoint AddStrut(Part p, Part pp)
        {
            if (p && p != pp && p.Rigidbody != pp.Rigidbody && pp.Rigidbody is Rigidbody rb &&
                p.gameObject.AddComponent<ConfigurableJoint>() is ConfigurableJoint joint)
            {
                joint.xMotion = ConfigurableJointMotion.Locked;
                joint.yMotion = ConfigurableJointMotion.Locked;
                joint.zMotion = ConfigurableJointMotion.Locked;
                joint.angularXMotion = ConfigurableJointMotion.Locked;
                joint.angularYMotion = ConfigurableJointMotion.Locked;
                joint.angularZMotion = ConfigurableJointMotion.Locked;
                joint.projectionDistance = 0.1f;
                joint.projectionAngle = 5;
                joint.breakForce = p.breakingForce;
                joint.breakTorque = p.breakingTorque;
                joint.connectedBody = rb;

                joints.Add(joint);
                return joint;
            }
            return null;
        }

        #endregion

        #region LineRenderers

        LineRenderer MakeLineRenderer(string gameObjectName, Color color, float wd)
        {
            var o = new GameObject (gameObjectName);

            o.transform.parent = part.transform;
            o.transform.localPosition = Vector3.zero;
            o.transform.localRotation = Quaternion.identity;

            var lineRenderer = o.AddComponent<LineRenderer>();

            lineRenderer.useWorldSpace = false;
            lineRenderer.material = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.startWidth = wd;
            lineRenderer.endWidth = wd;
            lineRenderer.positionCount = 0;

            return lineRenderer;
        }

        void DestroyOutline()
        {
            foreach (LineRenderer r in outline)
                Destroy(r.gameObject);
            outline.Clear();
        }

        /// <summary>
        /// Fix for the blue ghost lines showing invalid outlines when cloning or symmetry-placing fairing bases in the editor.
        /// Find any already assigned (copied) LineRenderers and delete them.
        /// </summary>

        void DestroyAllLineRenderers()
        {
            foreach (LineRenderer r in FindObjectsOfType<LineRenderer>())
            {
                if (r?.transform?.parent?.gameObject is GameObject go &&
                    (go == this || go == this.gameObject))
                {
                    Destroy(r.gameObject);
                }
            }
        }

        #endregion

        #region Fairing Shapes and Outline
        private void InitializeFairingOutline(int slices, Vector4 color, float width)
        {
            for (int i = 0; i < slices; ++i)
            {
                var r = MakeLineRenderer("fairing outline", color, width);
                outline.Add(r);
                r.transform.Rotate(0, i * 360f / slices, 0);
            }
        }

        private void InitializeHintTexts()
        {
            var rotToCamera = new Vector3(0f, Camera.main.transform.rotation.eulerAngles.y, 0f);
            if (decouplerHint == null)
            {
                decouplerHint = InitializeHint("attach-node-hint1", "<- This node decouples", rotToCamera, part.transform);
                decouplerHint.alignment = TextAlignmentOptions.Center;
            }

            if (nonDecouplerHint == null)
            {
                nonDecouplerHint = InitializeHint("attach-node-hint2", "This does not decouple ->", rotToCamera, part.transform);
                nonDecouplerHint.alignment = TextAlignmentOptions.Baseline;
            }
        }

        private TextMeshPro InitializeHint(string name, string text, Vector3 rotToCamera, Transform parent)
        {
            var o = new GameObject(name);
            o.transform.localPosition = Vector3.zero;
            o.transform.localRotation = Quaternion.identity;

            var hint = o.AddComponent<TextMeshPro>();
            hint.SetText(text);
            hint.color = Color.green;
            hint.alpha = 0.75f;
            hint.fontSize = 2;
            //hint.alignment = TextAlignmentOptions.Center;
            hint.enabled = false;
            hint.gameObject.transform.SetParent(parent, false);
            hint.gameObject.transform.eulerAngles = rotToCamera;
            return hint;
        }

        static public Vector3 [] buildFairingShape (float baseRad, float maxRad, float cylStart, float cylEnd, float noseHeightRatio, Vector4 baseConeShape, Vector4 noseConeShape, int baseConeSegments, int noseConeSegments, Vector4 vertMapping, float mappingScaleY)
        {
            float baseConeRad = maxRad - baseRad;
            float tip = maxRad * noseHeightRatio;

            var baseSlope = new BezierSlope (baseConeShape);
            var noseSlope = new BezierSlope (noseConeShape);

            float baseV0 = vertMapping.x / mappingScaleY;
            float baseV1 = vertMapping.y / mappingScaleY;
            float noseV0 = vertMapping.z / mappingScaleY;
            float noseV1 = vertMapping.w / mappingScaleY;

            var shape = new Vector3 [1 + (cylStart.Equals (0) ? 0 : baseConeSegments) + 1 + noseConeSegments];

            int vi = 0;

            if (!cylStart.Equals (0))
            {
                for (int i = 0; i <= baseConeSegments; ++i, ++vi)
                {
                    float t = (float) i / baseConeSegments;

                    var p = baseSlope.interp (t);

                    shape [vi] = new Vector3 (p.x * baseConeRad + baseRad, p.y * cylStart, Mathf.Lerp (baseV0, baseV1, t));
                }
            }
            else
            {
                shape [vi++] = new Vector3 (baseRad, 0, baseV1);
            }

            for (int i = 0; i <= noseConeSegments; ++i, ++vi)
            {
                float t = (float) i / noseConeSegments;

                var p = noseSlope.interp (1 - t);

                shape [vi] = new Vector3 (p.x * maxRad, (1 - p.y) * tip + cylEnd, Mathf.Lerp (noseV0, noseV1, t));
            }

            return shape;
        }

        static public Vector3 [] buildInlineFairingShape (float baseRad, float maxRad, float topRad, float cylStart, float cylEnd, float top, Vector4 baseConeShape, int baseConeSegments, Vector4 vertMapping, float mappingScaleY)
        {
            float baseConeRad = maxRad - baseRad;
            float topConeRad = maxRad - topRad;

            var baseSlope = new BezierSlope (baseConeShape);

            float baseV0 = vertMapping.x / mappingScaleY;
            float baseV1 = vertMapping.y / mappingScaleY;
            float noseV0 = vertMapping.z / mappingScaleY;

            var shape = new Vector3 [2 + (cylStart.Equals (0) ? 0 : baseConeSegments + 1) + (cylEnd.Equals (top) ? 0 : baseConeSegments + 1)];

            int vi = 0;

            if (!cylStart.Equals(0))
            {
                for (int i = 0; i <= baseConeSegments; ++i, ++vi)
                {
                    float t = (float) i / baseConeSegments;

                    var p = baseSlope.interp (t);

                    shape [vi] = new Vector3 (p.x * baseConeRad + baseRad, p.y * cylStart, Mathf.Lerp (baseV0, baseV1, t));
                }
            }

            shape [vi++] = new Vector3 (maxRad, cylStart, baseV1);
            shape [vi++] = new Vector3 (maxRad, cylEnd, noseV0);

            if (!cylEnd.Equals (top))
            {
                for (int i = 0; i <= baseConeSegments; ++i, ++vi)
                {
                    float t = (float) i / baseConeSegments;

                    var p = baseSlope.interp (1 - t);

                    shape [vi] = new Vector3 (p.x * topConeRad + topRad, Mathf.Lerp (top, cylEnd, p.y), Mathf.Lerp (baseV1, baseV0, t));
                }
            }

            return shape;
        }

        private System.Collections.IEnumerator DisplayFairingOutline()
        {
            yield return new WaitForFixedUpdate();
            SetFairingOutlineEnabled(!HasTopOrSideNode());
        }

        private void SetFairingOutlineEnabled(bool enabled)
        {
            foreach (LineRenderer lr in outline)
                lr.enabled = enabled;
        }

        private void BuildFairingOutline(Vector3[] shape)
        {
            foreach (LineRenderer lr in outline)
            {
                lr.positionCount = shape.Length;
                for (int i = 0; i < shape.Length; ++i)
                {
                    lr.SetPosition(i, new Vector3(shape[i].x, shape[i].y));
                }
            }
        }

        private void ToggleNodeHints(bool isVisible)
        {
            isVisible &= HighLogic.CurrentGame.Parameters.CustomParams<PFSettings>().showNodeHint
                         && Mode == BaseMode.Adapter && part.FindModuleImplementing<ModuleDecouplerBase>() is ModuleDecouplerBase;
            decouplerHint.enabled = isVisible;
            nonDecouplerHint.enabled = isVisible;
        }
        #endregion


        private PayloadScan ScanPayload()
        {
            //  Scan the payload and build it's profile.
            var scan = new PayloadScan (part, verticalStep, extraRadius);

            if (part.FindAttachNode("top") is AttachNode node)
            {
                scan.ofs = node.position.y;
                if (node.attachedPart != null)
                    scan.addPart (node.attachedPart, part);
            }

            if (part.FindAttachNodes("interstage") is AttachNode[] nodes)
            {
                foreach (AttachNode n in nodes)
                {
                    if (n.attachedPart != null)
                        scan.addPart (n.attachedPart, part);
                }
            }
            for (int i = 0; i < scan.payload.Count; ++i)
            {
                var cp = scan.payload[i];

                //  Add any connected payload parts.
                scan.addPart (cp.parent, cp);
                foreach (Part child in cp.children)
                {
                    scan.addPart(child, cp);
                }

                //  Scan for the part colliders.
                foreach (Collider coll in cp.FindModelComponents<Collider>())
                {
                    //  Skip ladders etc...
                    if (coll.tag.Equals("Untagged"))
                        scan.addPayload(coll);
                }
            }

            return scan;
        }

        AttachNode HasNodeComponent<type>(AttachNode[] nodes)
        {
            if (nodes != null)
            {
                foreach (AttachNode node in nodes)
                {
                    if (node.attachedPart is Part p && p.GetComponent<type>() is type)
                        return node;
                }
            }
            return null;
        }

        private void FillProfileOutline(PayloadScan scan)
        {
            if (line is LineRenderer)
            {
                line.positionCount = scan.profile.Count * 2 + 2;
                float prevRad = 0;
                int hi = 0;
                for (int i = 0; i < scan.profile.Count; i++)
                {
                    var r = scan.profile[i];

                    line.SetPosition(hi * 2, new Vector3(prevRad, hi * verticalStep + scan.ofs, 0));
                    line.SetPosition(hi * 2 + 1, new Vector3(r, hi * verticalStep + scan.ofs, 0));

                    hi++; prevRad = r;
                }

                line.SetPosition(hi * 2, new Vector3(prevRad, hi * verticalStep + scan.ofs, 0));
                line.SetPosition(hi * 2 + 1, new Vector3(0, hi * verticalStep + scan.ofs, 0));
            }
        }

        public bool forcePartPosition = true;
        public void recalcShape ()
        {
            if (Mode != BaseMode.Payload && Mode != BaseMode.Adapter)
                return;
            var scan = ScanPayload ();

            //  Check for reversed bases (inline fairings).

            float topY = 0;
            float topRad = 0;

            AttachNode topSideNode = null;
            bool isInline = false;

            if (Mode == BaseMode.Adapter)
            {
                isInline = true;
                topY = Mathf.Max(scan.ofs, height + extraHeight);
                topRad = (topSize / 2) - CalcSideThickness();
            }
            else if (scan.targets.Count > 0)
            {
                isInline = true;
                var topBase = scan.targets [0].GetComponent<ProceduralFairingBase>();
                topY = Mathf.Max(scan.ofs, scan.w2l.MultiplyPoint3x4(topBase.part.transform.position).y);
                topSideNode = HasNodeComponent<ProceduralFairingSide>(topBase.part.FindAttachNodes ("connect"));
                topRad = (topBase.baseSize / 2) - CalcSideThickness();
            }

            //  No payload case.

            if (scan.profile.Count <= 0)
            {
                scan.profile.Add (extraRadius);
            }

            //  Fill profile outline (for debugging).
            FillProfileOutline(scan);

            //  Check for attached side parts.
            var attached = part.FindAttachNodes("connect") ?? new AttachNode[1];
            var sideNode = HasNodeComponent<ProceduralFairingSide>(attached);

            //  Get the number of available fairing attachment nodes from NodeNumberTweaker.
            var nnt = part.GetComponent<KzNodeNumberTweaker>();
            int numSideParts = nnt.numNodes;

            ProceduralFairingSide sf = sideNode?.attachedPart.GetComponent<ProceduralFairingSide>();

            var baseConeShape = sf ? sf.baseConeShape : new Vector4 (0, 0, 0, 0);
            var noseConeShape = sf ? sf.noseConeShape : new Vector4 (0, 0, 0, 0);
            var mappingScale = sf ? sf.mappingScale : new Vector2 (1024, 1024);
            var stripMapping = sf ? sf.stripMapping : new Vector2 (992, 1024);
            var horMapping = sf ? sf.horMapping : new Vector4 (0, 480, 512, 992);
            var vertMapping = sf ? sf.vertMapping : new Vector4 (0, 160, 704, 1024);

            float baseCurveStartX = sf ? sf.baseCurveStartX : 0;
            float baseCurveStartY = sf ? sf.baseCurveStartY : 0;
            float baseCurveEndX = sf ? sf.baseCurveEndX : 0;
            float baseCurveEndY = sf ? sf.baseCurveEndY : 0;
            int baseConeSegments = sf ? Convert.ToInt32(sf.baseConeSegments) : 1;

            float noseCurveStartX = sf ? sf.noseCurveStartX : 0;
            float noseCurveStartY = sf ? sf.noseCurveStartY : 0;
            float noseCurveEndX = sf ? sf.noseCurveEndX : 0;
            float noseCurveEndY = sf ? sf.noseCurveEndY : 0;
            int noseConeSegments = sf ? Convert.ToInt32(sf.noseConeSegments) : 1 ;
            float noseHeightRatio = sf ? sf.noseHeightRatio : 1;
            float minBaseConeAngle = sf ? sf.minBaseConeAngle : 20;
            float density = sf ? sf.density : 0;

            //   Compute the fairing shape.

            float baseRad = (baseSize / 2) - CalcSideThickness();
            float minBaseConeTan = Mathf.Tan (minBaseConeAngle * Mathf.Deg2Rad);

            float cylStart = 0;
            float maxRad;

            int profTop = scan.profile.Count;

            if (isInline)
            {
                profTop = Mathf.CeilToInt ((topY - scan.ofs) / verticalStep);
                profTop = Math.Min(profTop, scan.profile.Count);

                maxRad = 0;
                for (int i = 0; i < profTop; ++i)
                {
                    maxRad = Mathf.Max (maxRad, scan.profile [i]);
                }

                maxRad = Mathf.Max (maxRad, topRad);
            }
            else
            {
                maxRad = PFUtils.GetMaxValueFromList (scan.profile);
            }

            if (maxRad > baseRad)
            {
                //  Try to fit the base cone as high as possible.

                cylStart = scan.ofs;

                for (int i = 1; i < scan.profile.Count; ++i)
                {
                    float y = i * verticalStep + scan.ofs;
                    float r0 = baseRad;
                    float k = (maxRad - r0) / y;

                    if (k < minBaseConeTan)
                    {
                        break;
                    }

                    bool ok = true;

                    float r = r0 + k * scan.ofs;

                    for (int j = 0; j < i; ++j, r += k * verticalStep)
                    {
                        if (scan.profile [j] > r)
                        {
                            ok = false;

                            break;
                        }
                    }

                    if (!ok)
                    {
                        break;
                    }

                    cylStart = y;
                }
            }
            else
            {
                //  No base cone, just a cylinder and a nose.

                maxRad = baseRad;
            }

            float cylEnd = scan.profile.Count * verticalStep + scan.ofs;

            if (isInline)
            {
                float r0 = topRad;

                if (profTop > 0 && profTop < scan.profile.Count)
                {
                    r0 = Mathf.Max (r0, scan.profile [profTop - 1]);

                    if (profTop - 2 >= 0) r0 = Mathf.Max (r0, scan.profile [profTop - 2]);
                }

                if (maxRad > r0)
                {
                    if (cylEnd > topY)
                    {
                        cylEnd = topY - verticalStep;
                    }

                    //  Try to fit the top cone as low as possible.

                    for (int i = profTop - 1; i >= 0; --i)
                    {
                        float y = i * verticalStep + scan.ofs;
                        float k = (maxRad - r0) / (y - topY);

                        bool ok = true;

                        float r = maxRad + k * verticalStep;

                        for (int j = i; j < profTop; ++j, r += k * verticalStep)
                        {
                            if (r < r0)
                            {
                                r = r0;
                            }

                            if (scan.profile [j] > r)
                            {
                                ok = false;

                                break;
                            }
                        }

                        if (!ok)
                        {
                            break;
                        }

                        cylEnd = y;
                    }
                }
                else
                {
                    cylEnd = topY;
                }
            }
            else
            {
                //  Try to fit the nose cone as low as possible.

                for (int i = scan.profile.Count - 1; i >= 0; --i)
                {
                    float s = verticalStep / noseHeightRatio;

                    bool ok = true;

                    float r = maxRad - s;

                    for (int j = i; j < scan.profile.Count; ++j, r -= s)
                    {
                        if (scan.profile [j] > r)
                        {
                            ok = false;

                            break;
                        }
                    }

                    if (!ok) break;

                    float y = i * verticalStep + scan.ofs;

                    cylEnd = y;
                }
            }

            if (autoShape)
            {
                manualMaxSize = maxRad * 2;
                manualCylStart = cylStart;
                manualCylEnd = cylEnd;
            }
            else
            {
                maxRad = manualMaxSize * 0.5f;
                cylStart = manualCylStart;
                cylEnd = manualCylEnd;
            }
            cylStart = Math.Min(cylStart, cylEnd);

            //  Build the fairing shape line.

            Vector3[] shape = isInline ? buildInlineFairingShape(baseRad, maxRad, topRad, cylStart, cylEnd, topY, baseConeShape, baseConeSegments, vertMapping, mappingScale.y) :
                                        buildFairingShape(baseRad, maxRad, cylStart, cylEnd, noseHeightRatio, baseConeShape, noseConeShape, baseConeSegments, noseConeSegments, vertMapping, mappingScale.y);

            BuildFairingOutline(shape);
            SetFairingOutlineEnabled(sideNode == null && topSideNode == null);

            //  Rebuild the side parts.

            int numSegs = Math.Max(2, circleSegments / numSideParts);

            foreach (AttachNode sn in attached)
            {
                if (sn.attachedPart is Part sp &&
                    sp.GetComponent<ProceduralFairingSide>() is ProceduralFairingSide sf2)
                {
                    var nodePos = sn.position;
                    Vector3 oppNodePos = sn.FindOpposingNode() is AttachNode opp ? opp.position : Vector3.zero;

                    // Move and orient the part regardless of shapelock.
                    sf2.meshPos = Vector3.zero;
                    sf2.meshRot = Quaternion.identity;
                    if (forcePartPosition)
                    {
                        // place the part on its attachnode.
                        Vector3 peerPos = part.transform.TransformPoint(sn.position - oppNodePos);
                        sp.transform.position = peerPos;

                        // FairingSide model plate:  +X is inward, +Z is up, right-handed coordinates
                        // FairingSide model's meshFilter.transform.localRotation seems to start non-zero
                        // FairingSide mesh generation will do +Y up, +X radially out

                        // d is in world space
                        Vector3 d = part.transform.position - peerPos;
                        Vector3 worldUp = part.transform.TransformDirection(Vector3.up);
                        Vector3 norm = Vector3.Cross(worldUp, d);

                        sp.transform.rotation = Quaternion.LookRotation(norm, worldUp);

                        // We've re-oriented the part correctly no matter which node it was placed or how it was oriented there.
                        // Now let's move it into the center.
                        float off = sn.position.y;
                        off -= oppNodePos.y;
                        sf2.meshPos = new Vector3(-norm.magnitude, -off, 0);
                        var currentOffset = sf2.meshPos + (openFairing ? editorOpenOffset : Vector3.zero);
                        sf2.SetOffset(currentOffset);
                    }

                    // Only rebuild the shape if shapelock is not set.
                    if (!sf2.shapeLock)
                    {
                        sf2.numSegs = numSegs;
                        sf2.numSideParts = numSideParts;
                        sf2.baseRad = baseRad;
                        sf2.maxRad = maxRad;
                        sf2.cylStart = cylStart;
                        sf2.cylEnd = cylEnd;
                        sf2.topRad = topRad;
                        sf2.inlineHeight = topY;
                        sf2.sideThickness = CalcSideThickness();
                        sf2.baseCurveStartX = baseCurveStartX;
                        sf2.baseCurveStartY = baseCurveStartY;
                        sf2.baseCurveEndX = baseCurveEndX;
                        sf2.baseCurveEndY = baseCurveEndY;
                        sf2.baseConeSegments = baseConeSegments;
                        sf2.noseCurveStartX = noseCurveStartX;
                        sf2.noseCurveStartY = noseCurveStartY;
                        sf2.noseCurveEndX = noseCurveEndX;
                        sf2.noseCurveEndY = noseCurveEndY;
                        sf2.noseConeSegments = noseConeSegments;
                        sf2.noseHeightRatio = noseHeightRatio;
                        sf2.density = density;

                        sf2.rebuildMesh();
                        var currentOffset = sf2.meshPos + (openFairing ? editorOpenOffset : Vector3.zero);
                        sf2.SetOffset(currentOffset);
                    }
                }
            }

            if (part.GetComponent<KzFairingBaseShielding>() is KzFairingBaseShielding shielding)
                shielding.reset ();
        }
    }
}
