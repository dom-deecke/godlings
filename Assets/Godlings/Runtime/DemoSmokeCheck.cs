#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace Godlings
{
    /// <summary>
    /// Opt-in development smoke check. It runs only with -godlingsSmoke and
    /// drives the same input actions used by players, without changing rules.
    /// </summary>
    public sealed class DemoSmokeCheck : MonoBehaviour
    {
        private static bool installed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (!installed && Array.IndexOf(Environment.GetCommandLineArgs(),"-godlingsSmoke") >= 0)
            {
                installed = true;
                var runner = new GameObject("Development smoke check");
                DontDestroyOnLoad(runner);
                runner.AddComponent<DemoSmokeCheck>();
            }
        }

        private IEnumerator Start()
        {
            Application.runInBackground = true;
            // The unattended test must retain its virtual devices when another
            // app is foreground. This changes only the opt-in smoke runner.
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            yield return null;
            var game = FindFirstObjectByType<DemoGame>();
            if (game == null || !game.Ready) { Fail("Game did not initialize."); yield break; }
            if (game.PlayerCurrentHealth != 10 || game.BossCurrentHealth != 50)
            { Fail("Initial health does not match confirmed rules."); yield break; }
            Vector2Int initialEnemyCell = game.FirstEnemyCell;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D));
            yield return new WaitForSeconds(0.14f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            yield return null;
            if (game.PlayerCell != new Vector2Int(2,2))
            { Fail("A movement press did not produce one completed grid step."); yield break; }

            // Diagonal movement and a differently directed attack must coexist.
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.D,Key.DownArrow));
            yield return new WaitForSeconds(0.14f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            yield return null;
            if (game.PlayerCell != new Vector2Int(3,3) || game.AttackDirection != Vector2Int.down)
            { Fail("Movement and attack directions were not independent."); yield break; }
            if (game.FirstEnemyCell != initialEnemyCell)
            { Fail("An out-of-range enemy pursued Fi."); yield break; }

            var pad = InputSystem.AddDevice<Gamepad>();
            InputSystem.QueueStateEvent(pad,new GamepadState { rightStick = new Vector2(-0.9f,0.2f) });
            yield return null;
            yield return null;
            if (game.AttackDirection != Vector2Int.left)
            { Fail("Controller aim did not snap to the nearest cardinal direction."); yield break; }
            InputSystem.RemoveDevice(keyboard);
            InputSystem.RemoveDevice(pad);

            // Verify a remapped key is actually read, then restore it without saving.
            using (var testInput = new DemoInput())
            {
                var remapKeyboard = InputSystem.AddDevice<Keyboard>();
                testInput.Map["Move right"].ApplyBindingOverride(0,"<Keyboard>/j");
                InputSystem.QueueStateEvent(remapKeyboard,new KeyboardState(Key.J));
                yield return null;
                if (testInput.Movement() != Vector2Int.right)
                { Fail("A keyboard binding override was not applied."); yield break; }
                InputSystem.RemoveDevice(remapKeyboard);
            }
            yield return null; // Also runs in batch-mode smoke tests.
            string[] args = Environment.GetCommandLineArgs();
            int imageArgument = Array.IndexOf(args,"-godlingsCapture");
            if (imageArgument >= 0 && imageArgument+1 < args.Length)
            {
                string output = args[imageArgument+1];
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                // Explicit camera rendering also works in unattended player tests.
                var camera = Camera.main;
                var target = new RenderTexture(1280,720,24);
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                var image = new Texture2D(1280,720,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,1280,720),0,0);
                image.Apply();
                File.WriteAllBytes(output,image.EncodeToPNG());
                camera.targetTexture = null;
                RenderTexture.active = null;
                Destroy(image);
                target.Release();
                Destroy(target);
            }

            // An unattended player must lose through ordinary enemy contact,
            // then Restart must restore the real sketch and confirmed health.
            game.TryMovePlayer(Vector2Int.right);
            yield return new WaitForSeconds(0.25f);
            float deadline = Time.realtimeSinceStartup + 12;
            while (!game.HasEnded && Time.realtimeSinceStartup < deadline)
                yield return new WaitForSecondsRealtime(0.1f);
            if (!game.HasEnded || game.HasWon || Time.timeScale != 0)
            { Fail($"Contact defeat: ended={game.HasEnded}, won={game.HasWon}, FiHP={game.PlayerCurrentHealth}, cell={game.PlayerCell}, timeScale={Time.timeScale}."); yield break; }
            game.Restart();
            yield return null;
            yield return null;
            game = FindFirstObjectByType<DemoGame>();
            if (game == null || !game.Ready || game.PlayerCurrentHealth != 10 || game.HasEnded)
            { Fail("Restart did not restore the demo."); yield break; }

            // A separate test-only fixture isolates the boss encounter. No
            // fixture is saved into the artist's level or the shipped scene.
            SceneManager.sceneLoaded += PrepareBossFixture;
            game.Restart();
            yield return null;
            yield return null;
            SceneManager.sceneLoaded -= PrepareBossFixture;
            game = FindFirstObjectByType<DemoGame>();
            var attackKeyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(attackKeyboard,new KeyboardState(Key.RightArrow));
            deadline = Time.realtimeSinceStartup + 18;
            while (!game.HasEnded && Time.realtimeSinceStartup < deadline)
                yield return new WaitForSecondsRealtime(0.1f);
            InputSystem.RemoveDevice(attackKeyboard);
            if (!game.HasWon || game.BossCurrentHealth != 0 || game.PlayerCurrentHealth != 10)
            { Fail($"Sword encounter: won={game.HasWon}, bossHP={game.BossCurrentHealth}, FiHP={game.PlayerCurrentHealth}, cell={game.PlayerCell}, aim={game.AttackDirection}."); yield break; }
            Debug.Log("GODLINGS_RUNTIME_SMOKE_PASSED");
            Application.Quit(0);
        }

        private static void PrepareBossFixture(Scene scene, LoadSceneMode mode)
        {
            var game = FindFirstObjectByType<DemoGame>();
            game.LevelSketch = new TextAsset("{\"rowsTopToBottom\":[\"#P##B\"],\"bossArena\":{\"minimumCell\":[20,0],\"maximumCellInclusive\":[28,4]}}");
        }

        private static void Fail(string reason)
        {
            Debug.LogError("GODLINGS_RUNTIME_SMOKE_FAILED: " + reason);
            Application.Quit(1);
        }
    }
}
#endif
