using System;
using UnityEngine.UIElements;

namespace NetRunner.Menu
{
    /// <summary>
    /// Builds the menu's buttons and labels with the menu style's classes.
    /// Buttons play the click sound the owner passes in (the main menu has
    /// one; the pause menu has none) before they act.
    /// </summary>
    public sealed class MenuWidgets
    {
        private readonly Action click;

        /// <param name="clickSound">Called before every button's action; null for silence.</param>
        public MenuWidgets(Action clickSound)
        {
            click = clickSound;
        }

        public static Label Label(string text, string style)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList(style);
            return label;
        }

        /// <summary>An element that ignores the mouse, with one style class.</summary>
        public static VisualElement Box(string style)
        {
            var element = new VisualElement { pickingMode = PickingMode.Ignore };
            element.AddToClassList(style);
            return element;
        }

        public Button Button(string text, Action action)
        {
            var button = new Button(() =>
            {
                click?.Invoke();
                action();
            })
            {
                text = text,
            };
            button.AddToClassList("menu-button");
            return button;
        }
    }
}
