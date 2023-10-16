using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GTGrass
{
    public class GTGrassMenu : MonoBehaviour
    {
        [MenuItem("GameObject/GTGrass/GTGrass", false, 10)]
        static void CreateGTGrass(MenuCommand menuCommand)
        {
            // Create a empty game object
            GameObject go = new GameObject("GTGrass");
            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            //Make sure its lossyscale is 1
            if (go.transform.parent != null)
            {
                var parentScale = go.transform.parent.lossyScale;
                go.transform.localScale = new Vector3(1 / parentScale.x, 1 / parentScale.y, 1 / parentScale.z);
            }
            //Add GTGrassPainter
            go.AddComponent<GTGrassPainter>();
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }

        [MenuItem("GameObject/GTGrass/PlayerInteraction", false, 10)]
        static void CreateGTGrassPlayerInteraction(MenuCommand menuCommand)
        {
            // Create a empty game object
            GameObject go = new GameObject("GTGrassPlayerInteraction");
            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            //Add GTGrassPainter
            go.AddComponent<GTGrassPlayerInteraction>();
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }
    }
}
