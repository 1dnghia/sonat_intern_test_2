using System;
using System.Collections.Generic;
using System.IO;
using TapAway;
using TapAway.Core;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace TapAway.Editor
{
    public static class AddressablesAutoSetup
    {
        private const string LEVEL_GROUP = "Levels";
        private const string PREFAB_GROUP = "Prefabs_Grid";
        private const string AUDIO_BGM_GROUP = "Audio_BGM";
        private const string AUDIO_SFX_GAMEPLAY_GROUP = "Audio_SFX_Gameplay";
        private const string AUDIO_SFX_UI_GROUP = "Audio_SFX_UI";
        private const string VFX_GROUP = "VFX";
        private const string ROOT_FOLDER_GROUP = "Folders_Root";

        private const string DATA_FOLDER = "Assets/Data";
        private const string LEVEL_FOLDER = "Assets/Data/Levels";
        private const string AUDIO_BGM_FOLDER = "Assets/Audio/BGM";
        private const string AUDIO_BMG_FOLDER_ALT = "Assets/Audio/BMG";
        private const string AUDIO_SFX_GAMEPLAY_FOLDER = "Assets/Audio/SFX/Gameplay";
        private const string AUDIO_SFX_UI_FOLDER = "Assets/Audio/SFX/UI";
        private const string AUDIO_FOLDER = "Assets/Audio";
        private const string PREFABS_FOLDER = "Assets/Prefabs";

        [MenuItem("TapAway/Addressables/Auto Setup Project")]
        public static void AutoSetupProjectAddressables()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                Debug.LogError("[AddressablesAutoSetup] Cannot create/find Addressables settings.");
                return;
            }

            AddressableAssetGroup levelGroup = GetOrCreateGroup(settings, LEVEL_GROUP);
            AddressableAssetGroup prefabGroup = GetOrCreateGroup(settings, PREFAB_GROUP);
            AddressableAssetGroup bgmGroup = GetOrCreateGroup(settings, AUDIO_BGM_GROUP);
            AddressableAssetGroup gameplaySfxGroup = GetOrCreateGroup(settings, AUDIO_SFX_GAMEPLAY_GROUP);
            AddressableAssetGroup uiSfxGroup = GetOrCreateGroup(settings, AUDIO_SFX_UI_GROUP);
            AddressableAssetGroup vfxGroup = GetOrCreateGroup(settings, VFX_GROUP);
            AddressableAssetGroup rootFolderGroup = GetOrCreateGroup(settings, ROOT_FOLDER_GROUP);

            // Add folder root entries để Inspector của folder cũng hiển thị Addressable đã bật.
            CreateFolderEntry(settings, rootFolderGroup, DATA_FOLDER, "folders/data");
            CreateFolderEntry(settings, rootFolderGroup, LEVEL_FOLDER, "folders/data/levels");
            CreateFolderEntry(settings, rootFolderGroup, PREFABS_FOLDER, "folders/prefabs");
            CreateFolderEntry(settings, rootFolderGroup, AUDIO_FOLDER, "folders/audio");
            CreateFolderEntry(settings, rootFolderGroup, AUDIO_BGM_FOLDER, "folders/audio/bgm");
            CreateFolderEntry(settings, rootFolderGroup, AUDIO_BMG_FOLDER_ALT, "folders/audio/bmg");
            CreateFolderEntry(settings, rootFolderGroup, AUDIO_SFX_GAMEPLAY_FOLDER, "folders/audio/sfx/gameplay");
            CreateFolderEntry(settings, rootFolderGroup, AUDIO_SFX_UI_FOLDER, "folders/audio/sfx/ui");

            List<(string guid, string path)> levelAssets = FindLevelAssets();
            foreach ((string guid, string path) in levelAssets)
            {
                CreateOrMoveEntry(settings, levelGroup, guid, BuildLevelAddress(path));
            }

            string normalGuid = GetGuidByPath("Assets/Prefabs/NormalBlock.prefab");
            string gearGuid = GetGuidByPath("Assets/Prefabs/GearBlock.prefab");
            string rotatorGuid = GetGuidByPath("Assets/Prefabs/RotatorBlock.prefab");
            string linkGuid = GetGuidByPath("Assets/Prefabs/Link.prefab");

            CreateOrMoveEntry(settings, prefabGroup, normalGuid, "prefabs/grid/normal_block");
            CreateOrMoveEntry(settings, prefabGroup, gearGuid, "prefabs/grid/gear_block");
            CreateOrMoveEntry(settings, prefabGroup, rotatorGuid, "prefabs/grid/rotator_block");
            CreateOrMoveEntry(settings, prefabGroup, linkGuid, "prefabs/rotator/link");

            // VFX prefab theo convention tên file; nếu chưa có thì bỏ qua an toàn.
            string gearHitVfxGuid = FindFirstGuidByNameContains("Assets", "gear", "hit", "prefab");
            CreateOrMoveEntry(settings, vfxGroup, gearHitVfxGuid, "vfx/block/gear_hit");

            CreateFolderAudioEntries(settings, bgmGroup, AUDIO_BGM_FOLDER, "audio/bgm");
            CreateFolderAudioEntries(settings, bgmGroup, AUDIO_BMG_FOLDER_ALT, "audio/bmg");
            CreateFolderAudioEntries(settings, gameplaySfxGroup, AUDIO_SFX_GAMEPLAY_FOLDER, "audio/sfx/gameplay");
            CreateFolderAudioEntries(settings, uiSfxGroup, AUDIO_SFX_UI_FOLDER, "audio/sfx/ui");

            AssignSceneReferences(levelAssets, normalGuid, gearGuid, rotatorGuid, linkGuid, gearHitVfxGuid);

            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(settings);
            Debug.Log("[AddressablesAutoSetup] Auto setup completed.");
        }

        private static void AssignSceneReferences(
            List<(string guid, string path)> levelAssets,
            string normalGuid,
            string gearGuid,
            string rotatorGuid,
            string linkGuid,
            string gearHitVfxGuid)
        {
            // Đồng bộ scene object để team không phải gán tay từng field mỗi scene.
            LevelManager[] levelManagers = Resources.FindObjectsOfTypeAll<LevelManager>();
            for (int i = 0; i < levelManagers.Length; i++)
            {
                LevelManager manager = levelManagers[i];
                if (!IsEditableSceneObject(manager))
                {
                    continue;
                }

                SerializedObject so = new SerializedObject(manager);

                SerializedProperty levelRefs = so.FindProperty("_levelReferences");
                if (levelRefs == null)
                {
                    continue;
                }

                levelRefs.arraySize = levelAssets.Count;
                for (int index = 0; index < levelAssets.Count; index++)
                {
                    SerializedProperty element = levelRefs.GetArrayElementAtIndex(index);
                    SetAssetReferenceGuid(element, levelAssets[index].guid);
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(manager);
            }

            GridView[] gridViews = Resources.FindObjectsOfTypeAll<GridView>();
            for (int i = 0; i < gridViews.Length; i++)
            {
                GridView gridView = gridViews[i];
                if (!IsEditableSceneObject(gridView))
                {
                    continue;
                }

                SerializedObject so = new SerializedObject(gridView);
                SetAssetReferenceGuid(so.FindProperty("_normalBlockPrefabRef"), normalGuid);
                SetAssetReferenceGuid(so.FindProperty("_gearBlockPrefabRef"), gearGuid);
                SetAssetReferenceGuid(so.FindProperty("_rotatorBlockPrefabRef"), rotatorGuid);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(gridView);
            }

            RotatorView[] rotatorViews = Resources.FindObjectsOfTypeAll<RotatorView>();
            for (int i = 0; i < rotatorViews.Length; i++)
            {
                RotatorView rotatorView = rotatorViews[i];
                if (!IsEditableSceneObject(rotatorView))
                {
                    continue;
                }

                SerializedObject so = new SerializedObject(rotatorView);
                SetAssetReferenceGuid(so.FindProperty("_linkPrefabRef"), linkGuid);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(rotatorView);
            }

            BlockView[] blockViews = Resources.FindObjectsOfTypeAll<BlockView>();
            for (int i = 0; i < blockViews.Length; i++)
            {
                BlockView blockView = blockViews[i];
                if (!IsEditableSceneObject(blockView))
                {
                    continue;
                }

                SerializedObject so = new SerializedObject(blockView);
                SetAssetReferenceGuid(so.FindProperty("_gearHitVfxPrefabRef"), gearHitVfxGuid);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(blockView);
            }

            AudioManager[] audioManagers = Resources.FindObjectsOfTypeAll<AudioManager>();
            for (int i = 0; i < audioManagers.Length; i++)
            {
                AudioManager audioManager = audioManagers[i];
                if (!IsEditableSceneObject(audioManager))
                {
                    continue;
                }

                SerializedObject so = new SerializedObject(audioManager);

                SetAssetReferenceGuid(so.FindProperty("_gameplayBgmRef"), FindAudioGuidInBgmFolders("gameplay"));
                SetAssetReferenceGuid(so.FindProperty("_winBgmRef"), FindAudioGuidInBgmFolders("win"));
                SetAssetReferenceGuid(so.FindProperty("_loseBgmRef"), FindAudioGuidInBgmFolders("lose"));

                SetAssetReferenceGuid(so.FindProperty("_tapNormalSfxRef"), FindAudioGuid(AUDIO_SFX_GAMEPLAY_FOLDER, "tap", "normal"));
                SetAssetReferenceGuid(so.FindProperty("_tapRotatorSfxRef"), FindAudioGuid(AUDIO_SFX_GAMEPLAY_FOLDER, "tap", "rotator"));
                SetAssetReferenceGuid(so.FindProperty("_normalHitGearSfxRef"), FindAudioGuid(AUDIO_SFX_GAMEPLAY_FOLDER, "hit", "gear"));
                SetAssetReferenceGuid(so.FindProperty("_bombExplodeSfxRef"), FindAudioGuid(AUDIO_SFX_GAMEPLAY_FOLDER, "bomb"));
                SetAssetReferenceGuid(so.FindProperty("_uiClickSfxRef"), FindAudioGuid(AUDIO_SFX_UI_FOLDER, "click"));

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(audioManager);
            }
        }

        private static bool IsEditableSceneObject(UnityEngine.Object obj)
        {
            if (obj == null)
            {
                return false;
            }

            string path = AssetDatabase.GetAssetPath(obj);
            return string.IsNullOrEmpty(path);
        }

        private static List<(string guid, string path)> FindLevelAssets()
        {
            string[] guids = AssetDatabase.FindAssets("t:LevelData", new[] { LEVEL_FOLDER });
            List<(string guid, string path)> result = new List<(string guid, string path)>();
            for (int i = 0; i < guids.Length; i++)
            {
                string guid = guids[i];
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path);
                if (!fileName.StartsWith("Level_", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                result.Add((guid, path));
            }

            result.Sort((a, b) => CompareLevelPath(a.path, b.path));
            return result;
        }

        private static int CompareLevelPath(string pathA, string pathB)
        {
            int a = ExtractTrailingNumber(Path.GetFileNameWithoutExtension(pathA));
            int b = ExtractTrailingNumber(Path.GetFileNameWithoutExtension(pathB));
            if (a != b)
            {
                return a.CompareTo(b);
            }

            return string.Compare(pathA, pathB, StringComparison.OrdinalIgnoreCase);
        }

        private static int ExtractTrailingNumber(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return int.MaxValue;
            }

            int underscoreIndex = input.LastIndexOf('_');
            if (underscoreIndex < 0 || underscoreIndex >= input.Length - 1)
            {
                return int.MaxValue;
            }

            string suffix = input.Substring(underscoreIndex + 1);
            return int.TryParse(suffix, out int value) ? value : int.MaxValue;
        }

        private static void CreateFolderAudioEntries(
            AddressableAssetSettings settings,
            AddressableAssetGroup group,
            string folder,
            string keyPrefix)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folder });
            for (int i = 0; i < guids.Length; i++)
            {
                string guid = guids[i];
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                CreateOrMoveEntry(settings, group, guid, keyPrefix + "/" + fileName);
            }
        }

        private static string BuildLevelAddress(string path)
        {
            string fileName = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            return "levels/" + fileName;
        }

        private static string GetGuidByPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) || !File.Exists(assetPath))
            {
                return string.Empty;
            }

            return AssetDatabase.AssetPathToGUID(assetPath);
        }

        private static string FindAudioGuid(string folder, params string[] keywords)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return string.Empty;
            }

            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                string name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                bool matches = true;
                for (int k = 0; k < keywords.Length; k++)
                {
                    if (!name.Contains(keywords[k].ToLowerInvariant()))
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                {
                    return guids[i];
                }
            }

            return string.Empty;
        }

        private static string FindAudioGuidInBgmFolders(params string[] keywords)
        {
            string guid = FindAudioGuid(AUDIO_BGM_FOLDER, keywords);
            if (!string.IsNullOrEmpty(guid))
            {
                return guid;
            }

            guid = FindAudioGuid(AUDIO_BMG_FOLDER_ALT, keywords);
            if (!string.IsNullOrEmpty(guid))
            {
                return guid;
            }

            Debug.LogWarning("[AddressablesAutoSetup] Cannot auto-find BGM clip by keywords: "
                + string.Join(",", keywords));
            return string.Empty;
        }

        private static string FindFirstGuidByNameContains(string folder, params string[] keywords)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return string.Empty;
            }

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                string name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                bool matches = true;
                for (int k = 0; k < keywords.Length; k++)
                {
                    if (!name.Contains(keywords[k].ToLowerInvariant()))
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                {
                    return guids[i];
                }
            }

            return string.Empty;
        }

        private static AddressableAssetGroup GetOrCreateGroup(AddressableAssetSettings settings, string groupName)
        {
            AddressableAssetGroup group = settings.FindGroup(groupName);
            if (group != null)
            {
                return group;
            }

            return settings.CreateGroup(
                groupName,
                false,
                false,
                false,
                null,
                typeof(BundledAssetGroupSchema),
                typeof(ContentUpdateGroupSchema));
        }

        private static void CreateOrMoveEntry(
            AddressableAssetSettings settings,
            AddressableAssetGroup group,
            string guid,
            string address)
        {
            if (string.IsNullOrEmpty(guid) || group == null)
            {
                return;
            }

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, false, false);
            if (entry == null)
            {
                return;
            }

            entry.address = address;
        }

        private static void CreateFolderEntry(
            AddressableAssetSettings settings,
            AddressableAssetGroup group,
            string folderPath,
            string address)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string guid = AssetDatabase.AssetPathToGUID(folderPath);
            CreateOrMoveEntry(settings, group, guid, address);
        }

        private static void SetAssetReferenceGuid(SerializedProperty assetReferenceProperty, string guid)
        {
            if (assetReferenceProperty == null)
            {
                return;
            }

            // Không tìm được GUID mới thì giữ nguyên reference hiện tại, tránh mất gán tay của user.
            if (string.IsNullOrEmpty(guid))
            {
                return;
            }

            SerializedProperty guidProperty = assetReferenceProperty.FindPropertyRelative("m_AssetGUID");
            if (guidProperty != null)
            {
                guidProperty.stringValue = guid ?? string.Empty;
            }

            SerializedProperty subObjectProperty = assetReferenceProperty.FindPropertyRelative("m_SubObjectName");
            if (subObjectProperty != null)
            {
                subObjectProperty.stringValue = string.Empty;
            }

            SerializedProperty subObjectTypeProperty = assetReferenceProperty.FindPropertyRelative("m_SubObjectType");
            if (subObjectTypeProperty != null)
            {
                subObjectTypeProperty.stringValue = string.Empty;
            }
        }
    }
}
