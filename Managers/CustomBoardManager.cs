/*
 * ii Reborn
 * Portions Copyright (C) 2025–2026 Goldentrophy Software
 * Licensed under GNU GPL v3.0-or-later — see LICENSE and NOTICE.
 * This file is part of a derivative work; see NOTICE for attribution
 * and modification history. Do not remove this notice.
 */

using GorillaNetworking;
using iiMenu.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using static iiMenu.Menu.Main;

namespace iiMenu.Managers
{
    public class CustomBoardManager : MonoBehaviour
    {
        public static CustomBoardManager instance;
        public void Awake()
        {
            instance = this;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            foreach (GameObject board in objectBoards.Values)
            {
                if (board != null)
                    Destroy(board);
            }
            objectBoards.Clear();
            textMeshPro.RemoveAll(t => t == null);
            foreach (var k in characterDistanceArchive.Keys.Where(k => k == null).ToList()) characterDistanceArchive.Remove(k);
            foreach (var k in textColorArchive.Keys.Where(k => k == null).ToList()) textColorArchive.Remove(k);
            boardPanelMaterials.Clear();
            foreach (ScreenTarget target in screenTargets.Values)
                RemoveScreenOverlay(target);
            screenTargets.Clear();
            screenTargetsDirty = true;
            if (ownsBoardMaterial && _boardMaterial != null)
            {
                Destroy(_boardMaterial);
                _boardMaterial = null;
            }
            instance = null;
        }

        private static bool _customBoardsEnabled = true;
        public static bool CustomBoardsEnabled
        {
            get => _customBoardsEnabled;
            set
            {
                if (_customBoardsEnabled == value)
                    return;

                _customBoardsEnabled = value;

                if (instance == null)
                    return;

                if (value)
                {
                    instance.ReloadBoards();
                    if (instance.motdTitle != null)
                        instance.motdTitle.SetActive(true);
                    if (instance.motdText != null)
                        instance.motdText.SetActive(true);

                    GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom/motdBodyText").SetActive(false);
                    GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom/motdHeadingText").SetActive(false);
                } else
                {
                    RestoreJoinTriggerScreens();

                    foreach (GameObject board in instance.objectBoards.Values)
                        Destroy(board);

                    instance.objectBoards.Clear();

                    instance.motdTitle.SetActive(false);
                    instance.motdText.SetActive(false);

                    GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom/motdHeadingText").SetActive(true);
                    GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom/motdBodyText").SetActive(true);

                    instance.ReloadBoards();
                }
            }
        }

        private static readonly Dictionary<TextMeshPro, float> characterDistanceArchive = new Dictionary<TextMeshPro, float>();

        private static readonly Dictionary<TextMeshPro, Color> textColorArchive = new Dictionary<TextMeshPro, Color>();

        private static bool _customBoardFonts;
        public static bool CustomBoardFonts
        {
            get => _customBoardFonts;
            set
            {
                if (!value && _customBoardFonts)
                {
                    foreach (TextMeshPro txt in instance.textMeshPro.Where(text => text.isActiveAndEnabled))
                    {
                        txt.SafeSetFont(instance.archiveGorillaTagFont);
                        txt.SafeSetFontStyle(FontStyles.Normal);

                        if (characterDistanceArchive.TryGetValue(txt, out float charDistance))
                            txt.characterSpacing = charDistance;
                    }
                }

                _customBoardFonts = value;
            }
        }

        private static Material _screenRed;
        private static Material _screenBlack;

        public static bool CustomBoardTextEnabled = true;
        public static bool CustomBoardOrange = true;
        private static readonly Color32 boardOrange = new Color32(255, 138, 0, 255);

        public static Color BoardTintColor
        {
            get
            {
                try
                {
                    if (buttonColors != null && buttonColors.Length > 0)
                        return buttonColors[0].GetCurrentColor();
                }
                catch { }

                return boardOrange;
            }
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material == null)
                return;

            try
            {
                if (material.HasProperty("_Color"))
                    material.SetColor("_Color", color);

                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", color);
            }
            catch { }
        }
        private static Material _boardMaterial = new Material(Shader.Find("GorillaTag/UberShader"));
        private static bool ownsBoardMaterial = true;
        public static Material BoardMaterial
        {
            get => _boardMaterial;
            set
            {
                bool ownsNextMaterial = value == null;
                Material nextMaterial = value ?? new Material(Shader.Find("GorillaTag/UberShader"));

                if (_boardMaterial != null && ownsBoardMaterial && _boardMaterial != nextMaterial)
                {
                    try { Destroy(_boardMaterial); } catch { }
                }

                _boardMaterial = nextMaterial;
                ownsBoardMaterial = ownsNextMaterial;
                instance?.ReloadBoards();
            }
        }

        #region Game Boards
        // The green board plates are baked into the merged "UnityTempFile" mesh chunks,
        // so they get colored by swapping the whole chunk's material at a fixed index.
        public const int StumpLeaderboardIndex = 3;
        public const int ForestLeaderboardIndex = 6;

        public static string motdTemplate = "You are using build {0} of ii Reborn. " +
        "This menu is completely free and open sourced, if you paid for this menu you have been scammed. " +
        "There are a total of <b>{1}</b> mods on this menu. " +
        "<color=red>We are not responsible for any bans using this menu.</color> " +
        "If you get banned while using this, it's your responsibility.\n\n" +
        "Current menu status: <b>Loading...</b>\n" +
        "Made with <3 by the ii Reborn contributors\n\n" +
        "<alpha=128>{2} {0} {3} — ii Reborn is a derivative work based on ii's Stupid Menu, the original work of Goldentrophy Software. It is not affiliated with or endorsed by Goldentrophy Software or iiDk.<alpha=255>";

        public Material forestMaterial;
        public Material stumpMaterial;
        private Material originalComputerMonitorMaterial;
        private Material originalConductScreenMaterial;
        private GameObject conductScreen;

        public GameObject motdTitle;
        public GameObject motdText;

        private TMP_FontAsset archiveGorillaTagFont;

        private bool hasFoundAllBoards;
        private bool loggedMissingBoardObjects;
        public void ReloadBoards()
        {
            hasFoundAllBoards = false;
            loggedMissingBoardObjects = false;
        }

        private static void ApplyJoinTriggerScreens()
        {
            try
            {
                foreach (GorillaNetworkJoinTrigger joinTrigger in PhotonNetworkController.Instance.allJoinTriggers)
                {
                    try
                    {
                        JoinTriggerUI ui = joinTrigger.ui;
                        JoinTriggerUITemplate temp = ui.template;

                        temp.ScreenBG_AbandonPartyAndSoloJoin = BoardMaterial;
                        temp.ScreenBG_AlreadyInRoom = BoardMaterial;
                        temp.ScreenBG_ChangingGameModeSoloJoin = BoardMaterial;
                        temp.ScreenBG_Error = BoardMaterial;
                        temp.ScreenBG_InPrivateRoom = BoardMaterial;
                        temp.ScreenBG_LeaveRoomAndGroupJoin = BoardMaterial;
                        temp.ScreenBG_LeaveRoomAndSoloJoin = BoardMaterial;
                        temp.ScreenBG_NotConnectedSoloJoin = BoardMaterial;

                        TextMeshPro text = ui.screenText;
                        if (text != null && !instance.textMeshPro.Contains(text))
                            instance.textMeshPro.Add(text);
                    }
                    catch { }
                }

                PhotonNetworkController.Instance.UpdateTriggerScreens();
            }
            catch { }
        }

        private static void RestoreJoinTriggerScreens()
        {
            try
            {
                foreach (GorillaNetworkJoinTrigger joinTrigger in PhotonNetworkController.Instance.allJoinTriggers)
                {
                    try
                    {
                        JoinTriggerUI ui = joinTrigger.ui;
                        JoinTriggerUITemplate temp = ui.template;

                        if (_screenRed == null)
                        {
                            _screenRed = new Material(Shader.Find("GorillaTag/UberShader"))
                            {
                                color = new Color32(226, 73, 41, 255)
                            };
                        }

                        if (_screenBlack == null)
                        {
                            _screenBlack = new Material(Shader.Find("GorillaTag/UberShader"))
                            {
                                color = new Color32(39, 34, 28, 255)
                            };
                        }

                        temp.ScreenBG_AbandonPartyAndSoloJoin = _screenRed;
                        temp.ScreenBG_AlreadyInRoom = _screenBlack;
                        temp.ScreenBG_Error = _screenRed;
                    }
                    catch { }
                }

                PhotonNetworkController.Instance.UpdateTriggerScreens();
            }
            catch { }
        }

        private static readonly HashSet<string> loggedBoardSurfaces = new HashSet<string>();

        private static void SwapLeaderboardPlate(Transform root, int index, ref Material archive)
        {
            if (root == null)
                return;

            List<Transform> plates = new List<Transform>();
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name.Contains("UnityTempFile"))
                    plates.Add(child);
            }

            if (index < 0 || index >= plates.Count)
            {
                LogManager.Log($"Board plate: UnityTempFile index {index} out of range ({plates.Count} chunks under {root.name})");
                return;
            }

            Renderer renderer = plates[index].GetComponent<Renderer>();
            if (renderer == null)
                return;

            if (archive == null)
                archive = renderer.sharedMaterial;

            if (CustomBoardsEnabled)
            {
                renderer.material = BoardMaterial;

                if (loggedBoardSurfaces.Add("plate:" + plates[index].name))
                    LogManager.Log($"Board plate: tinted {PathOf(plates[index])} chunks={plates.Count} original={(archive == null ? "none" : archive.name)}");
            }
            else if (archive != null)
                renderer.material = archive;
        }

        public void Update()
        {
            if (!hasFoundAllBoards)
            {
                try
                {
                    if (CustomBoardsEnabled)
                        ApplyJoinTriggerScreens();

                    string[] objectsWithTMPro = {
                            "Environment Objects/LocalObjects_Prefab/TreeRoom/CodeOfConductHeadingText",
                            "Environment Objects/LocalObjects_Prefab/TreeRoom/COCBodyText_TitleData",
                            "Environment Objects/LocalObjects_Prefab/TreeRoom/Data",
                            "Environment Objects/LocalObjects_Prefab/TreeRoom/FunctionSelect"
                        };
                    foreach (string objectName in objectsWithTMPro)
                    {
                        GameObject obj = GetObject(objectName);
                        if (obj != null)
                        {
                            TextMeshPro text = obj.GetComponent<TextMeshPro>();
                            if (!textMeshPro.Contains(text))
                                textMeshPro.Add(text);
                        }
                        else if (!loggedMissingBoardObjects)
                            LogManager.Log("Could not find " + objectName);
                    }

                    // Stump (motd + Code of Conduct) and Forest leaderboard plates
                    // are re-applied every second by EnsureBoardSurfaces().

                    GameObject forestBoard = GetObject("Environment Objects/LocalObjects_Prefab/Forest/ForestScoreboardAnchor/GorillaScoreBoard");
                    if (forestBoard != null)
                    {
                        Transform forestTransform = forestBoard.transform;
                        for (int i = 0; i < forestTransform.childCount; i++)
                        {
                            GameObject v = forestTransform.GetChild(i).gameObject;
                            if ((!v.name.Contains("Board Text") && !v.name.Contains("Scoreboard_OfflineText")) ||
                                !v.activeSelf) continue;

                            TextMeshPro text = v.GetComponent<TextMeshPro>();
                            if (!textMeshPro.Contains(text))
                                textMeshPro.Add(text);
                        }
                    }

                    loggedMissingBoardObjects = true;
                    hasFoundAllBoards = true;
                }
                catch (Exception exc)
                {
                    LogManager.LogError($"Error with board colors at {exc.StackTrace}: {exc.Message}");
                    hasFoundAllBoards = false;
                }
            }

            if (computerMonitor == null)
                computerMonitor = GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom/TreeRoomInteractables/GorillaComputerObject/ComputerUI/monitor/monitorScreen");

            Renderer computerMonitorRenderer = computerMonitor?.GetComponent<Renderer>();
            if (computerMonitorRenderer != null)
            {
                originalComputerMonitorMaterial ??= computerMonitorRenderer.sharedMaterial;
                if (CustomBoardsEnabled)
                    computerMonitorRenderer.material = BoardMaterial;
            }

            // The green panel under the Code of Conduct text is a plain mesh at a
            // fixed path, so it is tinted exactly like the computer monitor.
            if (conductScreen == null)
                conductScreen = GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom/TreeRoomInteractables/UI/CodeOfConduct_Group/StaticUnlit/screen");

            Renderer conductScreenRenderer = conductScreen?.GetComponent<Renderer>();
            if (conductScreenRenderer != null)
            {
                originalConductScreenMaterial ??= conductScreenRenderer.sharedMaterial;
                if (CustomBoardsEnabled)
                    conductScreenRenderer.material = BoardMaterial;
            }

            try
            {
                if (CustomBoardsEnabled)
                {
                SetMaterialColor(BoardMaterial, CustomBoardOrange ? BoardTintColor : backgroundColor.GetCurrentColor());

                if (motdTitle == null)
                {
                    GameObject motdObject = GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom/motdHeadingText");
                    motdTitle = Instantiate(motdObject, motdObject.transform.parent);
                    motdObject.SetActive(false);
                }

                TextMeshPro motdHeadingText = motdTitle.GetComponent<TextMeshPro>();
                if (!textMeshPro.Contains(motdHeadingText))
                    textMeshPro.Add(motdHeadingText);

                motdHeadingText.richText = true;
                motdHeadingText.SafeSetFontSize(100);
                motdHeadingText.SafeSetText($"Thanks for using {(doCustomName ? customMenuName : "ii <b>Reborn</b>")}!");
                motdHeadingText.SafeSetFontStyle(activeFontStyle);
                motdHeadingText.SafeSetFont(activeFont);
                FollowMenuSettings(motdHeadingText, -4f);

                if (doCustomName)
                    motdHeadingText.SafeSetText("Thanks for using " + NoRichtextTags(customMenuName) + "!");

                motdHeadingText.SafeSetText(FollowMenuSettings(motdHeadingText.text));

                motdHeadingText.color = textColors[0].GetCurrentColor();
                motdHeadingText.overflowMode = TextOverflowModes.Overflow;

                if (motdText == null)
                {
                    GameObject motdObject = GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom/motdBodyText");
                    motdText = Instantiate(motdObject, motdObject.transform.parent);
                    motdObject.SetActive(false);

                    motdText.GetComponent<PlayFabTitleDataTextDisplay>().enabled = false;
                }

                TextMeshPro motdBodyText = motdText.GetComponent<TextMeshPro>();
                if (!textMeshPro.Contains(motdBodyText))
                    textMeshPro.Add(motdBodyText);

                motdBodyText.richText = true;
                motdBodyText.SafeSetFontSize(100);
                motdBodyText.color = textColors[0].GetCurrentColor();
                motdBodyText.SafeSetFontStyle(activeFontStyle);
                motdBodyText.SafeSetFont(activeFont);
                FollowMenuSettings(motdBodyText, -4f);

                motdBodyText.SafeSetText(FollowMenuSettings(string.Format(motdTemplate, PluginInfo.Version, fullModAmount, PluginInfo.BetaBuild ? "Beta" : "Release", PluginInfo.BuildTimestamp )));
                }
                else
                    RestoreOriginalBoardScreens();
            }
            catch { }
            
            try
            {
                bool tintBoardText = CustomBoardsEnabled && CustomBoardTextEnabled;
                Color targetColor = textColors[0].GetCurrentColor();

                // Dropping destroyed references is pure cleanup, so it runs on a slow
                // cadence instead of every frame. Doing it per frame meant three LINQ
                // chains (and their garbage) on every single frame.
                if (Time.time >= nextArchivePruneTime)
                {
                    nextArchivePruneTime = Time.time + 1f;

                    textMeshPro.RemoveAll(t => t == null);
                    var deadKeys = characterDistanceArchive.Keys.Where(k => k == null).ToList();
                    foreach (var k in deadKeys) characterDistanceArchive.Remove(k);
                    var deadColorKeys = textColorArchive.Keys.Where(k => k == null).ToList();
                    foreach (var k in deadColorKeys) textColorArchive.Remove(k);
                }

                for (int i = textMeshPro.Count - 1; i >= 0; i--)
                {
                    TextMeshPro txt = textMeshPro[i];

                    if (txt == null || !txt.isActiveAndEnabled)
                        continue;

                    if (tintBoardText)
                    {
                        if (!textColorArchive.ContainsKey(txt))
                            textColorArchive[txt] = txt.color;

                        if (txt.color != targetColor)
                            txt.color = targetColor;
                    }
                    else if (textColorArchive.TryGetValue(txt, out Color archivedColor) && txt.color != archivedColor)
                        txt.color = archivedColor;

                    if (!CustomBoardFonts) continue;
                    archiveGorillaTagFont ??= txt.font;

                    if (!characterDistanceArchive.ContainsKey(txt))
                        characterDistanceArchive[txt] = txt.characterSpacing;

                    if (txt.characterSpacing != 0f)
                        txt.characterSpacing = 0f;

                    txt.SafeSetFont(activeFont);
                    txt.SafeSetFontStyle(activeFontStyle);
                }
            }
            catch { }

            try
            {
                EnsureBoardSurfaces();

                bool tintSurfaces = CustomBoardsEnabled && CustomBoardOrange;

                TintKnownScreens(tintSurfaces);

                if (GorillaScoreboardTotalUpdater.allScoreboards != null)
                {
                    foreach (GorillaScoreBoard scoreboard in GorillaScoreboardTotalUpdater.allScoreboards)
                    {
                        if (scoreboard == null)
                            continue;

                        TintBoardPanels(scoreboard.transform, tintSurfaces, 0);

                        if (scoreboard.leftPanel != null)
                            TintBoardPanels(scoreboard.leftPanel.transform, tintSurfaces, 0);

                        if (scoreboard.rightPanel != null)
                            TintBoardPanels(scoreboard.rightPanel.transform, tintSurfaces, 0);
                    }
                }
            }
            catch { }
        }

        // Screens are re-forced after every Update, so a material the game reassigns
        // during its own Update cannot beat the board color for a frame.
        public void LateUpdate()
        {
            if (CustomBoardsEnabled && CustomBoardOrange)
                ForceKnownScreens();
        }

        private static readonly HashSet<string> loggedObjectBoardFailures = new HashSet<string>();
        private static float nextBoardSurfacePass;
        private static float nextArchivePruneTime;

        // Keeps every colored surface present: the game resets materials on room
        // changes, map scenes reload their objects, and map anchors can spawn after
        // SceneLoaded fires. Everything re-applies on a one second cadence.
        private static void EnsureBoardSurfaces()
        {
            if (instance == null)
                return;

            if (Time.time < nextBoardSurfacePass)
                return;

            nextBoardSurfacePass = Time.time + 1f;

            SwapLeaderboardPlate(GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom")?.transform, StumpLeaderboardIndex, ref instance.stumpMaterial);
            SwapLeaderboardPlate(GetObject("Environment Objects/LocalObjects_Prefab/Forest")?.transform, ForestLeaderboardIndex, ref instance.forestMaterial);

            if (!CustomBoardsEnabled)
                return;

            foreach (KeyValuePair<string, BoardInformation> entry in BoardInformations)
            {
                try
                {
                    Scene scene = SceneManager.GetSceneByName(entry.Key);

                    if (!scene.IsValid() || !scene.isLoaded)
                        continue;

                    if (instance.objectBoards.TryGetValue(entry.Key, out GameObject existing) && existing != null)
                        continue;

                    instance.CreateObjectBoard(entry.Key, entry.Value.GameObjectPath, entry.Value.Position, entry.Value.Rotation, entry.Value.Scale);
                }
                catch { }
            }
        }

        private static bool IsBoardControl(string name) =>
            name.IndexOf("text", StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("button", StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("toggle", StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("icon", StringComparison.OrdinalIgnoreCase) >= 0;

        private static readonly Dictionary<Renderer, Material> boardPanelMaterials = new Dictionary<Renderer, Material>();

        private static void TintBoardPanels(Transform target, bool tint, int depth)
        {
            if (target == null || depth > 2)
                return;

            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null && renderer.GetComponent<TMP_Text>() == null && !IsBoardControl(target.name))
            {
                if (tint)
                {
                    if (!boardPanelMaterials.ContainsKey(renderer))
                        boardPanelMaterials[renderer] = renderer.sharedMaterial;

                    if (renderer.sharedMaterial != BoardMaterial)
                        renderer.material = BoardMaterial;
                }
                else if (boardPanelMaterials.TryGetValue(renderer, out Material originalMaterial))
                {
                    try
                    {
                        if (originalMaterial != null)
                            renderer.sharedMaterial = originalMaterial;
                    }
                    catch { }

                    boardPanelMaterials.Remove(renderer);
                }
            }

            for (int i = 0; i < target.childCount; i++)
                TintBoardPanels(target.GetChild(i), tint, depth + 1);
        }

        // Computers and wall monitors all use one of a handful of exact screen names,
        // so they are matched by name equality only - no heuristics.
        private static readonly string[] ScreenNames =
        {
            "monitorscreen", "motdscreen", "wallmonitorscreen_small", "screengreen", "screenred"
        };

        private class ScreenTarget
        {
            public Renderer renderer;
            public Material[] originals;
            public GameObject overlay;
            public Material overlayMaterial;
            public Material overlayArchive;
            public int reapplies;
            public bool loggedOverwrite;
        }

        private static Dictionary<Renderer, ScreenTarget> screenTargets = new Dictionary<Renderer, ScreenTarget>();
        private static readonly HashSet<Renderer> loggedScreenDiagnostics = new HashSet<Renderer>();
        private static float nextScreenPass;
        private static readonly HashSet<int> scannedScenes = new HashSet<int>();
        private static readonly Dictionary<int, int> sceneScanPasses = new Dictionary<int, int>();
        private const int SceneScanPasses = 4;

        private static string NormalizedName(Transform target)
        {
            string name = target.name;
            int suffix = name.LastIndexOf(" (", StringComparison.Ordinal);

            if (suffix > 0 && name.EndsWith(")"))
                name = name.Substring(0, suffix);

            return name.ToLowerInvariant();
        }

        private static void TintKnownScreens(bool tint)
        {
            // Static map geometry is merged by the game's EdMeshCombiner (the
            // "UnityTempFile-... (combined by EdMeshCombiner)" chunks) and the source
            // renderers are left disabled, so a screen object is not always the mesh
            // that draws it. Matched screens are therefore re-applied every frame:
            // material swap when the screen owns its own mesh, and an overlay quad
            // fitted to the screen's mesh when the face is baked into a chunk.
            if (tint)
                ForceKnownScreens();
            else if (screenTargets.Count > 0)
            {
                foreach (KeyValuePair<Renderer, ScreenTarget> entry in screenTargets.ToList())
                {
                    Renderer renderer = entry.Key;
                    ScreenTarget target = entry.Value;

                    RemoveScreenOverlay(target);

                    try
                    {
                        if (renderer != null && target.originals != null)
                            renderer.sharedMaterials = target.originals;
                    }
                    catch { }

                    if (renderer != null && target.overlayArchive != null)
                    {
                        try { renderer.sharedMaterial = target.overlayArchive; }
                        catch { }
                    }

                    screenTargets.Remove(renderer);
                    screenTargetsDirty = true;
                }
            }

            if (Time.time < nextScreenPass)
                return;

            nextScreenPass = Time.time + 1f;

            if (!tint)
                return;

            try
            {
                screenTargets = screenTargets.Where(entry => entry.Key != null)
                    .ToDictionary(entry => entry.Key, entry => entry.Value);
                screenTargetsDirty = true;

                List<Renderer> sceneRenderers = new List<Renderer>();
                List<Renderer> candidates = new List<Renderer>();

                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene scene = SceneManager.GetSceneAt(i);

                    if (!scene.IsValid() || !scene.isLoaded || scannedScenes.Contains(scene.handle))
                        continue;

                    sceneScanPasses.TryGetValue(scene.handle, out int pass);
                    pass++;
                    sceneScanPasses[scene.handle] = pass;

                    if (pass >= SceneScanPasses)
                        scannedScenes.Add(scene.handle);

                    foreach (GameObject root in scene.GetRootGameObjects())
                        sceneRenderers.AddRange(root.GetComponentsInChildren<Renderer>(true));
                }

                if (sceneRenderers.Count == 0)
                    return;

                foreach (Renderer renderer in sceneRenderers)
                {
                    if (renderer == null || renderer.GetComponent<TMP_Text>() != null)
                        continue;

                    if (!ScreenNames.Contains(NormalizedName(renderer.transform)))
                        continue;

                    candidates.Add(renderer);
                }

                bool needsDiagnostic = false;

                foreach (Renderer renderer in candidates)
                {
                    if (loggedScreenDiagnostics.Contains(renderer))
                        continue;

                    needsDiagnostic = true;
                    break;
                }

                Dictionary<Material, int> materialUse = null;

                if (needsDiagnostic)
                {
                    materialUse = new Dictionary<Material, int>();

                    foreach (Renderer renderer in sceneRenderers)
                    {
                        if (renderer == null)
                            continue;

                        Material material = renderer.sharedMaterial;

                        if (material == null)
                            continue;

                        materialUse.TryGetValue(material, out int used);
                        materialUse[material] = used + 1;
                    }
                }

                foreach (Renderer renderer in candidates)
                {
                    if (!screenTargets.ContainsKey(renderer))
                    {
                        screenTargets[renderer] = new ScreenTarget
                        {
                            renderer = renderer,
                            originals = CaptureScreenMaterials(renderer)
                        };

                        LogManager.Log($"Orange board: screen matched {PathOf(renderer.transform)}");
                    }

                    ApplyScreenTint(screenTargets[renderer]);

                    if (materialUse != null)
                        LogScreenDiagnostic(renderer, materialUse);
                }
            }
            catch { }
        }

        private static Material[] CaptureScreenMaterials(Renderer renderer)
        {
            Material[] materials = renderer.sharedMaterials;

            return materials == null || materials.Length == 0 ? new Material[1] : materials;
        }

        private static void ForceKnownScreens()
        {
            // screenTargets is walked every frame, so the key list is cached and only
            // rebuilt when the dictionary actually changes. screenTargets.ToList() here
            // allocated a fresh List (and boxed the enumerator) twice per frame.
            if (screenTargetsDirty)
                RebuildScreenTargetCache();

            for (int i = 0; i < screenTargetCache.Count; i++)
            {
                Renderer renderer = screenTargetCache[i];
                ScreenTarget target;

                if (!screenTargets.TryGetValue(renderer, out target))
                    continue;

                if (renderer == null)
                {
                    RemoveScreenOverlay(target);
                    screenTargets.Remove(renderer);
                    screenTargetsDirty = true;
                    continue;
                }

                ApplyScreenTint(target);
            }
        }

        private static readonly List<Renderer> screenTargetCache = new List<Renderer>();
        private static bool screenTargetsDirty = true;

        private static void RebuildScreenTargetCache()
        {
            screenTargetCache.Clear();
            foreach (KeyValuePair<Renderer, ScreenTarget> entry in screenTargets)
                screenTargetCache.Add(entry.Key);

            screenTargetsDirty = false;
        }

        private static void ApplyScreenTint(ScreenTarget target)
        {
            Renderer renderer = target.renderer;

            if (renderer == null)
                return;

            if (CanTintScreenDirectly(renderer))
            {
                RemoveScreenOverlay(target);

                // Renderer.sharedMaterials allocates a fresh Material[] on every read and
                // this runs a few times per frame, so the single material case (which is
                // the normal one) checks sharedMaterial instead and never allocates.
                bool tinted;
                if (target.originals.Length == 1)
                {
                    tinted = renderer.sharedMaterial == BoardMaterial;
                }
                else
                {
                    Material[] current = renderer.sharedMaterials;
                    tinted = current != null && current.Length == target.originals.Length;

                    for (int i = 0; tinted && i < current.Length; i++)
                        tinted = current[i] == BoardMaterial;
                }

                if (tinted)
                    return;

                target.reapplies++;

                if (target.reapplies > 120 && !target.loggedOverwrite)
                {
                    target.loggedOverwrite = true;
                    LogManager.Log($"Orange board: screen material keeps getting replaced {PathOf(renderer.transform)}");
                }

                Material[] tintedMaterials = new Material[target.originals.Length];

                for (int i = 0; i < tintedMaterials.Length; i++)
                    tintedMaterials[i] = BoardMaterial;

                renderer.sharedMaterials = tintedMaterials;
                return;
            }

            EnsureScreenOverlay(target);
        }

        private static bool CanTintScreenDirectly(Renderer renderer)
        {
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                return false;

            Mesh mesh = GetScreenMesh(renderer);

            if (mesh == null)
                return false;

            Vector3 size = mesh.bounds.size;

            return Mathf.Max(size.x, Mathf.Max(size.y, size.z)) <= 3f;
        }

        private static Mesh GetScreenMesh(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned)
                return skinned.sharedMesh;

            MeshFilter filter = renderer.GetComponent<MeshFilter>();

            return filter == null ? null : filter.sharedMesh;
        }

        private static void EnsureScreenOverlay(ScreenTarget target)
        {
            Renderer renderer = target.renderer;

            if (renderer == null)
                return;

            if (!TryGetScreenRect(renderer, out Vector3 center, out Vector3 widthAxis, out float width, out Vector3 heightAxis, out float height, out Vector3 normal))
                return;

            if (target.overlay == null)
            {
                target.overlay = new GameObject("ii Reborn Screen Overlay");
                target.overlay.layer = renderer.gameObject.layer;
                target.overlay.AddComponent<MeshFilter>().sharedMesh = CreateQuadMesh();

                MeshRenderer meshRenderer = target.overlay.AddComponent<MeshRenderer>();
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
                meshRenderer.sharedMaterial = BoardMaterial;
                target.overlayMaterial = BoardMaterial;

                LogManager.Log($"Orange board: screen overlay {PathOf(renderer.transform)} size=({width:F2},{height:F2})");
            }

            target.overlay.SetActive(true);
            target.overlay.transform.SetPositionAndRotation(center, Quaternion.LookRotation(normal, heightAxis));
            target.overlay.transform.localScale = new Vector3(width, height, 1f);

            if (target.overlayMaterial != BoardMaterial)
            {
                target.overlayMaterial = BoardMaterial;

                MeshRenderer meshRenderer = target.overlay.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                    meshRenderer.sharedMaterial = BoardMaterial;
            }

            if (renderer.sharedMaterial == BoardMaterial)
            {
                // The material swap never showed because the face is baked into a map
                // chunk, so the disabled original is put back to its own material.
                if (target.overlayArchive == null)
                {
                    target.overlayArchive = target.originals.Length > 0 ? target.originals[0] : null;

                    try
                    {
                        if (target.overlayArchive != null)
                            renderer.sharedMaterial = target.overlayArchive;
                    }
                    catch { }
                }
            }
        }

        private static bool TryGetScreenRect(Renderer renderer, out Vector3 center, out Vector3 widthAxis, out float width, out Vector3 heightAxis, out float height, out Vector3 normal)
        {
            center = Vector3.zero;
            widthAxis = Vector3.right;
            heightAxis = Vector3.up;
            normal = Vector3.forward;
            width = 0f;
            height = 0f;

            Mesh mesh = GetScreenMesh(renderer);

            if (mesh == null)
                return false;

            Transform transform = renderer.transform;
            Bounds bounds = mesh.bounds;
            Vector3 size = bounds.size;

            // A screen is a flat panel, so the smallest mesh axis is its normal.
            int thin = 0;

            if (size.y <= size.x && size.y <= size.z)
                thin = 1;

            if (size.z <= size.x && size.z <= size.y)
                thin = 2;

            int first = thin == 0 ? 1 : 0;
            int second = thin == 2 ? 1 : 2;

            Vector3 scale = transform.lossyScale;
            float[] axisSize =
            {
                Mathf.Abs(size.x * scale.x), Mathf.Abs(size.y * scale.y), Mathf.Abs(size.z * scale.z)
            };

            width = axisSize[first];
            height = axisSize[second];

            // Anything larger is a combined map chunk, not a screen, so it is left alone.
            if (width < 0.02f || height < 0.02f || width > 3f || height > 3f)
                return false;

            widthAxis = transform.TransformDirection(UnitAxis(first));
            heightAxis = transform.TransformDirection(UnitAxis(second));

            Vector3 meshNormal = Vector3.zero;
            Vector3[] normals = mesh.normals;

            if (normals != null && normals.Length > 0)
                meshNormal = transform.TransformDirection(normals[0]);

            normal = meshNormal.sqrMagnitude > 0.0001f ? meshNormal.normalized : transform.TransformDirection(UnitAxis(thin)).normalized;
            center = transform.TransformPoint(bounds.center) + normal * 0.008f;

            return true;
        }

        private static Vector3 UnitAxis(int axis) => axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward;

        private static Mesh CreateQuadMesh()
        {
            Mesh mesh = new Mesh { name = "iiRebornScreenOverlay" };

            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f)
            };

            mesh.uv = new[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f)
            };

            mesh.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };

            // Both windings, so the plate is visible no matter which way the screen faces.
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3, 0, 3, 2, 0, 2, 1 };

            return mesh;
        }

        private static void RemoveScreenOverlay(ScreenTarget target)
        {
            if (target.overlay == null)
                return;

            try { Destroy(target.overlay); }
            catch { }

            target.overlay = null;
            target.overlayMaterial = null;
        }

        private static void LogScreenDiagnostic(Renderer renderer, Dictionary<Material, int> materialUse)
        {
            if (renderer == null || !loggedScreenDiagnostics.Add(renderer))
                return;

            Mesh mesh = GetScreenMesh(renderer);
            Material material = renderer.sharedMaterial;
            int sharedBy = 0;

            if (material != null)
                materialUse.TryGetValue(material, out sharedBy);

            LogManager.Log($"Screen diag {PathOf(renderer.transform)}: {renderer.GetType().Name} enabled={renderer.enabled} active={renderer.gameObject.activeInHierarchy} " +
                $"mesh={(mesh == null ? "none" : mesh.name)} verts={(mesh == null ? 0 : mesh.vertexCount)} meshSize={(mesh == null ? Vector3.zero : mesh.bounds.size)} worldSize={renderer.bounds.size} scale={renderer.transform.lossyScale} " +
                $"mat={(material == null ? "none" : material.name)} sharedBy={sharedBy} shader={(material == null || material.shader == null ? "none" : material.shader.name)} atlas={(material != null && material.HasProperty("_BaseMap_Atlas") ? "yes" : "no")}");
        }

        private static string PathOf(Transform target)
        {
            if (target == null)
                return null;

            string path = target.name;

            while (target.parent != null)
            {
                target = target.parent;
                path = target.name + "/" + path;
            }

            return path;
        }

        private void RestoreOriginalBoardScreens()
        {
            Renderer renderer = computerMonitor?.GetComponent<Renderer>();
            if (renderer != null && originalComputerMonitorMaterial != null)
                renderer.material = originalComputerMonitorMaterial;

            Renderer conductRenderer = conductScreen?.GetComponent<Renderer>();
            if (conductRenderer != null && originalConductScreenMaterial != null)
                conductRenderer.material = originalConductScreenMaterial;
        }
        #endregion

        #region Object Boards
        public readonly Dictionary<string, GameObject> objectBoards = new Dictionary<string, GameObject>();
        public List<GorillaNetworkJoinTrigger> triggers = new List<GorillaNetworkJoinTrigger>();
        public readonly List<TextMeshPro> textMeshPro = new List<TextMeshPro>();
        public GameObject computerMonitor;

        public void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            LogManager.Log($"SceneLoaded: {scene.name} ({mode})");
            loggedObjectBoardFailures.Remove(scene.name);
            scannedScenes.Remove(scene.handle);
            sceneScanPasses[scene.handle] = 0;

            // Join triggers on freshly loaded maps pick up the shared screen template,
            // but late-loaded triggers may have registered after the initial pass.
            if (CustomBoardsEnabled)
                ApplyJoinTriggerScreens();

            if (!BoardInformations.TryGetValue(scene.name, out var config)) return;

            CreateObjectBoard(scene.name, config.GameObjectPath, config.Position, config.Rotation, config.Scale);
        }

        public void CreateObjectBoard(string scene, string gameObject, Vector3? position = null, Vector3? rotation = null, Vector3? scale = null)
        {
            try
            {
                if (objectBoards.TryGetValue(scene, out GameObject existingBoard))
                {
                    if (existingBoard != null)
                        Destroy(existingBoard);

                    objectBoards.Remove(scene);
                }

                GameObject board = GameObject.CreatePrimitive(PrimitiveType.Plane);
                board.transform.parent = GetObject(gameObject).transform;
                board.transform.localPosition = position ?? new Vector3(-22.1964f, -34.9f, 0.57f);
                board.transform.localRotation = Quaternion.Euler(rotation ?? new Vector3(270f, 0f, 0f));
                board.transform.localScale = scale ?? new Vector3(21.6f, 2.4f, 22f);

                Destroy(board.GetComponent<Collider>());
                board.GetComponent<Renderer>().material = BoardMaterial;

                objectBoards.Add(scene, board);

                LogManager.Log($"Object board: created for {scene} under {gameObject} pos={board.transform.position:0.00} worldScale={board.transform.lossyScale:0.00}");
            }
            catch (Exception e)
            {
                if (loggedObjectBoardFailures.Add(scene))
                    LogManager.LogError($"Failed to create object board for scene {scene}: {e}");
            }
        }

        private readonly struct BoardInformation
        {
            public readonly string GameObjectPath;
            public readonly Vector3 Position;
            public readonly Vector3 Rotation;
            public readonly Vector3 Scale;

            public BoardInformation(string path, Vector3 pos, Vector3 rot, Vector3 scale)
            {
                GameObjectPath = path;
                Position = pos;
                Rotation = rot;
                Scale = scale;
            }
        }

        private static readonly Dictionary<string, BoardInformation> BoardInformations = new Dictionary<string, BoardInformation>
        {
            ["Canyon2"] = new BoardInformation(
                "Canyon/CanyonScoreboardAnchor/GorillaScoreBoard",
                new Vector3(-24.5019f, -28.7746f, 0.1f),
                new Vector3(270f, 0f, 0f),
                new Vector3(21.5946f, 1f, 22.1782f)
            ),
            ["Skyjungle"] = new BoardInformation(
                "skyjungle/UI/Scoreboard/GorillaScoreBoard",
                new Vector3(-21.2764f, -32.1928f, 0f),
                new Vector3(270.2987f, 0.2f, 359.9f),
                new Vector3(21.6f, 0.1f, 20.4909f)
            ),
            ["Mountain"] = new BoardInformation(
                "Mountain/MountainScoreboardAnchor/GorillaScoreBoard",
                Vector3.zero,
                Vector3.zero,
                Vector3.one
            ),
            ["Metropolis"] = new BoardInformation(
                "MetroMain/ComputerArea/Scoreboard/GorillaScoreBoard",
                new Vector3(-25.1f, -31f, 0.1502f),
                new Vector3(270.1958f, 0.2086f, 0f),
                new Vector3(21f, 102.9727f, 21.4f)
            ),
            ["Bayou"] = new BoardInformation(
                "BayouMain/ComputerArea/GorillaScoreBoardPhysical",
                new Vector3(-28.3419f, -26.851f, 0.3f),
                new Vector3(270f, 0f, 0f),
                new Vector3(21.3636f, 38f, 21f)
            ),
            ["Beach"] = new BoardInformation(
                "BeachScoreboardAnchor/GorillaScoreBoard",
                new Vector3(-22.1964f, -33.7126f, 0.1f),
                new Vector3(270.056f, 0f, 0f),
                new Vector3(21.2f, 2f, 21.6f)
            ),
            ["Cave"] = new BoardInformation(
                "Cave_Main_Prefab/CrystalCaveScoreboardAnchor/GorillaScoreBoard",
                new Vector3(-22.1964f, -33.7126f, 0.1f),
                new Vector3(270.056f, 0f, 0f),
                new Vector3(21.2f, 2f, 21.6f)
            ),
            ["Rotating"] = new BoardInformation(
                "RotatingPermanentEntrance/UI (1)/RotatingScoreboard/RotatingScoreboardAnchor/GorillaScoreBoard",
                new Vector3(-22.1964f, -33.7126f, 0.1f),
                new Vector3(270.056f, 0f, 0f),
                new Vector3(21.2f, 2f, 21.6f)
            ),
            ["MonkeBlocks"] = new BoardInformation(
                "Environment Objects/MonkeBlocksRoomPersistent/AtticScoreBoard/AtticScoreboardAnchor/GorillaScoreBoard",
                new Vector3(-22.1964f, -24.5091f, 0.57f),
                new Vector3(270.1856f, 0.1f, 0f),
                new Vector3(21.6f, 1.2f, 20.8f)
            ),
            ["Basement"] = new BoardInformation(
                "Basement/BasementScoreboardAnchor/GorillaScoreBoard/",
                new Vector3(-22.1964f, -24.5091f, 0.57f),
                new Vector3(270.1856f, 0.1f, 0f),
                new Vector3(21.6f, 1.2f, 20.8f)
            ),
            ["City"] = new BoardInformation(
                "City_Pretty/CosmeticsScoreboardAnchor/GorillaScoreBoard",
                new Vector3(-22.1964f, -34.9f, 0.57f),
                new Vector3(270f, 0f, 0f),
                new Vector3(21.6f, 2.4f, 22f)
            )
        };
        #endregion
    }
}
