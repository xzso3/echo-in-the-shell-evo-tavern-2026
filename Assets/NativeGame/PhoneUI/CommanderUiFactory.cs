using TMPro;
using UnityEngine;

namespace Echo.NativeGame.PhoneUI
{
    public static class CommanderUiFactory
    {
        // Parent must be under a Canvas with a GraphicRaycaster and one scene EventSystem.
        public static CommanderHomeView CreateHome(Transform canvasParent, TMP_FontAsset chineseFont)
        {
            var root = PhoneUiElements.Fill("Commander Home", canvasParent);
            var view = root.gameObject.AddComponent<CommanderHomeView>();
            view.Build(chineseFont);
            return view;
        }

    }
}
