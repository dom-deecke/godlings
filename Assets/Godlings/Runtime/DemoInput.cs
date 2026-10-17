using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Godlings
{
    /// <summary>
    /// Separate actions keep movement and attacks independent. Binding overrides
    /// are local player preferences, never changes to the supplied artwork/data.
    /// </summary>
    public sealed class DemoInput : IDisposable
    {
        public readonly InputActionMap Map = new("Godlings");
        private InputActionRebindingExtensions.RebindingOperation rebind;
        private const string PreferencesKey = "Godlings.Demo.Bindings.v1";
        public bool IsRebinding => rebind != null;
        public string RebindingLabel { get; private set; }

        public DemoInput()
        {
            Add("Move up", "<Keyboard>/w", "<Keyboard>/numpad8");
            Add("Move down", "<Keyboard>/s", "<Keyboard>/numpad2");
            Add("Move left", "<Keyboard>/a", "<Keyboard>/numpad4");
            Add("Move right", "<Keyboard>/d", "<Keyboard>/numpad6");
            Add("Move up-left", "<Keyboard>/numpad7");
            Add("Move up-right", "<Keyboard>/numpad9");
            Add("Move down-left", "<Keyboard>/numpad1");
            Add("Move down-right", "<Keyboard>/numpad3");
            Map.AddAction("Move stick", InputActionType.Value, "<Gamepad>/leftStick", expectedControlLayout: "Vector2");
            Add("Attack up", "<Keyboard>/upArrow");
            Add("Attack down", "<Keyboard>/downArrow");
            Add("Attack left", "<Keyboard>/leftArrow");
            Add("Attack right", "<Keyboard>/rightArrow");
            Map.AddAction("Aim stick", InputActionType.Value, "<Gamepad>/rightStick", expectedControlLayout: "Vector2");
            Add("Controller attack", "<Gamepad>/rightTrigger");
            if (PlayerPrefs.HasKey(PreferencesKey))
            {
                try { Map.LoadBindingOverridesFromJson(PlayerPrefs.GetString(PreferencesKey)); }
                catch (Exception e) { Debug.LogWarning("Saved controls could not be loaded: " + e.Message); }
            }
            Map.Enable();
        }

        private void Add(string name, params string[] bindings)
        {
            var action = Map.AddAction(name, InputActionType.Button);
            foreach (string binding in bindings) action.AddBinding(binding);
        }

        private int Pressed(string name) => Map[name].IsPressed() ? 1 : 0;

        public Vector2Int Movement()
        {
            Vector2 stick = Map["Move stick"].ReadValue<Vector2>();
            int x = Pressed("Move right") - Pressed("Move left") + Pressed("Move up-right")
                + Pressed("Move down-right") - Pressed("Move up-left") - Pressed("Move down-left");
            int y = Pressed("Move up") - Pressed("Move down") + Pressed("Move up-right")
                + Pressed("Move up-left") - Pressed("Move down-right") - Pressed("Move down-left");
            if (stick.sqrMagnitude > 0.25f) { x = Mathf.RoundToInt(stick.x); y = Mathf.RoundToInt(stick.y); }
            return new Vector2Int(Math.Sign(x), Math.Sign(y));
        }

        public bool Attack(ref Vector2Int aim)
        {
            Vector2 stick = Map["Aim stick"].ReadValue<Vector2>();
            if (stick.sqrMagnitude > 0.25f) aim = AttackPatterns.SnapCardinal(stick, aim);
            Vector2 arrows = new(Pressed("Attack right") - Pressed("Attack left"),
                Pressed("Attack up") - Pressed("Attack down"));
            if (arrows != Vector2.zero) { aim = AttackPatterns.SnapCardinal(arrows, aim); return true; }
            return Map["Controller attack"].IsPressed();
        }

        public void BeginRebind(InputAction action, int index)
        {
            if (IsRebinding) return;
            Map.Disable();
            RebindingLabel = action.name;
            rebind = action.PerformInteractiveRebinding(index)
                .WithControlsExcluding("<Mouse>")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnMatchWaitForAnother(0.1f)
                .OnComplete(_ => FinishRebind(true))
                .OnCancel(_ => FinishRebind(false));
            rebind.Start();
        }

        private void FinishRebind(bool save)
        {
            rebind.Dispose();
            rebind = null;
            Map.Enable();
            if (!save) return;
            PlayerPrefs.SetString(PreferencesKey, Map.SaveBindingOverridesAsJson());
            PlayerPrefs.Save();
        }

        public void ResetBindings()
        {
            Map.RemoveAllBindingOverrides();
            PlayerPrefs.DeleteKey(PreferencesKey);
            PlayerPrefs.Save();
        }

        public void Dispose() { rebind?.Dispose(); Map.Dispose(); }
    }
}
