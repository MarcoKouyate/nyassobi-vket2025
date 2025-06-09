#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VitDeck.Validator;
using VketTools.Utilities;
using Object = UnityEngine.Object;

namespace VketTools.Validator
{
    public class VketTargetFinder : IValidationTargetFinder
    {
        bool finded = false;

        string[] assetGUIDs;
        Object[] assetObjects;
        string[] assetPaths;
        GameObject[] rootObjects;
        GameObject[] allObjects;
        Scene[] scenes;
        ValidationTarget target;
        
        public IReadonlyReferenceDictionary ReferenceDictionary { get; private set; }

        private void FindInternal(string baseFolder, bool forceOpenScene = false)
        {
            if (!Directory.Exists(baseFolder))
            {
                throw new FatalValidationErrorException(AssetUtility.GetValidator("VketTargetFinder.PackageFolderNotFound"));
            }

            var exhibitorID = Path.GetFileName(baseFolder);

            var targetScene = OpenPackageScene(exhibitorID);

            scenes = new Scene[] { targetScene };
            var exhibitRootObject = GetExhibitRootObject(exhibitorID, targetScene);

            rootObjects = new GameObject[] { exhibitRootObject };

            allObjects = exhibitRootObject
                .GetComponentsInChildren<Transform>(true)
                .Select(tf => tf.gameObject)
                .ToArray();

            ReferenceDictionary = GetContainsAndReferredAssets(baseFolder, allObjects);
            assetObjects = ReferenceDictionary.Reverse.Keys
                .Where(asset => IsTargetAsset(AssetDatabase.GetAssetPath(asset)))
                .ToArray();

            assetPaths = assetObjects
                .Select(asset => AssetDatabase.GetAssetPath(asset))
                .ToArray();

            assetGUIDs = assetPaths
                .Select(path => AssetDatabase.AssetPathToGUID(path))
                .ToArray();

            target = new ValidationTarget(
                baseFolder,
                assetGUIDs,
                assetPaths,
                assetObjects,
                scenes,
                rootObjects,
                allObjects);

            finded = true;
        }

        private static bool IsTargetAsset(string assetPath)
        {
            return assetPath.StartsWith("Assets/") || assetPath.StartsWith("Packages/com.vrchat.");
        }

        private static GameObject GetExhibitRootObject(string exhibitorID, Scene targetScene)
        {
            var exhibitRootObjects = targetScene
                .GetRootGameObjects()
                .Where(obj => obj.name == exhibitorID)
                .ToArray();

            if (exhibitRootObjects.Length == 0)
            {
                throw new FatalValidationErrorException(AssetUtility.GetValidator("VketTargetFinder.ExhibitNotFound"));

            }
            else if (exhibitRootObjects.Length > 1)
            {
                throw new FatalValidationErrorException(AssetUtility.GetValidator("VketTargetFinder.ManyExhibits"));
            }
            else
            {
                return exhibitRootObjects[0];
            }
        }

        private static IReadonlyReferenceDictionary GetContainsAndReferredAssets(string baseFolder, GameObject[] gameObjects)
        {
            var referenceChain = VketUnityObjectReferenceChain
                .ExploreFrom(
                    Enumerable.Concat(
                        VitDeck.Utilities.AssetUtility.EnumerateAssets(baseFolder),
                        gameObjects
                    ));

            return referenceChain.Result;
        }

        private static Scene OpenPackageScene(string exhibitorID)
        {
            var scenePath = string.Format("Assets/{0}/{0}.unity", exhibitorID);
            if (!File.Exists(scenePath))
            {
                throw new FatalValidationErrorException(AssetUtility.GetValidator("VketTargetFinder.SceneNotFound", scenePath));
            }
            var targetScene = EditorSceneManager.GetSceneByPath(scenePath);

            if (!targetScene.isLoaded)
            {
                if (!EditorUtility.DisplayDialog(
                    AssetUtility.GetValidator("VketTargetFinder.SceneOpenDialog.Title"),
                    AssetUtility.GetValidator("VketTargetFinder.SceneOpenDialog") + Environment.NewLine + targetScene.path,
                    AssetUtility.GetValidator("VketTargetFinder.SceneOpenDialog.Continue"),
                    AssetUtility.GetValidator("VketTargetFinder.SceneOpenDialog.Abort")))
                {
                    throw new FatalValidationErrorException(AssetUtility.GetValidator("VketTargetFinder.ValidationAborted"));
                }

                DoSaveIfNecessary();

                targetScene = EditorSceneManager.OpenScene(scenePath);
                EditorSceneManager.SetActiveScene(targetScene);
            }

            return targetScene;
        }

        private static void DoSaveIfNecessary()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                throw new FatalValidationErrorException(AssetUtility.GetValidator("VketTargetFinder.UserDidntSave"));
            }
        }


        #region Interface Imprements

        public ValidationTarget Find(string baseFolder, bool forceOpenScene = false)
        {
            FindInternal(baseFolder, forceOpenScene);

            return target;
        }

        public GameObject[] FindAllObjects(string baseFolderPath, bool forceOpenScene = false)
        {
            if (!finded)
            {
                FindInternal(baseFolderPath, forceOpenScene);
            }

            return allObjects;
        }

        public string[] FindAssetGuids(string baseFolderPath)
        {
            if (!finded)
            {
                FindInternal(baseFolderPath);
            }

            return assetGUIDs;
        }

        public Object[] FindAssetObjects(string baseFolderPath)
        {
            if (!finded)
            {
                FindInternal(baseFolderPath);
            }

            return assetObjects;
        }

        public string[] FindAssetPaths(string baseFolderPath)
        {
            if (!finded)
            {
                FindInternal(baseFolderPath);
            }

            return assetPaths;
        }

        public GameObject[] FindRootObjects(string baseFolderPath, bool forceOpenScene = false)
        {
            if (!finded)
            {
                FindInternal(baseFolderPath, forceOpenScene);
            }

            return rootObjects;
        }

        public Scene[] FindScenes(string baseFolderPath, bool forceOpenScene = false)
        {
            if (!finded)
            {
                FindInternal(baseFolderPath);
            }

            return scenes;
        }

        #endregion
    }
}
#endif