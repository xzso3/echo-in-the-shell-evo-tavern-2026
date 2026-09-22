using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Echo.NativeGame.Editor
{
    // One-time authoring step; run only when the shared Unity Editor slot is free.
    public static class NativeNorthPulseBuilder
    {
        const string ScenePath = "Assets/Scenes/NativeDemo.unity";
        static readonly Vector2 TerminalPosition = new Vector2(-12, 22);

        [MenuItem("Echo/Native/Connect P2 North Pulse Support")]
        public static void Connect()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before connecting the north pulse.");
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var run = UnityEngine.Object.FindObjectOfType<NativeRunController>();
            if (!run || !run.map || !run.hud || !run.combat || !run.narrative || !run.dialogue ||
                run.map.northRouteChunks == null || run.map.northRouteChunks.Length < 2 ||
                UnityEngine.Object.FindObjectOfType<NativeNorthPulseSupport>())
                throw new InvalidOperationException("North pulse needs one connected run and two north chunks, and must not already exist.");
            var sentries = UnityEngine.Object.FindObjectsOfType<NativeEnemy>()
                .Where(enemy => enemy.name.StartsWith("ArcSentry", StringComparison.Ordinal)).ToArray();
            if (sentries.Length != 1) throw new InvalidOperationException("Expected exactly one scene ArcSentry.");
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/CyberCity/Sprites/Maintenance plate.asset");
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/CyberCity/Materials/Actors.mat");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/NativeGame/Fonts/FusionPixel/FusionPixel12 Bitmap.asset");
            if (!sprite || !material || !font) throw new InvalidOperationException("North pulse artwork or Chinese font is missing.");

            var terminal = new GameObject("North pulse terminal");
            terminal.transform.SetParent(run.map.transform, false);
            terminal.transform.position = TerminalPosition;
            terminal.transform.localScale = Vector3.one * .8f;
            var view = terminal.AddComponent<SpriteRenderer>();
            view.sprite = sprite;
            view.sharedMaterial = material;
            view.sortingOrder = 180;
            view.color = new Color(.74f, .37f, 1f);
            var interaction = terminal.AddComponent<NativeInteraction>();
            interaction.run = run;
            interaction.map = run.map;
            interaction.indicator = view;
            interaction.promptOverride = "E / 北侧定向脉冲（可拒绝）";
            var support = terminal.AddComponent<NativeNorthPulseSupport>();
            support.level = run;
            support.target = sentries[0];
            var eca = terminal.AddComponent<NativeNorthPulseEcaRules>();
            eca.level = run;
            eca.terminal = interaction;
            eca.support = support;
            eca.narrative = run.narrative;
            eca.dialogue = run.dialogue;

            var label = new GameObject("North pulse label").AddComponent<TextMeshPro>();
            label.transform.SetParent(terminal.transform, false);
            label.transform.localPosition = new Vector3(0, 1.2f, 0);
            label.transform.localScale = Vector3.one * 1.25f;
            label.font = font;
            label.fontSharedMaterial = font.material;
            label.fontSize = 2.5f;
            label.text = "定向脉冲";
            label.color = new Color(.9f, .65f, 1f);
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(6, 1);
            label.GetComponent<MeshRenderer>().sortingOrder = 200;

            run.hud.interactables = run.hud.interactables.Concat(new[] { interaction }).ToArray();
            EditorUtility.SetDirty(run.hud);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("P2-08 north pulse connected at " + TerminalPosition + " to scene ArcSentry: choose execute/refuse; actual Combat hit before one Narrative cost.");
        }
    }
}
