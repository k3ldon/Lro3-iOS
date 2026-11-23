using System;
using UnityEngine;

namespace Ro3Ios.Hooks;

public static class Clicks
{
    private static readonly Type GraphicType = Type.GetType("UnityEngine.UI.Graphic, UnityEngine.UI");
    private static readonly System.Reflection.PropertyInfo RaycastTarget = GraphicType?.GetProperty("raycastTarget");

    public static void EnableDeadClickTargets(GameObject root)
    {
        if (root == null || GraphicType == null || RaycastTarget == null) return;
    }
}
