#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    public class VketSceneStructureRule : BaseRule
    {
        public VketSceneStructureRule(string name) : base(name)
        {
        }

        protected override void Logic(ValidationTarget target)
        {
            var loadedScenes =  new HashSet<Scene>(GetLoadedScenes());
            var targetScenes= target.GetScenes();
            
            foreach (var scene in targetScenes)
            {
                loadedScenes.Remove(scene);
            }

            foreach (var unrelatedScene in loadedScenes)
            {
                // 入稿対象外のシーンが開かれています。
                var message = AssetUtility.GetValidator("SceneStructureRule.UnrelatedSceneDetected", unrelatedScene.name);
                var solution = AssetUtility.GetValidator("SceneStructureRule.UnrelatedSceneDetected.Solution");

                var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(unrelatedScene.path);
                AddIssue(new Issue(sceneAsset, IssueLevel.Error, message, solution));
            }

            foreach (var targetScene in targetScenes)
            {
                ValidateRootObject(target, targetScene);
            }
        }

        private void ValidateRootObject(ValidationTarget target, Scene scene)
        {
            var allowedRootObjects = target.GetRootObjects();
            var rootObjects = new HashSet<GameObject>(scene.GetRootGameObjects());

            // TODO: VitDeckは[Reference Object]なので合わせるか考える
            rootObjects.RemoveWhere(obj => obj.name == "ReferenceObjects");
            foreach (var allowedRootObject in allowedRootObjects)
            {
                rootObjects.Remove(allowedRootObject);
            }

            foreach (var unrelatedRootObject in rootObjects)
            {
                // 入稿対象物及びサポート用オブジェクト以外のオブジェクトがシーンに配置されています。
                var message = AssetUtility.GetValidator("SceneStructureRule.UnrelatedRootObjectDetected", unrelatedRootObject.name);
                var solution = AssetUtility.GetValidator("SceneStructureRule.UnrelatedRootObjectDetected.Solution");

                AddIssue(new Issue(unrelatedRootObject, IssueLevel.Error, message, solution));
            }
        }


        private Scene[] GetLoadedScenes()
        {
            var sceneCount = SceneManager.sceneCount;
            var scenes = new List<Scene>(sceneCount);
            
            for (var i = 0; i < sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                {
                    continue;
                }
                scenes.Add(scene);
            }

            return scenes.ToArray();
        }
    }
}
#endif