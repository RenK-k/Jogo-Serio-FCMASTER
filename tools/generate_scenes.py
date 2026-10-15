import os, uuid

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "Assets", "Scenes"))
os.makedirs(ROOT, exist_ok=True)

IMG = "fe87c0e1cc204ed48ad3b37840f39efc"
TXT = "5f7201a12d95ffc409449d95f23cf332"
BTN = "4e29b1a8efbd4b44bb3f3716e73f07ff"
SCALER = "0ce3a353d20bb8d47bffead74c59ff1a"
RAY = "dc42784cf3c2ed440b1f863fa61fa73c"
SCROLL = "1aa08ab6e0800fa44ae55d278d1423e3"
MASK = "3312d7739989d2b4e91e6319e9a96d71"
VLG = "59f8146938fff824cb5fd77236b75775"
FIT = "3245ec927dfc6584f8dc043c556c8a80"
ES = "76c392e42b5098c458856bb4b7d6d2ff"
SIM = "4f231c4fb786d0446b5a3da6f0b56ab3"
INP = "d0b148fe25e64451814022348290b609"
URP = "a79441f348de89743a2939f4d699eac1"
FONT = "{fileID: 10102, guid: 0000000000000000e000000000000000, type: 0}"

SCR = {
    "GameRoot": "9f3a1c2e4b5d67890a1b2c3d4e5f6a70",
    "MainMenu": "1a2b3c4d5e6f708192a3b4c5d6e7f801",
    "LeagueSelect": "2b3c4d5e6f708192a3b4c5d6e7f80192",
    "TeamSelect": "3c4d5e6f708192a3b4c5d6e7f80192a3",
    "ClubInfo": "4d5e6f708192a3b4c5d6e7f80192a3b4",
    "CreateCareer": "5e6f708192a3b4c5d6e7f80192a3b4c5",
    "CareerHub": "6f708192a3b4c5d6e7f80192a3b4c5d6",
    "MatchDay": "708192a3b4c5d6e7f80192a3b4c5d6e7",
}

BG = (0.043, 0.043, 0.051, 1)
HD = (0.02, 0.02, 0.02, 1)
PN = (0.086, 0.094, 0.11, 1)
ALT = (0.118, 0.133, 0.157, 1)
GN = (0.0, 0.651, 0.318, 1)
TX = (0.957, 0.957, 0.957, 1)
MU = (0.655, 0.678, 0.714, 1)
DK = (0.165, 0.176, 0.20, 1)
PI = (0.05, 0.28, 0.16, 1)


def rgba(c):
    r, g, b, a = c
    return f"{{r: {r}, g: {g}, b: {b}, a: {a}}}"


class S:
    def __init__(self):
        self.n = 300000
        self.chunks = []
        self.roots = []

    def id(self):
        self.n += 1
        return self.n

    def emit(self, text):
        self.chunks.append(text)


class N:
    def __init__(self, scene, name, amin=(0, 0), amax=(1, 1), color=None, text=None, tsize=18,
                 align=0, style=0, button=False, active=1, pivot=(0.5, 0.5), pos=(0, 0), size=(0, 0),
                 scroll=False, vlg=False, input_field=False, script=None, layer=5):
        self.s = scene
        self.name = name
        self.amin, self.amax = amin, amax
        self.color, self.text, self.tsize = color, text, tsize
        self.align, self.style, self.button = align, style, button
        self.active, self.pivot, self.pos, self.size = active, pivot, pos, size
        self.scroll, self.vlg, self.input_field = scroll, vlg, input_field
        self.script = script
        self.layer = layer
        self.children = []
        self.gid = scene.id()
        self.rid = scene.id()
        self.cr = scene.id()
        self.img = scene.id() if (color is not None or button or scroll) else None
        self.txt = scene.id() if text is not None else None
        self.btn = scene.id() if button else None
        self.scr = scene.id() if scroll else None
        self.msk = scene.id() if scroll else None
        self.vlgid = scene.id() if vlg else None
        self.fit = scene.id() if vlg else None
        self.inp = scene.id() if input_field else None
        self.scrp = scene.id() if script else None

    def add(self, child):
        self.children.append(child)
        return child

    def dump(self, father=0, root=False):
        s = self.s
        comps = [self.rid, self.cr]
        if self.img: comps.append(self.img)
        if self.txt: comps.append(self.txt)
        if self.btn: comps.append(self.btn)
        if self.scr: comps.append(self.scr)
        if self.msk: comps.append(self.msk)
        if self.vlgid: comps.append(self.vlgid)
        if self.fit: comps.append(self.fit)
        if self.inp: comps.append(self.inp)
        if self.scrp: comps.append(self.scrp)
        cl = "\n".join([f"  - component: {{fileID: {c}}}" for c in comps])
        ch = "\n".join([f"  - {{fileID: {c.rid}}}" for c in self.children])
        s.emit(f"""--- !u!1 &{self.gid}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
{cl}
  m_Layer: {self.layer}
  m_Name: {self.name}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: {self.active}
--- !u!224 &{self.rid}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {self.gid}}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children:
{ch}
  m_Father: {{fileID: {father}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
  m_AnchorMin: {{x: {self.amin[0]}, y: {self.amin[1]}}}
  m_AnchorMax: {{x: {self.amax[0]}, y: {self.amax[1]}}}
  m_AnchoredPosition: {{x: {self.pos[0]}, y: {self.pos[1]}}}
  m_SizeDelta: {{x: {self.size[0]}, y: {self.size[1]}}}
  m_Pivot: {{x: {self.pivot[0]}, y: {self.pivot[1]}}}
--- !u!222 &{self.cr}
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {self.gid}}}
  m_CullTransparentMesh: 1""")
        if root:
            s.roots.append(self.rid)
        if self.img:
            s.emit(f"""--- !u!114 &{self.img}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {self.gid}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {IMG}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Material: {{fileID: 0}}
  m_Color: {rgba(self.color if self.color is not None else PN)}
  m_RaycastTarget: 1
  m_RaycastPadding: {{x: 0, y: 0, z: 0, w: 0}}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {{fileID: 0}}
  m_Type: 0
  m_PreserveAspect: 1
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1""")
        if self.txt:
            s.emit(f"""--- !u!114 &{self.txt}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {self.gid}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {TXT}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Material: {{fileID: 0}}
  m_Color: {rgba(TX)}
  m_RaycastTarget: 0
  m_RaycastPadding: {{x: 0, y: 0, z: 0, w: 0}}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_FontData:
    m_Font: {FONT}
    m_FontSize: {self.tsize}
    m_FontStyle: {self.style}
    m_BestFit: 0
    m_MinSize: 10
    m_MaxSize: 64
    m_Alignment: {self.align}
    m_AlignByGeometry: 0
    m_RichText: 1
    m_HorizontalOverflow: 0
    m_VerticalOverflow: 1
    m_LineSpacing: 1
  m_Text: {self.text}""")
        if self.btn:
            s.emit(f"""--- !u!114 &{self.btn}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {self.gid}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {BTN}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Navigation:
    m_Mode: 3
    m_WrapAround: 0
    m_SelectOnUp: {{fileID: 0}}
    m_SelectOnDown: {{fileID: 0}}
    m_SelectOnLeft: {{fileID: 0}}
    m_SelectOnRight: {{fileID: 0}}
  m_Transition: 1
  m_Colors:
    m_NormalColor: {{r: 1, g: 1, b: 1, a: 1}}
    m_HighlightedColor: {{r: 0.9, g: 0.9, b: 0.9, a: 1}}
    m_PressedColor: {{r: 0.75, g: 0.75, b: 0.75, a: 1}}
    m_SelectedColor: {{r: 0.95, g: 0.95, b: 0.95, a: 1}}
    m_DisabledColor: {{r: 0.8, g: 0.8, b: 0.8, a: 0.5}}
    m_ColorMultiplier: 1
    m_FadeDuration: 0.1
  m_SpriteState:
    m_HighlightedSprite: {{fileID: 0}}
    m_PressedSprite: {{fileID: 0}}
    m_SelectedSprite: {{fileID: 0}}
    m_DisabledSprite: {{fileID: 0}}
  m_AnimationTriggers:
    m_NormalTrigger: Normal
    m_HighlightedTrigger: Highlighted
    m_PressedTrigger: Pressed
    m_SelectedTrigger: Selected
    m_DisabledTrigger: Disabled
  m_Interactable: 1
  m_TargetGraphic: {{fileID: {self.img}}}
  m_OnClick:
    m_PersistentCalls:
      m_Calls: []""")
        if self.scr:
            vp = self.children[0].rid if self.children else 0
            ct = self.children[0].children[0].rid if self.children and self.children[0].children else 0
            s.emit(f"""--- !u!114 &{self.scr}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {self.gid}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {SCROLL}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Content: {{fileID: {ct}}}
  m_Horizontal: 0
  m_Vertical: 1
  m_MovementType: 2
  m_Elasticity: 0.1
  m_Inertia: 1
  m_DecelerationRate: 0.135
  m_ScrollSensitivity: 28
  m_Viewport: {{fileID: {vp}}}
  m_HorizontalScrollbar: {{fileID: 0}}
  m_VerticalScrollbar: {{fileID: 0}}
--- !u!114 &{self.msk}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {self.gid}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {MASK}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: """)
        if self.vlgid:
            s.emit(f"""--- !u!114 &{self.vlgid}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {self.gid}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {VLG}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Padding:
    m_Left: 8
    m_Right: 8
    m_Top: 8
    m_Bottom: 8
  m_ChildAlignment: 1
  m_Spacing: 8
  m_ChildForceExpandWidth: 1
  m_ChildForceExpandHeight: 0
  m_ChildControlWidth: 1
  m_ChildControlHeight: 0
--- !u!114 &{self.fit}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {self.gid}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {FIT}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_HorizontalFit: 0
  m_VerticalFit: 2""")
        if self.inp:
            text_comp = 0
            placeholder = 0
            for child in self.children:
                if child.name == "Text" and child.txt:
                    text_comp = child.txt
                if child.name == "Placeholder" and child.txt:
                    placeholder = child.txt
            s.emit(f"""--- !u!114 &{self.inp}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {self.gid}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {INP}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Navigation:
    m_Mode: 3
    m_WrapAround: 0
    m_SelectOnUp: {{fileID: 0}}
    m_SelectOnDown: {{fileID: 0}}
    m_SelectOnLeft: {{fileID: 0}}
    m_SelectOnRight: {{fileID: 0}}
  m_Transition: 1
  m_Colors:
    m_NormalColor: {{r: 1, g: 1, b: 1, a: 1}}
    m_HighlightedColor: {{r: 0.9, g: 0.9, b: 0.9, a: 1}}
    m_PressedColor: {{r: 0.75, g: 0.75, b: 0.75, a: 1}}
    m_SelectedColor: {{r: 0.95, g: 0.95, b: 0.95, a: 1}}
    m_DisabledColor: {{r: 0.8, g: 0.8, b: 0.8, a: 0.5}}
    m_ColorMultiplier: 1
    m_FadeDuration: 0.1
  m_Interactable: 1
  m_TargetGraphic: {{fileID: {self.img if self.img else 0}}}
  m_TextComponent: {{fileID: {text_comp}}}
  m_Placeholder: {{fileID: {placeholder}}}
  m_CaretColor: {{r: 0.196, g: 0.196, b: 0.196, a: 1}}
  m_CustomCaretColor: 0
  m_SelectionColor: {{r: 0.65882355, g: 0.80784315, b: 1, a: 0.7529412}}
  m_Text: 
  m_CaretBlinkRate: 0.85
  m_CaretWidth: 1
  m_ReadOnly: 0
  m_CharacterLimit: 40
  m_OnEndEdit:
    m_PersistentCalls:
      m_Calls: []
  m_OnSubmit:
    m_PersistentCalls:
      m_Calls: []
  m_OnValueChanged:
    m_PersistentCalls:
      m_Calls: []""")
        if self.scrp:
            s.emit(f"""--- !u!114 &{self.scrp}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {self.gid}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {self.script}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: """)
        for c in self.children:
            c.dump(self.rid)


HEAD = r"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!29 &1
OcclusionCullingSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_OcclusionBakeSettings:
    smallestOccluder: 5
    smallestHole: 0.25
    backfaceThreshold: 100
  m_SceneGUID: 00000000000000000000000000000000
  m_OcclusionCullingData: {fileID: 0}
--- !u!104 &2
RenderSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 9
  m_Fog: 0
  m_FogColor: {r: 0.5, g: 0.5, b: 0.5, a: 1}
  m_FogMode: 3
  m_FogDensity: 0.01
  m_LinearFogStart: 0
  m_LinearFogEnd: 300
  m_AmbientSkyColor: {r: 0.212, g: 0.227, b: 0.259, a: 1}
  m_AmbientEquatorColor: {r: 0.114, g: 0.125, b: 0.133, a: 1}
  m_AmbientGroundColor: {r: 0.047, g: 0.043, b: 0.035, a: 1}
  m_AmbientIntensity: 1
  m_AmbientMode: 3
  m_SubtractiveShadowColor: {r: 0.42, g: 0.478, b: 0.627, a: 1}
  m_SkyboxMaterial: {fileID: 0}
  m_HaloStrength: 0.5
  m_FlareStrength: 1
  m_FlareFadeSpeed: 3
  m_HaloTexture: {fileID: 0}
  m_SpotCookie: {fileID: 10001, guid: 0000000000000000e000000000000000, type: 0}
  m_DefaultReflectionMode: 0
  m_DefaultReflectionResolution: 128
  m_ReflectionBounces: 1
  m_ReflectionIntensity: 1
  m_CustomReflection: {fileID: 0}
  m_Sun: {fileID: 0}
  m_IndirectSpecularColor: {r: 0, g: 0, b: 0, a: 1}
  m_UseRadianceAmbientProbe: 0
--- !u!157 &3
LightmapSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 12
  m_GIWorkflowMode: 1
  m_GISettings:
    serializedVersion: 2
    m_BounceScale: 1
    m_IndirectOutputScale: 1
    m_AlbedoBoost: 1
    m_EnvironmentLightingMode: 0
    m_EnableBakedLightmaps: 0
    m_EnableRealtimeLightmaps: 0
  m_LightmapEditorSettings:
    serializedVersion: 12
    m_Resolution: 2
    m_BakeResolution: 40
    m_AtlasSize: 1024
    m_AO: 0
    m_AOMaxDistance: 1
    m_CompAOExponent: 1
    m_CompAOExponentDirect: 0
    m_ExtractAmbientOcclusion: 0
    m_Padding: 2
    m_LightmapParameters: {fileID: 0}
    m_LightmapsBakeMode: 1
    m_TextureCompression: 1
    m_FinalGather: 0
    m_FinalGatherFiltering: 1
    m_FinalGatherRayCount: 256
    m_ReflectionCompression: 2
    m_MixedBakeMode: 2
    m_BakeBackend: 1
    m_PVRSampling: 1
    m_PVRDirectSampleCount: 32
    m_PVRSampleCount: 512
    m_PVRBounces: 2
    m_PVREnvironmentSampleCount: 256
    m_PVREnvironmentReferencePointCount: 2048
    m_PVRFilteringMode: 1
    m_PVRDenoiserTypeDirect: 1
    m_PVRDenoiserTypeIndirect: 1
    m_PVRDenoiserTypeAO: 1
    m_PVRFilterTypeDirect: 0
    m_PVRFilterTypeIndirect: 0
    m_PVRFilterTypeAO: 0
    m_PVREnvironmentMIS: 1
    m_PVRCulling: 1
    m_PVRFilteringGaussRadiusDirect: 1
    m_PVRFilteringGaussRadiusIndirect: 5
    m_PVRFilteringGaussRadiusAO: 2
    m_PVRFilteringAtrousPositionSigmaDirect: 0.5
    m_PVRFilteringAtrousPositionSigmaIndirect: 2
    m_PVRFilteringAtrousPositionSigmaAO: 1
    m_ExportTrainingData: 0
    m_TrainingDataDestination: TrainingData
    m_LightProbeSampleCountMultiplier: 4
  m_LightingDataAsset: {fileID: 0}
  m_LightingSettings: {fileID: 0}
--- !u!196 &4
NavMeshSettings:
  serializedVersion: 2
  m_ObjectHideFlags: 0
  m_BuildSettings:
    serializedVersion: 3
    agentTypeID: 0
    agentRadius: 0.5
    agentHeight: 2
    agentSlope: 45
    agentClimb: 0.4
    ledgeDropHeight: 0
    maxJumpAcrossDistance: 0
    minRegionArea: 2
    manualCellSize: 0
    cellSize: 0.16666667
    manualTileSize: 0
    tileSize: 256
    buildHeightMesh: 0
    maxJobWorkers: 0
    preserveTilesOutsideBounds: 0
    debug:
      m_Flags: 0
  m_NavMeshData: {fileID: 0}
"""


def camera_event(s):
    go, tr, al, urp, cam = s.id(), s.id(), s.id(), s.id(), s.id()
    s.emit(f"""--- !u!1 &{go}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {tr}}}
  - component: {{fileID: {cam}}}
  - component: {{fileID: {al}}}
  - component: {{fileID: {urp}}}
  m_Layer: 0
  m_Name: Main Camera
  m_TagString: MainCamera
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!81 &{al}
AudioListener:
  m_ObjectHideFlags: 0
  m_GameObject: {{fileID: {go}}}
  m_Enabled: 1
--- !u!114 &{urp}
MonoBehaviour:
  m_GameObject: {{fileID: {go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {URP}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_RenderShadows: 0
  m_RequiresDepthTextureOption: 2
  m_RequiresOpaqueTextureOption: 2
  m_CameraType: 0
  m_Cameras: []
  m_RendererIndex: -1
  m_VolumeLayerMask:
    serializedVersion: 2
    m_Bits: 1
  m_VolumeTrigger: {{fileID: 0}}
  m_RenderPostProcessing: 0
  m_Antialiasing: 0
  m_AntialiasingQuality: 2
  m_StopNaN: 0
  m_Dithering: 0
  m_ClearDepth: 1
  m_AllowXRRendering: 1
  m_RequiresDepthTexture: 0
  m_RequiresColorTexture: 0
  m_Version: 2
--- !u!20 &{cam}
Camera:
  m_GameObject: {{fileID: {go}}}
  m_Enabled: 1
  serializedVersion: 2
  m_ClearFlags: 2
  m_BackGroundColor: {rgba(BG)}
  m_projectionMatrixMode: 1
  m_GateFitMode: 2
  m_FOVAxisMode: 0
  m_SensorSize: {{x: 36, y: 24}}
  m_LensShift: {{x: 0, y: 0}}
  m_FocalLength: 50
  m_NormalizedViewPortRect:
    serializedVersion: 2
    x: 0
    y: 0
    width: 1
    height: 1
  near clip plane: 0.3
  far clip plane: 1000
  field of view: 34
  orthographic: 1
  orthographic size: 5
  m_Depth: -1
  m_CullingMask:
    serializedVersion: 2
    m_Bits: 4294967295
  m_RenderingPath: -1
  m_TargetTexture: {{fileID: 0}}
  m_TargetDisplay: 0
  m_TargetEye: 0
  m_HDR: 1
  m_AllowMSAA: 0
  m_AllowDynamicResolution: 0
  m_ForceIntoRT: 0
  m_OcclusionCulling: 0
  m_StereoConvergence: 10
  m_StereoSeparation: 0.022
--- !u!4 &{tr}
Transform:
  m_GameObject: {{fileID: {go}}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: -10}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}""")
    s.roots.append(tr)
    ego, etr, ees, esim = s.id(), s.id(), s.id(), s.id()
    s.emit(f"""--- !u!1 &{ego}
GameObject:
  m_ObjectHideFlags: 0
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {etr}}}
  - component: {{fileID: {ees}}}
  - component: {{fileID: {esim}}}
  m_Layer: 0
  m_Name: EventSystem
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{etr}
Transform:
  m_GameObject: {{fileID: {ego}}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!114 &{ees}
MonoBehaviour:
  m_GameObject: {{fileID: {ego}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {ES}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_FirstSelected: {{fileID: 0}}
  m_sendNavigationEvents: 1
  m_DragThreshold: 10
--- !u!114 &{esim}
MonoBehaviour:
  m_GameObject: {{fileID: {ego}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {SIM}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_SendPointerHoverToParent: 1
  m_HorizontalAxis: Horizontal
  m_VerticalAxis: Vertical
  m_SubmitButton: Submit
  m_CancelButton: Cancel
  m_InputActionsPerSecond: 10
  m_RepeatDelay: 0.5
  m_ForceModuleActive: 0""")
    s.roots.append(etr)


def game_root(s):
    go, tr, mb = s.id(), s.id(), s.id()
    s.emit(f"""--- !u!1 &{go}
GameObject:
  m_ObjectHideFlags: 0
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {tr}}}
  - component: {{fileID: {mb}}}
  m_Layer: 0
  m_Name: GameRoot
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{tr}
Transform:
  m_GameObject: {{fileID: {go}}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!114 &{mb}
MonoBehaviour:
  m_GameObject: {{fileID: {go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {SCR['GameRoot']}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: """)
    s.roots.append(tr)


def canvas(s, script):
    n = N(s, "Canvas", script=script)
    n.cr = s.id()  # unused extra ok
    # Canvas needs Canvas/Scaler/Raycaster instead of Image. Handle as special dump.
    return n


def dump_canvas(s, canvas_node, script):
    gid, rid = s.id(), s.id()
    canv, scaler, ray, ctrl = s.id(), s.id(), s.id(), s.id()
    ch = "\n".join([f"  - {{fileID: {c.rid}}}" for c in canvas_node.children])
    s.emit(f"""--- !u!1 &{gid}
GameObject:
  m_ObjectHideFlags: 0
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {rid}}}
  - component: {{fileID: {canv}}}
  - component: {{fileID: {scaler}}}
  - component: {{fileID: {ray}}}
  - component: {{fileID: {ctrl}}}
  m_Layer: 5
  m_Name: Canvas
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &{rid}
RectTransform:
  m_GameObject: {{fileID: {gid}}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children:
{ch}
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
  m_AnchorMin: {{x: 0, y: 0}}
  m_AnchorMax: {{x: 1, y: 1}}
  m_AnchoredPosition: {{x: 0, y: 0}}
  m_SizeDelta: {{x: 0, y: 0}}
  m_Pivot: {{x: 0.5, y: 0.5}}
--- !u!223 &{canv}
Canvas:
  m_GameObject: {{fileID: {gid}}}
  m_Enabled: 1
  serializedVersion: 3
  m_RenderMode: 0
  m_Camera: {{fileID: 0}}
  m_PlaneDistance: 100
  m_PixelPerfect: 0
  m_ReceivesEvents: 1
  m_OverrideSorting: 0
  m_OverridePixelPerfect: 0
  m_SortingBucketNormalizedSize: 0
  m_VertexColorAlwaysGammaSpace: 0
  m_AdditionalShaderChannelsFlag: 0
  m_UpdateRectTransformForStandalone: 0
  m_SortingLayerID: 0
  m_SortingOrder: 0
  m_TargetDisplay: 0
--- !u!114 &{scaler}
MonoBehaviour:
  m_GameObject: {{fileID: {gid}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {SCALER}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_UiScaleMode: 1
  m_ReferencePixelsPerUnit: 100
  m_ScaleFactor: 1
  m_ReferenceResolution: {{x: 1600, y: 900}}
  m_ScreenMatchMode: 0
  m_MatchWidthOrHeight: 0.5
  m_PhysicalUnit: 3
  m_FallbackScreenDPI: 96
  m_DefaultSpriteDPI: 96
  m_DynamicPixelsPerUnit: 1
  m_PresetInfoIsWorld: 0
--- !u!114 &{ray}
MonoBehaviour:
  m_GameObject: {{fileID: {gid}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {RAY}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_IgnoreReversedGraphics: 1
  m_BlockingObjects: 0
  m_BlockingMask:
    serializedVersion: 2
    m_Bits: 4294967295
--- !u!114 &{ctrl}
MonoBehaviour:
  m_GameObject: {{fileID: {gid}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {script}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: """)
    s.roots.append(rid)
    for c in canvas_node.children:
        c.dump(rid)


def btn(s, name, amin, amax, label, color=GN, tsize=18):
    n = N(s, name, amin, amax, color=color, button=True)
    n.add(N(s, "Label", text=label, tsize=tsize, align=4, style=1))
    return n


def header(s, title, subtitle, back=True):
    h = N(s, "Header", (0, 0.9), (1, 1), color=HD)
    if back:
        h.add(btn(s, "Back", (0.02, 0.22), (0.13, 0.78), "VOLTAR", DK, 16))
    h.add(N(s, "Title", (0.16 if back else 0.04, 0.46), (0.98, 0.92), text=title, tsize=26, align=3, style=1))
    h.add(N(s, "Subtitle", (0.16 if back else 0.04, 0.08), (0.98, 0.5), text=subtitle, tsize=15, align=3))
    return h


def row_template(s, with_kit=True):
    row = N(s, "RowTemplate", (0, 1), (1, 1), color=PN, pivot=(0.5, 1), pos=(0, -10), size=(0, 108))
    if with_kit:
        row.add(N(s, "Kit", (0.02, 0.08), (0.14, 0.92), color=ALT))
        row.add(N(s, "Name", (0.16, 0.52), (0.72, 0.92), text="CLUBE", tsize=20, align=3, style=1))
        row.add(N(s, "Meta", (0.16, 0.16), (0.72, 0.52), text="Cidade", tsize=14, align=3))
        row.add(btn(s, "Select", (0.74, 0.28), (0.97, 0.72), "SELECIONAR", GN, 16))
    else:
        row.add(N(s, "Name", (0.04, 0.15), (0.96, 0.85), text="Jogo", tsize=16, align=3))
        row.add(N(s, "Desc", (0, 0), (0.01, 0.01), text=" ", tsize=12))
        row.add(N(s, "Meta", (0, 0), (0.01, 0.01), text=" ", tsize=12))
        row.add(btn(s, "Select", (0.8, 0.25), (0.97, 0.75), "OK", GN, 14))
    return row


def scroll(s, with_kit=True):
    sc = N(s, "Scroll", color=(0, 0, 0, 0.12), scroll=True)
    vp = N(s, "Viewport", color=(0, 0, 0, 0.04))
    content = N(s, "Content", (0, 1), (1, 1), pivot=(0.5, 1), size=(0, 200), vlg=True)
    content.add(row_template(s, with_kit))
    vp.add(content)
    sc.add(vp)
    return sc


def write_scene(name, builder, guid=None, include_root=False):
    s = S()
    s.emit(HEAD)
    camera_event(s)
    if include_root:
        game_root(s)
    children = builder(s)
    holder = type("H", (), {"children": children})()
    dump_canvas(s, holder, SCR[name if name != "MainMenu" else "MainMenu"])
    roots = "\n".join([f"  - {{fileID: {r}}}" for r in s.roots])
    s.emit(f"""--- !u!1660057539 &9223372036854775807
SceneRoots:
  m_ObjectHideFlags: 0
  m_Roots:
{roots}
""")
    path = os.path.join(ROOT, name + ".unity")
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(s.chunks) + "\n")
    g = guid or uuid.uuid4().hex
    with open(path + ".meta", "w", encoding="utf-8", newline="\n") as f:
        f.write(f"fileFormatVersion: 2\nguid: {g}\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
    print("wrote", path)
    return g


def main_menu(s):
    kids = []
    kids.append(N(s, "Background", color=BG))
    kids.append(N(s, "GreenStripe", (0, 0), (0.008, 1), color=GN))
    kids.append(N(s, "HeaderBar", (0, 0.9), (1, 1), color=HD))
    kids[-1].add(N(s, "GameTitle", (0.04, 0.2), (0.6, 0.85), text="COACH MASTER", tsize=20, align=3, style=1))
    tile = N(s, "CareerTile", (0.05, 0.08), (0.55, 0.82), color=PN)
    tile.add(N(s, "Eyebrow", (0.06, 0.82), (0.94, 0.92), text="FIFA 15  ·  CARREIRA", tsize=14, align=3, style=1))
    tile.add(N(s, "Title", (0.06, 0.58), (0.94, 0.82), text="MODO CARREIRA", tsize=42, align=3, style=1))
    tile.add(N(s, "Body", (0.06, 0.28), (0.94, 0.58), text="Escolhe um clube, assume o banco e constrói a tua história.", tsize=18, align=0))
    tile.add(btn(s, "NovaCarreira", (0.06, 0.08), (0.55, 0.22), "NOVA CARREIRA", GN, 22))
    kids.append(tile)
    kids.append(N(s, "FooterBar", (0, 0), (1, 0.06), color=HD))
    kids[-1].add(N(s, "Hint", (0.04, 0.15), (0.96, 0.85), text="A  Selecionar     B  Voltar", tsize=14, align=3))
    return kids


def league(s):
    kids = [N(s, "Background", color=BG), header(s, "NOVA CARREIRA", "Escolhe a liga."), N(s, "AccentBar", (0, 0.894), (1, 0.9), color=GN)]
    body = N(s, "Body", (0.04, 0.04), (0.96, 0.88), color=(0, 0, 0, 0))
    body.add(scroll(s, False))
    kids.append(body)
    return kids


def team(s):
    kids = [N(s, "Background", color=BG), header(s, "SELECIONAR CLUBE", "Escolhe o clube. A camisola é um placeholder."), N(s, "AccentBar", (0, 0.894), (1, 0.9), color=GN)]
    body = N(s, "Body", (0.03, 0.04), (0.68, 0.88), color=(0, 0, 0, 0))
    body.add(scroll(s, True))
    kids.append(body)
    prev = N(s, "Preview", (0.7, 0.08), (0.97, 0.88), color=PN)
    prev.add(N(s, "Kit", (0.12, 0.28), (0.88, 0.88), color=ALT))
    prev.add(N(s, "ClubName", (0.06, 0.14), (0.94, 0.26), text="CLUBE", tsize=18, align=4, style=1))
    prev.add(N(s, "Hint", (0.06, 0.04), (0.94, 0.14), text="Assets/CoachMaster/Resources/Kits/nome-do-clube.png", tsize=12, align=4))
    kids.append(prev)
    return kids


def club(s):
    kids = [N(s, "Background", color=BG), header(s, "CLUBE", "Dossier"), N(s, "AccentBar", (0, 0.894), (1, 0.9), color=GN)]
    slot = N(s, "KitSlot", (0.03, 0.18), (0.28, 0.86), color=PN)
    slot.add(N(s, "Kit", (0.08, 0.18), (0.92, 0.92), color=ALT))
    slot.add(N(s, "Hint", (0.04, 0.04), (0.96, 0.16), text="Resources/Kits/slug.png", tsize=12, align=4))
    kids.append(slot)
    kids.append(N(s, "Dossier", (0.3, 0.18), (0.97, 0.86), color=PN, text="Dossier do clube", tsize=16, align=0))
    kids.append(btn(s, "Choose", (0.3, 0.04), (0.97, 0.14), "ESCOLHER ESTE CLUBE", GN, 22))
    return kids


def create(s):
    kids = [N(s, "Background", color=BG), header(s, "CRIAR CARREIRA", "Nome do treinador"), N(s, "AccentBar", (0, 0.894), (1, 0.9), color=GN)]
    slot = N(s, "KitSlot", (0.08, 0.42), (0.32, 0.86), color=PN)
    slot.add(N(s, "Kit", (0.1, 0.18), (0.9, 0.92), color=ALT))
    slot.add(N(s, "Hint", (0.04, 0.04), (0.96, 0.16), text="placeholder", tsize=12, align=4))
    kids.append(slot)
    field = N(s, "CoachName", (0.36, 0.62), (0.9, 0.74), color=ALT, input_field=True)
    field.add(N(s, "Text", (0.04, 0.1), (0.96, 0.9), text=" ", tsize=22, align=3))
    field.add(N(s, "Placeholder", (0.04, 0.1), (0.96, 0.9), text="Nome do treinador", tsize=22, align=3))
    kids.append(field)
    kids.append(N(s, "Error", (0.36, 0.54), (0.9, 0.62), text=" ", tsize=16, align=3))
    kids.append(btn(s, "Confirm", (0.36, 0.4), (0.9, 0.52), "ASSUMIR O CLUBE", GN, 22))
    return kids


def hub(s):
    kids = [N(s, "Background", color=BG)]
    hd = N(s, "Header", (0, 0.88), (1, 1), color=HD)
    hd.add(N(s, "Kit", (0.015, 0.12), (0.09, 0.88), color=ALT))
    hd.add(N(s, "ClubName", (0.1, 0.48), (0.98, 0.9), text="CLUBE", tsize=26, align=3, style=1))
    hd.add(N(s, "Details", (0.1, 0.1), (0.98, 0.5), text="Data", tsize=14, align=3))
    kids.append(hd)
    kids.append(N(s, "AccentBar", (0, 0.874), (1, 0.88), color=GN))
    tabs = N(s, "Tabs", (0.02, 0.81), (0.98, 0.87), color=(0, 0, 0, 0))
    tabs.add(btn(s, "Inicio", (0.0, 0.1), (0.18, 0.9), "INÍCIO", GN, 16))
    tabs.add(btn(s, "Noticias", (0.19, 0.1), (0.37, 0.9), "NOTÍCIAS", DK, 16))
    tabs.add(btn(s, "Equipa", (0.38, 0.1), (0.56, 0.9), "EQUIPA", DK, 16))
    tabs.add(btn(s, "Staff", (0.57, 0.1), (0.75, 0.9), "STAFF", DK, 16))
    tabs.add(btn(s, "Calendario", (0.76, 0.1), (1.0, 0.9), "CALENDÁRIO", DK, 16))
    kids.append(tabs)
    pages = N(s, "Pages", (0.02, 0.14), (0.98, 0.8), color=PN)
    for name in ("Home", "News", "Team", "Staff"):
        p = N(s, name, color=PN, active=1 if name == "Home" else 0)
        p.add(N(s, "Body", (0.03, 0.04), (0.97, 0.96), text="Conteúdo", tsize=18, align=0))
        pages.add(p)
    cal = N(s, "Calendar", color=PN, active=0)
    cal.add(scroll(s, False))
    pages.add(cal)
    kids.append(pages)
    kids.append(btn(s, "PlayMatch", (0.02, 0.03), (0.45, 0.12), "JOGAR PARTIDA", GN, 20))
    return kids


def match(s):
    kids = [N(s, "Background", color=BG)]
    hud = N(s, "Hud", (0, 0.86), (1, 1), color=HD)
    hud.add(N(s, "HomeName", (0.04, 0.15), (0.3, 0.85), text="CASA", tsize=18, align=3, style=1))
    hud.add(N(s, "Score", (0.35, 0.2), (0.65, 0.9), text="0  -  0", tsize=32, align=4, style=1))
    hud.add(N(s, "AwayName", (0.7, 0.15), (0.96, 0.85), text="FORA", tsize=18, align=5, style=1))
    hud.add(N(s, "Clock", (0.4, 0.02), (0.6, 0.28), text="00'", tsize=16, align=4))
    hud.add(N(s, "Event", (0.04, -0.55), (0.96, -0.05), text="Início", tsize=16, align=4))
    kids.append(hud)
    pitch = N(s, "Pitch", (0.08, 0.12), (0.92, 0.78), color=PI)
    for i in range(11):
        pitch.add(N(s, f"HomePlayer{i}", (0.48, 0.48), (0.52, 0.52), color=GN, size=(28, 28), pivot=(0.5, 0.5)))
        pitch.add(N(s, f"AwayPlayer{i}", (0.48, 0.48), (0.52, 0.52), color=(0.7, 0.2, 0.2, 1), size=(28, 28)))
    pitch.add(N(s, "Ball", (0.49, 0.49), (0.51, 0.51), color=(1, 1, 1, 1), size=(16, 16)))
    kids.append(pitch)
    kids.append(btn(s, "PauseButton", (0.82, 0.03), (0.97, 0.1), "PAUSA", DK, 16))
    pause = N(s, "PausePanel", (0.22, 0.22), (0.78, 0.78), color=PN, active=0)
    pause.add(N(s, "Tactics", (0.06, 0.55), (0.94, 0.9), text="Tática", tsize=18, align=0))
    pause.add(btn(s, "CycleFormation", (0.06, 0.4), (0.48, 0.52), "FORMAÇÃO", GN, 16))
    pause.add(btn(s, "CyclePressing", (0.52, 0.4), (0.94, 0.52), "PRESSÃO", GN, 16))
    pause.add(btn(s, "Sub", (0.06, 0.24), (0.94, 0.36), "SUBSTITUIR (último no XI)", DK, 16))
    pause.add(btn(s, "Resume", (0.2, 0.08), (0.8, 0.2), "CONTINUAR", GN, 18))
    kids.append(pause)
    ht = N(s, "HalfTimePanel", (0.18, 0.22), (0.82, 0.78), color=PN, active=0)
    ht.add(N(s, "Title", (0.06, 0.78), (0.94, 0.95), text="INTERVALO  ·  CONVERSA COM O PLANTEL", tsize=20, align=4, style=1))
    ht.add(btn(s, "TalkCalm", (0.08, 0.55), (0.92, 0.72), "MANTER O PLANO", GN, 18))
    ht.add(btn(s, "TalkDemand", (0.08, 0.35), (0.92, 0.52), "EXIGIR MAIS", GN, 18))
    ht.add(btn(s, "TalkAttack", (0.08, 0.15), (0.92, 0.32), "SUBIR AS LINHAS", GN, 18))
    kids.append(ht)
    ft = N(s, "FullTimePanel", (0.22, 0.28), (0.78, 0.72), color=PN, active=0)
    ft.add(N(s, "Result", (0.06, 0.45), (0.94, 0.85), text="0-0", tsize=28, align=4, style=1))
    ft.add(btn(s, "Continue", (0.2, 0.12), (0.8, 0.36), "CONTINUAR", GN, 20))
    kids.append(ft)
    return kids


def write_all():
    g_main = write_scene("MainMenu", main_menu, guid="8c9cfa26abfee488c85f1582747f6a02", include_root=True)
    scenes = {
        "MainMenu": g_main,
        "LeagueSelect": write_scene("LeagueSelect", league),
        "TeamSelect": write_scene("TeamSelect", team),
        "ClubInfo": write_scene("ClubInfo", club),
        "CreateCareer": write_scene("CreateCareer", create),
        "CareerHub": write_scene("CareerHub", hub),
        "MatchDay": write_scene("MatchDay", match),
    }
    build = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "ProjectSettings", "EditorBuildSettings.asset"))
    lines = ["%YAML 1.1", "%TAG !u! tag:unity3d.com,2011:", "--- !u!1045 &1", "EditorBuildSettings:", "  m_ObjectHideFlags: 0", "  serializedVersion: 2", "  m_Scenes:"]
    for name, guid in scenes.items():
        lines.append("  - enabled: 1")
        lines.append(f"    path: Assets/Scenes/{name}.unity")
        lines.append(f"    guid: {guid}")
    lines += [
        "  m_configObjects:",
        "    com.unity.input.settings.actions: {fileID: -944628639613478452, guid: 2bcd2660ca9b64942af0de543d8d7100, type: 3}",
        "  m_UseUCBPForAssetBundles: 0",
        "",
    ]
    with open(build, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(lines))
    print("updated build settings")


if __name__ == "__main__":
    write_all()
