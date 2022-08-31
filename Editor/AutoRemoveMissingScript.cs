using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kogane.Internal
{
    /// <summary>
    /// シーンを保存する時や Unity を再生する時に Missing Script を削除するエディタ拡張
    /// </summary>
    [InitializeOnLoad]
    internal static class AutoRemoveMissingScript
    {
        //================================================================================
        // 関数(static)
        //================================================================================
        /// <summary>
        /// コンストラクタ
        /// </summary>
        static AutoRemoveMissingScript()
        {
            // シーンが保存された時に呼び出されます
            EditorSceneManager.sceneSaving += ( scene, _ ) => Remove( scene.GetRootGameObjects() );

            // シーンの変更を破棄しようとした時に勝手に保存されてしまうため
            // シーンが閉じられる時は無視しています
            // // シーンが閉じられる時に呼び出されます
            // EditorSceneManager.sceneClosing += ( scene, _ ) =>
            // {
            //     if ( !Remove( scene.GetRootGameObjects() ) ) return;
            //     EditorSceneManager.SaveScene( scene );
            // };

            // プレハブのシーンが保存された時に呼び出されます
            PrefabStage.prefabSaving += gameObject => Remove( gameObject );

            // プレハブステージの変更を破棄しようとした時に勝手に保存されてしまうため
            // プレハブステージが閉じられる時は無視しています
            // // プレハブステージが閉じられる時に呼び出されます
            // PrefabStage.prefabStageClosing += stage =>
            // {
            //     if ( !Remove( stage.prefabContentsRoot ) ) return;
            //
            //     var prefabStageType = typeof( PrefabStage );
            //     var saveMethod      = prefabStageType.GetMethod( "Save", BindingFlags.Instance | BindingFlags.NonPublic );
            //
            //     Debug.Assert( saveMethod != null, nameof( saveMethod ) + " != null" );
            //
            //     saveMethod.Invoke( stage, Array.Empty<object>() );
            // };

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
        private static bool Remove( params GameObject[] rootGameObjects )
        {
            var gameObjectArray = rootGameObjects
                    .SelectMany( x => x.GetComponentsInChildren<Transform>( true ) )
                    .Select( x => x.gameObject )
                    .Where( x => 0 < GameObjectUtility.GetMonoBehavioursWithMissingScriptCount( x ) )
                    .ToArray()
                ;

            if ( gameObjectArray.Length <= 0 ) return false;

            // 保存時に Undo できるようにすると
            // シーン保存後にも Dirty フラグが付いてしまうため
            // Undo には登録しないようにしています

            foreach ( var gameObject in gameObjectArray )
            {
                var isPartOfPrefabInstance = PrefabUtility.IsPartOfPrefabInstance( gameObject );

                // 通常のシーンの Hierarchy において
                // プレハブのインスタンスであれば対象外
                if ( isPartOfPrefabInstance ) continue;

                GameObjectUtility.RemoveMonoBehavioursWithMissingScript( gameObject );
            }

            return true;
        }
    }
}