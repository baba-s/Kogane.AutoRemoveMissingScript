using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kogane.Internal
{
    /// <summary>
    /// シーンを保存する時や Unity を再生する時に Missing Script を削除するエディタ拡張
    /// </summary>
    [InitializeOnLoad]
    internal sealed class AutoRemoveMissingScript : IProcessSceneWithReport
    {
        //================================================================================
        // プロパティ
        //================================================================================
        public int callbackOrder => 0;

        //================================================================================
        // 関数
        //================================================================================
        /// <summary>
        /// シーンがビルドされる時に呼び出されます
        /// </summary>
        void IProcessSceneWithReport.OnProcessScene( Scene scene, BuildReport report )
        {
            Remove( scene.GetRootGameObjects() );
        }

        //================================================================================
        // 関数(static)
        //================================================================================
        /// <summary>
        /// コンストラクタ
        /// </summary>
        static AutoRemoveMissingScript()
        {
            // シーンが開かれた時に呼び出されます
            EditorSceneManager.sceneOpened += ( scene, _ ) => Remove( scene.GetRootGameObjects() );

            // シーンが保存された時に呼び出されます
            EditorSceneManager.sceneSaving += ( scene, _ ) => Remove( scene.GetRootGameObjects() );

            // プレハブのシーンが開かれた時に呼び出されます
            PrefabStage.prefabStageOpened += prefabStage => Remove( prefabStage.prefabContentsRoot );

            // プレハブのシーンが保存された時に呼び出されます
            PrefabStage.prefabSaving += gameObject => Remove( gameObject );

            // Unity の再生状態が変更された時に呼び出されます
            EditorApplication.playModeStateChanged += change =>
            {
                if ( change != PlayModeStateChange.ExitingEditMode ) return;
                Remove( SceneManager.GetActiveScene().GetRootGameObjects() );
            };
        }

        /// <summary>
        /// Missing Script を削除します
        /// </summary>
        private static void Remove( params GameObject[] rootGameObjects )
        {
            var gameObjectArray = rootGameObjects
                    .SelectMany( x => x.GetComponentsInChildren<Transform>( true ) )
                    .Select( x => x.gameObject )
                    .Where( x => 0 < GameObjectUtility.GetMonoBehavioursWithMissingScriptCount( x ) )
                    // 通常のシーンの Hierarchy において
                    // プレハブのインスタンスであれば対象外
                    .Where( x => !PrefabUtility.IsPartOfPrefabInstance( x ) )
                    .ToArray()
                ;

            if ( gameObjectArray.Length <= 0 ) return;

            // 保存時に Undo できるようにすると
            // シーン保存後にも Dirty フラグが付いてしまうため
            // Undo には登録しないようにしています

            foreach ( var gameObject in gameObjectArray )
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript( gameObject );
            }
        }
    }
}