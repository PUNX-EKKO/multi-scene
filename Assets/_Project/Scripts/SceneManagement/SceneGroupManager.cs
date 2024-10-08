using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Eflatun.SceneReference;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace System.SceneManagement{
    public class SceneGroupManager 
    {
       public event Action<string> OnSceneLoaded = delegate{};
       public event Action<string> OnSceneUnloaded = delegate {};
       public event Action OnSceneGroupLoaded = delegate {};

       SceneGroup ActiveSceneGroup;
       public async Task LoadScenes(SceneGroup group,IProgress<float> progress, bool reloadDupScenes = false){
            ActiveSceneGroup = group;
            var loadedScenes = new List<string>();
            await UnloadScenes();
            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                loadedScenes.Add(SceneManager.GetSceneAt(i).name);
            }
            var totalSceneToLoad = ActiveSceneGroup.Scenes.Count;
            var operationGroup = new AsyncOperationGroup(totalSceneToLoad);
            for (int i = 0; i < totalSceneToLoad; i++)
            {
                var sceneData = group.Scenes[i];
                if(reloadDupScenes == false && loadedScenes.Contains(sceneData.Name)) continue;
                var operation = SceneManager.LoadSceneAsync(sceneData.Reference.Path,LoadSceneMode.Additive);
              
                await Task.Delay(TimeSpan.FromSeconds(2.5f));  //NOTE: For testing purpose only for adding loading time.
                operationGroup.Operations.Add(operation);
                OnSceneLoaded.Invoke(sceneData.Name);
            }

            while (!operationGroup.IsDone)
            {
                progress?.Report(operationGroup.Progress);
                await Task.Delay(100);
            }
            Scene activeScene = SceneManager.GetSceneByName(ActiveSceneGroup.FindSceneNameByType(SceneType.ActiveScene));
            if (activeScene.IsValid()) {
                SceneManager.SetActiveScene(activeScene);
            }

            OnSceneGroupLoaded.Invoke();
       }
       public async Task UnloadScenes(){
            var scenes = new List<string>();
            var activeScene = SceneManager.GetActiveScene().name;

             int sceneCount = SceneManager.sceneCount;

            for (var i = sceneCount - 1; i > 0; i--) {
                var sceneAt = SceneManager.GetSceneAt(i);
                if (!sceneAt.isLoaded) continue;
                
                var sceneName = sceneAt.name;
                if (sceneName.Equals(activeScene) || sceneName == "Bootstrapper") continue;  
                scenes.Add(sceneName);
            }
       
            var operationGroup = new AsyncOperationGroup(scenes.Count);
            foreach (var scene in scenes) { 
                var operation = SceneManager.UnloadSceneAsync(scene);
                if (operation == null) continue;
                
                operationGroup.Operations.Add(operation);

                OnSceneUnloaded.Invoke(scene);
            }

             // Wait until all AsyncOperations in the group are done
            while (!operationGroup.IsDone) {
                await Task.Delay(100); // delay to avoid tight loop
            }

           // await Resources.UnloadUnusedAssets();
       }

    }

    public readonly struct AsyncOperationGroup{
        public readonly List<AsyncOperation> Operations;
        public float Progress => Operations.Count == 0?0: Operations.Average(o =>o.progress);
        public bool IsDone => Operations.All(o => o.isDone);

        public AsyncOperationGroup(int initialCapacity){
            Operations = new List<AsyncOperation>(initialCapacity);
        }
    }
}
